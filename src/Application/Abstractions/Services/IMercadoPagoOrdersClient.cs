namespace Application.Abstractions.Services;

public interface IMercadoPagoOrdersClient
{
    Task<MercadoPagoOrder> CriarOrderPixAsync(
        CriarOrderPixRequest request,
        CancellationToken cancellationToken = default);

    Task<MercadoPagoOrder?> ObterOrderAsync(string orderId, CancellationToken cancellationToken = default);

    Task CancelarOrderAsync(string orderId, CancellationToken cancellationToken = default);
}
