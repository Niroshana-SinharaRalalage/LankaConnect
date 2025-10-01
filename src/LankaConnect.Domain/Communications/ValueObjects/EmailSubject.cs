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