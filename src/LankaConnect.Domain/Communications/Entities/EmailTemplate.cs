using LankaConnect.Domain.Common;
using LankaConnect.Domain.Communications.Enums;
using LankaConnect.Domain.Communications.ValueObjects;
using LankaConnect.Domain.Communications.Services;

namespace LankaConnect.Domain.Communications.Entities;

public class EmailTemplate : BaseEntity
{
    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public EmailSubject SubjectTemplate { get; private set; } = null!;
    public string TextTemplate { get; private set; } = string.Empty;
    public string? HtmlTemplate { get; private set; }
    public EmailType Type { get; private set; }
    public EmailTemplateCategory Category { get; private set; } = null!;
    public bool IsActive { get; private set; }
    public string? Tags { get; private set; }

    // For EF Core
    private EmailTemplate() { }

    private EmailTemplate(
        string name,
        string description,
        EmailSubject subjectTemplate,
        string textTemplate,
        string? htmlTemplate,
        EmailType type)
    {
        Name = name;
        Description = description;
        SubjectTemplate = subjectTemplate;
        TextTemplate = textTemplate;
        HtmlTemplate = htmlTemplate;
        Type = type;
        Category = EmailTemplateCategory.ForEmailType(type); // Auto-assign category based on type
        IsActive = true;
        MarkAsUpdated(); // Initialize UpdatedAt timestamp
    }

    public static Result<EmailTemplate> Create(
        string name,
        string description,
        EmailSubject subjectTemplate,
        string textTemplate,
        string? htmlTemplate = null,
        EmailType type = EmailType.Transactional)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Result<EmailTemplate>.Failure("Template name is required");

        if (string.IsNullOrWhiteSpace(description))
            return Result<EmailTemplate>.Failure("Template description is required");

        if (string.IsNullOrWhiteSpace(textTemplate))
            return Result<EmailTemplate>.Failure("Text template is required");

        var template = new EmailTemplate(name, description, subjectTemplate, textTemplate, htmlTemplate, type);
        return Result<EmailTemplate>.Success(template);
    }

    public Result UpdateTemplate(EmailSubject subjectTemplate, string textTemplate, string? htmlTemplate = null)
    {
        if (string.IsNullOrWhiteSpace(textTemplate))
            return Result.Failure("Text template is required");

        SubjectTemplate = subjectTemplate;
        TextTemplate = textTemplate;
        HtmlTemplate = htmlTemplate;
        MarkAsUpdated();

        return Result.Success();
    }

    public Result SetActive(bool isActive)
    {
        IsActive = isActive;
        MarkAsUpdated();
        return Result.Success();
    }

    public Result SetTags(string? tags)
    {
        Tags = tags;
        MarkAsUpdated();
        return Result.Success();
    }

    public Result UpdateType(EmailType newType)
    {
        Type = newType;
        Category = EmailTemplateCategory.ForEmailType(newType); // Update category when type changes
        MarkAsUpdated();
        return Result.Success();
    }

    /// <summary>
    /// Validates if the current template configuration is consistent
    /// </summary>
    public Result ValidateConfiguration()
    {
        var expectedCategory = EmailTemplateCategory.ForEmailType(Type);
        if (!Category.Equals(expectedCategory))
        {
            return Result.Failure($"Template type '{Type}' does not match category '{Category}'. Expected category: '{expectedCategory}'");
        }

        return Result.Success();
    }
}