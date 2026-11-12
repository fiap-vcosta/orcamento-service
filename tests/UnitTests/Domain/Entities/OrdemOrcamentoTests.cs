using Domain.Entities;

namespace UnitTests.Domain.Entities;

public class OrdemOrcamentoTests
{
    private static readonly Guid OrdemServicoId = Guid.Parse("8d1e4b67-9a0c-4e35-93f2-6b7a8c9d0e45");

    [Fact]
    public void Criar_InicializaSemOrcamentoENaoDescartada()
    {
        // Arrange & Act
        var ordem = OrdemOrcamento.Criar(OrdemServicoId);

        // Assert
        Assert.Equal(OrdemServicoId, ordem.OrdemServicoId);
        Assert.Null(ordem.VersaoVigente);
        Assert.Null(ordem.OrcamentoIdVigente);
        Assert.False(ordem.Descartada);
    }

    [Fact]
    public void RegistrarOrcamento_AtualizaPonteiroVigente()
    {
        // Arrange
        var ordem = OrdemOrcamento.Criar(OrdemServicoId);

        // Act
        ordem.RegistrarOrcamento("orc-10-2", 2);

        // Assert
        Assert.Equal(2, ordem.VersaoVigente);
        Assert.Equal("orc-10-2", ordem.OrcamentoIdVigente);
    }

    [Fact]
    public void MarcarDescartada_SetaFlag()
    {
        // Arrange
        var ordem = OrdemOrcamento.Criar(OrdemServicoId);

        // Act
        ordem.MarcarDescartada();

        // Assert
        Assert.True(ordem.Descartada);
    }
}
