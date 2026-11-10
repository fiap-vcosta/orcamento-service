using BuildingBlocks.Events;
using Domain.Entities;
using Infrastructure.Messaging;
using UnitTests.Fakes;

namespace UnitTests.Infrastructure.Messaging;

public class OrcamentoIntegrationPublisherTests
{
    private readonly RecordingEventPublisher _events = new();
    private readonly OrcamentoIntegrationPublisher _publisher;

    public OrcamentoIntegrationPublisherTests()
    {
        _publisher = new OrcamentoIntegrationPublisher(_events);
    }

    private static Cobranca CobrancaComPix()
    {
        var cobranca = Cobranca.CriarPendente("ord_1", 10, "orc-10-1", "key", 150m);
        cobranca.RegistrarPix("qr", "ticket");
        return cobranca;
    }

    [Fact]
    public async Task PublicarOrcamentoGerado_PublicaGeradoENotificacaoDisponivel()
    {
        // Arrange
        var orcamento = Orcamento.Criar(
            10,
            1,
            new ClienteOrcamento { Nome = "Ada", Email = "a@b.c", Documento = "1" },
            [
                new OrcamentoServico { Id = 1, Nome = "Óleo", ValorCobrado = 150m, Status = "Sugerido" },
                new OrcamentoServico { Id = 2, Nome = "Freio", ValorCobrado = 90m, Status = "Rejeitado" }
            ]);

        // Act
        await _publisher.PublicarOrcamentoGeradoAsync(orcamento);

        // Assert
        Assert.Equal(2, _events.Published.Count);
        var gerado = _events.Published[0];
        Assert.Equal(TopicIds.OrcamentoGerado, gerado.TopicId);
        Assert.Equal(EventTypes.OrcamentoGerado, gerado.EventType);
        var geradoData = Assert.IsType<OrcamentoGeradoData>(gerado.Data);
        Assert.Equal("orc-10-1", geradoData.OrcamentoId);
        Assert.Equal(1, geradoData.VersaoDiagnostico);
        var notificacao = _events.Published[1];
        Assert.Equal(TopicIds.Notificacoes, notificacao.TopicId);
        Assert.Equal(EventTypes.OrcamentoDisponivel, notificacao.EventType);
        var disponivel = Assert.IsType<OrcamentoDisponivelData>(notificacao.Data);
        Assert.Equal(150m, disponivel.ValorTotal);
        var servico = Assert.Single(disponivel.Servicos);
        Assert.Equal("Óleo", servico.Nome);
    }

    [Fact]
    public async Task PublicarOrcamentoFalhou_PublicaMotivo()
    {
        // Arrange & Act
        await _publisher.PublicarOrcamentoFalhouAsync(10, 2, "falha");

        // Assert
        var published = Assert.Single(_events.Published);
        Assert.Equal(TopicIds.OrcamentoFalhou, published.TopicId);
        Assert.Equal(EventTypes.OrcamentoFalhou, published.EventType);
        var data = Assert.IsType<OrcamentoFalhouData>(published.Data);
        Assert.Equal(10, data.OrdemServicoId);
        Assert.Equal(2, data.VersaoDiagnostico);
        Assert.Equal("falha", data.Motivo);
    }

    [Fact]
    public async Task PublicarCobrancaGerada_PublicaGeradaEPixDisponivel()
    {
        // Arrange
        var cobranca = CobrancaComPix();

        // Act
        await _publisher.PublicarCobrancaGeradaAsync(cobranca);

        // Assert
        Assert.Equal(2, _events.Published.Count);
        Assert.Equal(TopicIds.CobrancaGerada, _events.Published[0].TopicId);
        var gerada = Assert.IsType<CobrancaGeradaData>(_events.Published[0].Data);
        Assert.Equal("ord_1", gerada.CobrancaId);
        Assert.Equal(TopicIds.Notificacoes, _events.Published[1].TopicId);
        Assert.Equal(EventTypes.CobrancaPixDisponivel, _events.Published[1].EventType);
        var pix = Assert.IsType<CobrancaPixDisponivelData>(_events.Published[1].Data);
        Assert.Equal("qr", pix.QrCode);
        Assert.Equal("ticket", pix.Ticket);
        Assert.Equal(150m, pix.ValorTotal);
    }

    [Fact]
    public async Task PublicarCobrancaGerada_SemPix_UsaTextoVazio()
    {
        // Arrange
        var cobranca = Cobranca.CriarPendente("ord_2", 11, "orc-11-1", "key", 10m);

        // Act
        await _publisher.PublicarCobrancaGeradaAsync(cobranca);

        // Assert
        var pix = Assert.IsType<CobrancaPixDisponivelData>(_events.Published[1].Data);
        Assert.Equal(string.Empty, pix.QrCode);
        Assert.Equal(string.Empty, pix.Ticket);
    }

    [Fact]
    public async Task PublicarPagamentoConfirmado_PublicaNoTopicoDePagamento()
    {
        // Arrange
        var cobranca = CobrancaComPix();

        // Act
        await _publisher.PublicarPagamentoConfirmadoAsync(cobranca);

        // Assert
        var published = Assert.Single(_events.Published);
        Assert.Equal(TopicIds.PagamentoConfirmado, published.TopicId);
        Assert.Equal(EventTypes.PagamentoConfirmado, published.EventType);
        var data = Assert.IsType<PagamentoConfirmadoData>(published.Data);
        Assert.Equal(10, data.OrdemServicoId);
        Assert.Equal("ord_1", data.CobrancaId);
    }

    [Fact]
    public async Task PublicarPagamentoRecusado_PublicaMotivo()
    {
        // Arrange
        var cobranca = CobrancaComPix();

        // Act
        await _publisher.PublicarPagamentoRecusadoAsync(cobranca, PagamentoRecusadoMotivos.Expirado);

        // Assert
        var published = Assert.Single(_events.Published);
        Assert.Equal(TopicIds.PagamentoRecusado, published.TopicId);
        Assert.Equal(EventTypes.PagamentoRecusado, published.EventType);
        var data = Assert.IsType<PagamentoRecusadoData>(published.Data);
        Assert.Equal("expirado", data.Motivo);
    }
}
