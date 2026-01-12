using LankaConnect.Domain.Common;

namespace LankaConnect.Domain.Communications.ValueObjects;

/// <summary>
/// Newsletter title value object
/// Phase 6A.74: Newsletter title with validation
/// </summary>
public class NewsletterTitle : ValueObject
{
    public string Value { get; }
    private const int MaxLength = 200;

    private NewsletterTitle(string value)
    {
        Value = value;
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
