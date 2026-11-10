using System.Globalization;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Application.Abstractions.Services;
using Microsoft.Extensions.Options;

namespace Infrastructure.MercadoPago;

public sealed class MercadoPagoOrdersClient(HttpClient httpClient, IOptions<MercadoPagoOptions> options)
    : IMercadoPagoOrdersClient
{
    private const string IdempotencyHeader = "X-Idempotency-Key";

    public async Task<MercadoPagoOrder> CriarOrderPixAsync(
        CriarOrderPixRequest request,
        CancellationToken cancellationToken = default)
    {
        var valor = request.ValorTotal.ToString("F2", CultureInfo.InvariantCulture);
        var documento = new string(request.Cliente.Documento.Where(char.IsDigit).ToArray());

        var body = new JsonObject
        {
            ["type"] = "online",
            ["total_amount"] = valor,
            ["external_reference"] = request.OrdemServicoId.ToString(CultureInfo.InvariantCulture),
            ["processing_mode"] = "automatic",
            ["transactions"] = new JsonObject
            {
                ["payments"] = new JsonArray(new JsonObject
                {
                    ["amount"] = valor,
                    ["payment_method"] = new JsonObject
                    {
                        ["id"] = "pix",
                        ["type"] = "bank_transfer"
                    },
                    ["expiration_time"] = options.Value.ExpirationTime
                })
            },
            ["payer"] = new JsonObject
            {
                ["email"] = request.Cliente.Email,
                ["first_name"] = request.Cliente.Nome,
                ["identification"] = new JsonObject
                {
                    ["type"] = documento.Length == 14 ? "CNPJ" : "CPF",
                    ["number"] = documento
                }
            }
        };

        using var message = new HttpRequestMessage(HttpMethod.Post, "v1/orders")
        {
            Content = new StringContent(body.ToJsonString(), Encoding.UTF8, "application/json")
        };
        message.Headers.Add(IdempotencyHeader, request.IdempotencyKey);

        using var response = await httpClient.SendAsync(message, cancellationToken);
        var json = await ReadOrThrowAsync(response, cancellationToken);

        var order = Parse(json);
        if (string.IsNullOrWhiteSpace(order.QrCode))
        {
            throw new InvalidOperationException("Order Pix do Mercado Pago sem qr_code na resposta.");
        }

        return order;
    }

    public async Task<MercadoPagoOrder?> ObterOrderAsync(string orderId, CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.GetAsync($"v1/orders/{Uri.EscapeDataString(orderId)}", cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        return Parse(await ReadOrThrowAsync(response, cancellationToken));
    }

    public async Task CancelarOrderAsync(string orderId, CancellationToken cancellationToken = default)
    {
        using var message = new HttpRequestMessage(HttpMethod.Post, $"v1/orders/{Uri.EscapeDataString(orderId)}/cancel");
        message.Headers.Add(IdempotencyHeader, Guid.NewGuid().ToString());

        using var response = await httpClient.SendAsync(message, cancellationToken);
        if (response.StatusCode == HttpStatusCode.Conflict)
        {
            return;
        }

        await ReadOrThrowAsync(response, cancellationToken);
    }

    public static MercadoPagoOrderSituacao MapearSituacao(string? status) =>
        status?.ToLowerInvariant() switch
        {
            "processed" or "paid" or "approved" => MercadoPagoOrderSituacao.Aprovada,
            "expired" => MercadoPagoOrderSituacao.Expirada,
            "failed" or "canceled" or "cancelled" or "rejected" or "refunded" => MercadoPagoOrderSituacao.Recusada,
            _ => MercadoPagoOrderSituacao.Pendente
        };

    private static async Task<string> ReadOrThrowAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException(
                $"Mercado Pago respondeu {(int)response.StatusCode}: {content}",
                null,
                response.StatusCode);
        }

        return content;
    }

    private static MercadoPagoOrder Parse(string json)
    {
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        var id = root.GetProperty("id").GetString()
            ?? throw new InvalidOperationException("Order do Mercado Pago sem id na resposta.");
        var status = root.TryGetProperty("status", out var statusElement) ? statusElement.GetString() : null;

        string? qrCode = null;
        string? ticket = null;
        if (root.TryGetProperty("transactions", out var transactions)
            && transactions.TryGetProperty("payments", out var payments)
            && payments.ValueKind == JsonValueKind.Array
            && payments.GetArrayLength() > 0
            && payments[0].TryGetProperty("payment_method", out var paymentMethod))
        {
            qrCode = ReadString(paymentMethod, "qr_code");
            ticket = ReadString(paymentMethod, "ticket_url");
        }

        return new MercadoPagoOrder(id, MapearSituacao(status), qrCode, ticket);
    }

    private static string? ReadString(JsonElement element, string name) =>
        element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;
}
