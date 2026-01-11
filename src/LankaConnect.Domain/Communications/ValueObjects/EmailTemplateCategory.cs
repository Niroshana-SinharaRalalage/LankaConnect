using LankaConnect.Domain.Common;
using LankaConnect.Domain.Communications.Enums;

namespace LankaConnect.Domain.Communications.ValueObjects;

/// <summary>
/// Value object representing an email template category for organizational and filtering purposes
/// </summary>
public sealed class EmailTemplateCategory : ValueObject
{
    public string Value { get; private set; }
    public string DisplayName { get; private set; }
    public string Description { get; private set; }

    // For EF Core
    private EmailTemplateCategory() 
    { 
        Value = string.Empty;
        DisplayName = string.Empty;
        Description = string.Empty;
    }

    private EmailTemplateCategory(string value, string displayName, string description)
    {
        Value = value;
        DisplayName = displayName;
        Description = description;
    }

    /// <summary>
    /// Predefined email template categories
    /// </summary>
    public static readonly EmailTemplateCategory Authentication = new("Authentication", "Authentication", "User authentication and security related emails");
    public static readonly EmailTemplateCategory Business = new("Business", "Business", "Business operation and notification emails");
    public static readonly EmailTemplateCategory Marketing = new("Marketing", "Marketing", "Marketing and promotional emails");
    public static readonly EmailTemplateCategory System = new("System", "System", "System and administrative emails");
    public static readonly EmailTemplateCategory Notification = new("Notification", "Notification", "General notification emails");

    /// <summary>
    /// All available categories
    /// </summary>
    public static readonly IReadOnlyList<EmailTemplateCategory> All = new[]
    {
        Authentication,
        Business,
        Marketing,
        System,
        Notification
    };

    /// <summary>
    /// Creates a category from its string value
    /// </summary>
    public static Result<EmailTemplateCategory> FromValue(string value)
    {
        var category = All.FirstOrDefault(c => c.Value.Equals(value, StringComparison.OrdinalIgnoreCase));
        return category != null
            ? Result<EmailTemplateCategory>.Success(category)
            : Result<EmailTemplateCategory>.Failure($"Invalid email template category: {value}");
    }

    /// <summary>
    /// Creates a category from database value, used by EF Core during entity hydration.
    /// Returns System category as fallback for invalid values to prevent "Cannot access value of a failed result" exceptions.
    /// </summary>
    public static EmailTemplateCategory FromDatabase(string value)
    {
        var category = All.FirstOrDefault(c => c.Value.Equals(value, StringComparison.OrdinalIgnoreCase));
        return category ?? System; // Fallback to System category for invalid values
    }

    /// <summary>
    /// Gets category for a given email type using domain business logic
    /// </summary>
    public static EmailTemplateCategory ForEmailType(EmailType emailType)
    {
        return emailType switch
        {
            EmailType.EmailVerification or EmailType.PasswordReset or EmailType.MemberEmailVerification => Authentication,
            EmailType.BusinessNotification or EmailType.OrganizerCustomMessage => Business,
            EmailType.Marketing or EmailType.Newsletter => Marketing,
            EmailType.Welcome or EmailType.EventNotification or EmailType.EventReminder => Notification,
            EmailType.SignupCommitmentConfirmation or EmailType.RegistrationCancellationConfirmation => Notification,
            EmailType.Transactional => System,
            _ => System
        };
    }

    /// <summary>
    /// Implicit conversion to string for easy usage
    /// </summary>
    public static implicit operator string(EmailTemplateCategory category) => category.Value;

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;
}