using Bogus;
using LankaConnect.Domain.Communications.Entities;
using LankaConnect.Domain.Communications.Enums;
using LankaConnect.Domain.Communications.ValueObjects;

namespace LankaConnect.Infrastructure.Tests.Common;

/// <summary>
/// Test data builder for EmailTemplate entities using the Builder pattern
/// Provides fluent API for creating test data with realistic values
/// </summary>
public class EmailTemplateTestDataBuilder
{
    private readonly Faker _faker;
    private string _name;
    private string _description;
    private EmailSubject _subjectTemplate;
    private string _textTemplate;
    private string? _htmlTemplate;
    private EmailType _emailType;
    private bool _isActive;
    private string? _tags;

    public EmailTemplateTestDataBuilder()
    {
        _faker = new Faker();
        
        // Set realistic defaults
        _name = _faker.Lorem.Words(2).Join(" ");
        _description = _faker.Lorem.Sentence();
        _subjectTemplate = EmailSubject.Create($"{{{{companyName}}}} - {_faker.Lorem.Words(3).Join(" ")}").Value;
        _textTemplate = _faker.Lorem.Paragraphs(2);
        _htmlTemplate = $"<html><body><h1>{_faker.Lorem.Sentence()}</h1><p>{_faker.Lorem.Paragraphs(2)}</p></body></html>";
        _emailType = _faker.PickRandom<EmailType>();
        _isActive = true;
        _tags = string.Join(", ", _faker.Lorem.Words(3));
    }

    public EmailTemplateTestDataBuilder WithName(string name)
    {
        _name = name;
        return this;
    }

    public EmailTemplateTestDataBuilder WithDescription(string description)
    {
        _description = description;
        return this;
    }

    public EmailTemplateTestDataBuilder WithSubjectTemplate(string subjectTemplate)
    {
        _subjectTemplate = EmailSubject.Create(subjectTemplate).Value;
        return this;
    }

    public EmailTemplateTestDataBuilder WithSubjectTemplate(EmailSubject subjectTemplate)
    {
        _subjectTemplate = subjectTemplate;
        return this;
    }

    public EmailTemplateTestDataBuilder WithTextTemplate(string textTemplate)
    {
        _textTemplate = textTemplate;
        return this;
    }

    public EmailTemplateTestDataBuilder WithHtmlTemplate(string? htmlTemplate)
    {
        _htmlTemplate = htmlTemplate;
        return this;
    }

    public EmailTemplateTestDataBuilder WithEmailType(EmailType emailType)
    {
        _emailType = emailType;
        return this;
    }

    public EmailTemplateTestDataBuilder WithIsActive(bool isActive)
    {
        _isActive = isActive;
        return this;
    }

    public EmailTemplateTestDataBuilder WithTags(string? tags)
    {
        _tags = tags;
        return this;
    }

    public EmailTemplateTestDataBuilder AsActive()
    {
        _isActive = true;
        return this;
    }

    public EmailTemplateTestDataBuilder AsInactive()
    {
        _isActive = false;
        return this;
    }

    public EmailTemplateTestDataBuilder AsWelcomeEmail()
    {
        _emailType = EmailType.Welcome;
        _name = "Welcome Email Template";
        _description = "Template for welcoming new users";
        _subjectTemplate = EmailSubject.Create("Welcome to {{companyName}}!").Value;
        _textTemplate = "Welcome {{userName}} to our platform!";
        _htmlTemplate = "<html><body><h1>Welcome {{userName}}!</h1><p>We're excited to have you join {{companyName}}.</p></body></html>";
        return this;
    }

    public EmailTemplateTestDataBuilder AsPasswordResetEmail()
    {
        _emailType = EmailType.PasswordReset;
        _name = "Password Reset Template";
        _description = "Template for password reset emails";
        _subjectTemplate = EmailSubject.Create("Reset Your Password - {{companyName}}").Value;
        _textTemplate = "Click here to reset your password: {{resetLink}}";
        _htmlTemplate = "<html><body><p>Click <a href=\"{{resetLink}}\">here</a> to reset your password.</p></body></html>";
        return this;
    }

    public EmailTemplateTestDataBuilder AsEmailVerificationTemplate()
    {
        _emailType = EmailType.EmailVerification;
        _name = "Email Verification Template";
        _description = "Template for email verification";
        _subjectTemplate = EmailSubject.Create("Verify Your Email - {{companyName}}").Value;
        _textTemplate = "Please verify your email: {{verificationLink}}";
        _htmlTemplate = "<html><body><p>Please <a href=\"{{verificationLink}}\">verify your email</a>.</p></body></html>";
        return this;
    }

    public EmailTemplateTestDataBuilder AsMarketingTemplate()
    {
        _emailType = EmailType.Marketing;
        _name = "Marketing Campaign Template";
        _description = "Template for marketing campaigns";
        _subjectTemplate = EmailSubject.Create("Special Offer from {{companyName}}!").Value;
        _textTemplate = "Don't miss our special offer! {{offerDetails}}";
        _htmlTemplate = "<html><body><h1>Special Offer!</h1><p>{{offerDetails}}</p></body></html>";
        return this;
    }

    public EmailTemplate Build()
    {
        var template = EmailTemplate.Create(
            _name,
            _description,
            _subjectTemplate,
            _textTemplate,
            _htmlTemplate,
            _emailType
        );

        if (!template.IsSuccess)
            throw new InvalidOperationException($"Failed to create email template: {template.Error}");

        var emailTemplate = template.Value;
        
        if (_isActive != emailTemplate.IsActive)
        {
            emailTemplate.SetActive(_isActive);
        }

        if (!string.IsNullOrEmpty(_tags))
        {
            emailTemplate.SetTags(_tags);
        }

        return emailTemplate;
    }

    /// <summary>
    /// Builds multiple email templates with variations
    /// </summary>
    public List<EmailTemplate> BuildMany(int count)
    {
        var templates = new List<EmailTemplate>();
        var emailTypes = Enum.GetValues<EmailType>().ToList();

        for (int i = 0; i < count; i++)
        {
            var builder = new EmailTemplateTestDataBuilder()
                .WithName($"Template {i + 1}")
                .WithEmailType(emailTypes[i % emailTypes.Count])
                .WithIsActive(_faker.Random.Bool(0.8f)); // 80% chance of being active

            templates.Add(builder.Build());
        }

        return templates;
    }

    /// <summary>
    /// Creates a set of templates covering all email types
    /// </summary>
    public static List<EmailTemplate> CreateCompleteSuite()
    {
        var templates = new List<EmailTemplate>();
        
        foreach (EmailType emailType in Enum.GetValues<EmailType>())
        {
            var template = new EmailTemplateTestDataBuilder()
                .WithEmailType(emailType)
                .WithName($"{emailType} Template")
                .WithDescription($"Template for {emailType} emails")
                .Build();
            
            templates.Add(template);
        }

        return templates;
    }
}