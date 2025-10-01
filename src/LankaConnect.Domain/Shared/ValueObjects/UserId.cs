using LankaConnect.Domain.Common;

namespace LankaConnect.Domain.Shared.ValueObjects;

/// <summary>
/// Value object representing a unique user identifier
/// </summary>
public sealed class UserId : ValueObject
{
    public Guid Value { get; }

    private UserId(Guid value)
    {
        Value = value;
    }

    /// <summary>
    /// Creates a new UserId with validation
    /// </summary>
    public static Result<UserId> Create(Guid value)
    {
        if (value == Guid.Empty)
            return Result<UserId>.Failure("User ID cannot be empty");

        return Result<UserId>.Success(new UserId(value));
    }

    /// <summary>
    /// Creates a new unique UserId
    /// </summary>
    public static UserId NewId()
    {
        return new UserId(Guid.NewGuid());
    }

    /// <summary>
    /// Implicit conversion from Guid
    /// </summary>
    public static implicit operator Guid(UserId userId)
    {
        return userId.Value;
    }

    public override string ToString() => Value.ToString();

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }
}