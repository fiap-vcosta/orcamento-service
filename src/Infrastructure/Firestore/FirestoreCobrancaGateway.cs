using Application.Abstractions.Gateways;
using Domain.Entities;

namespace Infrastructure.Firestore;

public sealed class FirestoreCobrancaGateway(IDocumentRecordStore recordStore) : ICobrancaGateway
{
    public async Task<Cobranca?> GetByIdAsync(string cobrancaId, CancellationToken cancellationToken = default)
    {
        var fields = await recordStore.GetAsync(FirestoreCollections.Cobrancas, cobrancaId, cancellationToken);
        return fields is null ? null : ToEntity(fields);
    }

    public async Task<IReadOnlyList<Cobranca>> ListarPorOrdemServicoAsync(
        Guid ordemServicoId,
        CancellationToken cancellationToken = default)
    {
        var documentos = await recordStore.QueryEqualAsync(
            FirestoreCollections.Cobrancas,
            "ordemServicoId",
            ordemServicoId.ToString(),
            cancellationToken);
        return documentos.Select(ToEntity).ToList();
    }

    public Task SalvarAsync(Cobranca cobranca, CancellationToken cancellationToken = default) =>
        recordStore.SetAsync(
            FirestoreCollections.Cobrancas,
            cobranca.Id,
            new Dictionary<string, object?>
            {
                ["id"] = cobranca.Id,
                ["ordemServicoId"] = cobranca.OrdemServicoId.ToString(),
                ["orcamentoId"] = cobranca.OrcamentoId,
                ["idempotencyKey"] = cobranca.IdempotencyKey,
                ["valorTotal"] = DocumentFields.FormatDecimal(cobranca.ValorTotal),
                ["qrCode"] = cobranca.QrCode,
                ["ticket"] = cobranca.Ticket,
                ["status"] = cobranca.Status.ToString(),
                ["criadaEm"] = DocumentFields.FormatDate(cobranca.CriadaEm)
            },
            cancellationToken);

    private static Cobranca ToEntity(IReadOnlyDictionary<string, object?> fields)
    {
        var cobranca = new Cobranca
        {
            Id = DocumentFields.GetString(fields, "id"),
            OrdemServicoId = DocumentFields.GetGuid(fields, "ordemServicoId"),
            OrcamentoId = DocumentFields.GetString(fields, "orcamentoId"),
            IdempotencyKey = DocumentFields.GetString(fields, "idempotencyKey"),
            ValorTotal = DocumentFields.GetDecimal(fields, "valorTotal"),
            CriadaEm = DocumentFields.GetDate(fields, "criadaEm")
        };

        var qrCode = DocumentFields.GetNullableString(fields, "qrCode");
        var ticket = DocumentFields.GetNullableString(fields, "ticket");
        if (qrCode is not null && ticket is not null)
        {
            cobranca.RegistrarPix(qrCode, ticket);
        }

        switch (DocumentFields.GetEnum<CobrancaStatus>(fields, "status"))
        {
            case CobrancaStatus.Confirmada:
                cobranca.Confirmar();
                break;
            case CobrancaStatus.Recusada:
                cobranca.Recusar();
                break;
            case CobrancaStatus.Expirada:
                cobranca.Expirar();
                break;
            case CobrancaStatus.Cancelada:
                cobranca.Cancelar();
                break;
        }

        return cobranca;
    }
}
