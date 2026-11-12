namespace Domain.Entities;

public sealed class Cobranca
{
    public required string Id { get; init; }

    public required Guid OrdemServicoId { get; init; }

    public required string OrcamentoId { get; init; }

    public required string IdempotencyKey { get; init; }

    public required decimal ValorTotal { get; init; }

    public string? QrCode { get; private set; }

    public string? Ticket { get; private set; }

    public CobrancaStatus Status { get; private set; } = CobrancaStatus.Pendente;

    public DateTimeOffset CriadaEm { get; init; } = DateTimeOffset.UtcNow;

    public static Cobranca CriarPendente(
        string cobrancaId,
        Guid ordemServicoId,
        string orcamentoId,
        string idempotencyKey,
        decimal valorTotal) =>
        new()
        {
            Id = cobrancaId,
            OrdemServicoId = ordemServicoId,
            OrcamentoId = orcamentoId,
            IdempotencyKey = idempotencyKey,
            ValorTotal = valorTotal,
            Status = CobrancaStatus.Pendente,
            CriadaEm = DateTimeOffset.UtcNow
        };

    public void RegistrarPix(string qrCode, string ticket)
    {
        QrCode = qrCode;
        Ticket = ticket;
    }

    public void Confirmar() => Status = CobrancaStatus.Confirmada;

    public void Recusar() => Status = CobrancaStatus.Recusada;

    public void Expirar() => Status = CobrancaStatus.Expirada;

    public void Cancelar() => Status = CobrancaStatus.Cancelada;
}
