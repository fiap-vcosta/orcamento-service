using Domain.Entities;

namespace Application.Abstractions.Gateways;

public interface IOrcamentoGateway
{
    Task<Orcamento?> GetByIdAsync(string orcamentoId, CancellationToken cancellationToken = default);

    Task SalvarAsync(Orcamento orcamento, CancellationToken cancellationToken = default);
}
