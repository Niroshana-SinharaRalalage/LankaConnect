using LankaConnect.Products.LankaEvents.Contracts.LegacyPromotions; // W4.4.d.2: 3 repo interfaces moved here
using LankaConnect.SharedKernel.Identity;
using System.Diagnostics;
using LankaConnect.BuildingBlocks.Application.Common.Interfaces;
using LankaConnect.BuildingBlocks.Domain;
using LankaConnect.Products.LankaEvents.Domain;
using LankaConnect.Modules.Identity.Domain.DomainEvents;
using LankaConnect.Products.LankaEvents.Domain.Enums;
using LankaConnect.Products.LankaEvents.Domain.Repositories;
using Microsoft.Extensions.Logging;
using Serilog.Context;
namespace LankaConnect.Modules.Payments.Application.Commands.RefundRequests;

/// <summary>
/// Phase 6A.148: Attendee withdraws their own Pending refund request. Returns the
/// Registration to Confirmed. Distinct from the legacy 6A.91 withdraw handler which
/// operates on Registration's column-based refund state (kept behind feature flag).
/// </summary>
public class WithdrawRefundRequestV2CommandHandler : ICommandHandler<WithdrawRefundRequestV2Command>
{
    private readonly IRegistrationRepository _registrationRepo;
    private readonly IRefundRequestRepository _refundRepo;
    private readonly IUnitOfWork _uow;
    private readonly ILogger<WithdrawRefundRequestV2CommandHandler> _logger;

    public WithdrawRefundRequestV2CommandHandler(
        IRegistrationRepository registrationRepo,
        IRefundRequestRepository refundRepo,
        IUnitOfWork uow,
        ILogger<WithdrawRefundRequestV2CommandHandler> logger)
    {
        _registrationRepo = registrationRepo;
        _refundRepo = refundRepo;
        _uow = uow;
        _logger = logger;
    }

    public async Task<Result> Handle(
        WithdrawRefundRequestV2Command request, CancellationToken cancellationToken)
    {
        using (LogContext.PushProperty("Operation", "WithdrawRefundRequestV2"))
        using (LogContext.PushProperty("EventId", request.EventId))
        using (LogContext.PushProperty("CallerUserId", request.CallerUserId))
        {
            var sw = Stopwatch.StartNew();
            _logger.LogInformation(
                "[6A.148] WithdrawRefundRequestV2 START: EventId={EventId} UserId={UserId}",
                request.EventId, request.CallerUserId);

            try
            {
                // Find the attendee's most recent request for this event.
                var current = await _refundRepo.GetMyMostRecentForEventAsync(
                    request.EventId, request.CallerUserId, cancellationToken);
                if (current is null || current.Status != RefundRequestStatus.Pending)
                {
                    sw.Stop();
                    _logger.LogWarning(
                        "[6A.148] WithdrawRefundRequestV2 not-applicable: no Pending request for EventId={EventId} UserId={UserId} CurrentStatus={Status}",
                        request.EventId, request.CallerUserId, current?.Status.ToString() ?? "none");
                    return Result.Failure("No pending refund request to withdraw");
                }

                // Re-load tracked copy via parent registration so xmin + state change persist.
                var tracked = await _registrationRepo.GetByIdAsync(current.RegistrationId, cancellationToken);
                if (tracked is null)
                {
                    sw.Stop();
                    _logger.LogError(
                        "[6A.148] WithdrawRefundRequestV2 FAILED: tracked load null for RegId={RegId}",
                        current.RegistrationId);
                    return Result.Failure("Failed to load registration for update");
                }

                // Find the matching tracked refund request inside the registration aggregate.
                var trackedRequest = tracked.RefundRequests.FirstOrDefault(r => r.Id == current.Id);
                if (trackedRequest is null)
                {
                    sw.Stop();
                    _logger.LogError(
                        "[6A.148] WithdrawRefundRequestV2 FAILED: tracked refund request {RrId} not loaded via aggregate; check Include() in repository",
                        current.Id);
                    return Result.Failure("Refund request not loaded with registration");
                }

                // W4.D13.5: invoke via Registration aggregate so the RefundRequestWithdrawnEvent
                // is re-raised with EventId populated (the F1/G3 fix). Empirically proven during
                // W4 API smoke test T7 — previously the handler ran but exited silently with
                // "event not found EventId=00000000-0000-0000-0000-000000000000" because the
                // event raised on the child carried EventId=Guid.Empty.
                var withdrawResult = tracked.WithdrawRefundRequestV2(
                    refundRequestId: current.Id,
                    byUserId: request.CallerUserId);
                if (withdrawResult.IsFailure)
                {
                    sw.Stop();
                    _logger.LogWarning(
                        "[6A.148] WithdrawRefundRequestV2 Validation FAILED: RrId={RrId} Error={Error}",
                        current.Id, withdrawResult.Error);
                    return Result.Failure(withdrawResult.Error);
                }

                // Phase 6A.148 post-rework: cancel + refund are decoupled. Withdraw
                // marks the RefundRequest as Withdrawn but does NOT mutate the registration.
                // - Standalone-refund path: registration was Confirmed and stays Confirmed.
                // - Cancel+refund compound path: registration was Cancelled and stays Cancelled
                //   per product decision Q3 ("withdraw does NOT restore the seat; cancellation
                //   is final and no refund is issued"). The FE displays a clear warning before
                //   the user confirms the withdraw action.

                _registrationRepo.Update(tracked);
                await _uow.CommitAsync(cancellationToken);

                sw.Stop();
                _logger.LogInformation(
                    "[6A.148] WithdrawRefundRequestV2 COMPLETE: RegId={RegId} RrId={RrId} Duration={ElapsedMs}ms",
                    tracked.Id, current.Id, sw.ElapsedMilliseconds);
                return Result.Success();
            }
            catch (Exception ex)
            {
                sw.Stop();
                _logger.LogError(ex,
                    "[6A.148] WithdrawRefundRequestV2 EXCEPTION: EventId={EventId} UserId={UserId} Duration={ElapsedMs}ms",
                    request.EventId, request.CallerUserId, sw.ElapsedMilliseconds);
                throw;
            }
        }
    }
}
