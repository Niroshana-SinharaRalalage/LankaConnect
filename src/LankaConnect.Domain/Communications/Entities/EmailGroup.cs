using System.Text.RegularExpressions;
using LankaConnect.Domain.Common;

namespace LankaConnect.Domain.Communications.Entities;

/// <summary>
/// Email Group Entity
/// Phase 6A.25: Allows organizers and admins to manage groups of email addresses
/// for event announcements, invitations, and marketing communications.
/// </summary>
public class EmailGroup : BaseEntity
{
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public Guid OwnerId { get; private set; }
    public string EmailAddresses { get; private set; } = string.Empty;
    public bool IsActive { get; private set; } = true;

    // For EF Core
    private EmailGroup() { }

    /// <summary>
    /// Creates a new EmailGroup with validated email addresses
    /// </summary>
    public static Result<EmailGroup> Create(
        string name,
        Guid ownerId,
        string emailAddresses,
        string? description = null)
    {
        // Validate name
        if (string.IsNullOrWhiteSpace(name))
            return Result<EmailGroup>.Failure("Name is required");

        // Validate and clean email addresses
        var validationResult = ValidateAndCleanEmailAddresses(emailAddresses);
        if (validationResult.IsFailure)
            return Result<EmailGroup>.Failure(validationResult.Error);

        var group = new EmailGroup
        {
            Name = name.Trim(),
            OwnerId = ownerId,
            EmailAddresses = validationResult.Value,
            Description = description?.Trim(),
            IsActive = true
        };

        return Result<EmailGroup>.Success(group);
    }

    /// <summary>
    /// Updates the email group with new values
    /// </summary>
    public Result Update(string name, string emailAddresses, string? description)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Result.Failure("Name is required");

        var validationResult = ValidateAndCleanEmailAddresses(emailAddresses);
        if (validationResult.IsFailure)
            return Result.Failure(validationResult.Error);

        Name = name.Trim();
        EmailAddresses = validationResult.Value;
        Description = description?.Trim();
        MarkAsUpdated();

        return Result.Success();
    }

    /// <summary>
    /// Soft deletes the email group by marking it as inactive
    /// </summary>
    public void Deactivate()
    {
        IsActive = false;
        MarkAsUpdated();
    }

    /// <summary>
    /// Returns a list of individual email addresses
    /// </summary>
    public IReadOnlyList<string> GetEmailList()
    {
        if (string.IsNullOrWhiteSpace(EmailAddresses))
            return Array.Empty<string>();

        return EmailAddresses
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(e => e.Trim())
            .ToList()
            .AsReadOnly();
    }

    /// <summary>
    /// Returns the count of email addresses in the group
    /// </summary>
    public int GetEmailCount() => GetEmailList().Count;

    /// <summary>
    /// Validates and cleans email addresses
    /// - Splits by comma
    /// - Trims whitespace
    /// - Normalizes to lowercase
    /// - Removes duplicates
    /// - Validates email format
    /// </summary>
    private static Result<string> ValidateAndCleanEmailAddresses(string emailAddresses)
    {
        if (string.IsNullOrWhiteSpace(emailAddresses))
            return Result<string>.Failure("At least one email address is required");

        var emails = emailAddresses
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(e => e.Trim().ToLowerInvariant())
            .Where(e => !string.IsNullOrWhiteSpace(e))
            .Distinct()
            .ToList();

        if (emails.Count == 0)
            return Result<string>.Failure("At least one valid email address is required");

        // RFC 5322 compliant email regex (simplified version)
        var emailRegex = new Regex(
            @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$",
            RegexOptions.Compiled);

        var invalidEmails = emails.Where(e => !emailRegex.IsMatch(e)).ToList();

        if (invalidEmails.Count > 0)
            return Result<string>.Failure($"Invalid email format: {string.Join(", ", invalidEmails)}");

        return Result<string>.Success(string.Join(", ", emails));
    }
}
