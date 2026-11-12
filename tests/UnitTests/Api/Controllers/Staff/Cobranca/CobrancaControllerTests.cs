using Api.Controllers.Staff.Cobranca;
using Api.Presenters.Cobranca;
using Application.UseCases.Cobranca.Commands.GerarNovaCobranca;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace UnitTests.Api.Controllers.Staff.Cobranca;

public class CobrancaControllerTests
{
    private static readonly Guid OrdemServicoId = Guid.Parse("7b1f3c52-2f0e-4a73-9a3a-5d6c8e2b4f10");

    [Fact]
    public async Task GerarNova_RetornaCreatedComViewModel()
    {
        // Arrange
        var mediator = new Mock<IMediator>();
        mediator
            .Setup(m => m.Send(It.IsAny<GerarNovaCobrancaCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GerarNovaCobrancaResponse
            {
                CobrancaId = "ord_new",
                OrdemServicoId = OrdemServicoId,
                OrcamentoId = $"orc-{OrdemServicoId}-1",
                ValorTotal = 120m,
                QrCode = "qr",
                TicketUrl = "ticket",
                Status = "Pendente"
            });
        var controller = new CobrancaController(mediator.Object, new CobrancaPresenter());

        // Act
        var result = await controller.GerarNova(OrdemServicoId);

        // Assert
        var created = Assert.IsType<CreatedResult>(result);
        Assert.Equal($"/orcamento/ordens/{OrdemServicoId}/cobrancas/ord_new", created.Location);
        mediator.Verify(
            m => m.Send(
                It.Is<GerarNovaCobrancaCommand>(c => c.OrdemServicoId == OrdemServicoId),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
