namespace Infrastructure.Firestore;

public sealed class FirestoreOptions
{
    public const string SectionName = "Firestore";

    public string ProjectId { get; set; } = string.Empty;
    public string DatabaseId { get; set; } = "orcamento";
}
