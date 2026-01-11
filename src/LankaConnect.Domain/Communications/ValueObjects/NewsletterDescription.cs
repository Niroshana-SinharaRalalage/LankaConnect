using LankaConnect.Domain.Common;

namespace LankaConnect.Domain.Communications.ValueObjects;

/// <summary>
/// Value object representing a newsletter description/content
/// Phase 6A.74: Newsletter/News Alert Feature
/// </summary>
public class NewsletterDescription : ValueObject
{
    public const int MaxLength = 5000;

    public string Value { get; }

    private NewsletterDescription(string value)
    {
        Value = value;
    }

    /// <summary>
    /// Internal constructor for EF Core hydration.
    /// Bypasses validation to allow loading potentially invalid data from database.
    /// Should only be used by infrastructure layer during entity materialization.
    /// </summary>
    internal static NewsletterDescription FromDatabase(string value)
    {
        // For EF Core hydration, create instance even with empty/null value
        // This prevents "Cannot access value of a failed result" error during query materialization
        return new NewsletterDescription(value ?? string.Empty);
    }

    public static Result<NewsletterDescription> Create(string description)
    {
        if (string.IsNullOrWhiteSpace(description))
            return Result<NewsletterDescription>.Failure("Newsletter description is required");

        var trimmed = description.Trim();

        if (trimmed.Length > MaxLength)
            return Result<NewsletterDescription>.Failure($"Newsletter description cannot exceed {MaxLength} characters");

        return Result<NewsletterDescription>.Success(new NewsletterDescription(trimmed));
    }

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;
}
