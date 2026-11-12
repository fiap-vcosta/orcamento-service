using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using Application.Abstractions.Gateways;
using Application.Abstractions.Services;
using BuildingBlocks.Events;
using BuildingBlocks.Messaging;
using Domain.Entities;
using Microsoft.Extensions.DependencyInjection;

namespace IntegrationTests;

[Collection(nameof(IntegrationFixture))]
public class OrcamentoEndpointsTests(CustomWebApplicationFactory factory)
{
    private static readonly TimeSpan PollTimeout = TimeSpan.FromSeconds(5);

    private static string Assinar(string dataId, string requestId, string ts)
    {
        var manifest = $"id:{dataId.ToLowerInvariant()};request-id:{requestId};ts:{ts};";
        var hash = HMACSHA256.HashData(
            Encoding.UTF8.GetBytes(CustomWebApplicationFactory.WebhookSecret),
            Encoding.UTF8.GetBytes(manifest));
        return $"ts={ts},v1={Convert.ToHexString(hash).ToLowerInvariant()}";
    }

    private static readonly Guid OrdemServico501 = Guid.Parse("00000000-0000-0000-0000-000000000501");
    private static readonly Guid OrdemServico502 = Guid.Parse("00000000-0000-0000-0000-000000000502");
    private static readonly Guid OrdemServico503 = Guid.Parse("00000000-0000-0000-0000-000000000503");
    private static readonly Guid OrdemServico504 = Guid.Parse("00000000-0000-0000-0000-000000000504");
    private static readonly Guid OrdemServico505 = Guid.Parse("00000000-0000-0000-0000-000000000505");

    private async Task SemearCobrancaAsync(string cobrancaId, Guid ordemServicoId)
    {
        using var scope = factory.Services.CreateScope();
        var gateway = scope.ServiceProvider.GetRequiredService<ICobrancaGateway>();
        await gateway.SalvarAsync(Cobranca.CriarPendente(cobrancaId, ordemServicoId, $"orc-{ordemServicoId}-1", "key", 100m));
    }

    private IReadOnlyList<PubSubMessage> Publicadas(string topicId) =>
        factory.Services.GetRequiredService<InMemoryPubSubTransport>().GetPublished(topicId);

    private async Task<bool> AguardarPublicacaoAsync(string topicId, int quantidade = 1)
    {
        var limite = DateTime.UtcNow + PollTimeout;
        while (DateTime.UtcNow < limite)
        {
            if (Publicadas(topicId).Count >= quantidade)
            {
                return true;
            }

            await Task.Delay(25);
        }

        return false;
    }

