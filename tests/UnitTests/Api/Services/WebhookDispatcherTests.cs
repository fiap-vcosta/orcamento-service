using Api.Services;
using Application.UseCases.Webhook.Commands.ProcessarWebhookMercadoPago;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace UnitTests.Api.Services;

public class WebhookDispatcherTests
{
    private static WebhookDispatcher CriarDispatcher(Mock<IMediator> mediator)
    {
        var services = new ServiceCollection();
        services.AddSingleton(mediator.Object);
        var provider = services.BuildServiceProvider();
        return new WebhookDispatcher(
            provider.GetRequiredService<IServiceScopeFactory>(),
            NullLogger<WebhookDispatcher>.Instance);
    }

    [Fact]
    public async Task Dispatch_EnviaOComandoAoMediator()
    {
        // Arrange
        var mediator = new Mock<IMediator>();
        var dispatcher = CriarDispatcher(mediator);
        var command = new ProcessarWebhookMercadoPagoCommand("ord1", "sig", "req");

        // Act
        await dispatcher.Dispatch(command);

        // Assert
        mediator.Verify(m => m.Send(command, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Dispatch_FalhaNoMediator_NaoPropagaExcecao()
    {
        // Arrange
        var mediator = new Mock<IMediator>();
        mediator
            .Setup(m => m.Send(It.IsAny<ProcessarWebhookMercadoPagoCommand>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("boom"));
        var dispatcher = CriarDispatcher(mediator);

        // Act
        var task = dispatcher.Dispatch(new ProcessarWebhookMercadoPagoCommand("ord1", null, null));
        await task;

        // Assert
        Assert.True(task.IsCompletedSuccessfully);
        mediator.Verify(
            m => m.Send(It.IsAny<ProcessarWebhookMercadoPagoCommand>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
