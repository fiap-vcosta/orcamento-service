using MediatR;

namespace Application.UseCases.Cobranca.Commands.GerarNovaCobranca;

public sealed class GerarNovaCobrancaCommand : IRequest<GerarNovaCobrancaResponse>
{
    public required int OrdemServicoId { get; init; }
}
