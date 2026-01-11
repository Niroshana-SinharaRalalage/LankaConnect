using LankaConnect.Domain.Common;

namespace LankaConnect.Domain.Communications.ValueObjects;

/// <summary>
/// Value object representing a newsletter title
/// Phase 6A.74: Newsletter/News Alert Feature
/// </summary>
public class NewsletterTitle : ValueObject
{
    public const int MaxLength = 200;

    public string Value { get; }

    private NewsletterTitle(string value)
    {
        Value = value;
    }

    /// <summary>
    /// Internal constructor for EF Core hydration.
    /// Bypasses validation to allow loading potentially invalid data from database.
    /// Should only be used by infrastructure layer during entity materialization.
    /// </summary>
    internal static NewsletterTitle FromDatabase(string value)
    {
        // For EF Core hydration, create instance even with empty/null value
        // This prevents "Cannot access value of a failed result" error during query materialization
        return new NewsletterTitle(value ?? string.Empty);
    }

    public static Result<NewsletterTitle> Create(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
            return Result<NewsletterTitle>.Failure("Newsletter title is required");

        var trimmed = title.Trim();

        if (trimmed.Length > MaxLength)
            return Result<NewsletterTitle>.Failure($"Newsletter title cannot exceed {MaxLength} characters");

        return Result<NewsletterTitle>.Success(new NewsletterTitle(trimmed));
    }

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;
}
