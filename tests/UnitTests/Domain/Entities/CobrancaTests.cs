using Domain.Entities;

namespace UnitTests.Domain.Entities;

public class CobrancaTests
{
    private static readonly Guid OrdemServicoId = Guid.Parse("3e8f1a24-5c6d-4b90-a7e1-2f4d6b8c0a13");

    private static Cobranca NovaCobranca() =>
        Cobranca.CriarPendente("ord_1", OrdemServicoId, $"orc-{OrdemServicoId}-1", "key-1", 150m);

    [Fact]
    public void CriarPendente_InicializaPendenteSemPix()
    {
        // Arrange & Act
        var cobranca = NovaCobranca();

        // Assert
        Assert.Equal("ord_1", cobranca.Id);
        Assert.Equal(OrdemServicoId, cobranca.OrdemServicoId);
        Assert.Equal($"orc-{OrdemServicoId}-1", cobranca.OrcamentoId);
        Assert.Equal("key-1", cobranca.IdempotencyKey);
        Assert.Equal(150m, cobranca.ValorTotal);
        Assert.Equal(CobrancaStatus.Pendente, cobranca.Status);
        Assert.Null(cobranca.QrCode);
        Assert.Null(cobranca.Ticket);
    }

    [Fact]
    public void RegistrarPix_GuardaQrCodeETicket()
    {
        // Arrange
        var cobranca = NovaCobranca();

        // Act
        cobranca.RegistrarPix("qr", "https://ticket");

        // Assert
        Assert.Equal("qr", cobranca.QrCode);
        Assert.Equal("https://ticket", cobranca.Ticket);
    }

    [Theory]
    [InlineData(CobrancaStatus.Confirmada)]
    [InlineData(CobrancaStatus.Recusada)]
    [InlineData(CobrancaStatus.Expirada)]
    [InlineData(CobrancaStatus.Cancelada)]
    public void Transicoes_AtualizamStatus(CobrancaStatus esperado)
    {
        // Arrange
        var cobranca = NovaCobranca();

        // Act
        switch (esperado)
        {
            case CobrancaStatus.Confirmada:
                cobranca.Confirmar();
                break;
            case CobrancaStatus.Recusada:
                cobranca.Recusar();
                break;
            case CobrancaStatus.Expirada:
                cobranca.Expirar();
                break;
            default:
                cobranca.Cancelar();
                break;
        }

        // Assert
        Assert.Equal(esperado, cobranca.Status);
    }
}
