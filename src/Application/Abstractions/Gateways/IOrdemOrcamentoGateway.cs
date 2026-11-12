using Domain.Entities;

namespace Application.Abstractions.Gateways;

public interface IOrdemOrcamentoGateway
{
    Task<OrdemOrcamento?> GetByOrdemServicoIdAsync(Guid ordemServicoId, CancellationToken cancellationToken = default);

    Task SalvarAsync(OrdemOrcamento ordem, CancellationToken cancellationToken = default);
}
