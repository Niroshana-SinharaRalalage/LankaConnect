using FluentAssertions;
using LankaConnect.Domain.Events.ValueObjects;
using LankaConnect.Domain.Shared.ValueObjects;
using Xunit;

namespace LankaConnect.Domain.UnitTests.Events.ValueObjects;

public class AttendeeInfoTests
{
    [Fact]
    public void Create_WithValidData_ShouldSucceed()
    {
        // Arrange
        var name = "John Doe";
        var age = 30;
        var address = "123 Main St, Columbus, OH 43215";
        var email = "john.doe@example.com";
        var phone = "+1-614-555-1234";

        // Act
        var result = AttendeeInfo.Create(name, age, address, email, phone);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be(name);
        result.Value.Age.Should().Be(age);
        result.Value.Address.Should().Be(address);
        result.Value.Email.Value.Should().Be(email);
        result.Value.PhoneNumber.Value.Should().Be(phone);
    }

    [Theory]
    [InlineData(null, 30, "123 Main St", "test@example.com", "+1-614-555-1234", "Name is required")]
    [InlineData("", 30, "123 Main St", "test@example.com", "+1-614-555-1234", "Name is required")]
    [InlineData("   ", 30, "123 Main St", "test@example.com", "+1-614-555-1234", "Name is required")]
    public void Create_WithInvalidName_ShouldFail(string? name, int age, string address, string email, string phone, string expectedError)
    {
        // Act
        var result = AttendeeInfo.Create(name!, age, address, email, phone);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain(expectedError);
    }

    [Theory]
    [InlineData(0, "Age must be between 1 and 150")]
    [InlineData(-1, "Age must be between 1 and 150")]
    [InlineData(151, "Age must be between 1 and 150")]
    public void Create_WithInvalidAge_ShouldFail(int age, string expectedError)
    {
        // Arrange
        var name = "John Doe";
        var address = "123 Main St";
        var email = "test@example.com";
        var phone = "+1-614-555-1234";

        // Act
        var result = AttendeeInfo.Create(name, age, address, email, phone);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain(expectedError);
    }

    [Theory]
    [InlineData(null, "Address is required")]
    [InlineData("", "Address is required")]
    [InlineData("   ", "Address is required")]
    public void Create_WithInvalidAddress_ShouldFail(string? address, string expectedError)
    {
        // Arrange
        var name = "John Doe";
        var age = 30;
        var email = "test@example.com";
        var phone = "+1-614-555-1234";

        // Act
        var result = AttendeeInfo.Create(name, age, address!, email, phone);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain(expectedError);
    }

    [Theory]
    [InlineData("invalid-email")]
    [InlineData("@example.com")]
    [InlineData("test@")]
    public void Create_WithInvalidEmail_ShouldFail(string invalidEmail)
    {
        // Arrange
        var name = "John Doe";
        var age = 30;
        var address = "123 Main St";
        var phone = "+1-614-555-1234";

        // Act
        var result = AttendeeInfo.Create(name, age, address, invalidEmail, phone);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Invalid email");
    }

    [Theory]
    [InlineData("123")]  // Too short
    [InlineData("abc")]  // Not numeric
    public void Create_WithInvalidPhone_ShouldFail(string invalidPhone)
    {
        // Arrange
        var name = "John Doe";
        var age = 30;
        var address = "123 Main St";
        var email = "test@example.com";

        // Act
        var result = AttendeeInfo.Create(name, age, address, email, invalidPhone);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Invalid phone");
    }

    [Fact]
    public void ValueEquality_WithSameValues_ShouldBeEqual()
    {
        // Arrange
        var info1 = AttendeeInfo.Create("John Doe", 30, "123 Main St", "john@example.com", "+1-614-555-1234").Value;
        var info2 = AttendeeInfo.Create("John Doe", 30, "123 Main St", "john@example.com", "+1-614-555-1234").Value;

        // Act & Assert
        info1.Should().Be(info2);
        (info1 == info2).Should().BeTrue();
    }

    [Fact]
    public void ValueEquality_WithDifferentValues_ShouldNotBeEqual()
    {
        // Arrange
        var info1 = AttendeeInfo.Create("John Doe", 30, "123 Main St", "john@example.com", "+1-614-555-1234").Value;
        var info2 = AttendeeInfo.Create("Jane Smith", 25, "456 Oak Ave", "jane@example.com", "+1-614-555-5678").Value;

        // Act & Assert
        info1.Should().NotBe(info2);
        (info1 == info2).Should().BeFalse();
    }
}
