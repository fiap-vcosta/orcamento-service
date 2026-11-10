using System.Diagnostics.CodeAnalysis;
using Application.Services;
using BuildingBlocks.Events;
using BuildingBlocks.Messaging;
using Microsoft.Extensions.DependencyInjection;

namespace Application.Messaging;

[ExcludeFromCodeCoverage(Justification = "Thin DI adapter; covered via OrcamentoSagaService tests.")]
public class DiagnosticoFinalizadoConsumer(IServiceScopeFactory scopeFactory)
    : IIntegrationEventConsumer<DiagnosticoFinalizadoData>
{
    public string EventType => EventTypes.DiagnosticoFinalizado;

    public async Task HandleAsync(EventEnvelope<DiagnosticoFinalizadoData> envelope, CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var service = scope.ServiceProvider.GetRequiredService<OrcamentoSagaService>();
        await service.ProcessarDiagnosticoFinalizadoAsync(envelope.EventId, envelope.Data, cancellationToken);
    }
}
