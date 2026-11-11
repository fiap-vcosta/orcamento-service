using Api.ViewModels.Cobranca;
using Application.UseCases.Cobranca.Commands.GerarNovaCobranca;

namespace Api.Presenters.Cobranca;

public sealed class CobrancaPresenter
{
    public CobrancaViewModel Present(GerarNovaCobrancaResponse response) => new()
    {
        CobrancaId = response.CobrancaId,
        OrdemServicoId = response.OrdemServicoId,
        OrcamentoId = response.OrcamentoId,
        ValorTotal = response.ValorTotal,
        QrCode = response.QrCode,
        TicketUrl = response.TicketUrl,
        Status = response.Status
    };
}
