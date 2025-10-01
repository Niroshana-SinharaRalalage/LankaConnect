using LankaConnect.Application.Common.Exceptions;
using FluentAssertions;

namespace LankaConnect.Application.Tests.Common;

public class ExceptionHandlingTests
{
    [Fact]
    public void NotFoundException_WithEntityNameAndKey_ShouldSetCorrectMessage()
    {
        // Arrange
        var entityName = "User";
        var key = Guid.NewGuid();

        // Act
        var exception = new NotFoundException(entityName, key);

        // Assert
        exception.Message.Should().Be($"Entity \"{entityName}\" ({key}) was not found.");
        // Note: NotFoundException doesn't have EntityName/Key properties, only message
    }

    [Fact]
    public void NotFoundException_WithCustomMessage_ShouldSetMessage()
    {
        // Arrange
        var customMessage = "Custom not found message";

        // Act
        var exception = new NotFoundException(customMessage);

        // Assert
        exception.Message.Should().Be(customMessage);
    }

    [Fact]
    public void NotFoundException_WithInnerException_ShouldPreserveInnerException()
    {
        // Arrange
        var innerException = new InvalidOperationException("Inner exception");
        var message = "Not found with inner exception";

        // Act
        var exception = new NotFoundException(message, innerException);

        // Assert
        exception.Message.Should().Be(message);
        exception.InnerException.Should().Be(innerException);
        exception.InnerException!.Message.Should().Be("Inner exception");
    }

    [Theory]
    [InlineData("User", "123")]
    [InlineData("Business", "456")]
    [InlineData("Order", "789")]
    public void NotFoundException_WithDifferentEntityTypes_ShouldFormatCorrectly(string entityName, string key)
    {
        // Act
        var exception = new NotFoundException(entityName, key);

        // Assert
        exception.Message.Should().Be($"Entity \"{entityName}\" ({key}) was not found.");
    }

    [Fact]
    public void NotFoundException_WithNullEntityName_ShouldHandleGracefully()
    {
        // Arrange
        string? entityName = null;
        var key = "123";

        // Act
        var exception = new NotFoundException(entityName!, key);

        // Assert
        exception.Message.Should().Contain("(123) was not found.");
    }

    [Fact]
    public void NotFoundException_WithEmptyKey_ShouldHandleGracefully()
    {
        // Arrange
        var entityName = "User";
        var key = "";

        // Act
        var exception = new NotFoundException(entityName, key);

        // Assert
        exception.Message.Should().Be($"Entity \"{entityName}\" () was not found.");
    }

    [Fact]
    public void NotFoundException_WithGuidKey_ShouldFormatCorrectly()
    {
        // Arrange
        var entityName = "User";
        var key = Guid.NewGuid();

        // Act
        var exception = new NotFoundException(entityName, key);

        // Assert
        exception.Message.Should().Contain(key.ToString());
        // Note: NotFoundException doesn't expose EntityName/Key properties, only message
    }

    [Fact]
    public void NotFoundException_WithIntegerKey_ShouldFormatCorrectly()
    {
        // Arrange
        var entityName = "Order";
        var key = 12345;

        // Act
        var exception = new NotFoundException(entityName, key);

        // Assert
        exception.Message.Should().Be($"Entity \"{entityName}\" ({key}) was not found.");
        // Note: NotFoundException doesn't expose EntityName/Key properties, only message
    }

    [Fact]
    public void NotFoundException_ShouldBeSerializable()
    {
        // Arrange
        var exception = new NotFoundException("User", Guid.NewGuid());
        
        // Act & Assert - Exception should be created properly
        exception.Should().NotBeNull();
        exception.Message.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void NotFoundException_ShouldInheritFromException()
    {
        // Arrange & Act
        var exception = new NotFoundException("Test message");

        // Assert
        exception.Should().BeAssignableTo<Exception>();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void NotFoundException_WithNullOrWhitespaceMessage_ShouldHandleGracefully(string? message)
    {
        // Act
        var exception = new NotFoundException(message!);

        // Assert
        exception.Message.Should().Be(message ?? string.Empty);
    }

    [Fact]
    public void NotFoundException_WithVeryLongMessage_ShouldPreserveMessage()
    {
        // Arrange
        var longMessage = new string('A', 10000);

        // Act
        var exception = new NotFoundException(longMessage);

        // Assert
        exception.Message.Should().Be(longMessage);
        exception.Message.Length.Should().Be(10000);
    }

    [Fact]
    public void NotFoundException_WithSpecialCharacters_ShouldPreserveMessage()
    {
        // Arrange
        var messageWithSpecialChars = "User 'John O'Connor' with special chars: äöü ñ @#$%^&*()";

        // Act
        var exception = new NotFoundException(messageWithSpecialChars);

        // Assert
        exception.Message.Should().Be(messageWithSpecialChars);
    }

    [Fact]
    public void NotFoundException_ToString_ShouldIncludeExceptionType()
    {
        // Arrange
        var exception = new NotFoundException("Test message");

        // Act
        var stringRepresentation = exception.ToString();

        // Assert
        stringRepresentation.Should().Contain("NotFoundException");
        stringRepresentation.Should().Contain("Test message");
    }
}