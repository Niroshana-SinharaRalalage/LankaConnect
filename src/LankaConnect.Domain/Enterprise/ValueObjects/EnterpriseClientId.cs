using LankaConnect.Domain.Common;

namespace LankaConnect.Domain.Enterprise.ValueObjects;

/// <summary>
/// Enterprise Client identifier for Fortune 500 and large organization contracts
/// </summary>
public class EnterpriseClientId : ValueObject
{
    public Guid Value { get; }

    public EnterpriseClientId(Guid value)
    {
        if (value == Guid.Empty)
            throw new ArgumentException("Enterprise client ID cannot be empty.", nameof(value));
            
        Value = value;
    }

    public EnterpriseClientId() : this(Guid.NewGuid()) { }

    public static implicit operator Guid(EnterpriseClientId clientId) => clientId.Value;
    public static implicit operator EnterpriseClientId(Guid value) => new(value);

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value.ToString();
}