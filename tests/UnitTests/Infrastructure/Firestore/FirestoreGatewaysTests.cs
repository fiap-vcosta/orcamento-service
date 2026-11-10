using Domain.Entities;
using Infrastructure.Firestore;
using UnitTests.Fakes;

namespace UnitTests.Infrastructure.Firestore;

public class FirestoreGatewaysTests
{
    private readonly InMemoryDocumentRecordStore _store = new();

    [Fact]
    public async Task Orcamento_SalvarELer_PreservaTodosOsCampos()
    {
        // Arrange
        var gateway = new FirestoreOrcamentoGateway(_store);
        var orcamento = Orcamento.Criar(
            10,
            2,
            new ClienteOrcamento { Nome = "Ada", Email = "ada@oficina.dev", Documento = "86421537090" },
            [
                new OrcamentoServico { Id = 3, Nome = "Óleo", ValorCobrado = 150.55m, Status = "Sugerido" },
                new OrcamentoServico { Id = 4, Nome = "Freio", ValorCobrado = 99m, Status = "Rejeitado" }
            ]);

        // Act
        await gateway.SalvarAsync(orcamento);
        var lido = await gateway.GetByIdAsync("orc-10-2");

        // Assert
        Assert.NotNull(lido);
        Assert.Equal(orcamento.Id, lido.Id);
        Assert.Equal(10, lido.OrdemServicoId);
        Assert.Equal(2, lido.VersaoDiagnostico);
        Assert.Equal("Ada", lido.Cliente.Nome);
        Assert.Equal("ada@oficina.dev", lido.Cliente.Email);
        Assert.Equal("86421537090", lido.Cliente.Documento);
        Assert.Equal(150.55m, lido.ValorTotal);
        var servico = Assert.Single(lido.Servicos);
        Assert.Equal(3, servico.Id);
        Assert.Equal(150.55m, servico.ValorCobrado);
        Assert.Equal(OrcamentoStatus.Ativo, lido.Status);
        Assert.Equal(orcamento.CriadoEm, lido.CriadoEm);
    }

    [Fact]
    public async Task Orcamento_Cancelado_PreservaStatus()
    {
        // Arrange
        var gateway = new FirestoreOrcamentoGateway(_store);
        var orcamento = Orcamento.Criar(
            1,
            1,
            new ClienteOrcamento { Nome = "Ada", Email = "a@b.c", Documento = "1" },
            []);
        orcamento.Cancelar();
        await gateway.SalvarAsync(orcamento);

        // Act
        var lido = await gateway.GetByIdAsync(orcamento.Id);

        // Assert
        Assert.Equal(OrcamentoStatus.Cancelado, lido!.Status);
        Assert.Empty(lido.Servicos);
    }

    [Fact]
    public async Task Orcamento_Inexistente_RetornaNulo()
    {
        // Arrange
        var gateway = new FirestoreOrcamentoGateway(_store);

        // Act
        var lido = await gateway.GetByIdAsync("orc-0-0");

        // Assert
        Assert.Null(lido);
    }

    [Fact]
    public async Task Orcamento_ConverteInteirosVindosComoLong()
    {
        // Arrange
        var gateway = new FirestoreOrcamentoGateway(_store);
        await _store.SetAsync(
            FirestoreCollections.Orcamentos,
            "orc-5-1",
            new Dictionary<string, object?>
            {
                ["id"] = "orc-5-1",
                ["ordemServicoId"] = 5L,
                ["versaoDiagnostico"] = 1L,
                ["cliente"] = new Dictionary<string, object> { ["nome"] = "A", ["email"] = "e", ["documento"] = "d" },
                ["servicos"] = new List<object>
                {
                    new Dictionary<string, object>
                    {
                        ["id"] = 9L,
                        ["nome"] = "S",
                        ["valorCobrado"] = "10",
                        ["status"] = "Aprovado"
                    }
                },
                ["valorTotal"] = "10",
                ["status"] = "Ativo",
                ["criadoEm"] = DocumentFields.FormatDate(DateTimeOffset.UtcNow)
            });

        // Act
        var lido = await gateway.GetByIdAsync("orc-5-1");

        // Assert
        Assert.Equal(5, lido!.OrdemServicoId);
        Assert.Equal(9, lido.Servicos.Single().Id);
    }

    [Fact]
    public async Task Ordem_SalvarELer_PreservaPonteiroEDescarte()
    {
        // Arrange
        var gateway = new FirestoreOrdemOrcamentoGateway(_store);
        var ordem = OrdemOrcamento.Criar(10);
        ordem.RegistrarOrcamento("orc-10-3", 3);
        ordem.MarcarDescartada();

        // Act
        await gateway.SalvarAsync(ordem);
        var lida = await gateway.GetByOrdemServicoIdAsync(10);

        // Assert
        Assert.NotNull(lida);
        Assert.Equal(3, lida.VersaoVigente);
        Assert.Equal("orc-10-3", lida.OrcamentoIdVigente);
        Assert.True(lida.Descartada);
    }

    [Fact]
    public async Task Ordem_SemOrcamento_LeComPonteiroNulo()
    {
        // Arrange
        var gateway = new FirestoreOrdemOrcamentoGateway(_store);
        await gateway.SalvarAsync(OrdemOrcamento.Criar(11));

        // Act
        var lida = await gateway.GetByOrdemServicoIdAsync(11);

        // Assert
        Assert.NotNull(lida);
        Assert.Null(lida.VersaoVigente);
        Assert.Null(lida.OrcamentoIdVigente);
        Assert.False(lida.Descartada);
    }

