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
    public async Task<IActionResult> MercadoPago(
        [FromHeader(Name = "x-signature")] string? xSignature,
        [FromHeader(Name = "x-request-id")] string? xRequestId,
        [FromQuery(Name = "data.id")] string? dataIdFromQuery,
        CancellationToken cancellationToken)
    {
        var dataId = await ReadDataIdFromBodyAsync(cancellationToken) ?? dataIdFromQuery;
        if (!string.IsNullOrWhiteSpace(dataId))
        {
            _ = dispatcher.Dispatch(new ProcessarWebhookMercadoPagoCommand(
                dataId,
                xSignature,
                xRequestId));
        }

        return Ok();
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
