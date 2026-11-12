namespace Api.ViewModels.Cobranca;

public sealed class CobrancaViewModel
{
    public required string CobrancaId { get; init; }
    public required Guid OrdemServicoId { get; init; }
    public required string OrcamentoId { get; init; }
    public required decimal ValorTotal { get; init; }
    public required string QrCode { get; init; }
    public required string TicketUrl { get; init; }
    public required string Status { get; init; }
}
