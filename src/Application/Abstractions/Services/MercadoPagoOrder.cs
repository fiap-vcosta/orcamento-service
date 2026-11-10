namespace Application.Abstractions.Services;

public sealed record MercadoPagoOrder(
    string Id,
    MercadoPagoOrderSituacao Situacao,
    string? QrCode,
    string? Ticket);
