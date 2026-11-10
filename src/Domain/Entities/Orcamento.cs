namespace Domain.Entities;

public sealed class Orcamento
{
    public required string Id { get; init; }

    public required int OrdemServicoId { get; init; }

    public required int VersaoDiagnostico { get; init; }

    public required ClienteOrcamento Cliente { get; init; }

    public required IReadOnlyList<OrcamentoServico> Servicos { get; init; }

    public required decimal ValorTotal { get; init; }

    public OrcamentoStatus Status { get; private set; } = OrcamentoStatus.Ativo;

    public DateTimeOffset CriadoEm { get; init; } = DateTimeOffset.UtcNow;

    public static string CriarId(int ordemServicoId, int versaoDiagnostico) =>
        $"orc-{ordemServicoId}-{versaoDiagnostico}";

    public static Orcamento Criar(
        int ordemServicoId,
        int versaoDiagnostico,
        ClienteOrcamento cliente,
        IReadOnlyList<OrcamentoServico> servicos)
    {
        var cobraveis = servicos
            .Where(s => !string.Equals(s.Status, "Rejeitado", StringComparison.Ordinal))
            .ToList();

        return new Orcamento
        {
            Id = CriarId(ordemServicoId, versaoDiagnostico),
            OrdemServicoId = ordemServicoId,
            VersaoDiagnostico = versaoDiagnostico,
            Cliente = cliente,
            Servicos = cobraveis,
            ValorTotal = cobraveis.Sum(s => s.ValorCobrado),
            Status = OrcamentoStatus.Ativo,
            CriadoEm = DateTimeOffset.UtcNow
        };
    }

    public void Cancelar() => Status = OrcamentoStatus.Cancelado;
}
