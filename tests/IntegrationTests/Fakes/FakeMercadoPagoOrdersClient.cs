using System.Collections.Concurrent;
using Application.Abstractions.Services;

namespace IntegrationTests.Fakes;

public sealed class FakeMercadoPagoOrdersClient : IMercadoPagoOrdersClient
{
    private int _sequence;

    public ConcurrentDictionary<string, MercadoPagoOrderSituacao> Orders { get; } = new(StringComparer.Ordinal);

    public ConcurrentBag<string> Cancelamentos { get; } = [];

    public ConcurrentBag<CriarOrderPixRequest> Criacoes { get; } = [];

    public Task<MercadoPagoOrder> CriarOrderPixAsync(
        CriarOrderPixRequest request,
        CancellationToken cancellationToken = default)
    {
        Criacoes.Add(request);
        var id = $"ORD_FAKE_{Interlocked.Increment(ref _sequence)}";
        Orders[id] = MercadoPagoOrderSituacao.Pendente;
        return Task.FromResult(new MercadoPagoOrder(id, MercadoPagoOrderSituacao.Pendente, "000201fake", "https://ticket.fake"));
    }

    public Task<MercadoPagoOrder?> ObterOrderAsync(string orderId, CancellationToken cancellationToken = default)
    {
        MercadoPagoOrder? order = Orders.TryGetValue(orderId, out var situacao)
            ? new MercadoPagoOrder(orderId, situacao, null, null)
            : null;
        return Task.FromResult(order);
    }

    public Task CancelarOrderAsync(string orderId, CancellationToken cancellationToken = default)
    {
        Cancelamentos.Add(orderId);
        return Task.CompletedTask;
    }
}
