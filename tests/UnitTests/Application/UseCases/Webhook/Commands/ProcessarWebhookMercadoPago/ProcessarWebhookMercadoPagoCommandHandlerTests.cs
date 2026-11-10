using Application.Abstractions.Services;
using Application.Services;
using Application.UseCases.Webhook.Commands.ProcessarWebhookMercadoPago;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using UnitTests.Fakes;

namespace UnitTests.Application.UseCases.Webhook.Commands.ProcessarWebhookMercadoPago;

public class ProcessarWebhookMercadoPagoCommandHandlerTests
{
    private readonly Mock<IMercadoPagoWebhookSignatureValidator> _validator = new();
    private readonly Mock<IMercadoPagoOrdersClient> _mercadoPago = new();
    private readonly ProcessarWebhookMercadoPagoCommandHandler _handler;

    public ProcessarWebhookMercadoPagoCommandHandlerTests()
    {
        var saga = new OrcamentoSagaService(
            new InMemoryOrcamentoGateway(),
            new InMemoryOrdemOrcamentoGateway(),
            new InMemoryCobrancaGateway(),
            new InMemoryEventoProcessadoGateway(),
            _mercadoPago.Object,
            Mock.Of<IOrcamentoIntegrationPublisher>(),
            NullLogger<OrcamentoSagaService>.Instance);
        _handler = new ProcessarWebhookMercadoPagoCommandHandler(
            _validator.Object,
            saga,
            NullLogger<ProcessarWebhookMercadoPagoCommandHandler>.Instance);
    }

    [Fact]
    public async Task Handle_AssinaturaValida_ConsultaOrder()
    {
        // Arrange
        _validator.Setup(v => v.IsValid("sig", "req", "ord1")).Returns(true);
        var command = new ProcessarWebhookMercadoPagoCommand("ord1", "sig", "req");

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _mercadoPago.Verify(m => m.ObterOrderAsync("ord1", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_AssinaturaInvalida_NaoConsultaOrder()
    {
        // Arrange
        _validator.Setup(v => v.IsValid(It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string>())).Returns(false);
        var command = new ProcessarWebhookMercadoPagoCommand("ord1", "sig", "req");

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _mercadoPago.VerifyNoOtherCalls();
    }
}
