using LankaConnect.BuildingBlocks.Domain;

namespace LankaConnect.Modules.Forms.Domain.DomainEvents;

/// <summary>
/// Raised when a respondent deletes/cancels their form response.
/// Phase 6A.106: Enable email notifications and cleanup workflow.
/// Architect Review: Approved - allows email handler to send cancellation notification.
/// </summary>
public record FormResponseDeletedEvent(
    Guid FormId,
    Guid ResponseId,
    string? RespondentEmail,
    DateTime OccurredAt) : IDomainEvent;
