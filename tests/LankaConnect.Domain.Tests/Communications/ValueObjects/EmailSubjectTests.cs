using LankaConnect.Domain.Communications.ValueObjects;
using LankaConnect.Domain.Common;

namespace LankaConnect.Domain.Tests.Communications.ValueObjects;

public class EmailSubjectTests
{
    [Fact]
    public void Create_WithValidSubject_ShouldReturnSuccess()
    {
        // Arrange
        var subject = "Welcome to LankaConnect";

        // Act
        var result = EmailSubject.Create(subject);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(subject, result.Value.Value);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Create_WithNullOrWhitespace_ShouldReturnFailure(string subject)
    {
        // Act
        var result = EmailSubject.Create(subject);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Contains("Email subject is required", result.Errors);
    }

    [Fact]
    public void Create_WithTooLongSubject_ShouldReturnFailure()
    {
        // Arrange
        var longSubject = new string('A', EmailSubject.MaxLength + 1);

        // Act
        var result = EmailSubject.Create(longSubject);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Contains($"Email subject cannot exceed {EmailSubject.MaxLength} characters", result.Errors);
    }

    [Theory]
    [InlineData("Simple subject")]
    [InlineData("Subject with numbers 123")]
    [InlineData("Subject with symbols: Hello! How are you?")]
    [InlineData("Üñíçødé subject 中文")]
    public void Create_WithValidFormats_ShouldReturnSuccess(string subject)
    {
        // Act
        var result = EmailSubject.Create(subject);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(subject, result.Value.Value);
    }

    [Fact]
    public void Create_ShouldTrimWhitespace()
    {
        // Arrange
        var subjectWithWhitespace = "  Test Subject  ";
        var expectedSubject = "Test Subject";

        // Act
        var result = EmailSubject.Create(subjectWithWhitespace);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(expectedSubject, result.Value.Value);
    }

    [Fact]
    public void Equality_WithSameValue_ShouldBeEqual()
    {
        // Arrange
        var subject1 = EmailSubject.Create("Test").Value;
        var subject2 = EmailSubject.Create("Test").Value;

        // Act & Assert
        Assert.Equal(subject1, subject2);
        Assert.True(subject1.Equals(subject2));
    }

    [Fact]
    public void ToString_ShouldReturnSubjectValue()
    {
        // Arrange
        var subjectValue = "Test Subject";
        var subject = EmailSubject.Create(subjectValue).Value;

        // Act
        var result = subject.ToString();

        // Assert
        Assert.Equal(subjectValue, result);
    }
}