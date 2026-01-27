using FluentAssertions;
using LankaConnect.Shared.Email.Contracts;

namespace LankaConnect.Shared.Tests.Email.Contracts;

/// <summary>
/// Phase 6A.86: Tests for IEmailParameters interface (TDD - RED phase)
/// Ensures the base email parameter contract provides required functionality
/// </summary>
public class IEmailParametersTests
{
    /// <summary>
    /// Test implementation of IEmailParameters for testing purposes
    /// </summary>
    private class TestEmailParameters : IEmailParameters
    {
        public string TemplateName { get; set; } = string.Empty;
        public string RecipientEmail { get; set; } = string.Empty;
        public string RecipientName { get; set; } = string.Empty;
        public Dictionary<string, object> AdditionalParameters { get; } = new();

        public Dictionary<string, object> ToDictionary()
        {
            var parameters = new Dictionary<string, object>
            {
                { "RecipientEmail", RecipientEmail },
                { "RecipientName", RecipientName }
            };

            // Merge additional parameters
            foreach (var kvp in AdditionalParameters)
            {
                parameters[kvp.Key] = kvp.Value;
            }

            return parameters;
        }

        public bool Validate(out List<string> errors)
        {
            errors = new List<string>();

            if (string.IsNullOrWhiteSpace(TemplateName))
                errors.Add("TemplateName is required");

            if (string.IsNullOrWhiteSpace(RecipientEmail))
                errors.Add("RecipientEmail is required");

            if (string.IsNullOrWhiteSpace(RecipientName))
                errors.Add("RecipientName is required");

            return errors.Count == 0;
        }
    }

    [Fact]
    public void IEmailParameters_ShouldHaveTemplateName()
    {
        // Arrange
        var parameters = new TestEmailParameters
        {
            TemplateName = "template-test"
        };

        // Act
        var templateName = parameters.TemplateName;

        // Assert
        templateName.Should().Be("template-test");
    }

    [Fact]
    public void IEmailParameters_ShouldHaveRecipientEmail()
    {
        // Arrange
        var parameters = new TestEmailParameters
        {
            RecipientEmail = "test@example.com"
        };

        // Act
        var email = parameters.RecipientEmail;

        // Assert
        email.Should().Be("test@example.com");
    }

    [Fact]
    public void IEmailParameters_ShouldHaveRecipientName()
    {
        // Arrange
        var parameters = new TestEmailParameters
        {
            RecipientName = "Test User"
        };

        // Act
        var name = parameters.RecipientName;

        // Assert
        name.Should().Be("Test User");
    }

    [Fact]
    public void ToDictionary_ShouldIncludeBasicParameters()
    {
        // Arrange
        var parameters = new TestEmailParameters
        {
            TemplateName = "template-test",
            RecipientEmail = "test@example.com",
            RecipientName = "Test User"
        };

        // Act
        var dictionary = parameters.ToDictionary();

        // Assert
        dictionary.Should().ContainKey("RecipientEmail");
        dictionary.Should().ContainKey("RecipientName");
        dictionary["RecipientEmail"].Should().Be("test@example.com");
        dictionary["RecipientName"].Should().Be("Test User");
    }

    [Fact]
    public void ToDictionary_ShouldIncludeAdditionalParameters()
    {
        // Arrange
        var parameters = new TestEmailParameters
        {
            TemplateName = "template-test",
            RecipientEmail = "test@example.com",
            RecipientName = "Test User"
        };
        parameters.AdditionalParameters["CustomParam1"] = "Value1";
        parameters.AdditionalParameters["CustomParam2"] = 123;

        // Act
        var dictionary = parameters.ToDictionary();

        // Assert
        dictionary.Should().ContainKey("CustomParam1");
        dictionary.Should().ContainKey("CustomParam2");
        dictionary["CustomParam1"].Should().Be("Value1");
        dictionary["CustomParam2"].Should().Be(123);
    }

    [Fact]
    public void Validate_ShouldFailWhenTemplateNameIsEmpty()
    {
        // Arrange
        var parameters = new TestEmailParameters
        {
            TemplateName = "",
            RecipientEmail = "test@example.com",
            RecipientName = "Test User"
        };

        // Act
        var isValid = parameters.Validate(out var errors);

        // Assert
        isValid.Should().BeFalse();
        errors.Should().Contain("TemplateName is required");
    }

    [Fact]
    public void Validate_ShouldFailWhenRecipientEmailIsEmpty()
    {
        // Arrange
        var parameters = new TestEmailParameters
        {
            TemplateName = "template-test",
            RecipientEmail = "",
            RecipientName = "Test User"
        };

        // Act
        var isValid = parameters.Validate(out var errors);

        // Assert
        isValid.Should().BeFalse();
        errors.Should().Contain("RecipientEmail is required");
    }

    [Fact]
    public void Validate_ShouldFailWhenRecipientNameIsEmpty()
    {
        // Arrange
        var parameters = new TestEmailParameters
        {
            TemplateName = "template-test",
            RecipientEmail = "test@example.com",
            RecipientName = ""
        };

        // Act
        var isValid = parameters.Validate(out var errors);

        // Assert
        isValid.Should().BeFalse();
        errors.Should().Contain("RecipientName is required");
    }

    [Fact]
    public void Validate_ShouldSucceedWhenAllRequiredFieldsAreProvided()
    {
        // Arrange
        var parameters = new TestEmailParameters
        {
            TemplateName = "template-test",
            RecipientEmail = "test@example.com",
            RecipientName = "Test User"
        };

        // Act
        var isValid = parameters.Validate(out var errors);

        // Assert
        isValid.Should().BeTrue();
        errors.Should().BeEmpty();
    }

    [Fact]
    public void Validate_ShouldReturnMultipleErrorsWhenMultipleFieldsAreMissing()
    {
        // Arrange
        var parameters = new TestEmailParameters
        {
            TemplateName = "",
            RecipientEmail = "",
            RecipientName = ""
        };

        // Act
        var isValid = parameters.Validate(out var errors);

        // Assert
        isValid.Should().BeFalse();
        errors.Should().HaveCount(3);
        errors.Should().Contain("TemplateName is required");
        errors.Should().Contain("RecipientEmail is required");
        errors.Should().Contain("RecipientName is required");
    }
}
