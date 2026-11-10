namespace Domain.Entities;

public sealed class ClienteOrcamento
{
    public required string Nome { get; init; }

    public required string Email { get; init; }

    public required string Documento { get; init; }
}
