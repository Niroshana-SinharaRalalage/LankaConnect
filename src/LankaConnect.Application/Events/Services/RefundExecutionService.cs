using System.Diagnostics;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events.Enums;
using LankaConnect.Domain.Events.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace LankaConnect.Application.Events.Services;

/// <summary>
/// Phase 6A.148: Dispatches Stripe refund calls for an approved RefundRequest.
///
/// W5.D2 restructure: the per-line Stripe + DB save logic moved to
/// <see cref="IRefundLineDispatcher"/>. This service is now the orchestrator —
/// it loads the request, snapshots the list of Approved line IDs, delegates each
/// to the line dispatcher (which runs in its own fresh DI scope), then transitions
/// the request itself in a separate fresh scope.
///
/// Architect-mandated design (closes W5.D7 stuck-refund root cause): no single
/// DbContext spans both the parent Registration aggregate AND per-line dispatches.
/// Per-line saves only mutate <c>refund_request_line_items</c> rows, which carry
/// no xmin token, so concurrent Registration mutations (e.g. an attendee Cancel
/// flow) cannot roll back successful Stripe refunds.
///
/// Idempotency:
/// - Stripe-side: W5.D1 per-line idempotency key
/// - DB-side: line + RR state-machine guards refuse Refunded → Refunded etc.
/// - Re-dispatch from W5.D6 reconciler is automatically safe.
/// </summary>
public class RefundExecutionService : IRefundExecutionService
{
    private readonly IRefundRequestRepository _refundRepo;
    private readonly IRefundLineDispatcher _lineDispatcher;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<RefundExecutionService> _logger;

