using Domain.Entities;

namespace Application.Abstractions.Gateways;

public interface ICobrancaGateway
{
    Task<Cobranca?> GetByIdAsync(string cobrancaId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Cobranca>> ListarPorOrdemServicoAsync(
        Guid ordemServicoId,
        CancellationToken cancellationToken = default);

    Task SalvarAsync(Cobranca cobranca, CancellationToken cancellationToken = default);
}
