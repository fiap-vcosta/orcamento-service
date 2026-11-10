using Application.Abstractions.Services;
using Application.Services;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.UseCases.Webhook.Commands.ProcessarWebhookMercadoPago;

public sealed class ProcessarWebhookMercadoPagoCommandHandler(
    IMercadoPagoWebhookSignatureValidator signatureValidator,
    OrcamentoSagaService sagaService,
    ILogger<ProcessarWebhookMercadoPagoCommandHandler> logger)
    : IRequestHandler<ProcessarWebhookMercadoPagoCommand>
{
    public async Task Handle(ProcessarWebhookMercadoPagoCommand request, CancellationToken cancellationToken)
    {
        if (!signatureValidator.IsValid(request.XSignature, request.XRequestId, request.DataId))
        {
            logger.LogWarning("Webhook do Mercado Pago com assinatura inválida para {DataId}", request.DataId);
            return;
        }

        await sagaService.ProcessarWebhookOrderAsync(request.DataId, cancellationToken);
    }
}