    public RefundExecutionService(
        IRefundRequestRepository refundRepo,
        IRefundLineDispatcher lineDispatcher,
        IServiceScopeFactory scopeFactory,
        ILogger<RefundExecutionService> logger)
    {
        _refundRepo = refundRepo;
        _lineDispatcher = lineDispatcher;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task<Result> DispatchAsync(
        Guid refundRequestId, CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "RefundExecution.Dispatch"))
        using (LogContext.PushProperty("RefundRequestId", refundRequestId))
        {
            var sw = Stopwatch.StartNew();
            _logger.LogInformation(
                "[6A.148 EXEC] DispatchAsync START: RrId={RrId}", refundRequestId);

            try
            {
                // Outer read of the request — snapshots which lines to dispatch.
                var req = await _refundRepo.GetByIdAsync(refundRequestId, cancellationToken);
                if (req is null)
                {
                    _logger.LogWarning("[6A.148 EXEC] RrId={RrId} not found", refundRequestId);
                    return Result.NotFound("Refund request not found");
                }

                if (req.Status != RefundRequestStatus.Approved &&
                    req.Status != RefundRequestStatus.Processing)
                {
                    _logger.LogInformation(
                        "[6A.148 EXEC] RrId={RrId} not in Approved/Processing (Status={Status}); skipping",
                        refundRequestId, req.Status);
                    return Result.Success();
                }

                // Capture line IDs by snapshot — each dispatcher call may mutate state in its
                // own scope, but we drive iteration off this initial Approved-set list.
                var registrationId = req.RegistrationId;
                var lineIdsToDispatch = req.LineItems
                    .Where(l => l.Status == RefundLineItemStatus.Approved
                                && l.ApprovedAmount is not null
                                && l.ApprovedAmount.Amount > 0)
                    .Select(l => l.Id)
                    .ToList();

                _logger.LogInformation(
                    "[6A.148 EXEC] Dispatching {Count} line(s) for RrId={RrId} RegistrationId={RegId}",
                    lineIdsToDispatch.Count, refundRequestId, registrationId);

                var dispatchedAny = false;
                var firstError = (string?)null;
                foreach (var lineId in lineIdsToDispatch)
                {
                    try
                    {
                        var dispatchResult = await _lineDispatcher.DispatchAsync(
                            lineId, registrationId, cancellationToken);
                        if (dispatchResult.IsSuccess)
                            dispatchedAny = true;
                        else
                            firstError ??= dispatchResult.Error;
                    }
                    catch (Exception ex)
                    {
                        // Line-dispatcher exceptions are isolated by the per-line scope.
                        // Continue with remaining lines; reconciler will retry the failed one.
                        _logger.LogError(ex,
                            "[6A.148 EXEC] Line dispatch threw for LineId={LineId} RrId={RrId}; " +
                            "continuing with remaining lines (reconciler will retry)",
                            lineId, refundRequestId);
                        firstError ??= ex.Message;
                    }
                }

                // Request-level transition in a SEPARATE fresh scope. The per-line dispatches
                // already committed their line state — we reload the RR here to see those
                // committed states and decide whether to flip Approved → Processing and/or
                // Processing → Completed.
                if (dispatchedAny)
                {
                    await TransitionRequestInOwnScopeAsync(refundRequestId, cancellationToken);
                }

                sw.Stop();
                _logger.LogInformation(
                    "[6A.148 EXEC] DispatchAsync COMPLETE: RrId={RrId} LinesDispatched={Dispatched} " +
                    "FirstError={Error} Duration={ElapsedMs}ms",
                    refundRequestId, dispatchedAny, firstError ?? "none", sw.ElapsedMilliseconds);

                if (!dispatchedAny && firstError is not null)
                    return Result.Failure(firstError);

                return Result.Success();
            }
            catch (Exception ex)
            {
                sw.Stop();
                _logger.LogError(ex,
                    "[6A.148 EXEC] DispatchAsync EXCEPTION: RrId={RrId} Duration={ElapsedMs}ms",
                    refundRequestId, sw.ElapsedMilliseconds);
                throw;
            }
        }
    }

    /// <summary>
    /// W5.D3: Open a fresh DI scope to handle the request-level state transition.
    /// Reads the committed line states (from the per-line dispatcher's saves) and
    /// transitions the RR Approved → Processing → Completed as appropriate. The
    /// xmin token on refund_requests is fine here — concurrent Approve/Reject on
    /// the same RR doesn't happen in practice (organizer UI prevents it), and if
    /// it ever does, the loser's CommitAsync throws DbUpdateConcurrencyException
    /// which is propagated to the caller (which is the post-commit dispatch hook
    /// that already swallows exceptions safely).
    /// </summary>
    private async Task TransitionRequestInOwnScopeAsync(
        Guid refundRequestId, CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var sp = scope.ServiceProvider;
            var freshRefundRepo = sp.GetRequiredService<IRefundRequestRepository>();
            var freshUow = sp.GetRequiredService<IUnitOfWork>();

            var freshReq = await freshRefundRepo.GetByIdAsync(refundRequestId, cancellationToken);
            if (freshReq is null)
            {
                _logger.LogWarning(
                    "[6A.148 EXEC.D3] Fresh load of RrId={RrId} returned null; skipping request-level transition",
                    refundRequestId);
                return;
            }

            if (freshReq.Status == RefundRequestStatus.Approved)
            {
                var beginResult = freshReq.BeginProcessing();
                if (beginResult.IsFailure)
                {
                    _logger.LogWarning(
                        "[6A.148 EXEC.D3] BeginProcessing failed for RrId={RrId}: {Error}",
                        refundRequestId, beginResult.Error);
                }
            }

            var allLinesTerminal = freshReq.LineItems.All(l =>
                l.Status == RefundLineItemStatus.Refunded ||
                l.Status == RefundLineItemStatus.Failed ||
                l.Status == RefundLineItemStatus.Rejected);
            if (allLinesTerminal)
            {
                freshReq.MarkCompletedIfAllSettled();
            }

            await freshUow.CommitAsync(cancellationToken);

            _logger.LogInformation(
                "[6A.148 EXEC.D3] Request-level transition committed for RrId={RrId} " +
                "FinalStatus={Status} AllLinesTerminal={AllTerminal}",
                refundRequestId, freshReq.Status, allLinesTerminal);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[6A.148 EXEC.D3] Request-level transition EXCEPTION for RrId={RrId}; " +
                "lines are already committed individually, reconciler will close out the RR",
                refundRequestId);
            // Swallow — the per-line state is durable; reconciler picks this up.
        }
    }
}
