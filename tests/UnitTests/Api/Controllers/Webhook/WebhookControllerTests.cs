using System.Text;
using Api.Controllers.Webhook;
using Api.Services;
using Application.UseCases.Webhook.Commands.ProcessarWebhookMercadoPago;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace UnitTests.Api.Controllers.Webhook;

public class WebhookControllerTests
{
    private readonly Mock<IWebhookDispatcher> _dispatcher = new();

    public WebhookControllerTests()
    {
        _dispatcher
            .Setup(d => d.Dispatch(It.IsAny<ProcessarWebhookMercadoPagoCommand>()))
            .Returns(Task.CompletedTask);
    }

    private WebhookController CriarController(string? body, string? query)
    {
        var context = new DefaultHttpContext();
        context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(body ?? string.Empty));
        if (query is not null)
        {
            context.Request.QueryString = new QueryString(query);
        }

        return new WebhookController(_dispatcher.Object)
        {
            ControllerContext = new ControllerContext { HttpContext = context }
        };
    }

    [Fact]
    public async Task MercadoPago_DataIdNaQuery_RetornaOkEDespacha()
    {
        // Arrange
        var controller = CriarController(null, "?data.id=ord01&type=order");

        // Act
        var result = await controller.MercadoPago("ts=1,v1=abc", "req-1", "ord01", CancellationToken.None);

        // Assert
        Assert.IsType<OkResult>(result);
        _dispatcher.Verify(
            d => d.Dispatch(new ProcessarWebhookMercadoPagoCommand("ord01", "ts=1,v1=abc", "req-1")),
            Times.Once);
    }

    [Fact]
    public async Task MercadoPago_DataIdNoCorpo_TemPrioridadeSobreAQuery()
    {
        // Arrange
        var controller = CriarController("""{ "action": "order.processed", "data": { "id": "ORD01" } }""", "?data.id=ord01");

        // Act
        var result = await controller.MercadoPago("ts=1,v1=abc", "req-1", "ord01", CancellationToken.None);

        // Assert
        Assert.IsType<OkResult>(result);
        _dispatcher.Verify(
            d => d.Dispatch(It.Is<ProcessarWebhookMercadoPagoCommand>(c => c.DataId == "ORD01")),
            Times.Once);
    }

    [Fact]
    public async Task MercadoPago_DataIdNumericoNoCorpo_ConverteParaTexto()
    {
        // Arrange
        var controller = CriarController("""{ "data": { "id": 123 } }""", null);

        // Act
        await controller.MercadoPago("ts=1,v1=abc", "req-1", null, CancellationToken.None);

        // Assert
        _dispatcher.Verify(
            d => d.Dispatch(It.Is<ProcessarWebhookMercadoPagoCommand>(c => c.DataId == "123")),
            Times.Once);
    }

    [Theory]
    [InlineData("não é json")]
    [InlineData("[]")]
    [InlineData("""{ "data": "x" }""")]
    [InlineData("""{ "data": { } }""")]
    [InlineData("")]
    public async Task MercadoPago_SemDataId_RetornaOkSemDespachar(string body)
    {
        // Arrange
        var controller = CriarController(body, null);

        // Act
        var result = await controller.MercadoPago(null, null, null, CancellationToken.None);

        // Assert
        Assert.IsType<OkResult>(result);
        _dispatcher.Verify(d => d.Dispatch(It.IsAny<ProcessarWebhookMercadoPagoCommand>()), Times.Never);
    }

    [Fact]
    public async Task MercadoPago_CorpoInvalidoComDataIdNaQuery_UsaQuery()
    {
        // Arrange
        var controller = CriarController("lixo", "?data.id=ord9");

        // Act
        await controller.MercadoPago(null, null, "ord9", CancellationToken.None);

        // Assert
        _dispatcher.Verify(
            d => d.Dispatch(new ProcessarWebhookMercadoPagoCommand("ord9", null, null)),
            Times.Once);
    }
}
