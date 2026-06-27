using LankaConnect.Domain.Common;

namespace LankaConnect.Domain.Events.ValueObjects;

public class PassName : ValueObject
{
    public string Value { get; }

    private PassName(string value)
    {
        Value = value;
    }

    public static Result<PassName> Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Result<PassName>.Failure("Pass name cannot be empty");

        if (value.Length > 100)
            return Result<PassName>.Failure("Pass name cannot exceed 100 characters");

        return Result<PassName>.Success(new PassName(value.Trim()));
    }

    public override string ToString() => Value;

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }

    public static implicit operator string(PassName passName) => passName.Value;
}
