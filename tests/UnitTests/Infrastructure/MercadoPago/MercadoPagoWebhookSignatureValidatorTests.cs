using System.Security.Cryptography;
using System.Text;
using Infrastructure.MercadoPago;
using Microsoft.Extensions.Options;

namespace UnitTests.Infrastructure.MercadoPago;

public class MercadoPagoWebhookSignatureValidatorTests
{
    private const string Secret = "segredo-do-webhook";

    private static MercadoPagoWebhookSignatureValidator CriarValidator(string secret = Secret) =>
        new(Options.Create(new MercadoPagoOptions { WebhookSecret = secret }));

    private static string Assinar(string manifest, string secret = Secret) =>
        Convert.ToHexString(HMACSHA256.HashData(Encoding.UTF8.GetBytes(secret), Encoding.UTF8.GetBytes(manifest)))
            .ToLowerInvariant();

    [Fact]
    public void IsValid_AssinaturaCorreta_RetornaTrue()
    {
        // Arrange
        var v1 = Assinar("id:ord01abc;request-id:req-1;ts:1742505638683;");
        var validator = CriarValidator();

        // Act
        var valido = validator.IsValid($"ts=1742505638683,v1={v1}", "req-1", "ORD01ABC");

        // Assert
        Assert.True(valido);
    }

    [Fact]
    public void IsValid_SemRequestId_OmiteParteNoManifest()
    {
        // Arrange
        var v1 = Assinar("id:ord01abc;ts:99;");
        var validator = CriarValidator();

        // Act
        var valido = validator.IsValid($"ts=99, v1={v1}", null, "ord01abc");

        // Assert
        Assert.True(valido);
    }

    [Fact]
    public void IsValid_HashDiferente_RetornaFalse()
    {
        // Arrange
        var v1 = Assinar("id:ord01abc;request-id:req-1;ts:1;", "outro-segredo");
        var validator = CriarValidator();

        // Act
        var valido = validator.IsValid($"ts=1,v1={v1}", "req-1", "ord01abc");

        // Assert
        Assert.False(valido);
    }

    [Fact]
    public void IsValid_RequestIdAdulterado_RetornaFalse()
    {
        // Arrange
        var v1 = Assinar("id:ord01abc;request-id:req-1;ts:1;");
        var validator = CriarValidator();

        // Act
        var valido = validator.IsValid($"ts=1,v1={v1}", "req-2", "ord01abc");

        // Assert
        Assert.False(valido);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("lixo")]
    [InlineData("ts=1")]
    [InlineData("v1=abc")]
    [InlineData("=x,ts=1,v1=abc")]
    public void IsValid_AssinaturaMalformada_RetornaFalse(string? assinatura)
    {
        // Arrange
        var validator = CriarValidator();

        // Act
        var valido = validator.IsValid(assinatura, "req", "ord01abc");

        // Assert
        Assert.False(valido);
    }

    [Fact]
    public void IsValid_SemSecret_RetornaFalse()
    {
        // Arrange
        var v1 = Assinar("id:ord01abc;ts:1;", string.Empty);
        var validator = CriarValidator(string.Empty);

        // Act
        var valido = validator.IsValid($"ts=1,v1={v1}", null, "ord01abc");

        // Assert
        Assert.False(valido);
    }

    [Fact]
    public void IsValid_SemDataId_RetornaFalse()
    {
        // Arrange
        var validator = CriarValidator();

        // Act
        var valido = validator.IsValid("ts=1,v1=abc", "req", " ");

        // Assert
        Assert.False(valido);
    }

    [Fact]
    public void BuildManifest_MontaNoFormatoDoMercadoPago()
    {
        // Arrange & Act
        var manifest = MercadoPagoWebhookSignatureValidator.BuildManifest("ABC", "req", "10");

        // Assert
        Assert.Equal("id:abc;request-id:req;ts:10;", manifest);
    }
}
