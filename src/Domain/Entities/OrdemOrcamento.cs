namespace Domain.Entities;

public sealed class OrdemOrcamento
{
    public required Guid OrdemServicoId { get; init; }

    public int? VersaoVigente { get; private set; }

    public string? OrcamentoIdVigente { get; private set; }

    public bool Descartada { get; private set; }

    public static OrdemOrcamento Criar(Guid ordemServicoId) =>
        new() { OrdemServicoId = ordemServicoId };

    public void RegistrarOrcamento(string orcamentoId, int versaoDiagnostico)
    {
        VersaoVigente = versaoDiagnostico;
        OrcamentoIdVigente = orcamentoId;
    }

    public void MarcarDescartada() => Descartada = true;
}
