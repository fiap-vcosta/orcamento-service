using Google.Cloud.Firestore;
using Grpc.Core;

namespace Infrastructure.Firestore;

public sealed class FirestoreDocumentRecordStore(FirestoreDb firestoreDb) : IDocumentRecordStore
{
    public async Task<IReadOnlyDictionary<string, object?>?> GetAsync(
        string collection,
        string documentId,
        CancellationToken cancellationToken = default)
    {
        var snapshot = await firestoreDb
            .Collection(collection)
            .Document(documentId)
            .GetSnapshotAsync(cancellationToken);

        return snapshot.Exists ? ToFields(snapshot) : null;
    }

    public Task SetAsync(
        string collection,
        string documentId,
        IDictionary<string, object?> fields,
        CancellationToken cancellationToken = default) =>
        firestoreDb
            .Collection(collection)
            .Document(documentId)
            .SetAsync(fields, cancellationToken: cancellationToken);

    public async Task<bool> CreateIfAbsentAsync(
        string collection,
        string documentId,
        IDictionary<string, object?> fields,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await firestoreDb
                .Collection(collection)
                .Document(documentId)
                .CreateAsync(fields, cancellationToken);
            return true;
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.AlreadyExists)
        {
            return false;
        }
    }

    public async Task<IReadOnlyList<IReadOnlyDictionary<string, object?>>> QueryEqualAsync(
        string collection,
        string field,
        object value,
        CancellationToken cancellationToken = default)
    {
        var snapshot = await firestoreDb
            .Collection(collection)
            .WhereEqualTo(field, value)
            .GetSnapshotAsync(cancellationToken);

        return snapshot.Documents.Select(ToFields).ToList();
    }

    private static Dictionary<string, object?> ToFields(DocumentSnapshot snapshot) =>
        snapshot.ToDictionary().ToDictionary(pair => pair.Key, pair => (object?)pair.Value);
}
