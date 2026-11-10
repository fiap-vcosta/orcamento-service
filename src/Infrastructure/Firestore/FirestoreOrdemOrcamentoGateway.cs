using Application.Abstractions.Gateways;
using Domain.Entities;

namespace Infrastructure.Firestore;

public sealed class FirestoreOrdemOrcamentoGateway(IDocumentRecordStore recordStore) : IOrdemOrcamentoGateway
{
    public async Task<OrdemOrcamento?> GetByOrdemServicoIdAsync(
        int ordemServicoId,
        CancellationToken cancellationToken = default)
    {
        var fields = await recordStore.GetAsync(FirestoreCollections.Ordens, DocumentId(ordemServicoId), cancellationToken);
        return fields is null ? null : ToEntity(fields);
    }

    public Task SalvarAsync(OrdemOrcamento ordem, CancellationToken cancellationToken = default) =>
        recordStore.SetAsync(
            FirestoreCollections.Ordens,
            DocumentId(ordem.OrdemServicoId),
            new Dictionary<string, object?>
            {
                ["ordemServicoId"] = ordem.OrdemServicoId,
                ["versaoVigente"] = ordem.VersaoVigente,
                ["orcamentoIdVigente"] = ordem.OrcamentoIdVigente,
                ["descartada"] = ordem.Descartada
            },
            cancellationToken);

    private static string DocumentId(int ordemServicoId) =>
        ordemServicoId.ToString(System.Globalization.CultureInfo.InvariantCulture);

    private static OrdemOrcamento ToEntity(IReadOnlyDictionary<string, object?> fields)
    {
        var ordem = OrdemOrcamento.Criar(DocumentFields.GetInt(fields, "ordemServicoId"));

        var versao = DocumentFields.GetNullableInt(fields, "versaoVigente");
        var orcamentoId = DocumentFields.GetNullableString(fields, "orcamentoIdVigente");
        if (versao is not null && orcamentoId is not null)
        {
            ordem.RegistrarOrcamento(orcamentoId, versao.Value);
        }

        if (DocumentFields.GetBool(fields, "descartada"))
        {
            ordem.MarcarDescartada();
        }

        return ordem;
    }
}
