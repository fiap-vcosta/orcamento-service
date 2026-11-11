using Api.Presenters.Cobranca;
using Application.UseCases.Cobranca.Commands.GerarNovaCobranca;

namespace UnitTests.Api.Presenters.Cobranca;

public class CobrancaPresenterTests
{
    [Fact]
    public void Present_MapeiaCampos()
    {
        // Arrange
        var presenter = new CobrancaPresenter();
        var response = new GerarNovaCobrancaResponse
        {
            CobrancaId = "ord1",
            OrdemServicoId = 9,
            OrcamentoId = "orc-9-1",
            ValorTotal = 50m,
            QrCode = "qr",
            TicketUrl = "ticket",
            Status = "Pendente"
        };

        // Act
        var view = presenter.Present(response);

        // Assert
        Assert.Equal("ord1", view.CobrancaId);
        Assert.Equal(9, view.OrdemServicoId);
        Assert.Equal("orc-9-1", view.OrcamentoId);
        Assert.Equal(50m, view.ValorTotal);
        Assert.Equal("qr", view.QrCode);
        Assert.Equal("ticket", view.TicketUrl);
        Assert.Equal("Pendente", view.Status);
    }
}