    [Fact]
    public async Task Ordem_Inexistente_RetornaNulo()
    {
        // Arrange
        var gateway = new FirestoreOrdemOrcamentoGateway(_store);

        // Act
        var lida = await gateway.GetByOrdemServicoIdAsync(404);

        // Assert
        Assert.Null(lida);
    }

    [Fact]
    public async Task Cobranca_SalvarELer_PreservaPixEStatus()
    {
        // Arrange
        var gateway = new FirestoreCobrancaGateway(_store);
        var cobranca = Cobranca.CriarPendente("ord_1", 10, "orc-10-1", "key", 150.5m);
        cobranca.RegistrarPix("qr", "ticket");
        cobranca.Confirmar();

        // Act
        await gateway.SalvarAsync(cobranca);
        var lida = await gateway.GetByIdAsync("ord_1");

        // Assert
        Assert.NotNull(lida);
        Assert.Equal(10, lida.OrdemServicoId);
        Assert.Equal("orc-10-1", lida.OrcamentoId);
        Assert.Equal("key", lida.IdempotencyKey);
        Assert.Equal(150.5m, lida.ValorTotal);
        Assert.Equal("qr", lida.QrCode);
        Assert.Equal("ticket", lida.Ticket);
        Assert.Equal(CobrancaStatus.Confirmada, lida.Status);
        Assert.Equal(cobranca.CriadaEm, lida.CriadaEm);
    }

    [Theory]
    [InlineData(CobrancaStatus.Pendente)]
    [InlineData(CobrancaStatus.Recusada)]
    [InlineData(CobrancaStatus.Expirada)]
    [InlineData(CobrancaStatus.Cancelada)]
    public async Task Cobranca_StatusSemPix_RoundTrip(CobrancaStatus status)
    {
        // Arrange
        var gateway = new FirestoreCobrancaGateway(_store);
        var cobranca = Cobranca.CriarPendente($"ord_{status}", 10, "orc-10-1", "key", 10m);
        switch (status)
        {
            case CobrancaStatus.Recusada:
                cobranca.Recusar();
                break;
            case CobrancaStatus.Expirada:
                cobranca.Expirar();
                break;
            case CobrancaStatus.Cancelada:
                cobranca.Cancelar();
                break;
        }

        await gateway.SalvarAsync(cobranca);

        // Act
        var lida = await gateway.GetByIdAsync(cobranca.Id);

        // Assert
        Assert.Equal(status, lida!.Status);
        Assert.Null(lida.QrCode);
        Assert.Null(lida.Ticket);
    }

    [Fact]
    public async Task Cobranca_Inexistente_RetornaNulo()
    {
        // Arrange
        var gateway = new FirestoreCobrancaGateway(_store);

        // Act
        var lida = await gateway.GetByIdAsync("nada");

        // Assert
        Assert.Null(lida);
    }

    [Fact]
    public async Task Cobranca_ListarPorOrdem_FiltraPelaOrdem()
    {
        // Arrange
        var gateway = new FirestoreCobrancaGateway(_store);
        await gateway.SalvarAsync(Cobranca.CriarPendente("a", 10, "orc-10-1", "k1", 1m));
        await gateway.SalvarAsync(Cobranca.CriarPendente("b", 10, "orc-10-1", "k2", 1m));
        await gateway.SalvarAsync(Cobranca.CriarPendente("c", 11, "orc-11-1", "k3", 1m));

        // Act
        var ordem10 = await gateway.ListarPorOrdemServicoAsync(10);
        var ordem99 = await gateway.ListarPorOrdemServicoAsync(99);

        // Assert
        Assert.Equal(["a", "b"], ordem10.Select(c => c.Id).OrderBy(id => id));
        Assert.Empty(ordem99);
    }

    [Fact]
    public async Task Evento_AddEConsulta_PorEventIdEPorChave()
    {
        // Arrange
        var gateway = new FirestoreEventoProcessadoGateway(_store);
        var eventId = Guid.NewGuid();
        await gateway.AddAsync(new EventoProcessado { EventId = eventId, Tipo = "T", ChaveNegocio = "10:1" });

        // Act
        var porId = await gateway.ExisteEventIdAsync(eventId);
        var porIdOutro = await gateway.ExisteEventIdAsync(Guid.NewGuid());
        var porChave = await gateway.ExisteChaveNegocioAsync("T", "10:1");
        var porChaveOutroTipo = await gateway.ExisteChaveNegocioAsync("X", "10:1");

        // Assert
        Assert.True(porId);
        Assert.False(porIdOutro);
        Assert.True(porChave);
        Assert.False(porChaveOutroTipo);
    }

    [Fact]
    public async Task Evento_TentarRegistrar_SoPrimeiraVezRetornaTrue()
    {
        // Arrange
        var gateway = new FirestoreEventoProcessadoGateway(_store);
        var evento = new EventoProcessado { EventId = Guid.NewGuid(), Tipo = "W", ChaveNegocio = "ord:Aprovada" };

        // Act
        var primeira = await gateway.TentarRegistrarAsync(evento);
        var segunda = await gateway.TentarRegistrarAsync(
            new EventoProcessado { EventId = Guid.NewGuid(), Tipo = "W", ChaveNegocio = "ord:Aprovada" });

        // Assert
        Assert.True(primeira);
        Assert.False(segunda);
    }
}
