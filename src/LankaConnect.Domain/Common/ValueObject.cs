namespace LankaConnect.Domain.Common;

public abstract class ValueObject : IEquatable<ValueObject>
{
    public abstract IEnumerable<object> GetEqualityComponents();

    public override bool Equals(object? obj)
    {
        return obj is ValueObject valueObject && Equals(valueObject);
    }

    public bool Equals(ValueObject? other)
    {
        if (other is null || other.GetType() != GetType())
            return false;

        return GetEqualityComponents().SequenceEqual(other.GetEqualityComponents());
    }

    public override int GetHashCode()
    {
        var hashCodes = GetEqualityComponents()
            .Select(x => x?.GetHashCode() ?? 0)
            .ToArray();
            
        return hashCodes.Length > 0 
            ? hashCodes.Aggregate((x, y) => x ^ y)
            : 0;
    }

    public static bool operator ==(ValueObject? left, ValueObject? right)
    {
        return EqualityComparer<ValueObject>.Default.Equals(left, right);
    }

    public static bool operator !=(ValueObject? left, ValueObject? right)
    {
        return !(left == right);
    }
}