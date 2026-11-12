using System.Globalization;

namespace Infrastructure.Firestore;

public static class DocumentFields
{
    public static string FormatDecimal(decimal value) => value.ToString(CultureInfo.InvariantCulture);

    public static string FormatDate(DateTimeOffset value) => value.ToString("O", CultureInfo.InvariantCulture);

    public static string GetString(IReadOnlyDictionary<string, object?> fields, string name) =>
        GetNullableString(fields, name) ?? throw new InvalidOperationException($"Campo '{name}' ausente no documento.");

    public static string? GetNullableString(IReadOnlyDictionary<string, object?> fields, string name) =>
        fields.TryGetValue(name, out var value) ? value as string : null;

    public static int GetInt(IReadOnlyDictionary<string, object?> fields, string name) =>
        GetNullableInt(fields, name) ?? throw new InvalidOperationException($"Campo '{name}' ausente no documento.");

    public static int? GetNullableInt(IReadOnlyDictionary<string, object?> fields, string name) =>
        fields.TryGetValue(name, out var value) && value is not null
            ? Convert.ToInt32(value, CultureInfo.InvariantCulture)
            : null;

    public static Guid GetGuid(IReadOnlyDictionary<string, object?> fields, string name) =>
        Guid.Parse(GetString(fields, name));

    public static bool GetBool(IReadOnlyDictionary<string, object?> fields, string name) =>
        fields.TryGetValue(name, out var value) && value is true;

    public static decimal GetDecimal(IReadOnlyDictionary<string, object?> fields, string name) =>
        decimal.Parse(GetString(fields, name), CultureInfo.InvariantCulture);

    public static DateTimeOffset GetDate(IReadOnlyDictionary<string, object?> fields, string name) =>
        DateTimeOffset.Parse(GetString(fields, name), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);

    public static T GetEnum<T>(IReadOnlyDictionary<string, object?> fields, string name)
        where T : struct, Enum =>
        Enum.Parse<T>(GetString(fields, name));

    public static IReadOnlyDictionary<string, object?> GetDocument(
        IReadOnlyDictionary<string, object?> fields,
        string name) =>
        fields.TryGetValue(name, out var value) && value is IReadOnlyDictionary<string, object?> document
            ? document
            : throw new InvalidOperationException($"Campo '{name}' ausente no documento.");

    public static IReadOnlyList<IReadOnlyDictionary<string, object?>> GetDocuments(
        IReadOnlyDictionary<string, object?> fields,
        string name) =>
        fields.TryGetValue(name, out var value) && value is IEnumerable<object?> items
            ? items.OfType<IReadOnlyDictionary<string, object?>>().ToList()
            : [];
}
