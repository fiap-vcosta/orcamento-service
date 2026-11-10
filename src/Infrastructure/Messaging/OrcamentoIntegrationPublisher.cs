using Application.Abstractions.Services;
using BuildingBlocks.Events;
using BuildingBlocks.Messaging;
using Domain.Entities;

namespace Infrastructure.Messaging;

public sealed class OrcamentoIntegrationPublisher(IEventPublisher eventPublisher) : IOrcamentoIntegrationPublisher
{
    public async Task PublicarOrcamentoGeradoAsync(Orcamento orcamento, CancellationToken cancellationToken = default)
    {
        await eventPublisher.PublishAsync(
            TopicIds.OrcamentoGerado,
            EventTypes.OrcamentoGerado,
            new OrcamentoGeradoData
            {
                OrdemServicoId = orcamento.OrdemServicoId,
                VersaoDiagnostico = orcamento.VersaoDiagnostico,
                OrcamentoId = orcamento.Id
            },
            cancellationToken);

        await eventPublisher.PublishAsync(
            TopicIds.Notificacoes,
            EventTypes.OrcamentoDisponivel,
            new OrcamentoDisponivelData
            {
                OrdemServicoId = orcamento.OrdemServicoId,
                VersaoDiagnostico = orcamento.VersaoDiagnostico,
                OrcamentoId = orcamento.Id,
                ValorTotal = orcamento.ValorTotal,
                Servicos = orcamento.Servicos
                    .Select(s => new OrcamentoDisponivelServico { Nome = s.Nome, ValorCobrado = s.ValorCobrado })
                    .ToList()
            },
            cancellationToken);
    }

    public Task PublicarOrcamentoFalhouAsync(
        int ordemServicoId,
        int versaoDiagnostico,
        string motivo,
        CancellationToken cancellationToken = default) =>
        eventPublisher.PublishAsync(
            TopicIds.OrcamentoFalhou,
            EventTypes.OrcamentoFalhou,
            new OrcamentoFalhouData
            {
                OrdemServicoId = ordemServicoId,
                VersaoDiagnostico = versaoDiagnostico,
                Motivo = motivo
            },
            cancellationToken);

    public async Task PublicarCobrancaGeradaAsync(Cobranca cobranca, CancellationToken cancellationToken = default)
    {
        await eventPublisher.PublishAsync(
            TopicIds.CobrancaGerada,
            EventTypes.CobrancaGerada,
            new CobrancaGeradaData
            {
                OrdemServicoId = cobranca.OrdemServicoId,
                CobrancaId = cobranca.Id
            },
            cancellationToken);

        await eventPublisher.PublishAsync(
            TopicIds.Notificacoes,
            EventTypes.CobrancaPixDisponivel,
            new CobrancaPixDisponivelData
            {
                OrdemServicoId = cobranca.OrdemServicoId,
                CobrancaId = cobranca.Id,
                ValorTotal = cobranca.ValorTotal,
                QrCode = cobranca.QrCode ?? string.Empty,
                Ticket = cobranca.Ticket ?? string.Empty
            },
            cancellationToken);
    }

    public Task PublicarPagamentoConfirmadoAsync(Cobranca cobranca, CancellationToken cancellationToken = default) =>
        eventPublisher.PublishAsync(
            TopicIds.PagamentoConfirmado,
            EventTypes.PagamentoConfirmado,
            new PagamentoConfirmadoData
            {
                OrdemServicoId = cobranca.OrdemServicoId,
                CobrancaId = cobranca.Id
            },
            cancellationToken);

    public Task PublicarPagamentoRecusadoAsync(
        Cobranca cobranca,
        string motivo,
        CancellationToken cancellationToken = default) =>
        eventPublisher.PublishAsync(
            TopicIds.PagamentoRecusado,
            EventTypes.PagamentoRecusado,
            new PagamentoRecusadoData
            {
                OrdemServicoId = cobranca.OrdemServicoId,
                CobrancaId = cobranca.Id,
                Motivo = motivo
            },
            cancellationToken);
}
