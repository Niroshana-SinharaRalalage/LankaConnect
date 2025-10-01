using LankaConnect.Domain.Common;

namespace LankaConnect.Domain.Events.ValueObjects;

public class EventTitle : ValueObject
{
    public const int MaxLength = 200;
    
    public string Value { get; }

    private EventTitle(string value)
    {
        Value = value;
    }

    public static Result<EventTitle> Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Result<EventTitle>.Failure("Title is required");

        value = value.Trim();

        if (value.Length > MaxLength)
            return Result<EventTitle>.Failure($"Title cannot exceed {MaxLength} characters");

        return Result<EventTitle>.Success(new EventTitle(value));
    }

    public override string ToString() => Value;

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }
}