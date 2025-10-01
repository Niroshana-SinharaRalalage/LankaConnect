using LankaConnect.Domain.Common;

namespace LankaConnect.Domain.Community.ValueObjects;

public class PostContent : ValueObject
{
    public const int MaxLength = 10000;
    
    public string Value { get; }

    private PostContent(string value)
    {
        Value = value;
    }

    public static Result<PostContent> Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Result<PostContent>.Failure("Content is required");

        value = value.Trim();

        if (value.Length > MaxLength)
            return Result<PostContent>.Failure($"Content cannot exceed {MaxLength} characters");

        return Result<PostContent>.Success(new PostContent(value));
    }

    public override string ToString() => Value;

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }
}