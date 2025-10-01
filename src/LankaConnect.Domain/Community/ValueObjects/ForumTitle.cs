using LankaConnect.Domain.Common;

namespace LankaConnect.Domain.Community.ValueObjects;

public class ForumTitle : ValueObject
{
    public const int MaxLength = 100;
    
    public string Value { get; }

    private ForumTitle(string value)
    {
        Value = value;
    }

    public static Result<ForumTitle> Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Result<ForumTitle>.Failure("Title is required");

        value = value.Trim();

        if (value.Length > MaxLength)
            return Result<ForumTitle>.Failure($"Title cannot exceed {MaxLength} characters");

        return Result<ForumTitle>.Success(new ForumTitle(value));
    }

    public override string ToString() => Value;

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }
}