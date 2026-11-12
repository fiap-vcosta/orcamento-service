using Application.Abstractions.Services;
using Application.Services;
using Application.UseCases.Cobranca.Commands.GerarNovaCobranca;
using BuildingBlocks.Events;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using UnitTests.Fakes;

namespace UnitTests.Application.UseCases.Cobranca.Commands.GerarNovaCobranca;

public class GerarNovaCobrancaCommandHandlerTests
{
    private static readonly Guid OrdemServicoId = Guid.Parse("f3a4b5c6-7d8e-4f90-b1a2-c3d4e5f60718");

    private readonly InMemoryOrcamentoGateway _orcamentos = new();
    private readonly InMemoryOrdemOrcamentoGateway _ordens = new();
    private readonly InMemoryCobrancaGateway _cobrancas = new();
    private readonly InMemoryEventoProcessadoGateway _eventos = new();
    private readonly Mock<IMercadoPagoOrdersClient> _mercadoPago = new();
    private readonly Mock<IOrcamentoIntegrationPublisher> _publisher = new();
    private readonly GerarNovaCobrancaCommandHandler _handler;
    private int _orderSequence;

    public GerarNovaCobrancaCommandHandlerTests()
    {
        _mercadoPago
            .Setup(m => m.CriarOrderPixAsync(It.IsAny<CriarOrderPixRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => new MercadoPagoOrder(
                $"ord_mp_{++_orderSequence}",
                MercadoPagoOrderSituacao.Pendente,
                "000201qr",
                "https://ticket"));

        var saga = new OrcamentoSagaService(
            _orcamentos,
            _ordens,
            _cobrancas,
            _eventos,
            _mercadoPago.Object,
            _publisher.Object,
            NullLogger<OrcamentoSagaService>.Instance);
        _handler = new GerarNovaCobrancaCommandHandler(saga);
    }

    [Fact]
    public async Task Handle_AposExpiracao_RetornaNovaCobranca()
    {
        // Arrange
        var diagnostico = new DiagnosticoFinalizadoData
        {
            OrdemServicoId = OrdemServicoId,
            VersaoDiagnostico = 1,
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
                    Id = 1,
                    Nome = "Óleo",
                    ValorCobrado = 100m,
                    Status = ServicoDiagnosticoStatus.Aprovado
                }
            ]
        };
        var saga = new OrcamentoSagaService(
            _orcamentos,
            _ordens,
            _cobrancas,
            _eventos,
            _mercadoPago.Object,
            _publisher.Object,
            NullLogger<OrcamentoSagaService>.Instance);
        await saga.ProcessarDiagnosticoFinalizadoAsync(Guid.NewGuid(), diagnostico);
        await saga.ProcessarOrdemServicoFinalizadaAsync(
            Guid.NewGuid(),
            new OrdemServicoFinalizadaData { OrdemServicoId = OrdemServicoId });
        var existente = _cobrancas.Items.Values.Single();
        existente.Expirar();
        await _cobrancas.SalvarAsync(existente);

        // Act
        var response = await _handler.Handle(
            new GerarNovaCobrancaCommand { OrdemServicoId = OrdemServicoId },
            CancellationToken.None);

        // Assert
        Assert.Equal(OrdemServicoId, response.OrdemServicoId);
        Assert.Equal("Pendente", response.Status);
        Assert.Equal("000201qr", response.QrCode);
        Assert.Equal("https://ticket", response.TicketUrl);
        Assert.Equal(2, _cobrancas.Items.Count);
    }
}
