using Infrastructure.Firestore;

namespace UnitTests.Fakes;

public sealed class InMemoryDocumentRecordStore : IDocumentRecordStore
{
    private readonly Dictionary<string, Dictionary<string, IReadOnlyDictionary<string, object?>>> _collections = new();

    public Task<IReadOnlyDictionary<string, object?>?> GetAsync(
        string collection,
        string documentId,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyDictionary<string, object?>? result = null;
        if (_collections.TryGetValue(collection, out var documents) && documents.TryGetValue(documentId, out var found))
        {
            result = found;
        }

        return Task.FromResult(result);
    }

    public Task SetAsync(
        string collection,
        string documentId,
        IDictionary<string, object?> fields,
        CancellationToken cancellationToken = default)
    {
        Store(collection, documentId, fields);
        return Task.CompletedTask;
    }

    public Task<bool> CreateIfAbsentAsync(
        string collection,
        string documentId,
        IDictionary<string, object?> fields,
        CancellationToken cancellationToken = default)
    {
        if (_collections.TryGetValue(collection, out var documents) && documents.ContainsKey(documentId))
        {
            return Task.FromResult(false);
        }

        Store(collection, documentId, fields);
        return Task.FromResult(true);
    }

    public Task<IReadOnlyList<IReadOnlyDictionary<string, object?>>> QueryEqualAsync(
        string collection,
        string field,
        object value,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<IReadOnlyDictionary<string, object?>> result = _collections.TryGetValue(collection, out var documents)
            ? documents.Values
                .Where(d => d.TryGetValue(field, out var current) && Equals(current, value))
                .ToList()
            : [];
        return Task.FromResult(result);
    }

    private void Store(string collection, string documentId, IDictionary<string, object?> fields)
    {
        if (!_collections.TryGetValue(collection, out var documents))
        {
            documents = new Dictionary<string, IReadOnlyDictionary<string, object?>>();
            _collections[collection] = documents;
        }

        documents[documentId] = new Dictionary<string, object?>(fields);
    }
}
