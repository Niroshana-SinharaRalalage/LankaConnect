namespace LankaConnect.Products.LankaEvents.Domain.Services;

/// <summary>
/// Phase 7F-B (architect-approved 2026-04-30): policy carried into
/// <see cref="Event.ConvertRegistrationMode"/>. Captures the organiser's intent + the
/// cross-aggregate context the domain method needs (registrations with pending additions
/// are owned by a separate aggregate, so the handler queries them upfront and passes the
/// IDs in via this policy).
/// </summary>
public sealed class ConversionPolicy
{
    /// <summary>The user triggering the conversion. Recorded in the audit row.</summary>
    public required Guid OrganiserId { get; init; }

    /// <summary>
    /// When true, compute the <see cref="ConversionReport"/> but do NOT mutate the aggregate.
    /// Architect plan §3 7F-B.5: drives the UI's diff-preview confirmation dialog.
    /// </summary>
    public bool DryRun { get; init; }

    /// <summary>
    /// Architect Q8: registration IDs that have a pending <c>RegistrationAddition</c>
    /// (status Pending or PaymentCompleted-not-yet-Merged). The handler queries this set
    /// upfront and passes it in; the domain rejects matching registrations with reason
    /// <c>PendingAdditionMustResolveFirst</c>.
    /// </summary>
    public IReadOnlySet<Guid> RegistrationIdsWithPendingAdditions { get; init; } = new HashSet<Guid>();

    /// <summary>
    /// Architect Q7: hard-cap on registrations migrated in a single call. Default 500.
    /// Beyond this, the conversion fails fast with batching guidance — prevents a single
    /// transaction from blowing past the request timeout on a large event.
    /// </summary>
    public int BatchCap { get; init; } = 500;
}
