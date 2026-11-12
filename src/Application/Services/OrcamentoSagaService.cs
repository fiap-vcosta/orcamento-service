using System.Security.Cryptography;
using System.Text;
using Application.Abstractions.Gateways;
using Application.Abstractions.Services;
using BuildingBlocks.Events;
using Domain.Entities;
using Domain.Exceptions;
using Microsoft.Extensions.Logging;

namespace Application.Services;

public class OrcamentoSagaService(
    IOrcamentoGateway orcamentoGateway,
    IOrdemOrcamentoGateway ordemGateway,
    ICobrancaGateway cobrancaGateway,
    IEventoProcessadoGateway eventoProcessadoGateway,
    IMercadoPagoOrdersClient mercadoPagoClient,
    IOrcamentoIntegrationPublisher publisher,
    ILogger<OrcamentoSagaService> logger)
{
    public const string TipoWebhookMercadoPago = "WebhookMercadoPago";
    public const string MotivoFalhaPersistencia = "falha ao gravar o orçamento";

    public async Task ProcessarDiagnosticoFinalizadoAsync(
        Guid eventId,
        DiagnosticoFinalizadoData data,
        CancellationToken cancellationToken = default)
    {
        var chave = ChaveDiagnostico(data.OrdemServicoId, data.VersaoDiagnostico);
        if (await JaProcessadoAsync(eventId, EventTypes.DiagnosticoFinalizado, chave, cancellationToken))
        {
            return;
        }

        var ordem = await ordemGateway.GetByOrdemServicoIdAsync(data.OrdemServicoId, cancellationToken)
            ?? OrdemOrcamento.Criar(data.OrdemServicoId);

        if (ordem.Descartada)
        {
            await MarcarProcessadoAsync(eventId, EventTypes.DiagnosticoFinalizado, chave, cancellationToken);
            return;
        }

        var orcamento = Orcamento.Criar(
            data.OrdemServicoId,
            data.VersaoDiagnostico,
            new ClienteOrcamento
            {
                Nome = data.Cliente.Nome,
                Email = data.Cliente.Email,
                Documento = data.Cliente.Documento
            },
            data.Servicos
                .Select(s => new OrcamentoServico
                {
                    Id = s.Id,
                    Nome = s.Nome,
                    ValorCobrado = s.ValorCobrado,
                    Status = s.Status.ToString()
                })
                .ToList());

        try
        {
            await orcamentoGateway.SalvarAsync(orcamento, cancellationToken);

            if (ordem.VersaoVigente is null || data.VersaoDiagnostico >= ordem.VersaoVigente)
            {
                ordem.RegistrarOrcamento(orcamento.Id, data.VersaoDiagnostico);
            }

            await ordemGateway.SalvarAsync(ordem, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(
                ex,
                "Falha ao gravar orçamento da ordem {OrdemServicoId} versão {VersaoDiagnostico}",
                data.OrdemServicoId,
                data.VersaoDiagnostico);

            await publisher.PublicarOrcamentoFalhouAsync(
                data.OrdemServicoId,
                data.VersaoDiagnostico,
                MotivoFalhaPersistencia,
                cancellationToken);
            await MarcarProcessadoAsync(eventId, EventTypes.DiagnosticoFinalizado, chave, cancellationToken);
            return;
        }

        await publisher.PublicarOrcamentoGeradoAsync(orcamento, cancellationToken);
        await MarcarProcessadoAsync(eventId, EventTypes.DiagnosticoFinalizado, chave, cancellationToken);
    }

    public async Task ProcessarOrdemServicoFinalizadaAsync(
        Guid eventId,
        OrdemServicoFinalizadaData data,
        CancellationToken cancellationToken = default)
    {
        var chave = ChaveOrdem(data.OrdemServicoId);
        if (await JaProcessadoAsync(eventId, EventTypes.OrdemServicoFinalizada, chave, cancellationToken))
        {
            return;
        }

        var orcamento = await ObterOrcamentoVigenteAsync(data.OrdemServicoId, cancellationToken);
        if (orcamento is null)
        {
            logger.LogWarning(
                "Ordem {OrdemServicoId} finalizada sem orçamento vigente; cobrança não gerada",
                data.OrdemServicoId);
            await MarcarProcessadoAsync(eventId, EventTypes.OrdemServicoFinalizada, chave, cancellationToken);
            return;
        }

        var existentes = await cobrancaGateway.ListarPorOrdemServicoAsync(data.OrdemServicoId, cancellationToken);
        if (existentes.Count == 0)
        {
            await CriarCobrancaAsync(orcamento, existentes.Count, cancellationToken);
        }
        else
        {
            var ultima = existentes.OrderByDescending(c => c.CriadaEm).First();
            if (ultima.Status == CobrancaStatus.Pendente)
            {
                await publisher.PublicarCobrancaGeradaAsync(ultima, cancellationToken);
            }
        }

        await MarcarProcessadoAsync(eventId, EventTypes.OrdemServicoFinalizada, chave, cancellationToken);
    }

    public async Task ProcessarOrdemServicoDescartadaAsync(
        Guid eventId,
        OrdemServicoDescartadaData data,
        CancellationToken cancellationToken = default)
    {
        var chave = ChaveOrdem(data.OrdemServicoId);
        if (await JaProcessadoAsync(eventId, EventTypes.OrdemServicoDescartada, chave, cancellationToken))
        {
            return;
        }

        var ordem = await ordemGateway.GetByOrdemServicoIdAsync(data.OrdemServicoId, cancellationToken)
            ?? OrdemOrcamento.Criar(data.OrdemServicoId);
        ordem.MarcarDescartada();
        await ordemGateway.SalvarAsync(ordem, cancellationToken);

        if (ordem.OrcamentoIdVigente is not null)
        {
            var orcamento = await orcamentoGateway.GetByIdAsync(ordem.OrcamentoIdVigente, cancellationToken);
            if (orcamento is not null && orcamento.Status != OrcamentoStatus.Cancelado)
            {
                orcamento.Cancelar();
                await orcamentoGateway.SalvarAsync(orcamento, cancellationToken);
            }
        }

        await MarcarProcessadoAsync(eventId, EventTypes.OrdemServicoDescartada, chave, cancellationToken);
    }

    public async Task ProcessarWebhookOrderAsync(string orderId, CancellationToken cancellationToken = default)
    {
        var order = await mercadoPagoClient.ObterOrderAsync(orderId, cancellationToken);
        if (order is null)
        {
            logger.LogWarning("Order {OrderId} não encontrada no Mercado Pago", orderId);
            return;
        }

        if (order.Situacao == MercadoPagoOrderSituacao.Pendente)
        {
            return;
        }

        var cobranca = await cobrancaGateway.GetByIdAsync(order.Id, cancellationToken);
        if (cobranca is null)
        {
            logger.LogWarning("Order {OrderId} sem cobrança correspondente", order.Id);
            return;
        }

        if (cobranca.Status != CobrancaStatus.Pendente)
        {
            return;
        }

        var reivindicada = await eventoProcessadoGateway.TentarRegistrarAsync(
            new EventoProcessado
            {
                EventId = Guid.NewGuid(),
                Tipo = TipoWebhookMercadoPago,
                ChaveNegocio = $"{cobranca.Id}:{order.Situacao}"
            },
            cancellationToken);
        if (!reivindicada)
        {
            return;
        }

        if (order.Situacao == MercadoPagoOrderSituacao.Aprovada)
        {
            cobranca.Confirmar();
            await cobrancaGateway.SalvarAsync(cobranca, cancellationToken);
            await publisher.PublicarPagamentoConfirmadoAsync(cobranca, cancellationToken);
            return;
        }

        await mercadoPagoClient.CancelarOrderAsync(cobranca.Id, cancellationToken);

        string motivo;
        if (order.Situacao == MercadoPagoOrderSituacao.Expirada)
        {
            cobranca.Expirar();
            motivo = PagamentoRecusadoMotivos.Expirado;
        }
        else
        {
            cobranca.Recusar();
            motivo = PagamentoRecusadoMotivos.Recusado;
        }

        await cobrancaGateway.SalvarAsync(cobranca, cancellationToken);
        await publisher.PublicarPagamentoRecusadoAsync(cobranca, motivo, cancellationToken);

        if (order.Situacao == MercadoPagoOrderSituacao.Expirada)
        {
            return;
        }

        var orcamento = await ObterOrcamentoVigenteAsync(cobranca.OrdemServicoId, cancellationToken);
        if (orcamento is null)
        {
            return;
        }

        var existentes = await cobrancaGateway.ListarPorOrdemServicoAsync(cobranca.OrdemServicoId, cancellationToken);
        await CriarCobrancaAsync(orcamento, existentes.Count, cancellationToken);
    }

    public async Task<Cobranca> GerarNovaCobrancaAposExpiracaoAsync(
        Guid ordemServicoId,
        CancellationToken cancellationToken = default)
    {
        var orcamento = await ObterOrcamentoVigenteAsync(ordemServicoId, cancellationToken);
        if (orcamento is null)
        {
            throw new DomainNotFoundException($"Orçamento vigente da ordem {ordemServicoId} não encontrado.");
        }

        var existentes = await cobrancaGateway.ListarPorOrdemServicoAsync(ordemServicoId, cancellationToken);
        if (existentes.Any(c => c.Status == CobrancaStatus.Pendente))
        {
            throw new BusinessRuleException(
                $"Ordem {ordemServicoId} já possui cobrança pendente; não é possível gerar outra.");
        }

        var ultima = existentes.OrderByDescending(c => c.CriadaEm).FirstOrDefault();
        if (ultima is null || ultima.Status != CobrancaStatus.Expirada)
        {
            throw new BusinessRuleException(
                $"Nova cobrança manual da ordem {ordemServicoId} só é permitida após expiração da cobrança anterior.");
        }

        return await CriarCobrancaAsync(orcamento, existentes.Count, cancellationToken);
    }

    public static string ChaveDiagnostico(Guid ordemServicoId, int versaoDiagnostico) =>
        $"{ordemServicoId}:{versaoDiagnostico}";

    public static string ChaveOrdem(Guid ordemServicoId) => ordemServicoId.ToString();

    public static string GerarIdempotencyKey(Guid ordemServicoId, int tentativa)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes($"cobranca:{ordemServicoId}:{tentativa}"));
        return new Guid(hash.AsSpan(0, 16)).ToString();
    }

    private async Task<Orcamento?> ObterOrcamentoVigenteAsync(Guid ordemServicoId, CancellationToken cancellationToken)
    {
        var ordem = await ordemGateway.GetByOrdemServicoIdAsync(ordemServicoId, cancellationToken);
        if (ordem is null || ordem.Descartada || ordem.OrcamentoIdVigente is null)
        {
            return null;
        }

        var orcamento = await orcamentoGateway.GetByIdAsync(ordem.OrcamentoIdVigente, cancellationToken);
        return orcamento is { Status: OrcamentoStatus.Ativo } ? orcamento : null;
    }

    private async Task<Cobranca> CriarCobrancaAsync(
        Orcamento orcamento,
        int cobrancasAnteriores,
        CancellationToken cancellationToken)
    {
        var idempotencyKey = GerarIdempotencyKey(orcamento.OrdemServicoId, cobrancasAnteriores + 1);

        var order = await mercadoPagoClient.CriarOrderPixAsync(
            new CriarOrderPixRequest(idempotencyKey, orcamento.OrdemServicoId, orcamento.ValorTotal, orcamento.Cliente),
            cancellationToken);

        var cobranca = Cobranca.CriarPendente(
            order.Id,
            orcamento.OrdemServicoId,
            orcamento.Id,
            idempotencyKey,
            orcamento.ValorTotal);
        cobranca.RegistrarPix(order.QrCode ?? string.Empty, order.Ticket ?? string.Empty);

        await cobrancaGateway.SalvarAsync(cobranca, cancellationToken);
        await publisher.PublicarCobrancaGeradaAsync(cobranca, cancellationToken);
        return cobranca;
    }

    private async Task<bool> JaProcessadoAsync(
        Guid eventId,
        string tipo,
        string chave,
        CancellationToken cancellationToken) =>
        await eventoProcessadoGateway.ExisteEventIdAsync(eventId, cancellationToken)
        || await eventoProcessadoGateway.ExisteChaveNegocioAsync(tipo, chave, cancellationToken);

    private Task MarcarProcessadoAsync(
        Guid eventId,
        string tipo,
        string chave,
        CancellationToken cancellationToken) =>
        eventoProcessadoGateway.AddAsync(
            new EventoProcessado
            {
                EventId = eventId,
                Tipo = tipo,
                ChaveNegocio = chave
            },
            cancellationToken);
}
