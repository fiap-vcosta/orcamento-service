namespace Application.Abstractions.Services;

public interface IMercadoPagoWebhookSignatureValidator
{
    bool IsValid(string? xSignature, string? xRequestId, string dataId);
}
