using System;
using System.Diagnostics.CodeAnalysis;

namespace LankaConnect.BuildingBlocks.Domain;

/// <summary>
/// Base class for aggregate roots in the legacy LankaConnect.Domain assembly.
/// W3E (2026-06-06): rebased from legacy <see cref="BaseEntity"/> onto
/// <see cref="LegacyBaseEntity"/> so it inherits BB.Domain.Entity&lt;Guid&gt; +
/// IAuditable; <c>Version</c> stays here for optimistic concurrency.
/// </summary>
public abstract class AggregateRoot : LegacyBaseEntity
{
    // Id, CreatedAt, CreatedBy, UpdatedAt, UpdatedBy are inherited from LegacyBaseEntity.

    /// <summary>
    /// Version for optimistic concurrency control.
    /// </summary>
    public long Version { get; protected set; }

    // Consult #13 amendment 2026-07-06: [SetsRequiredMembers] chained through from
    // LegacyBaseEntity (mandatory C# 11 spec — CS9039 requires derived ctors of a
    // [SetsRequiredMembers] base to also declare the attribute). Non-generic
    // AggregateRoot IS part of the LegacyBaseEntity transitional bridge lineage
    // (W3E rebase per file header); the exception scope is bridge + immediate
    // legacy descendants. Does NOT extend to Entity<T> or any generic
    // AggregateRoot<T>. Remove with LegacyBaseEntity post-Wave-6.5.
    [SetsRequiredMembers]
    protected AggregateRoot() : base()
    {
        Version = 0;
    }

    [SetsRequiredMembers]
    protected AggregateRoot(Guid id) : base(id)
    {
        Version = 0;
    }

    /// <summary>Validates the aggregate state.</summary>
    public abstract ValidationResult Validate();

    /// <summary>
    /// Bumps the optimistic-concurrency <see cref="Version"/>.
    /// <c>UpdatedAt</c>/<c>UpdatedBy</c> are now interceptor-populated via
    /// <see cref="LankaConnect.BuildingBlocks.Domain.IAuditable"/> — descendants
    /// no longer need to set timestamps; they still call this method when they
    /// want optimistic-concurrency to detect their state change.
    /// </summary>
    protected new void MarkAsUpdated()
    {
        Version++;
    }

    /// <summary>String representation.</summary>
    public override string ToString() => $"{GetType().Name} [Id: {Id}, Version: {Version}]";
}