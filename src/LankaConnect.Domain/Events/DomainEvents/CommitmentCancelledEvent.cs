using LankaConnect.Domain.Common;

namespace LankaConnect.Domain.Events.DomainEvents;

/// <summary>
/// Domain event raised when a user cancels their commitment to a sign-up item.
/// Phase 6A.28 Issue 4 Fix: This event bridges the gap between domain encapsulation
/// (private backing fields) and infrastructure persistence (EF Core change tracking).
///
/// When a SignUpCommitment is removed from the private _commitments collection,
/// EF Core cannot detect the deletion. This event allows the infrastructure layer
/// to explicitly mark the entity as Deleted in the change tracker.
///
/// Phase 6A.51+ Fix: Added ItemDescription, Quantity, and SignUpListId to support
/// email notifications without database queries (entity may be deleted by then).
///
/// See ADR-008 for full analysis of why this pattern is necessary.
/// </summary>
public record CommitmentCancelledEvent(
    Guid SignUpItemId,
    Guid CommitmentId,
    Guid UserId,
    Guid SignUpListId,
    string ItemDescription,
    int Quantity) : DomainEvent;
