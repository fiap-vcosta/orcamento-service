using Application.UseCases.Webhook.Commands.ProcessarWebhookMercadoPago;

namespace Api.Services;

public interface IWebhookDispatcher
{
    Task Dispatch(ProcessarWebhookMercadoPagoCommand command);
}
