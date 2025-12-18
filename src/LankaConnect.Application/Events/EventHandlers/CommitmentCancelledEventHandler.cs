using LankaConnect.Application.Common;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Events.DomainEvents;
using MediatR;
using Microsoft.Extensions.Logging;

namespace LankaConnect.Application.Events.EventHandlers;

/// <summary>
/// Phase 6A.28 Issue 4 Fix: Handles CommitmentCancelledEvent by explicitly marking
/// the SignUpCommitment entity as Deleted in EF Core's change tracker.
///
/// This handler bridges the gap between domain encapsulation (private backing fields)
/// and infrastructure persistence (EF Core change tracking).
///
/// Why This Is Necessary:
/// - Domain uses private List&lt;SignUpCommitment&gt; _commitments for encapsulation
/// - When domain removes from _commitments, EF Core cannot detect the change
/// - ChangeTracker.DetectChanges() only works with public navigation properties
/// - Without explicit deletion, entities remain in Unchanged state and aren't deleted
///
/// See ADR-008 for detailed analysis of the root cause and solution design.
/// </summary>
public class CommitmentCancelledEventHandler
    : INotificationHandler<DomainEventNotification<CommitmentCancelledEvent>>
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<CommitmentCancelledEventHandler> _logger;

    public CommitmentCancelledEventHandler(
        IApplicationDbContext context,
        ILogger<CommitmentCancelledEventHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task Handle(
        DomainEventNotification<CommitmentCancelledEvent> notification,
        CancellationToken cancellationToken)
    {
        var domainEvent = notification.DomainEvent;

        _logger.LogInformation(
            "[CommitmentCancelled] Handling deletion for CommitmentId={CommitmentId}, UserId={UserId}, SignUpItemId={SignUpItemId}",
            domainEvent.CommitmentId, domainEvent.UserId, domainEvent.SignUpItemId);

        // Find the commitment entity in EF Core's change tracker or database
        // Using FindAsync is optimal because it checks local cache first
        var commitment = await _context.SignUpCommitments
            .FindAsync(new object[] { domainEvent.CommitmentId }, cancellationToken);

        if (commitment != null)
        {
            // Explicitly mark as deleted in EF Core change tracker
            // This is what ChangeTracker.DetectChanges() cannot do for private collections
            _context.SignUpCommitments.Remove(commitment);

            _logger.LogInformation(
                "[CommitmentCancelled] Marked commitment {CommitmentId} as deleted (UserId={UserId})",
                domainEvent.CommitmentId, domainEvent.UserId);
        }
        else
        {
            // This shouldn't happen in normal flow, but log as warning for diagnostics
            _logger.LogWarning(
                "[CommitmentCancelled] Commitment {CommitmentId} not found in database (UserId={UserId})",
                domainEvent.CommitmentId, domainEvent.UserId);
        }
    }
}
