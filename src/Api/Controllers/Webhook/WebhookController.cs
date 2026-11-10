using System.Text.Json;
using Api.Services;
using Application.UseCases.Webhook.Commands.ProcessarWebhookMercadoPago;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers.Webhook;

[ApiController]
[Route("orcamento/webhook")]
public class WebhookController(IWebhookDispatcher dispatcher) : ControllerBase
{
    [HttpPost("mercado-pago")]
    public async Task<IActionResult> MercadoPago(CancellationToken cancellationToken)
    {
        var dataId = await ResolverDataIdAsync(cancellationToken);
        if (!string.IsNullOrWhiteSpace(dataId))
        {
            _ = dispatcher.Dispatch(new ProcessarWebhookMercadoPagoCommand(
                dataId,
                Request.Headers["x-signature"].FirstOrDefault(),
                Request.Headers["x-request-id"].FirstOrDefault()));
        }

        return Ok();
    }

    private async Task<string?> ResolverDataIdAsync(CancellationToken cancellationToken)
    {
        var fromBody = await ReadDataIdFromBodyAsync(cancellationToken);
        if (!string.IsNullOrWhiteSpace(fromBody))
        {
            return fromBody;
        }

        return Request.Query["data.id"].FirstOrDefault();
    }

    private async Task<string?> ReadDataIdFromBodyAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var document = await JsonDocument.ParseAsync(Request.Body, cancellationToken: cancellationToken);
            if (document.RootElement.ValueKind == JsonValueKind.Object
                && document.RootElement.TryGetProperty("data", out var data)
                && data.ValueKind == JsonValueKind.Object
                && data.TryGetProperty("id", out var id))
            {
                return id.ValueKind == JsonValueKind.String ? id.GetString() : id.ToString();
            }
        }
        catch (JsonException)
        {
        }

        return null;
    }
}
