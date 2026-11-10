using Domain.Exceptions;

namespace UnitTests.Domain.Exceptions;

public class DomainExceptionsTests
{
    [Fact]
    public void BusinessRuleException_PreservaMensagemEHerdaDeDomainException()
    {
        // Arrange & Act
        var exception = new BusinessRuleException("regra");

        // Assert
        Assert.IsAssignableFrom<DomainException>(exception);
        Assert.Equal("regra", exception.Message);
    }

    [Fact]
    public void DomainNotFoundException_PreservaMensagemEHerdaDeDomainException()
    {
        // Arrange & Act
        var exception = new DomainNotFoundException("não achou");

        // Assert
        Assert.IsAssignableFrom<DomainException>(exception);
        Assert.Equal("não achou", exception.Message);
    }
}
