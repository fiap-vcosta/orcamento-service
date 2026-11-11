using Application.Abstractions.Services;
using Application.Services;
using BuildingBlocks.Events;
using Domain.Entities;
using Domain.Exceptions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using UnitTests.Fakes;

namespace UnitTests.Application.Services;

public class OrcamentoSagaServiceTests
{
    private readonly InMemoryOrcamentoGateway _orcamentos = new();
    private readonly InMemoryOrdemOrcamentoGateway _ordens = new();
    private readonly InMemoryCobrancaGateway _cobrancas = new();
    private readonly InMemoryEventoProcessadoGateway _eventos = new();
    private readonly Mock<IMercadoPagoOrdersClient> _mercadoPago = new();
    private readonly Mock<IOrcamentoIntegrationPublisher> _publisher = new();
    private readonly OrcamentoSagaService _service;
    private int _orderSequence;

    public OrcamentoSagaServiceTests()
    {
        _mercadoPago
            .Setup(m => m.CriarOrderPixAsync(It.IsAny<CriarOrderPixRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => new MercadoPagoOrder(
                $"ord_mp_{++_orderSequence}",
                MercadoPagoOrderSituacao.Pendente,
                "000201qr",
                "https://ticket"));

        _service = new OrcamentoSagaService(
            _orcamentos,
            _ordens,
            _cobrancas,
            _eventos,
            _mercadoPago.Object,
            _publisher.Object,
            NullLogger<OrcamentoSagaService>.Instance);
    }

    private static DiagnosticoFinalizadoData Diagnostico(int ordemId = 10, int versao = 1) =>
        new()
        {
            OrdemServicoId = ordemId,
            VersaoDiagnostico = versao,
            Cliente = new DiagnosticoFinalizadoCliente
            {
                Nome = "Ada",
                Email = "ada@oficina.dev",
                Documento = "86421537090"
            },
            Servicos =
            [
                new DiagnosticoFinalizadoServico
                {
                    Id = 3,
                    Nome = "Troca de óleo",
                    ValorCobrado = 150m,
                    Status = ServicoDiagnosticoStatus.Sugerido
                },
                new DiagnosticoFinalizadoServico
                {
                    Id = 4,
                    Nome = "Alinhamento",
                    ValorCobrado = 80m,
                    Status = ServicoDiagnosticoStatus.Rejeitado
                }
            ]
        };

    private async Task<Orcamento> PrepararOrcamentoVigenteAsync(int ordemId = 10)
    {
        await _service.ProcessarDiagnosticoFinalizadoAsync(Guid.NewGuid(), Diagnostico(ordemId));
        return _orcamentos.Items[Orcamento.CriarId(ordemId, 1)];
    }

    private async Task<Cobranca> PrepararCobrancaPendenteAsync(int ordemId = 10)
    {
        await PrepararOrcamentoVigenteAsync(ordemId);
        await _service.ProcessarOrdemServicoFinalizadaAsync(
            Guid.NewGuid(),
            new OrdemServicoFinalizadaData { OrdemServicoId = ordemId });
        return _cobrancas.Items.Values.Single();
    }

    [Fact]
    public async Task DiagnosticoFinalizado_Sucesso_PersisteOrcamentoEPublica()
    {
        // Arrange
        var eventId = Guid.NewGuid();

        // Act
        await _service.ProcessarDiagnosticoFinalizadoAsync(eventId, Diagnostico());

        // Assert
        var orcamento = Assert.Single(_orcamentos.Items.Values);
        Assert.Equal("orc-10-1", orcamento.Id);
        Assert.Equal(150m, orcamento.ValorTotal);
        Assert.Equal("Ada", orcamento.Cliente.Nome);
        Assert.Equal("orc-10-1", _ordens.Items[10].OrcamentoIdVigente);
        Assert.Equal(1, _ordens.Items[10].VersaoVigente);
        _publisher.Verify(p => p.PublicarOrcamentoGeradoAsync(orcamento, It.IsAny<CancellationToken>()), Times.Once);
        Assert.Contains(_eventos.Items, e => e.EventId == eventId && e.ChaveNegocio == "10:1");
    }

    [Fact]
    public async Task DiagnosticoFinalizado_OrdemDescartada_NaoCriaOrcamentoEMarcaProcessado()
    {
        // Arrange
        var ordem = OrdemOrcamento.Criar(10);
        ordem.MarcarDescartada();
        _ordens.Items[10] = ordem;
        var eventId = Guid.NewGuid();

        // Act
        await _service.ProcessarDiagnosticoFinalizadoAsync(eventId, Diagnostico());

        // Assert
        Assert.Empty(_orcamentos.Items);
        _publisher.Verify(
            p => p.PublicarOrcamentoGeradoAsync(It.IsAny<Orcamento>(), It.IsAny<CancellationToken>()),
            Times.Never);
        Assert.Contains(_eventos.Items, e => e.EventId == eventId);
    }

    [Fact]
    public async Task DiagnosticoFinalizado_MesmoEventId_NaoReprocessa()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        await _service.ProcessarDiagnosticoFinalizadoAsync(eventId, Diagnostico());

        // Act
        await _service.ProcessarDiagnosticoFinalizadoAsync(eventId, Diagnostico());

        // Assert
        _publisher.Verify(
            p => p.PublicarOrcamentoGeradoAsync(It.IsAny<Orcamento>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task DiagnosticoFinalizado_MesmaChaveComOutroEventId_NaoReprocessa()
    {
        // Arrange
        await _service.ProcessarDiagnosticoFinalizadoAsync(Guid.NewGuid(), Diagnostico());

        // Act
        await _service.ProcessarDiagnosticoFinalizadoAsync(Guid.NewGuid(), Diagnostico());

        // Assert
        _publisher.Verify(
            p => p.PublicarOrcamentoGeradoAsync(It.IsAny<Orcamento>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task DiagnosticoFinalizado_NovaVersao_AtualizaPonteiroVigente()
    {
        // Arrange
        await _service.ProcessarDiagnosticoFinalizadoAsync(Guid.NewGuid(), Diagnostico(versao: 1));

        // Act
        await _service.ProcessarDiagnosticoFinalizadoAsync(Guid.NewGuid(), Diagnostico(versao: 2));

        // Assert
        Assert.Equal("orc-10-2", _ordens.Items[10].OrcamentoIdVigente);
        Assert.Equal(2, _ordens.Items[10].VersaoVigente);
    }

    [Fact]
    public async Task DiagnosticoFinalizado_VersaoAnteriorTardia_MantemPonteiroVigente()
    {
        // Arrange
        await _service.ProcessarDiagnosticoFinalizadoAsync(Guid.NewGuid(), Diagnostico(versao: 2));

        // Act
        await _service.ProcessarDiagnosticoFinalizadoAsync(Guid.NewGuid(), Diagnostico(versao: 1));

        // Assert
        Assert.Equal("orc-10-2", _ordens.Items[10].OrcamentoIdVigente);
        Assert.Equal(2, _orcamentos.Items.Count);
    }

    [Fact]
    public async Task DiagnosticoFinalizado_FalhaAoGravar_PublicaOrcamentoFalhouEMarcaProcessado()
    {
        // Arrange
        _orcamentos.SalvarException = new InvalidOperationException("firestore fora");
        var eventId = Guid.NewGuid();

        // Act
        await _service.ProcessarDiagnosticoFinalizadoAsync(eventId, Diagnostico());

        // Assert
        _publisher.Verify(
            p => p.PublicarOrcamentoFalhouAsync(
                10,
                1,
                OrcamentoSagaService.MotivoFalhaPersistencia,
                It.IsAny<CancellationToken>()),
            Times.Once);
        _publisher.Verify(
            p => p.PublicarOrcamentoGeradoAsync(It.IsAny<Orcamento>(), It.IsAny<CancellationToken>()),
            Times.Never);
        Assert.Contains(_eventos.Items, e => e.EventId == eventId);
    }

    [Fact]
    public async Task DiagnosticoFinalizado_Cancelamento_PropagaSemPublicarFalha()
    {
        // Arrange
        _orcamentos.SalvarException = new OperationCanceledException();

        // Act
        var act = () => _service.ProcessarDiagnosticoFinalizadoAsync(Guid.NewGuid(), Diagnostico());

        // Assert
        await Assert.ThrowsAsync<OperationCanceledException>(act);
        _publisher.Verify(
            p => p.PublicarOrcamentoFalhouAsync(
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task OrdemServicoFinalizada_ComOrcamento_CriaCobrancaPixComPayerPersistido()
    {
        // Arrange
        await PrepararOrcamentoVigenteAsync();
        CriarOrderPixRequest? capturado = null;
        _mercadoPago
            .Setup(m => m.CriarOrderPixAsync(It.IsAny<CriarOrderPixRequest>(), It.IsAny<CancellationToken>()))
            .Callback<CriarOrderPixRequest, CancellationToken>((r, _) => capturado = r)
            .ReturnsAsync(new MercadoPagoOrder("ord_mp_x", MercadoPagoOrderSituacao.Pendente, "qr", "ticket"));
        var eventId = Guid.NewGuid();

        // Act
        await _service.ProcessarOrdemServicoFinalizadaAsync(eventId, new OrdemServicoFinalizadaData { OrdemServicoId = 10 });

        // Assert
        Assert.NotNull(capturado);
        Assert.Equal(10, capturado.OrdemServicoId);
        Assert.Equal(150m, capturado.ValorTotal);
        Assert.Equal("Ada", capturado.Cliente.Nome);
        Assert.Equal(OrcamentoSagaService.GerarIdempotencyKey(10, 1), capturado.IdempotencyKey);
        var cobranca = Assert.Single(_cobrancas.Items.Values);
        Assert.Equal("ord_mp_x", cobranca.Id);
        Assert.Equal("orc-10-1", cobranca.OrcamentoId);
        Assert.Equal("qr", cobranca.QrCode);
        Assert.Equal("ticket", cobranca.Ticket);
        _publisher.Verify(p => p.PublicarCobrancaGeradaAsync(cobranca, It.IsAny<CancellationToken>()), Times.Once);
        Assert.Contains(_eventos.Items, e => e.EventId == eventId && e.Tipo == EventTypes.OrdemServicoFinalizada);
    }

    [Fact]
    public async Task OrdemServicoFinalizada_QrCodeETicketAusentes_RegistraVazios()
    {
        // Arrange
        await PrepararOrcamentoVigenteAsync();
        _mercadoPago
            .Setup(m => m.CriarOrderPixAsync(It.IsAny<CriarOrderPixRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MercadoPagoOrder("ord_mp_x", MercadoPagoOrderSituacao.Pendente, null, null));

        // Act
        await _service.ProcessarOrdemServicoFinalizadaAsync(
            Guid.NewGuid(),
            new OrdemServicoFinalizadaData { OrdemServicoId = 10 });

        // Assert
        var cobranca = Assert.Single(_cobrancas.Items.Values);
        Assert.Equal(string.Empty, cobranca.QrCode);
        Assert.Equal(string.Empty, cobranca.Ticket);
    }

    [Fact]
    public async Task OrdemServicoFinalizada_SemOrcamento_MarcaProcessadoSemChamarMercadoPago()
    {
        // Arrange
        var eventId = Guid.NewGuid();

        // Act
        await _service.ProcessarOrdemServicoFinalizadaAsync(eventId, new OrdemServicoFinalizadaData { OrdemServicoId = 99 });

        // Assert
        _mercadoPago.Verify(
            m => m.CriarOrderPixAsync(It.IsAny<CriarOrderPixRequest>(), It.IsAny<CancellationToken>()),
            Times.Never);
        Assert.Contains(_eventos.Items, e => e.EventId == eventId);
    }

    [Fact]
    public async Task OrdemServicoFinalizada_OrcamentoCancelado_NaoCriaCobranca()
    {
        // Arrange
        var orcamento = await PrepararOrcamentoVigenteAsync();
        orcamento.Cancelar();

        // Act
        await _service.ProcessarOrdemServicoFinalizadaAsync(
            Guid.NewGuid(),
            new OrdemServicoFinalizadaData { OrdemServicoId = 10 });

        // Assert
        Assert.Empty(_cobrancas.Items);
    }

    [Fact]
    public async Task OrdemServicoFinalizada_OrcamentoVigenteAusenteNoGateway_NaoCriaCobranca()
    {
        // Arrange
        await PrepararOrcamentoVigenteAsync();
        _orcamentos.Items.Clear();

        // Act
        await _service.ProcessarOrdemServicoFinalizadaAsync(
            Guid.NewGuid(),
            new OrdemServicoFinalizadaData { OrdemServicoId = 10 });

        // Assert
        Assert.Empty(_cobrancas.Items);
    }

    [Fact]
    public async Task OrdemServicoFinalizada_Reentrega_NaoCriaSegundaCobranca()
    {
        // Arrange
        await PrepararOrcamentoVigenteAsync();
        var eventId = Guid.NewGuid();
        var data = new OrdemServicoFinalizadaData { OrdemServicoId = 10 };
        await _service.ProcessarOrdemServicoFinalizadaAsync(eventId, data);

        // Act
        await _service.ProcessarOrdemServicoFinalizadaAsync(eventId, data);
        await _service.ProcessarOrdemServicoFinalizadaAsync(Guid.NewGuid(), data);

        // Assert
        Assert.Single(_cobrancas.Items);
        _mercadoPago.Verify(
            m => m.CriarOrderPixAsync(It.IsAny<CriarOrderPixRequest>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task OrdemServicoFinalizada_CobrancaPendenteJaExiste_RepublicaSemNovaOrder()
    {
        // Arrange
        await PrepararOrcamentoVigenteAsync();
        var existente = Cobranca.CriarPendente("ord_prev", 10, "orc-10-1", "k", 150m);
        _cobrancas.Items[existente.Id] = existente;

        // Act
        await _service.ProcessarOrdemServicoFinalizadaAsync(
            Guid.NewGuid(),
            new OrdemServicoFinalizadaData { OrdemServicoId = 10 });

        // Assert
        _mercadoPago.Verify(
            m => m.CriarOrderPixAsync(It.IsAny<CriarOrderPixRequest>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _publisher.Verify(p => p.PublicarCobrancaGeradaAsync(existente, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task OrdemServicoFinalizada_CobrancaJaResolvida_NaoRepublica()
    {
        // Arrange
        await PrepararOrcamentoVigenteAsync();
        var existente = Cobranca.CriarPendente("ord_prev", 10, "orc-10-1", "k", 150m);
        existente.Confirmar();
        _cobrancas.Items[existente.Id] = existente;

        // Act
        await _service.ProcessarOrdemServicoFinalizadaAsync(
            Guid.NewGuid(),
            new OrdemServicoFinalizadaData { OrdemServicoId = 10 });

        // Assert
        _publisher.Verify(
            p => p.PublicarCobrancaGeradaAsync(It.IsAny<Cobranca>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task OrdemServicoDescartada_ComOrcamento_CancelaOrcamentoSemChamarMercadoPago()
    {
        // Arrange
        var orcamento = await PrepararOrcamentoVigenteAsync();
        var eventId = Guid.NewGuid();

        // Act
        await _service.ProcessarOrdemServicoDescartadaAsync(eventId, new OrdemServicoDescartadaData { OrdemServicoId = 10 });

        // Assert
        Assert.Equal(OrcamentoStatus.Cancelado, orcamento.Status);
        Assert.True(_ordens.Items[10].Descartada);
        _mercadoPago.VerifyNoOtherCalls();
        Assert.Contains(_eventos.Items, e => e.EventId == eventId && e.Tipo == EventTypes.OrdemServicoDescartada);
    }

    [Fact]
    public async Task OrdemServicoDescartada_SemOrcamento_ReconheceSemChamarMercadoPago()
    {
        // Arrange
        var eventId = Guid.NewGuid();

        // Act
        await _service.ProcessarOrdemServicoDescartadaAsync(eventId, new OrdemServicoDescartadaData { OrdemServicoId = 55 });

        // Assert
        Assert.True(_ordens.Items[55].Descartada);
        Assert.Empty(_orcamentos.Items);
        _mercadoPago.VerifyNoOtherCalls();
        Assert.Contains(_eventos.Items, e => e.EventId == eventId);
    }

    [Fact]
    public async Task OrdemServicoDescartada_OrcamentoJaCancelado_NaoSalvaDeNovo()
    {
        // Arrange
        var orcamento = await PrepararOrcamentoVigenteAsync();
        orcamento.Cancelar();
        _orcamentos.SalvarException = new InvalidOperationException("não deveria salvar");

        // Act
        await _service.ProcessarOrdemServicoDescartadaAsync(
            Guid.NewGuid(),
            new OrdemServicoDescartadaData { OrdemServicoId = 10 });

        // Assert
        Assert.True(_ordens.Items[10].Descartada);
    }

    [Fact]
    public async Task OrdemServicoDescartada_OrcamentoVigenteAusenteNoGateway_Reconhece()
    {
        // Arrange
        await PrepararOrcamentoVigenteAsync();
        _orcamentos.Items.Clear();

        // Act
        await _service.ProcessarOrdemServicoDescartadaAsync(
            Guid.NewGuid(),
            new OrdemServicoDescartadaData { OrdemServicoId = 10 });

        // Assert
        Assert.True(_ordens.Items[10].Descartada);
    }

    [Fact]
    public async Task OrdemServicoDescartada_Reentrega_ProcessaUmaVez()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var data = new OrdemServicoDescartadaData { OrdemServicoId = 10 };
        await _service.ProcessarOrdemServicoDescartadaAsync(eventId, data);

        // Act
        await _service.ProcessarOrdemServicoDescartadaAsync(eventId, data);

        // Assert
        Assert.Single(_eventos.Items);
    }

    [Fact]
    public async Task Webhook_OrderInexistente_Ignora()
    {
        // Arrange
        _mercadoPago
            .Setup(m => m.ObterOrderAsync("x", It.IsAny<CancellationToken>()))
            .ReturnsAsync((MercadoPagoOrder?)null);

        // Act
        await _service.ProcessarWebhookOrderAsync("x");

        // Assert
        _publisher.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Webhook_OrderPendente_NaoPublica()
    {
        // Arrange
        var cobranca = await PrepararCobrancaPendenteAsync();
        _mercadoPago
            .Setup(m => m.ObterOrderAsync(cobranca.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MercadoPagoOrder(cobranca.Id, MercadoPagoOrderSituacao.Pendente, null, null));
        _publisher.Invocations.Clear();

        // Act
        await _service.ProcessarWebhookOrderAsync(cobranca.Id);

        // Assert
        _publisher.VerifyNoOtherCalls();
        Assert.Equal(CobrancaStatus.Pendente, cobranca.Status);
    }

    [Fact]
    public async Task Webhook_CobrancaDesconhecida_Ignora()
    {
        // Arrange
        _mercadoPago
            .Setup(m => m.ObterOrderAsync("ord_unknown", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MercadoPagoOrder("ord_unknown", MercadoPagoOrderSituacao.Aprovada, null, null));

        // Act
        await _service.ProcessarWebhookOrderAsync("ord_unknown");

        // Assert
        _publisher.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Webhook_Aprovada_ConfirmaEPublicaPagamentoConfirmado()
    {
        // Arrange
        var cobranca = await PrepararCobrancaPendenteAsync();
        _mercadoPago
            .Setup(m => m.ObterOrderAsync(cobranca.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MercadoPagoOrder(cobranca.Id, MercadoPagoOrderSituacao.Aprovada, null, null));

        // Act
        await _service.ProcessarWebhookOrderAsync(cobranca.Id);

        // Assert
        Assert.Equal(CobrancaStatus.Confirmada, cobranca.Status);
        _publisher.Verify(p => p.PublicarPagamentoConfirmadoAsync(cobranca, It.IsAny<CancellationToken>()), Times.Once);
        _mercadoPago.Verify(m => m.CancelarOrderAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Webhook_AprovadaDuplicada_PublicaUmaVez()
    {
        // Arrange
        var cobranca = await PrepararCobrancaPendenteAsync();
        _mercadoPago
            .Setup(m => m.ObterOrderAsync(cobranca.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MercadoPagoOrder(cobranca.Id, MercadoPagoOrderSituacao.Aprovada, null, null));
        await _service.ProcessarWebhookOrderAsync(cobranca.Id);

        // Act
        await _service.ProcessarWebhookOrderAsync(cobranca.Id);

        // Assert
        _publisher.Verify(
            p => p.PublicarPagamentoConfirmadoAsync(It.IsAny<Cobranca>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Webhook_NotificacaoJaReivindicada_NaoPublica()
    {
        // Arrange
        var cobranca = await PrepararCobrancaPendenteAsync();
        _mercadoPago
            .Setup(m => m.ObterOrderAsync(cobranca.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MercadoPagoOrder(cobranca.Id, MercadoPagoOrderSituacao.Aprovada, null, null));
        _eventos.Items.Add(new EventoProcessado
        {
            EventId = Guid.NewGuid(),
            Tipo = OrcamentoSagaService.TipoWebhookMercadoPago,
            ChaveNegocio = $"{cobranca.Id}:{MercadoPagoOrderSituacao.Aprovada}"
        });

        // Act
        await _service.ProcessarWebhookOrderAsync(cobranca.Id);

        // Assert
        _publisher.Verify(
            p => p.PublicarPagamentoConfirmadoAsync(It.IsAny<Cobranca>(), It.IsAny<CancellationToken>()),
            Times.Never);
        Assert.Equal(CobrancaStatus.Pendente, cobranca.Status);
    }

    [Fact]
    public async Task Webhook_Expirada_CancelaPublicaRecusadoENaoGeraNovaCobranca()
    {
        // Arrange
        var cobranca = await PrepararCobrancaPendenteAsync();
        _mercadoPago
            .Setup(m => m.ObterOrderAsync(cobranca.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MercadoPagoOrder(cobranca.Id, MercadoPagoOrderSituacao.Expirada, null, null));

        // Act
        await _service.ProcessarWebhookOrderAsync(cobranca.Id);

        // Assert
        Assert.Equal(CobrancaStatus.Expirada, cobranca.Status);
        _mercadoPago.Verify(m => m.CancelarOrderAsync(cobranca.Id, It.IsAny<CancellationToken>()), Times.Once);
        _publisher.Verify(
            p => p.PublicarPagamentoRecusadoAsync(
                cobranca,
                PagamentoRecusadoMotivos.Expirado,
                It.IsAny<CancellationToken>()),
            Times.Once);
        Assert.Single(_cobrancas.Items);
        _mercadoPago.Verify(
            m => m.CriarOrderPixAsync(It.IsAny<CriarOrderPixRequest>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GerarNovaCobrancaAposExpiracao_ComUltimaExpirada_CriaNova()
    {
        // Arrange
        var cobranca = await PrepararCobrancaPendenteAsync();
        cobranca.Expirar();
        await _cobrancas.SalvarAsync(cobranca);

        // Act
        var nova = await _service.GerarNovaCobrancaAposExpiracaoAsync(10);

        // Assert
        Assert.Equal(CobrancaStatus.Pendente, nova.Status);
        Assert.Equal(2, _cobrancas.Items.Count);
        Assert.Equal(OrcamentoSagaService.GerarIdempotencyKey(10, 2), nova.IdempotencyKey);
        _publisher.Verify(p => p.PublicarCobrancaGeradaAsync(nova, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GerarNovaCobrancaAposExpiracao_ComPendente_Lanca()
    {
        // Arrange
        await PrepararCobrancaPendenteAsync();

        // Act
        var act = () => _service.GerarNovaCobrancaAposExpiracaoAsync(10);

        // Assert
        await Assert.ThrowsAsync<BusinessRuleException>(act);
    }

    [Fact]
    public async Task Webhook_Recusada_PublicaMotivoRecusado()
    {
        // Arrange
        var cobranca = await PrepararCobrancaPendenteAsync();
        _mercadoPago
            .Setup(m => m.ObterOrderAsync(cobranca.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MercadoPagoOrder(cobranca.Id, MercadoPagoOrderSituacao.Recusada, null, null));

        // Act
        await _service.ProcessarWebhookOrderAsync(cobranca.Id);

        // Assert
        Assert.Equal(CobrancaStatus.Recusada, cobranca.Status);
        _publisher.Verify(
            p => p.PublicarPagamentoRecusadoAsync(
                cobranca,
                PagamentoRecusadoMotivos.Recusado,
                It.IsAny<CancellationToken>()),
            Times.Once);
        Assert.Equal(2, _cobrancas.Items.Count);
    }

    [Fact]
    public async Task Webhook_RecusadaComOrdemDescartada_NaoGeraNovaCobranca()
    {
        // Arrange
        var cobranca = await PrepararCobrancaPendenteAsync();
        _ordens.Items[10].MarcarDescartada();
        _mercadoPago
            .Setup(m => m.ObterOrderAsync(cobranca.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MercadoPagoOrder(cobranca.Id, MercadoPagoOrderSituacao.Recusada, null, null));

        // Act
        await _service.ProcessarWebhookOrderAsync(cobranca.Id);

        // Assert
        Assert.Single(_cobrancas.Items);
        _publisher.Verify(
            p => p.PublicarPagamentoRecusadoAsync(cobranca, It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public void GerarIdempotencyKey_EhDeterministicoEDistintoPorTentativa()
    {
        // Arrange & Act
        var primeira = OrcamentoSagaService.GerarIdempotencyKey(10, 1);
        var repetida = OrcamentoSagaService.GerarIdempotencyKey(10, 1);
        var segunda = OrcamentoSagaService.GerarIdempotencyKey(10, 2);

        // Assert
        Assert.Equal(primeira, repetida);
        Assert.NotEqual(primeira, segunda);
        Assert.True(Guid.TryParse(primeira, out _));
    }
}
