using Application.Abstractions.Gateways;
using Domain.Entities;

namespace Infrastructure.Firestore;

public sealed class FirestoreOrcamentoGateway(IDocumentRecordStore recordStore) : IOrcamentoGateway
{
    public async Task<Orcamento?> GetByIdAsync(string orcamentoId, CancellationToken cancellationToken = default)
    {
        var fields = await recordStore.GetAsync(FirestoreCollections.Orcamentos, orcamentoId, cancellationToken);
        return fields is null ? null : ToEntity(fields);
    }

    public Task SalvarAsync(Orcamento orcamento, CancellationToken cancellationToken = default) =>
        recordStore.SetAsync(FirestoreCollections.Orcamentos, orcamento.Id, ToFields(orcamento), cancellationToken);

    private static Dictionary<string, object?> ToFields(Orcamento orcamento) =>
        new()
        {
            ["id"] = orcamento.Id,
            ["ordemServicoId"] = orcamento.OrdemServicoId.ToString(),
            ["versaoDiagnostico"] = orcamento.VersaoDiagnostico,
            ["cliente"] = new Dictionary<string, object?>
            {
                ["nome"] = orcamento.Cliente.Nome,
                ["email"] = orcamento.Cliente.Email,
                ["documento"] = orcamento.Cliente.Documento
            },
            ["servicos"] = orcamento.Servicos
                .Select(s => (object?)new Dictionary<string, object?>
                {
                    ["id"] = s.Id,
                    ["nome"] = s.Nome,
                    ["valorCobrado"] = DocumentFields.FormatDecimal(s.ValorCobrado),
                    ["status"] = s.Status
                })
                .ToList(),
            ["valorTotal"] = DocumentFields.FormatDecimal(orcamento.ValorTotal),
            ["status"] = orcamento.Status.ToString(),
            ["criadoEm"] = DocumentFields.FormatDate(orcamento.CriadoEm)
        };

    private static Orcamento ToEntity(IReadOnlyDictionary<string, object?> fields)
    {
        var cliente = DocumentFields.GetDocument(fields, "cliente");
        var orcamento = new Orcamento
        {
            Id = DocumentFields.GetString(fields, "id"),
            OrdemServicoId = DocumentFields.GetGuid(fields, "ordemServicoId"),
            VersaoDiagnostico = DocumentFields.GetInt(fields, "versaoDiagnostico"),
            Cliente = new ClienteOrcamento
            {
                Nome = DocumentFields.GetString(cliente, "nome"),
                Email = DocumentFields.GetString(cliente, "email"),
                Documento = DocumentFields.GetString(cliente, "documento")
            },
            Servicos = DocumentFields.GetDocuments(fields, "servicos")
                .Select(s => new OrcamentoServico
                {
                    Id = DocumentFields.GetInt(s, "id"),
                    Nome = DocumentFields.GetString(s, "nome"),
                    ValorCobrado = DocumentFields.GetDecimal(s, "valorCobrado"),
                    Status = DocumentFields.GetString(s, "status")
                })
                .ToList(),
            ValorTotal = DocumentFields.GetDecimal(fields, "valorTotal"),
            CriadoEm = DocumentFields.GetDate(fields, "criadoEm")
        };

        if (DocumentFields.GetEnum<OrcamentoStatus>(fields, "status") == OrcamentoStatus.Cancelado)
        {
            orcamento.Cancelar();
        }

        return orcamento;
    }
}
