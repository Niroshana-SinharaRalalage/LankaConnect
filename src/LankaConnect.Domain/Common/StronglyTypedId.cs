using System;

namespace LankaConnect.Domain.Common;

/// <summary>
/// Base record for strongly typed identifiers
/// </summary>
/// <typeparam name="T">The underlying type of the identifier</typeparam>
public abstract record StronglyTypedId<T>(T Value) where T : notnull
{
    public override string ToString() => Value.ToString() ?? string.Empty;
    
    public static implicit operator T(StronglyTypedId<T> stronglyTypedId)
        => stronglyTypedId.Value;
}

/// <summary>
/// Base record for Guid-based strongly typed identifiers
/// </summary>
public abstract record StronglyTypedId : StronglyTypedId<Guid>
{
    protected StronglyTypedId(Guid value) : base(value) { }
    
    protected StronglyTypedId() : base(Guid.NewGuid()) { }
}