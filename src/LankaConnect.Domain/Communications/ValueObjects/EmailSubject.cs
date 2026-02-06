using LankaConnect.Domain.Common;

namespace LankaConnect.Domain.Communications.ValueObjects;

public class EmailSubject : ValueObject
{
    public const int MaxLength = 200;

    public string Value { get; }

    private EmailSubject(string value)
    {
        Value = value;
    }

    /// <summary>
    /// Phase 6A.41: Internal constructor for EF Core hydration.
    /// Bypasses validation to allow loading potentially invalid data from database.
    /// Should only be used by infrastructure layer during entity materialization.
    /// </summary>
    internal static EmailSubject FromDatabase(string value)
    {
        // For EF Core hydration, create instance even with empty/null value
        // This prevents "Cannot access value of a failed result" error during query materialization
        return new EmailSubject(value ?? string.Empty);
    }

    public static Result<EmailSubject> Create(string subject)
    {
        if (string.IsNullOrWhiteSpace(subject))
            return Result<EmailSubject>.Failure("Email subject is required");

        var trimmed = subject.Trim();

        if (trimmed.Length > MaxLength)
            return Result<EmailSubject>.Failure($"Email subject cannot exceed {MaxLength} characters");

        return Result<EmailSubject>.Success(new EmailSubject(trimmed));
    }

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;
}