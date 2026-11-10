namespace Infrastructure.MercadoPago;

public sealed class MercadoPagoOptions
{
    public const string SectionName = "MercadoPago";

    public string AccessToken { get; set; } = string.Empty;
    public string WebhookSecret { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = "https://api.mercadopago.com";
    public string ExpirationTime { get; set; } = "PT60S";
}
