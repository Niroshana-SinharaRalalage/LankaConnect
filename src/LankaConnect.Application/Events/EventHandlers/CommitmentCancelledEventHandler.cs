using System.Diagnostics;
using LankaConnect.Application.Common;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Events.DomainEvents;
using MediatR;
using Microsoft.Extensions.Logging;
using Serilog.Context;

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

        using (LogContext.PushProperty("Operation", "CommitmentCancelled"))
        using (LogContext.PushProperty("EntityType", "SignUpCommitment"))
        using (LogContext.PushProperty("CommitmentId", domainEvent.CommitmentId))
        using (LogContext.PushProperty("UserId", domainEvent.UserId))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(
                "CommitmentCancelled START: CommitmentId={CommitmentId}, UserId={UserId}, SignUpItemId={SignUpItemId}",
                domainEvent.CommitmentId, domainEvent.UserId, domainEvent.SignUpItemId);

            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                // Find the commitment entity in EF Core's change tracker or database
                // Using FindAsync is optimal because it checks local cache first
                _logger.LogInformation(
                    "CommitmentCancelled: Searching for commitment entity - CommitmentId={CommitmentId}",
                    domainEvent.CommitmentId);

                var commitment = await _context.SignUpCommitments
                    .FindAsync(new object[] { domainEvent.CommitmentId }, cancellationToken);

                if (commitment != null)
                {
                    // Explicitly mark as deleted in EF Core change tracker
                    // This is what ChangeTracker.DetectChanges() cannot do for private collections
                    _context.SignUpCommitments.Remove(commitment);

                    _logger.LogInformation(
                        "CommitmentCancelled: Commitment marked as deleted - CommitmentId={CommitmentId}, UserId={UserId}",
                        domainEvent.CommitmentId, domainEvent.UserId);
                }
                else
                {
                    // This shouldn't happen in normal flow, but log as warning for diagnostics
                    _logger.LogWarning(
                        "CommitmentCancelled: Commitment not found in database - CommitmentId={CommitmentId}, UserId={UserId}",
                        domainEvent.CommitmentId, domainEvent.UserId);
                }

                stopwatch.Stop();

                _logger.LogInformation(
                    "CommitmentCancelled COMPLETE: CommitmentId={CommitmentId}, Found={Found}, Duration={ElapsedMs}ms",
                    domainEvent.CommitmentId, commitment != null, stopwatch.ElapsedMilliseconds);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                stopwatch.Stop();

                _logger.LogWarning(
                    "CommitmentCancelled CANCELED: Operation was canceled - CommitmentId={CommitmentId}, Duration={ElapsedMs}ms",
                    domainEvent.CommitmentId, stopwatch.ElapsedMilliseconds);

                throw;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _logger.LogError(ex,
                    "CommitmentCancelled FAILED: Exception occurred - CommitmentId={CommitmentId}, Duration={ElapsedMs}ms, Error={ErrorMessage}",
                    domainEvent.CommitmentId, stopwatch.ElapsedMilliseconds, ex.Message);

                throw;
            }
        }
    }
}
