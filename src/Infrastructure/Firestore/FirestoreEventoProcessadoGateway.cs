using Application.Abstractions.Gateways;
using Domain.Entities;

namespace Infrastructure.Firestore;

public sealed class FirestoreEventoProcessadoGateway(IDocumentRecordStore recordStore) : IEventoProcessadoGateway
{
    public async Task<bool> ExisteEventIdAsync(Guid eventId, CancellationToken cancellationToken = default)
    {
        var documentos = await recordStore.QueryEqualAsync(
            FirestoreCollections.EventosAplicados,
            "eventId",
            eventId.ToString(),
            cancellationToken);
        return documentos.Count > 0;
    }

    public async Task<bool> ExisteChaveNegocioAsync(
        string tipo,
        string chaveNegocio,
        CancellationToken cancellationToken = default) =>
        await recordStore.GetAsync(
            FirestoreCollections.EventosAplicados,
            DocumentId(tipo, chaveNegocio),
            cancellationToken) is not null;

    public Task AddAsync(EventoProcessado evento, CancellationToken cancellationToken = default) =>
        recordStore.SetAsync(
            FirestoreCollections.EventosAplicados,
            DocumentId(evento.Tipo, evento.ChaveNegocio),
            ToFields(evento),
            cancellationToken);

    public Task<bool> TentarRegistrarAsync(EventoProcessado evento, CancellationToken cancellationToken = default) =>
        recordStore.CreateIfAbsentAsync(
            FirestoreCollections.EventosAplicados,
            DocumentId(evento.Tipo, evento.ChaveNegocio),
            ToFields(evento),
            cancellationToken);

    private static string DocumentId(string tipo, string chaveNegocio) => $"{tipo}_{chaveNegocio}";

    private static Dictionary<string, object?> ToFields(EventoProcessado evento) =>
        new()
        {
            ["eventId"] = evento.EventId.ToString(),
            ["tipo"] = evento.Tipo,
            ["chaveNegocio"] = evento.ChaveNegocio,
            ["processadoEm"] = DocumentFields.FormatDate(evento.ProcessadoEm)
        };
}
