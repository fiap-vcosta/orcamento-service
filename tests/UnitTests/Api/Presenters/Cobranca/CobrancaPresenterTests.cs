using Api.Presenters.Cobranca;
using Application.UseCases.Cobranca.Commands.GerarNovaCobranca;

namespace UnitTests.Api.Presenters.Cobranca;

public class CobrancaPresenterTests
{
    private static readonly Guid OrdemServicoId = Guid.Parse("9c4d8a10-6b7e-4f2a-8d31-0e5a7b9c1d22");

    [Fact]
    public void Present_MapeiaCampos()
    {
        // Arrange
        var presenter = new CobrancaPresenter();
        var response = new GerarNovaCobrancaResponse
        {
            CobrancaId = "ord1",
            OrdemServicoId = OrdemServicoId,
            OrcamentoId = $"orc-{OrdemServicoId}-1",
            ValorTotal = 50m,
            QrCode = "qr",
            TicketUrl = "ticket",
            Status = "Pendente"
        };

        // Act
        var view = presenter.Present(response);

        // Assert
        Assert.Equal("ord1", view.CobrancaId);
        Assert.Equal(OrdemServicoId, view.OrdemServicoId);
        Assert.Equal($"orc-{OrdemServicoId}-1", view.OrcamentoId);
        Assert.Equal(50m, view.ValorTotal);
        Assert.Equal("qr", view.QrCode);
        Assert.Equal("ticket", view.TicketUrl);
        Assert.Equal("Pendente", view.Status);
    }
}
