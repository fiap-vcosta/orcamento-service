using System.Net;
using System.Text.Json;
using Application.Abstractions.Services;
using Domain.Entities;
using Infrastructure.MercadoPago;
using Microsoft.Extensions.Options;

namespace UnitTests.Infrastructure.MercadoPago;

public class MercadoPagoOrdersClientTests
{
    private const string OrderCriada = """
        {
          "id": "ORD01",
          "status": "action_required",
          "transactions": {
            "payments": [
              {
                "id": "PAY01",
                "payment_method": { "id": "pix", "qr_code": "000201qr", "ticket_url": "https://ticket" }
              }
            ]
          }
        }
        """;

    private sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> responder) : HttpMessageHandler
    {
        public List<(HttpMethod Method, string? Path, string? Body, HttpRequestMessage Request)> Calls { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var body = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
            Calls.Add((request.Method, request.RequestUri?.AbsolutePath, body, request));
            return responder(request);
        }
    }

    private static HttpResponseMessage Json(HttpStatusCode status, string json) =>
        new(status) { Content = new StringContent(json) };

    private static (MercadoPagoOrdersClient Client, StubHandler Handler) Criar(
        Func<HttpRequestMessage, HttpResponseMessage> responder)
    {
        var handler = new StubHandler(responder);
        var http = new HttpClient(handler) { BaseAddress = new Uri("https://api.mercadopago.test/") };
        var client = new MercadoPagoOrdersClient(http, Options.Create(new MercadoPagoOptions()));
        return (client, handler);
    }

    private static CriarOrderPixRequest Request(string documento = "864.215.370-90") =>
        new(
            "key-123",
            10,
            150m,
            new ClienteOrcamento { Nome = "Ada Lovelace", Email = "ada@oficina.dev", Documento = documento });

    [Fact]
    public async Task CriarOrderPix_EnviaPayloadEsperadoEMapeiaResposta()
    {
        // Arrange
        var (client, handler) = Criar(_ => Json(HttpStatusCode.Created, OrderCriada));

        // Act
        var order = await client.CriarOrderPixAsync(Request());

        // Assert
        var call = Assert.Single(handler.Calls);
        Assert.Equal(HttpMethod.Post, call.Method);
        Assert.Equal("/v1/orders", call.Path);
        Assert.Equal("key-123", call.Request.Headers.GetValues("X-Idempotency-Key").Single());
        using var body = JsonDocument.Parse(call.Body!);
        var root = body.RootElement;
        Assert.Equal("online", root.GetProperty("type").GetString());
        Assert.Equal("150.00", root.GetProperty("total_amount").GetString());
        Assert.Equal("10", root.GetProperty("external_reference").GetString());
        var payment = root.GetProperty("transactions").GetProperty("payments")[0];
        Assert.Equal("P1D", payment.GetProperty("expiration_time").GetString());
        Assert.Equal("pix", payment.GetProperty("payment_method").GetProperty("id").GetString());
        var payer = root.GetProperty("payer");
        Assert.Equal("Ada Lovelace", payer.GetProperty("first_name").GetString());
        Assert.Equal("ada@oficina.dev", payer.GetProperty("email").GetString());
        Assert.Equal("CPF", payer.GetProperty("identification").GetProperty("type").GetString());
        Assert.Equal("86421537090", payer.GetProperty("identification").GetProperty("number").GetString());
        Assert.Equal("ORD01", order.Id);
        Assert.Equal(MercadoPagoOrderSituacao.Pendente, order.Situacao);
        Assert.Equal("000201qr", order.QrCode);
        Assert.Equal("https://ticket", order.Ticket);
    }

    [Fact]
    public async Task CriarOrderPix_DocumentoComQuatorzeDigitos_UsaCnpj()
    {
        // Arrange
        var (client, handler) = Criar(_ => Json(HttpStatusCode.Created, OrderCriada));

        // Act
        await client.CriarOrderPixAsync(Request("11.222.333/0001-81"));

        // Assert
        using var body = JsonDocument.Parse(handler.Calls.Single().Body!);
        var identification = body.RootElement.GetProperty("payer").GetProperty("identification");
        Assert.Equal("CNPJ", identification.GetProperty("type").GetString());
        Assert.Equal("11222333000181", identification.GetProperty("number").GetString());
    }

    [Fact]
    public async Task CriarOrderPix_SemQrCode_Lanca()
    {
        // Arrange
        var (client, _) = Criar(_ => Json(HttpStatusCode.Created, """{ "id": "ORD01", "status": "created" }"""));

        // Act
        var act = () => client.CriarOrderPixAsync(Request());

        // Assert
        await Assert.ThrowsAsync<InvalidOperationException>(act);
    }

    [Fact]
    public async Task CriarOrderPix_RespostaDeErro_LancaHttpRequestException()
    {
        // Arrange
        var (client, _) = Criar(_ => Json(HttpStatusCode.BadRequest, """{ "errors": [] }"""));

        // Act
        var act = () => client.CriarOrderPixAsync(Request());

        // Assert
        var ex = await Assert.ThrowsAsync<HttpRequestException>(act);
        Assert.Equal(HttpStatusCode.BadRequest, ex.StatusCode);
    }

    [Fact]
    public async Task CriarOrderPix_RespostaSemId_Lanca()
    {
        // Arrange
        var (client, _) = Criar(_ => Json(HttpStatusCode.Created, """{ "id": null }"""));

        // Act
        var act = () => client.CriarOrderPixAsync(Request());

        // Assert
        await Assert.ThrowsAsync<InvalidOperationException>(act);
    }

    [Fact]
    public async Task ObterOrder_Processada_MapeiaComoAprovada()
    {
        // Arrange
        var (client, handler) = Criar(_ => Json(HttpStatusCode.OK, """{ "id": "ORD01", "status": "processed" }"""));

        // Act
        var order = await client.ObterOrderAsync("ORD01");

        // Assert
        Assert.NotNull(order);
        Assert.Equal(MercadoPagoOrderSituacao.Aprovada, order.Situacao);
        Assert.Null(order.QrCode);
        Assert.Equal("/v1/orders/ORD01", handler.Calls.Single().Path);
    }

    [Fact]
    public async Task ObterOrder_PagamentosVazios_NaoLevantaQrCode()
    {
        // Arrange
        var (client, _) = Criar(_ => Json(
            HttpStatusCode.OK,
            """{ "id": "ORD01", "status": "expired", "transactions": { "payments": [] } }"""));

        // Act
        var order = await client.ObterOrderAsync("ORD01");

        // Assert
        Assert.NotNull(order);
        Assert.Equal(MercadoPagoOrderSituacao.Expirada, order.Situacao);
        Assert.Null(order.QrCode);
    }

    [Fact]
    public async Task ObterOrder_PaymentMethodSemCamposTexto_RetornaNulos()
    {
        // Arrange
        var (client, _) = Criar(_ => Json(
            HttpStatusCode.OK,
            """{ "id": "ORD01", "transactions": { "payments": [ { "payment_method": { "qr_code": 1 } } ] } }"""));

        // Act
        var order = await client.ObterOrderAsync("ORD01");

        // Assert
        Assert.NotNull(order);
        Assert.Equal(MercadoPagoOrderSituacao.Pendente, order.Situacao);
        Assert.Null(order.QrCode);
        Assert.Null(order.Ticket);
    }

    [Fact]
    public async Task ObterOrder_NaoEncontrada_RetornaNulo()
    {
        // Arrange
        var (client, _) = Criar(_ => Json(HttpStatusCode.NotFound, "{}"));

        // Act
        var order = await client.ObterOrderAsync("ORD404");

        // Assert
        Assert.Null(order);
    }

    [Fact]
    public async Task ObterOrder_ErroDoServidor_Lanca()
    {
        // Arrange
        var (client, _) = Criar(_ => Json(HttpStatusCode.InternalServerError, "{}"));

        // Act
        var act = () => client.ObterOrderAsync("ORD01");

        // Assert
        await Assert.ThrowsAsync<HttpRequestException>(act);
    }

    [Fact]
    public async Task CancelarOrder_Sucesso_PostaNoEndpointDeCancelamento()
    {
        // Arrange
        var (client, handler) = Criar(_ => Json(HttpStatusCode.OK, "{}"));

        // Act
        await client.CancelarOrderAsync("ORD01");

        // Assert
        var call = Assert.Single(handler.Calls);
        Assert.Equal(HttpMethod.Post, call.Method);
        Assert.Equal("/v1/orders/ORD01/cancel", call.Path);
        Assert.True(call.Request.Headers.Contains("X-Idempotency-Key"));
    }

    [Fact]
    public async Task CancelarOrder_Conflito_Ignora()
    {
        // Arrange
        var (client, _) = Criar(_ => Json(HttpStatusCode.Conflict, "{}"));

        // Act
        var act = () => client.CancelarOrderAsync("ORD01");

        // Assert
        await act();
    }

    [Fact]
    public async Task CancelarOrder_ErroDoServidor_Lanca()
    {
        // Arrange
        var (client, _) = Criar(_ => Json(HttpStatusCode.InternalServerError, "{}"));

        // Act
        var act = () => client.CancelarOrderAsync("ORD01");

        // Assert
        await Assert.ThrowsAsync<HttpRequestException>(act);
    }

    [Theory]
    [InlineData("processed", null, MercadoPagoOrderSituacao.Aprovada)]
    [InlineData("PAID", null, MercadoPagoOrderSituacao.Aprovada)]
    [InlineData("approved", null, MercadoPagoOrderSituacao.Aprovada)]
    [InlineData("expired", null, MercadoPagoOrderSituacao.Expirada)]
    [InlineData("canceled", "expired", MercadoPagoOrderSituacao.Expirada)]
    [InlineData("cancelled", "expired", MercadoPagoOrderSituacao.Expirada)]
    [InlineData("failed", null, MercadoPagoOrderSituacao.Recusada)]
    [InlineData("canceled", null, MercadoPagoOrderSituacao.Recusada)]
    [InlineData("rejected", null, MercadoPagoOrderSituacao.Recusada)]
    [InlineData("action_required", null, MercadoPagoOrderSituacao.Pendente)]
    [InlineData("created", null, MercadoPagoOrderSituacao.Pendente)]
    [InlineData(null, null, MercadoPagoOrderSituacao.Pendente)]
    public void MapearSituacao_TraduzStatusDoMercadoPago(
        string? status,
        string? statusDetail,
        MercadoPagoOrderSituacao esperado)
    {
        // Arrange & Act
        var situacao = MercadoPagoOrdersClient.MapearSituacao(status, statusDetail);

        // Assert
        Assert.Equal(esperado, situacao);
    }

    [Fact]
    public async Task ObterOrder_CanceledComStatusDetailExpired_MapeiaExpirada()
    {
        // Arrange
        var (client, _) = Criar(_ => Json(
            HttpStatusCode.OK,
            """{ "id": "ORD01", "status": "canceled", "status_detail": "expired" }"""));

        // Act
        var order = await client.ObterOrderAsync("ORD01");

        // Assert
        Assert.NotNull(order);
        Assert.Equal(MercadoPagoOrderSituacao.Expirada, order.Situacao);
    }
}