    [Fact]
    public async Task Health_ReturnsOk()
    {
        // Arrange
        var client = factory.CreateClient();

        // Act
        var response = await client.GetAsync("/orcamento/health");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Webhook_SemAssinatura_RetornaOkENaoPublica()
    {
        // Arrange
        var client = factory.CreateClient();
        await SemearCobrancaAsync("ORD_UNSIGNED", OrdemServico501);
        factory.MercadoPago.Orders["ORD_UNSIGNED"] = MercadoPagoOrderSituacao.Aprovada;

        // Act
        var response = await client.PostAsync("/orcamento/webhook/mercado-pago?data.id=ord_unsigned&type=order", null);
        await Task.Delay(300);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.DoesNotContain(
            Publicadas(TopicIds.PagamentoConfirmado),
            m => m.Data.Contains("ORD_UNSIGNED", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Webhook_AssinaturaValidaOrderAprovada_PublicaPagamentoConfirmado()
    {
        // Arrange
        var client = factory.CreateClient();
        await SemearCobrancaAsync("ORD_APPROVED", OrdemServico502);
        factory.MercadoPago.Orders["ORD_APPROVED"] = MercadoPagoOrderSituacao.Aprovada;
        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            "/orcamento/webhook/mercado-pago?data.id=ord_approved&type=order");
        request.Headers.Add("x-request-id", "req-approved");
        request.Headers.Add("x-signature", Assinar("ORD_APPROVED", "req-approved", "1742505638683"));
        request.Content = JsonContent.Create(new { action = "order.processed", data = new { id = "ORD_APPROVED" } });

        // Act
        var response = await client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(await AguardarPublicacaoAsync(TopicIds.PagamentoConfirmado));
        Assert.Contains(
            Publicadas(TopicIds.PagamentoConfirmado),
            m => m.Data.Contains("ORD_APPROVED", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Webhook_AssinaturaInvalida_RetornaOkENaoPublica()
    {
        // Arrange
        var client = factory.CreateClient();
        await SemearCobrancaAsync("ORD_BAD_SIG", OrdemServico503);
        factory.MercadoPago.Orders["ORD_BAD_SIG"] = MercadoPagoOrderSituacao.Aprovada;
        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            "/orcamento/webhook/mercado-pago?data.id=ord_bad_sig&type=order");
        request.Headers.Add("x-request-id", "req-bad");
        request.Headers.Add("x-signature", "ts=1,v1=deadbeef");

        // Act
        var response = await client.SendAsync(request);
        await Task.Delay(300);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.DoesNotContain(
            Publicadas(TopicIds.PagamentoConfirmado),
            m => m.Data.Contains("ORD_BAD_SIG", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Webhook_OrderExpirada_PublicaRecusadoENaoCriaNovaCobranca()
    {
        // Arrange
        var client = factory.CreateClient();
        using (var scope = factory.Services.CreateScope())
        {
            var orcamentos = scope.ServiceProvider.GetRequiredService<IOrcamentoGateway>();
            var ordens = scope.ServiceProvider.GetRequiredService<IOrdemOrcamentoGateway>();
            var orcamento = Orcamento.Criar(
                OrdemServico504,
                1,
                new ClienteOrcamento { Nome = "Ada Lovelace", Email = "ada@oficina.dev", Documento = "86421537090" },
                [new OrcamentoServico { Id = 1, Nome = "Óleo", ValorCobrado = 100m, Status = "Aprovado" }]);
            await orcamentos.SalvarAsync(orcamento);
            var ordem = OrdemOrcamento.Criar(OrdemServico504);
            ordem.RegistrarOrcamento(orcamento.Id, 1);
            await ordens.SalvarAsync(ordem);
        }

        await SemearCobrancaAsync("ORD_EXPIRED", OrdemServico504);
        factory.MercadoPago.Orders["ORD_EXPIRED"] = MercadoPagoOrderSituacao.Expirada;
        var criacoesAntes = factory.MercadoPago.Criacoes.Count;
        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            "/orcamento/webhook/mercado-pago?data.id=ord_expired&type=order");
        request.Headers.Add("x-request-id", "req-exp");
        request.Headers.Add("x-signature", Assinar("ORD_EXPIRED", "req-exp", "1"));
        request.Content = JsonContent.Create(new { action = "order.expired", data = new { id = "ORD_EXPIRED" } });

        // Act
        var response = await client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(await AguardarPublicacaoAsync(TopicIds.PagamentoRecusado));
        Assert.Contains(
            Publicadas(TopicIds.PagamentoRecusado),
            m => m.Data.Contains("ORD_EXPIRED", StringComparison.Ordinal)
                && m.Data.Contains("expirado", StringComparison.Ordinal));
        Assert.Contains("ORD_EXPIRED", factory.MercadoPago.Cancelamentos);
        Assert.Equal(criacoesAntes, factory.MercadoPago.Criacoes.Count);
    }

    [Fact]
    public async Task Staff_GerarNovaCobrancaAposExpiracao_CriaPix()
    {
        // Arrange
        var client = factory.CreateClient();
        AuthHelper.AuthenticateAsStaff(client);
        using (var scope = factory.Services.CreateScope())
        {
            var orcamentos = scope.ServiceProvider.GetRequiredService<IOrcamentoGateway>();
            var ordens = scope.ServiceProvider.GetRequiredService<IOrdemOrcamentoGateway>();
            var cobrancas = scope.ServiceProvider.GetRequiredService<ICobrancaGateway>();
            var orcamento = Orcamento.Criar(
                OrdemServico505,
                1,
                new ClienteOrcamento { Nome = "Ada Lovelace", Email = "ada@oficina.dev", Documento = "86421537090" },
                [new OrcamentoServico { Id = 1, Nome = "Óleo", ValorCobrado = 100m, Status = "Aprovado" }]);
            await orcamentos.SalvarAsync(orcamento);
            var ordem = OrdemOrcamento.Criar(OrdemServico505);
            ordem.RegistrarOrcamento(orcamento.Id, 1);
            await ordens.SalvarAsync(ordem);
            var cobranca = Cobranca.CriarPendente("ORD_EXP_MANUAL", OrdemServico505, orcamento.Id, "key-exp", 100m);
            cobranca.Expirar();
            await cobrancas.SalvarAsync(cobranca);
        }

        // Act
        var response = await client.PostAsync($"/orcamento/ordens/{OrdemServico505}/cobrancas", null);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.True(await AguardarPublicacaoAsync(TopicIds.CobrancaGerada));
        Assert.Contains(factory.MercadoPago.Criacoes, c => c.OrdemServicoId == OrdemServico505);
    }

    [Fact]
    public async Task Staff_GerarNovaCobranca_SemJwt_RetornaUnauthorized()
    {
        // Arrange
        var client = factory.CreateClient();

        // Act
        var response = await client.PostAsync($"/orcamento/ordens/{OrdemServico505}/cobrancas", null);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
