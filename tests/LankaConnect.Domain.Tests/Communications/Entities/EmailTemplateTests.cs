using LankaConnect.Domain.Communications.Entities;
using LankaConnect.Domain.Communications.ValueObjects;
using LankaConnect.Domain.Communications.Enums;
using LankaConnect.Domain.Tests.TestHelpers;

namespace LankaConnect.Domain.Tests.Communications.Entities;

public class EmailTemplateTests
{
    private readonly EmailSubject _validSubjectTemplate;

    public EmailTemplateTests()
    {
        _validSubjectTemplate = EmailSubject.Create("Welcome to {{companyName}}!").Value;
    }

    #region Creation Tests

    [Fact]
    public void Create_WithValidData_ShouldReturnSuccess()
    {
        // Arrange
        var name = "Welcome Email";
        var description = "Email sent to new users upon registration";
        var textTemplate = "Welcome {{userName}} to our platform!";
        var htmlTemplate = "<h1>Welcome {{userName}}</h1><p>Thank you for joining us!</p>";
        var type = EmailType.Welcome;

        // Act
        var result = EmailTemplate.Create(name, description, _validSubjectTemplate, textTemplate, htmlTemplate, type);

        // Assert
        Assert.True(result.IsSuccess);
        var template = result.Value;
        Assert.Equal(name, template.Name);
        Assert.Equal(description, template.Description);
        Assert.Equal(_validSubjectTemplate, template.SubjectTemplate);
        Assert.Equal(textTemplate, template.TextTemplate);
        Assert.Equal(htmlTemplate, template.HtmlTemplate);
        Assert.Equal(type, template.Type);
        Assert.True(template.IsActive); // Default should be active
        Assert.Null(template.Tags); // Default should be null
        Assert.NotEqual(Guid.Empty, template.Id);
        Assert.True((DateTime.UtcNow - template.CreatedAt).TotalSeconds < 1);
    }

