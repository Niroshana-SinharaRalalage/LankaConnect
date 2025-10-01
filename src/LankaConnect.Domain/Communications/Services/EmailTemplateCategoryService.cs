using LankaConnect.Domain.Communications.Enums;
using LankaConnect.Domain.Communications.ValueObjects;

namespace LankaConnect.Domain.Communications.Services;

/// <summary>
/// Domain service for email template categorization and business logic
/// </summary>
public class EmailTemplateCategoryService
{
    /// <summary>
    /// Determines the appropriate category for an email type based on business rules
    /// </summary>
    /// <param name="emailType">The email type to categorize</param>
    /// <returns>The appropriate email template category</returns>
    public EmailTemplateCategory DetermineCategory(EmailType emailType)
    {
        return EmailTemplateCategory.ForEmailType(emailType);
    }

    /// <summary>
    /// Gets all email types that belong to a specific category
    /// </summary>
    /// <param name="category">The category to filter by</param>
    /// <returns>List of email types in the category</returns>
    public IEnumerable<EmailType> GetEmailTypesForCategory(EmailTemplateCategory category)
    {
        var allEmailTypes = Enum.GetValues<EmailType>();
        
        return allEmailTypes.Where(emailType => 
            EmailTemplateCategory.ForEmailType(emailType).Equals(category));
    }

    /// <summary>
    /// Validates if an email type is appropriate for a given category
    /// </summary>
    /// <param name="emailType">The email type to validate</param>
    /// <param name="expectedCategory">The expected category</param>
    /// <returns>True if the email type belongs to the category</returns>
    public bool ValidateEmailTypeForCategory(EmailType emailType, EmailTemplateCategory expectedCategory)
    {
        var actualCategory = DetermineCategory(emailType);
        return actualCategory.Equals(expectedCategory);
    }

    /// <summary>
    /// Gets category counts for email types (useful for reporting)
    /// </summary>
    /// <returns>Dictionary mapping categories to the number of email types in each</returns>
    public Dictionary<EmailTemplateCategory, int> GetCategoryCounts()
    {
        var allEmailTypes = Enum.GetValues<EmailType>();
        
        return allEmailTypes
            .GroupBy(emailType => EmailTemplateCategory.ForEmailType(emailType))
            .ToDictionary(group => group.Key, group => group.Count());
    }
}