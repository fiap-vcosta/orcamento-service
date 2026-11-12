using Domain.Entities;

namespace Application.Abstractions.Services;

public interface IOrcamentoIntegrationPublisher
{
    Task PublicarOrcamentoGeradoAsync(Orcamento orcamento, CancellationToken cancellationToken = default);

    Task PublicarOrcamentoFalhouAsync(
        Guid ordemServicoId,
        int versaoDiagnostico,
        string motivo,
        CancellationToken cancellationToken = default);

    Task PublicarCobrancaGeradaAsync(Cobranca cobranca, CancellationToken cancellationToken = default);

    Task PublicarPagamentoConfirmadoAsync(Cobranca cobranca, CancellationToken cancellationToken = default);

    Task PublicarPagamentoRecusadoAsync(
        Cobranca cobranca,
        string motivo,
        CancellationToken cancellationToken = default);
}
