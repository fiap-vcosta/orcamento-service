using Api.Controllers.Staff.Cobranca;
using Api.Presenters.Cobranca;
using Application.UseCases.Cobranca.Commands.GerarNovaCobranca;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace UnitTests.Api.Controllers.Staff.Cobranca;

public class CobrancaControllerTests
{
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
                OrdemServicoId = 7,
                OrcamentoId = "orc-7-1",
                ValorTotal = 120m,
                QrCode = "qr",
                TicketUrl = "ticket",
                Status = "Pendente"
            });
        var controller = new CobrancaController(mediator.Object, new CobrancaPresenter());

        // Act
        var result = await controller.GerarNova(7);

        // Assert
        var created = Assert.IsType<CreatedResult>(result);
        Assert.Equal("/orcamento/ordens/7/cobrancas/ord_new", created.Location);
        mediator.Verify(
            m => m.Send(
                It.Is<GerarNovaCobrancaCommand>(c => c.OrdemServicoId == 7),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
