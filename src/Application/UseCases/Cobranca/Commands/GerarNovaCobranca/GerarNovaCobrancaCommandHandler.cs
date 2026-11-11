using Application.Services;
using MediatR;

namespace Application.UseCases.Cobranca.Commands.GerarNovaCobranca;

public sealed class GerarNovaCobrancaCommandHandler(OrcamentoSagaService sagaService)
    : IRequestHandler<GerarNovaCobrancaCommand, GerarNovaCobrancaResponse>
{
    public async Task<GerarNovaCobrancaResponse> Handle(
        GerarNovaCobrancaCommand request,
        CancellationToken cancellationToken)
    {
        var cobranca = await sagaService.GerarNovaCobrancaAposExpiracaoAsync(
            request.OrdemServicoId,
            cancellationToken);

        return new GerarNovaCobrancaResponse
        {
            CobrancaId = cobranca.Id,
            OrdemServicoId = cobranca.OrdemServicoId,
            OrcamentoId = cobranca.OrcamentoId,
            ValorTotal = cobranca.ValorTotal,
            QrCode = cobranca.QrCode ?? string.Empty,
            TicketUrl = cobranca.Ticket ?? string.Empty,
            Status = cobranca.Status.ToString()
        };
    }
}
