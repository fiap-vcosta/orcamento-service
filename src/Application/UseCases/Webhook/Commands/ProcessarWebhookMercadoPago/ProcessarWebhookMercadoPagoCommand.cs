using MediatR;

namespace Application.UseCases.Webhook.Commands.ProcessarWebhookMercadoPago;

public sealed record ProcessarWebhookMercadoPagoCommand(
    string DataId,
    string? XSignature,
    string? XRequestId) : IRequest;
