namespace Infrastructure.Firestore;

public interface IDocumentRecordStore
{
    Task<IReadOnlyDictionary<string, object?>?> GetAsync(
        string collection,
        string documentId,
        CancellationToken cancellationToken = default);

    Task SetAsync(
        string collection,
        string documentId,
        IDictionary<string, object?> fields,
        CancellationToken cancellationToken = default);

    Task<bool> CreateIfAbsentAsync(
        string collection,
        string documentId,
        IDictionary<string, object?> fields,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<IReadOnlyDictionary<string, object?>>> QueryEqualAsync(
        string collection,
        string field,
        object value,
        CancellationToken cancellationToken = default);
}
