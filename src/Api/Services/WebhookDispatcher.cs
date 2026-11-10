using Application.UseCases.Webhook.Commands.ProcessarWebhookMercadoPago;
using MediatR;

namespace Api.Services;

public sealed class WebhookDispatcher(
    IServiceScopeFactory scopeFactory,
    ILogger<WebhookDispatcher> logger) : IWebhookDispatcher
{
    public Task Dispatch(ProcessarWebhookMercadoPagoCommand command) =>
        Task.Run(async () =>
        {
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
                await mediator.Send(command);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Falha ao processar webhook do Mercado Pago para {DataId}", command.DataId);
            }
        });
}
