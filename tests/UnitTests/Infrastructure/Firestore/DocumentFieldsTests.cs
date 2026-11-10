using Infrastructure.Firestore;

namespace UnitTests.Infrastructure.Firestore;

public class DocumentFieldsTests
{
    private static readonly IReadOnlyDictionary<string, object?> Vazio = new Dictionary<string, object?>();

    [Fact]
    public void GetString_CampoAusente_Lanca()
    {
        // Arrange & Act
        Action act = () => DocumentFields.GetString(Vazio, "x");

        // Assert
        Assert.Throws<InvalidOperationException>(act);
    }

    [Fact]
    public void GetInt_CampoAusente_Lanca()
    {
        // Arrange & Act
        Action act = () => DocumentFields.GetInt(Vazio, "x");

        // Assert
        Assert.Throws<InvalidOperationException>(act);
    }

    [Fact]
    public void GetNullableInt_ValorNulo_RetornaNulo()
    {
        // Arrange
        var fields = new Dictionary<string, object?> { ["x"] = null };

        // Act
        var valor = DocumentFields.GetNullableInt(fields, "x");

        // Assert
        Assert.Null(valor);
    }

    [Fact]
    public void GetBool_AusenteOuNaoBooleano_RetornaFalse()
    {
        // Arrange
        var fields = new Dictionary<string, object?> { ["x"] = "true" };

        // Act & Assert
        Assert.False(DocumentFields.GetBool(Vazio, "x"));
        Assert.False(DocumentFields.GetBool(fields, "x"));
    }

    [Fact]
    public void GetDocument_AusenteOuInvalido_Lanca()
    {
        // Arrange
        var fields = new Dictionary<string, object?> { ["x"] = "texto" };

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => DocumentFields.GetDocument(Vazio, "x"));
        Assert.Throws<InvalidOperationException>(() => DocumentFields.GetDocument(fields, "x"));
    }

    [Fact]
    public void GetDocuments_AusenteOuNaoLista_RetornaVazio()
    {
        // Arrange
        var fields = new Dictionary<string, object?> { ["x"] = 1 };

        // Act & Assert
        Assert.Empty(DocumentFields.GetDocuments(Vazio, "x"));
        Assert.Empty(DocumentFields.GetDocuments(fields, "x"));
    }

    [Fact]
    public void FormatDecimalEGetDecimal_PreservamValorComCulturaInvariante()
    {
        // Arrange
        var texto = DocumentFields.FormatDecimal(1234.56m);
        var fields = new Dictionary<string, object?> { ["v"] = texto };

        // Act
        var valor = DocumentFields.GetDecimal(fields, "v");

        // Assert
        Assert.Equal("1234.56", texto);
        Assert.Equal(1234.56m, valor);
    }
}
