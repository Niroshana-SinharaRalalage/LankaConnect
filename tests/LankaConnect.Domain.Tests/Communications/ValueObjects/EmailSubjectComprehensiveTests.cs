using LankaConnect.Domain.Communications.ValueObjects;
using LankaConnect.Domain.Common;

namespace LankaConnect.Domain.Tests.Communications.ValueObjects;

public class EmailSubjectComprehensiveTests
{
    #region Creation Tests

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
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t")]
    [InlineData("\n")]
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

    [Fact]
    public void Create_WithMaxLengthSubject_ShouldReturnSuccess()
    {
        // Arrange
        var maxLengthSubject = new string('A', EmailSubject.MaxLength);

        // Act
        var result = EmailSubject.Create(maxLengthSubject);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(maxLengthSubject, result.Value.Value);
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

    #endregion

    #region Content Validation Tests

    [Theory]
    [InlineData("Simple subject")]
    [InlineData("Subject with numbers 123")]
    [InlineData("Subject with symbols: Hello! How are you?")]
    [InlineData("Üñíçødé subject 中文")]
    [InlineData("Email with @#$%^&*()")]
    [InlineData("Multi-word subject with hyphens")]
    [InlineData("Subject.with.dots")]
    [InlineData("Subject_with_underscores")]
    public void Create_WithValidFormats_ShouldReturnSuccess(string subject)
    {
        // Act
        var result = EmailSubject.Create(subject);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(subject, result.Value.Value);
    }

    [Fact]
    public void Create_WithEmailSpecialCharacters_ShouldReturnSuccess()
    {
        // Arrange
        var subject = "RE: Your order #12345 - Status Update";

        // Act
        var result = EmailSubject.Create(subject);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(subject, result.Value.Value);
    }

    [Fact]
    public void Create_WithCommonEmailSubjectFormats_ShouldReturnSuccess()
    {
        // Arrange
        var subjects = new[]
        {
            "[URGENT] Please verify your email",
            "FWD: Meeting tomorrow",
            "Re: Your application",
            "IMPORTANT: Password reset request",
            "Welcome to our service!",
            "Your order has been shipped",
            "Monthly newsletter - January 2024"
        };

        // Act & Assert
        foreach (var subject in subjects)
        {
            var result = EmailSubject.Create(subject);
            Assert.True(result.IsSuccess, $"Subject '{subject}' should be valid");
            Assert.Equal(subject, result.Value.Value);
        }
    }

    #endregion

    #region Value Object Behavior Tests

    [Fact]
    public void Equality_WithSameValue_ShouldBeEqual()
    {
        // Arrange
        var subject1 = EmailSubject.Create("Test Subject").Value;
        var subject2 = EmailSubject.Create("Test Subject").Value;

        // Act & Assert
        Assert.Equal(subject1, subject2);
        Assert.True(subject1.Equals(subject2));
        Assert.True(subject1 == subject2);
        Assert.False(subject1 != subject2);
        Assert.Equal(subject1.GetHashCode(), subject2.GetHashCode());
    }

    [Fact]
    public void Equality_WithDifferentValues_ShouldNotBeEqual()
    {
        // Arrange
        var subject1 = EmailSubject.Create("Test Subject 1").Value;
        var subject2 = EmailSubject.Create("Test Subject 2").Value;

        // Act & Assert
        Assert.NotEqual(subject1, subject2);
        Assert.False(subject1.Equals(subject2));
        Assert.False(subject1 == subject2);
        Assert.True(subject1 != subject2);
    }

    [Fact]
    public void Equality_WithNull_ShouldNotBeEqual()
    {
        // Arrange
        var subject = EmailSubject.Create("Test Subject").Value;

        // Act & Assert
        Assert.False(subject.Equals(null));
        Assert.False(subject == null);
        Assert.True(subject != null);
    }

    [Fact]
    public void Equality_WithDifferentType_ShouldNotBeEqual()
    {
        // Arrange
        var subject = EmailSubject.Create("Test Subject").Value;
        var stringValue = "Test Subject";

        // Act & Assert
        Assert.False(subject.Equals(stringValue));
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

    #endregion

    #region Immutability Tests

    [Fact]
    public void EmailSubject_ShouldBeImmutable()
    {
        // Arrange
        var originalValue = "Original Subject";
        var subject = EmailSubject.Create(originalValue).Value;

        // Act - Try to get reference to Value property
        var value = subject.Value;

        // Assert - Value should be the same and not modifiable
        Assert.Equal(originalValue, value);
        Assert.Equal(originalValue, subject.Value);
    }

    [Fact]
    public void Create_WithSameValue_ShouldCreateDistinctInstances()
    {
        // Arrange
        var subjectValue = "Test Subject";

        // Act
        var subject1 = EmailSubject.Create(subjectValue).Value;
        var subject2 = EmailSubject.Create(subjectValue).Value;

        // Assert
        Assert.Equal(subject1, subject2); // Value equality
        Assert.NotSame(subject1, subject2); // Different instances
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void Create_WithOnlySpaces_ShouldReturnFailure()
    {
        // Arrange
        var spacesOnly = new string(' ', 10);

        // Act
        var result = EmailSubject.Create(spacesOnly);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Contains("Email subject is required", result.Errors);
    }

    [Fact]
    public void Create_WithMixedWhitespaceCharacters_ShouldTrimCorrectly()
    {
        // Arrange
        var mixedWhitespace = "\t  Test Subject  \n";
        var expectedValue = "Test Subject";

        // Act
        var result = EmailSubject.Create(mixedWhitespace);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(expectedValue, result.Value.Value);
    }

    [Fact]
    public void Create_WithSingleCharacter_ShouldReturnSuccess()
    {
        // Arrange
        var singleChar = "A";

        // Act
        var result = EmailSubject.Create(singleChar);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(singleChar, result.Value.Value);
    }

    [Fact]
    public void Create_WithNumbersOnly_ShouldReturnSuccess()
    {
        // Arrange
        var numbersOnly = "12345";

        // Act
        var result = EmailSubject.Create(numbersOnly);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(numbersOnly, result.Value.Value);
    }

    [Fact]
    public void Create_WithSpecialCharactersOnly_ShouldReturnSuccess()
    {
        // Arrange
        var specialChars = "!@#$%^&*()";

        // Act
        var result = EmailSubject.Create(specialChars);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(specialChars, result.Value.Value);
    }

    #endregion

    #region Business Context Tests

    [Theory]
    [InlineData("Welcome to LankaConnect!")]
    [InlineData("Verify your email address")]
    [InlineData("Password reset request")]
    [InlineData("Your business listing has been approved")]
    [InlineData("New event in your area")]
    [InlineData("Weekly newsletter from LankaConnect")]
    public void Create_WithTypicalBusinessSubjects_ShouldReturnSuccess(string subject)
    {
        // Act
        var result = EmailSubject.Create(subject);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(subject, result.Value.Value);
    }

    [Fact]
    public void Create_WithMultiLanguageSupport_ShouldReturnSuccess()
    {
        // Arrange
        var subjects = new[]
        {
            "ලංකා කනෙක්ට් වෙත සාදරයෙන් පිළිගනිමු", // Sinhala
            "லங்கா கனெக்ட்டிற்கு வரவேற்கிறோம்", // Tamil
            "Welcome to LankaConnect", // English
            "स्वागत है लंका कनेक्ट में" // Hindi
        };

        // Act & Assert
        foreach (var subject in subjects)
        {
            var result = EmailSubject.Create(subject);
            Assert.True(result.IsSuccess, $"Subject '{subject}' should be valid");
        }
    }

    #endregion

    #region GetEqualityComponents Tests

    [Fact]
    public void GetEqualityComponents_ShouldReturnValueComponent()
    {
        // Arrange
        var subjectValue = "Test Subject";
        var subject = EmailSubject.Create(subjectValue).Value;

        // Act
        var components = subject.GetEqualityComponents().ToList();

        // Assert
        Assert.Single(components);
        Assert.Equal(subjectValue, components[0]);
    }

    [Fact]
    public void GetEqualityComponents_WithDifferentValues_ShouldReturnDifferentComponents()
    {
        // Arrange
        var subject1 = EmailSubject.Create("Subject 1").Value;
        var subject2 = EmailSubject.Create("Subject 2").Value;

        // Act
        var components1 = subject1.GetEqualityComponents().ToList();
        var components2 = subject2.GetEqualityComponents().ToList();

        // Assert
        Assert.NotEqual(components1[0], components2[0]);
    }

    #endregion
}