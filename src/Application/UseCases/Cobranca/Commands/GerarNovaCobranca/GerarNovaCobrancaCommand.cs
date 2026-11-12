using MediatR;

namespace Application.UseCases.Cobranca.Commands.GerarNovaCobranca;

public sealed class GerarNovaCobrancaCommand : IRequest<GerarNovaCobrancaResponse>
{
    public required Guid OrdemServicoId { get; init; }
}
