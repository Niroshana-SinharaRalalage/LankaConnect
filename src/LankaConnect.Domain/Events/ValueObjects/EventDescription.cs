using LankaConnect.Domain.Common;

namespace LankaConnect.Domain.Events.ValueObjects;

public class EventDescription : ValueObject
{
    public const int MaxLength = 2000;
    
    public string Value { get; }

    private EventDescription(string value)
    {
        Value = value;
    }

    public static Result<EventDescription> Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Result<EventDescription>.Failure("Description is required");

        value = value.Trim();

        if (value.Length > MaxLength)
            return Result<EventDescription>.Failure($"Description cannot exceed {MaxLength} characters");

        return Result<EventDescription>.Success(new EventDescription(value));
    }

    public override string ToString() => Value;

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }
}