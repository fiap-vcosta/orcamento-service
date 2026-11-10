namespace Domain.Entities;

public sealed class EventoProcessado
{
    public required Guid EventId { get; init; }

    public required string Tipo { get; init; }

    public required string ChaveNegocio { get; init; }

    public DateTimeOffset ProcessadoEm { get; init; } = DateTimeOffset.UtcNow;
}
