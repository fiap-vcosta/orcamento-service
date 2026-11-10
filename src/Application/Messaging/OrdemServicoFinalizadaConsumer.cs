using System.Diagnostics.CodeAnalysis;
using Application.Services;
using BuildingBlocks.Events;
using BuildingBlocks.Messaging;
using Microsoft.Extensions.DependencyInjection;

namespace Application.Messaging;

[ExcludeFromCodeCoverage(Justification = "Thin DI adapter; covered via OrcamentoSagaService tests.")]
public class OrdemServicoFinalizadaConsumer(IServiceScopeFactory scopeFactory)
    : IIntegrationEventConsumer<OrdemServicoFinalizadaData>
{
    public string EventType => EventTypes.OrdemServicoFinalizada;

    public async Task HandleAsync(EventEnvelope<OrdemServicoFinalizadaData> envelope, CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var service = scope.ServiceProvider.GetRequiredService<OrcamentoSagaService>();
        await service.ProcessarOrdemServicoFinalizadaAsync(envelope.EventId, envelope.Data, cancellationToken);
    }
}