    [Fact]
    public void Create_WithDefaultParameters_ShouldUseDefaultValues()
    {
        // Arrange
        var name = "Simple Template";
        var description = "A simple email template";
        var textTemplate = "Hello {{userName}}!";

        // Act
        var result = EmailTemplate.Create(name, description, _validSubjectTemplate, textTemplate);

        // Assert
        Assert.True(result.IsSuccess);
        var template = result.Value;
        Assert.Null(template.HtmlTemplate);
        Assert.Equal(EmailType.Transactional, template.Type); // Default type
        Assert.True(template.IsActive);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithInvalidName_ShouldReturnFailure(string name)
    {
        // Act
        var result = EmailTemplate.Create(name, "Description", _validSubjectTemplate, "Text");

        // Assert
        Assert.True(result.IsFailure);
        Assert.Contains("Template name is required", result.Errors);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithInvalidDescription_ShouldReturnFailure(string description)
    {
        // Act
        var result = EmailTemplate.Create("Name", description, _validSubjectTemplate, "Text");

        // Assert
        Assert.True(result.IsFailure);
        Assert.Contains("Template description is required", result.Errors);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithInvalidTextTemplate_ShouldReturnFailure(string textTemplate)
    {
        // Act
        var result = EmailTemplate.Create("Name", "Description", _validSubjectTemplate, textTemplate);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Contains("Text template is required", result.Errors);
    }

    #endregion

    #region Template Update Tests

    [Fact]
    public void UpdateTemplate_WithValidData_ShouldUpdateSuccessfully()
    {
        // Arrange
        var template = CreateValidTemplate();
        var newSubjectTemplate = EmailSubject.Create("Updated Subject: {{title}}").Value;
        var newTextTemplate = "Updated text content with {{variable}}";
        var newHtmlTemplate = "<p>Updated HTML content with {{variable}}</p>";
        var originalUpdatedAt = template.UpdatedAt;

        // Act
        var result = template.UpdateTemplate(newSubjectTemplate, newTextTemplate, newHtmlTemplate);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(newSubjectTemplate, template.SubjectTemplate);
        Assert.Equal(newTextTemplate, template.TextTemplate);
        Assert.Equal(newHtmlTemplate, template.HtmlTemplate);
        Assert.True(template.UpdatedAt > originalUpdatedAt);
    }

    [Fact]
    public void UpdateTemplate_WithoutHtmlTemplate_ShouldUpdateSuccessfully()
    {
        // Arrange
        var template = CreateValidTemplate();
        var newSubjectTemplate = EmailSubject.Create("Updated Subject").Value;
        var newTextTemplate = "Updated text content";

        // Act
        var result = template.UpdateTemplate(newSubjectTemplate, newTextTemplate);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(newSubjectTemplate, template.SubjectTemplate);
        Assert.Equal(newTextTemplate, template.TextTemplate);
        Assert.Null(template.HtmlTemplate);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void UpdateTemplate_WithInvalidTextTemplate_ShouldReturnFailure(string textTemplate)
    {
        // Arrange
        var template = CreateValidTemplate();
        var newSubjectTemplate = EmailSubject.Create("Updated Subject").Value;
        var originalTextTemplate = template.TextTemplate;
        var originalUpdatedAt = template.UpdatedAt;

        // Act
        var result = template.UpdateTemplate(newSubjectTemplate, textTemplate);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Contains("Text template is required", result.Errors);
        Assert.Equal(originalTextTemplate, template.TextTemplate); // Should not change
        Assert.Equal(originalUpdatedAt, template.UpdatedAt); // Should not update timestamp
    }

    #endregion

    #region Activation Tests

    [Fact]
    public void SetActive_WithTrue_ShouldActivateTemplate()
    {
        // Arrange
        var template = CreateValidTemplate();
        template.SetActive(false); // First deactivate
        var originalUpdatedAt = template.UpdatedAt;

        // Act
        var result = template.SetActive(true);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.True(template.IsActive);
        Assert.True(template.UpdatedAt > originalUpdatedAt);
    }

    [Fact]
    public void SetActive_WithFalse_ShouldDeactivateTemplate()
    {
        // Arrange
        var template = CreateValidTemplate();
        Assert.True(template.IsActive); // Default is active
        var originalUpdatedAt = template.UpdatedAt;

        // Act
        var result = template.SetActive(false);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.False(template.IsActive);
        Assert.True(template.UpdatedAt > originalUpdatedAt);
    }

    [Fact]
    public void SetActive_WithSameValue_ShouldStillUpdateTimestamp()
    {
        // Arrange
        var template = CreateValidTemplate();
        Assert.True(template.IsActive); // Default is active
        var originalUpdatedAt = template.UpdatedAt;

        // Act
        System.Threading.Thread.Sleep(1); // Ensure time difference
        var result = template.SetActive(true); // Setting to same value

        // Assert
        Assert.True(result.IsSuccess);
        Assert.True(template.IsActive);
        Assert.True(template.UpdatedAt > originalUpdatedAt); // Should still update timestamp
    }

    #endregion

    #region Tags Tests

    [Fact]
    public void SetTags_WithValidTags_ShouldUpdateSuccessfully()
    {
        // Arrange
        var template = CreateValidTemplate();
        var tags = "welcome,registration,user-onboarding";
        var originalUpdatedAt = template.UpdatedAt;

        // Act
        var result = template.SetTags(tags);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(tags, template.Tags);
        Assert.True(template.UpdatedAt > originalUpdatedAt);
    }

    [Fact]
    public void SetTags_WithNull_ShouldUpdateSuccessfully()
    {
        // Arrange
        var template = CreateValidTemplate();
        template.SetTags("some-tags"); // First set some tags
        var originalUpdatedAt = template.UpdatedAt;

        // Act
        System.Threading.Thread.Sleep(1); // Ensure time difference
        var result = template.SetTags(null);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Null(template.Tags);
        Assert.True(template.UpdatedAt > originalUpdatedAt);
    }

    [Fact]
    public void SetTags_WithEmptyString_ShouldUpdateSuccessfully()
    {
        // Arrange
        var template = CreateValidTemplate();
        var originalUpdatedAt = template.UpdatedAt;

        // Act
        System.Threading.Thread.Sleep(1); // Ensure time difference
        var result = template.SetTags("");

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("", template.Tags);
        Assert.True(template.UpdatedAt > originalUpdatedAt);
    }

    #endregion

    #region Template Types Tests

    [Theory]
    [InlineData(EmailType.Welcome)]
    [InlineData(EmailType.EmailVerification)]
    [InlineData(EmailType.PasswordReset)]
    [InlineData(EmailType.BusinessNotification)]
    [InlineData(EmailType.EventNotification)]
    [InlineData(EmailType.Newsletter)]
    [InlineData(EmailType.Marketing)]
    [InlineData(EmailType.Transactional)]
    public void Create_WithAllEmailTypes_ShouldReturnSuccess(EmailType emailType)
    {
        // Act
        var result = EmailTemplate.Create(
            $"{emailType} Template",
            $"Template for {emailType} emails",
            _validSubjectTemplate,
            "Template content",
            null,
            emailType
        );

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(emailType, result.Value.Type);
    }

    #endregion

    #region Integration Tests

    [Fact]
    public void EmailTemplate_FullLifecycle_ShouldMaintainConsistency()
    {
        // Arrange & Act - Create template
        var template = CreateValidTemplate();
        
        // Act - Update template content
        var newSubjectTemplate = EmailSubject.Create("Updated {{subject}}").Value;
        template.UpdateTemplate(newSubjectTemplate, "Updated text", "<p>Updated HTML</p>");

        // Act - Add tags
        template.SetTags("updated,modified,test");

        // Act - Deactivate and reactivate
        template.SetActive(false);
        template.SetActive(true);

        // Assert - Verify final state
        Assert.Equal(newSubjectTemplate, template.SubjectTemplate);
        Assert.Equal("Updated text", template.TextTemplate);
        Assert.Equal("<p>Updated HTML</p>", template.HtmlTemplate);
        Assert.Equal("updated,modified,test", template.Tags);
        Assert.True(template.IsActive);
    }

    [Fact]
    public void EmailTemplate_ShouldInheritFromBaseEntity()
    {
        // Act
        var template = CreateValidTemplate();

        // Assert
        Assert.IsAssignableFrom<LankaConnect.Domain.Common.BaseEntity>(template);
        Assert.NotEqual(Guid.Empty, template.Id);
        Assert.True((DateTime.UtcNow - template.CreatedAt).TotalSeconds < 1);
        Assert.NotNull(template.UpdatedAt);
    }

    #endregion

    #region Business Context Tests

    [Fact]
    public void Create_WelcomeEmailTemplate_ShouldHaveAppropriateStructure()
    {
        // Arrange
        var name = "User Welcome Email";
        var description = "Sent to new users after successful registration";
        var subjectTemplate = EmailSubject.Create("Welcome to LankaConnect, {{userName}}!").Value;
        var textTemplate = "Hello {{userName}},\n\nWelcome to LankaConnect! We're excited to have you join our community.\n\nBest regards,\nThe LankaConnect Team";
        var htmlTemplate = """
            <h1>Welcome to LankaConnect, {{userName}}!</h1>
            <p>We're excited to have you join our community of local businesses and residents.</p>
            <p>Get started by:</p>
            <ul>
                <li>Completing your profile</li>
                <li>Exploring local businesses</li>
                <li>Connecting with your community</li>
            </ul>
            <p>Best regards,<br>The LankaConnect Team</p>
            """;

        // Act
        var result = EmailTemplate.Create(name, description, subjectTemplate, textTemplate, htmlTemplate, EmailType.Welcome);

        // Assert
        Assert.True(result.IsSuccess);
        var template = result.Value;
        Assert.Equal(EmailType.Welcome, template.Type);
        Assert.Contains("{{userName}}", template.SubjectTemplate.Value);
        Assert.Contains("{{userName}}", template.TextTemplate);
        Assert.Contains("{{userName}}", template.HtmlTemplate);
    }

    [Fact]
    public void Create_EmailVerificationTemplate_ShouldHaveSecurityConsiderations()
    {
        // Arrange
        var name = "Email Verification";
        var description = "Template for email address verification";
        var subjectTemplate = EmailSubject.Create("Verify your email address").Value;
        var textTemplate = "Please verify your email by clicking: {{verificationUrl}} (expires in {{expiryHours}} hours)";
        var htmlTemplate = """
            <h2>Verify Your Email Address</h2>
            <p>Please click the button below to verify your email address:</p>
            <a href="{{verificationUrl}}" style="background: #007bff; color: white; padding: 10px 20px; text-decoration: none; border-radius: 5px;">Verify Email</a>
            <p>This link expires in {{expiryHours}} hours.</p>
            <p>If you didn't create this account, please ignore this email.</p>
            """;

        // Act
        var result = EmailTemplate.Create(name, description, subjectTemplate, textTemplate, htmlTemplate, EmailType.EmailVerification);

        // Assert
        Assert.True(result.IsSuccess);
        var template = result.Value;
        Assert.Equal(EmailType.EmailVerification, template.Type);
        Assert.Contains("{{verificationUrl}}", template.TextTemplate);
        Assert.Contains("{{expiryHours}}", template.TextTemplate);
    }

    [Fact]
    public void Create_PasswordResetTemplate_ShouldHaveSecurityWarnings()
    {
        // Arrange
        var name = "Password Reset";
        var description = "Template for password reset requests";
        var subjectTemplate = EmailSubject.Create("Reset your password").Value;
        var textTemplate = "Reset your password: {{resetUrl}} (expires in 1 hour). If you didn't request this, ignore this email.";
        var htmlTemplate = """
            <h2>Password Reset Request</h2>
            <p>Click the button below to reset your password:</p>
            <a href="{{resetUrl}}">Reset Password</a>
            <p><strong>This link expires in 1 hour.</strong></p>
            <p><strong>If you didn't request this password reset, please ignore this email and your password will remain unchanged.</strong></p>
            """;

        // Act
        var result = EmailTemplate.Create(name, description, subjectTemplate, textTemplate, htmlTemplate, EmailType.PasswordReset);

        // Assert
        Assert.True(result.IsSuccess);
        var template = result.Value;
        Assert.Equal(EmailType.PasswordReset, template.Type);
        Assert.Contains("{{resetUrl}}", template.TextTemplate);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void Create_WithVeryLongContent_ShouldReturnSuccess()
    {
        // Arrange
        var name = "Long Content Template";
        var description = "Template with very long content";
        var longTextTemplate = new string('A', 10000); // 10KB of text
        var longHtmlTemplate = $"<p>{new string('B', 10000)}</p>"; // 10KB of HTML

        // Act
        var result = EmailTemplate.Create(name, description, _validSubjectTemplate, longTextTemplate, longHtmlTemplate);

        // Assert
        Assert.True(result.IsSuccess);
        var template = result.Value;
        Assert.Equal(10000, template.TextTemplate.Length);
        Assert.True(template.HtmlTemplate!.Length > 10000); // Should be longer due to HTML tags
    }

    [Fact]
    public void UpdateTemplate_MultipleUpdates_ShouldMaintainConsistency()
    {
        // Arrange
        var template = CreateValidTemplate();

        // Act - Multiple rapid updates
        for (int i = 0; i < 5; i++)
        {
            var subject = EmailSubject.Create($"Subject {i}").Value;
            var result = template.UpdateTemplate(subject, $"Text content {i}", $"<p>HTML content {i}</p>");
            Assert.True(result.IsSuccess);
        }

        // Assert - Should have latest content
        Assert.Equal("Subject 4", template.SubjectTemplate.Value);
        Assert.Equal("Text content 4", template.TextTemplate);
        Assert.Equal("<p>HTML content 4</p>", template.HtmlTemplate);
    }

    [Fact]
    public void EmailTemplate_WithSpecialCharacters_ShouldHandleCorrectly()
    {
        // Arrange
        var name = "Special Characters Template";
        var description = "Template with émojis 🎉 and spëcial çhars";
        var textTemplate = "Hello {{userName}}! 🎉 Welcome to LankaConnect! We're excited to have you. Ça va?";
        var htmlTemplate = "<h1>Welcome {{userName}}! 🎉</h1><p>We're excited! Ça va très bien?</p>";

        // Act
        var result = EmailTemplate.Create(name, description, _validSubjectTemplate, textTemplate, htmlTemplate);

        // Assert
        Assert.True(result.IsSuccess);
        var template = result.Value;
        Assert.Contains("🎉", template.TextTemplate);
        Assert.Contains("Ça va", template.TextTemplate);
        Assert.Contains("🎉", template.HtmlTemplate);
    }

    #endregion

    #region Helper Methods

    private EmailTemplate CreateValidTemplate()
    {
        return EmailTemplate.Create(
            "Test Template",
            "A test email template",
            _validSubjectTemplate,
            "This is test content with {{variable}}",
            "<p>This is test HTML content with {{variable}}</p>",
            EmailType.Transactional
        ).Value;
    }

    #endregion
}