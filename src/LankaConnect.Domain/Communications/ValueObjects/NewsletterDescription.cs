using LankaConnect.Domain.Common;

namespace LankaConnect.Domain.Communications.ValueObjects;

/// <summary>
/// Newsletter description value object
/// Phase 6A.74: Newsletter description with validation
/// </summary>
public class NewsletterDescription : ValueObject
{
    public string Value { get; }
    private const int MaxLength = 5000;

    private NewsletterDescription(string value)
    {
        Value = value;
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
