using Domain.Entities;

namespace UnitTests.Domain.Entities;

public class OrcamentoTests
{
    private static readonly Guid OrdemServicoId = Guid.Parse("5a2c7e90-1d3b-4f68-b4a5-8c9e0f1a2b34");

    private static readonly ClienteOrcamento Cliente = new()
    {
        Nome = "Ada",
        Email = "ada@oficina.dev",
        Documento = "86421537090"
    };

    [Fact]
    public void Criar_SomaApenasServicosNaoRejeitados()
    {
        // Arrange
        var servicos = new List<OrcamentoServico>
        {
            new() { Id = 1, Nome = "Óleo", ValorCobrado = 150m, Status = "Sugerido" },
            new() { Id = 2, Nome = "Freio", ValorCobrado = 200m, Status = "Rejeitado" },
            new() { Id = 3, Nome = "Filtro", ValorCobrado = 50m, Status = "Aprovado" }
        };

        // Act
        var orcamento = Orcamento.Criar(OrdemServicoId, 2, Cliente, servicos);

        // Assert
        Assert.Equal($"orc-{OrdemServicoId}-2", orcamento.Id);
        Assert.Equal(OrdemServicoId, orcamento.OrdemServicoId);
        Assert.Equal(2, orcamento.VersaoDiagnostico);
        Assert.Equal(200m, orcamento.ValorTotal);
        Assert.Equal([1, 3], orcamento.Servicos.Select(s => s.Id));
        Assert.Equal(OrcamentoStatus.Ativo, orcamento.Status);
        Assert.Same(Cliente, orcamento.Cliente);
    }

    [Fact]
    public void Criar_SemServicos_GeraTotalZero()
    {
        // Arrange
        OrcamentoServico[] servicos = [];

        // Act
        var orcamento = Orcamento.Criar(OrdemServicoId, 1, Cliente, servicos);

        // Assert
        Assert.Equal(0m, orcamento.ValorTotal);
        Assert.Empty(orcamento.Servicos);
    }

    [Fact]
    public void CriarId_UsaPadraoOrcOrdemVersao()
    {
        // Arrange & Act
        var id = Orcamento.CriarId(OrdemServicoId, 3);

        // Assert
        Assert.Equal($"orc-{OrdemServicoId}-3", id);
    }

    [Fact]
    public void Cancelar_MarcaComoCancelado()
    {
        // Arrange
        var orcamento = Orcamento.Criar(OrdemServicoId, 1, Cliente, []);

        // Act
        orcamento.Cancelar();

        // Assert
        Assert.Equal(OrcamentoStatus.Cancelado, orcamento.Status);
    }
}
