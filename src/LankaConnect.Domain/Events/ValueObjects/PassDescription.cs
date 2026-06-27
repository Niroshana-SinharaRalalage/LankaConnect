using LankaConnect.Domain.Common;

namespace LankaConnect.Domain.Events.ValueObjects;

public class PassDescription : ValueObject
{
    public string Value { get; }

    private PassDescription(string value)
    {
        Value = value;
    }

    public static Result<PassDescription> Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Result<PassDescription>.Failure("Pass description cannot be empty");

        if (value.Length > 500)
            return Result<PassDescription>.Failure("Pass description cannot exceed 500 characters");

        return Result<PassDescription>.Success(new PassDescription(value.Trim()));
    }

    public override string ToString() => Value;

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }

    public static implicit operator string(PassDescription description) => description.Value;
}
