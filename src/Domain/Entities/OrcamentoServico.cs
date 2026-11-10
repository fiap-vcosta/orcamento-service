namespace Domain.Entities;

public sealed class OrcamentoServico
{
    public required int Id { get; init; }

    public required string Nome { get; init; }

    public required decimal ValorCobrado { get; init; }

    public required string Status { get; init; }
}
