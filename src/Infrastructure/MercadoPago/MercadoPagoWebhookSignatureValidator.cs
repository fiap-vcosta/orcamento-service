using System.Security.Cryptography;
using System.Text;
using Application.Abstractions.Services;
using Microsoft.Extensions.Options;

namespace Infrastructure.MercadoPago;

public sealed class MercadoPagoWebhookSignatureValidator(IOptions<MercadoPagoOptions> options)
    : IMercadoPagoWebhookSignatureValidator
{
    public bool IsValid(string? xSignature, string? xRequestId, string dataId)
    {
        var secret = options.Value.WebhookSecret;
        if (string.IsNullOrWhiteSpace(secret) || string.IsNullOrWhiteSpace(xSignature) || string.IsNullOrWhiteSpace(dataId))
        {
            return false;
        }

        var (ts, v1) = ParseSignature(xSignature);
        if (ts is null || v1 is null)
        {
            return false;
        }

        var manifest = BuildManifest(dataId, xRequestId, ts);
        var expected = Convert.ToHexString(HMACSHA256.HashData(Encoding.UTF8.GetBytes(secret), Encoding.UTF8.GetBytes(manifest)))
            .ToLowerInvariant();

        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(expected),
            Encoding.UTF8.GetBytes(v1.ToLowerInvariant()));
    }

    public static string BuildManifest(string dataId, string? xRequestId, string ts)
    {
        var manifest = new StringBuilder($"id:{dataId.ToLowerInvariant()};");
        if (!string.IsNullOrWhiteSpace(xRequestId))
        {
            manifest.Append($"request-id:{xRequestId};");
        }

        manifest.Append($"ts:{ts};");
        return manifest.ToString();
    }

    private static (string? Ts, string? V1) ParseSignature(string xSignature)
    {
        string? ts = null;
        string? v1 = null;

        foreach (var part in xSignature.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var separator = part.IndexOf('=');
            if (separator <= 0)
            {
                continue;
            }

            var key = part[..separator].Trim();
            var value = part[(separator + 1)..].Trim();
            if (key == "ts")
            {
                ts = value;
            }
            else if (key == "v1")
            {
                v1 = value;
            }
        }

        return (ts, v1);
    }
}
