using Domain.Entities;

namespace UnitTests.Domain.Entities;

public class OrdemOrcamentoTests
{
    [Fact]
    public void Criar_InicializaSemOrcamentoENaoDescartada()
    {
        // Arrange & Act
        var ordem = OrdemOrcamento.Criar(10);

        // Assert
        Assert.Equal(10, ordem.OrdemServicoId);
        Assert.Null(ordem.VersaoVigente);
        Assert.Null(ordem.OrcamentoIdVigente);
        Assert.False(ordem.Descartada);
    }

    [Fact]
    public void RegistrarOrcamento_AtualizaPonteiroVigente()
    {
        // Arrange
        var ordem = OrdemOrcamento.Criar(10);

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
        var ordem = OrdemOrcamento.Criar(10);

        // Act
        ordem.MarcarDescartada();

        // Assert
        Assert.True(ordem.Descartada);
    }
}
