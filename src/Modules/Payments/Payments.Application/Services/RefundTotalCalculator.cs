using LankaConnect.Modules.Payments.Domain.Repositories; // W4.4.d.2: 3 repo interfaces moved here
using LankaConnect.Application.Events.Services; // W4.4.c.4: interfaces stay in legacy (4 cross-module consumers; circular ref otherwise)
using LankaConnect.Domain.Events.Enums;
using LankaConnect.Domain.Events.Repositories;
using Microsoft.Extensions.Logging;

namespace LankaConnect.Modules.Payments.Application.Services;

/// <inheritdoc cref="IRefundTotalCalculator"/>
public class RefundTotalCalculator : IRefundTotalCalculator
{
    private readonly IRefundRequestRepository _refundRequestRepository;
    private readonly ILogger<RefundTotalCalculator> _logger;

    public RefundTotalCalculator(
        IRefundRequestRepository refundRequestRepository,
        ILogger<RefundTotalCalculator> logger)
    {
        _refundRequestRepository = refundRequestRepository;
        _logger = logger;
    }

    public async Task<decimal> ComputeAttendeeFacingTotalAsync(
        string stripeRefundId,
        decimal legacyFallbackTotal,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(stripeRefundId))
        {
            _logger.LogDebug(
                "[6A.148.W5.6.A] ComputeAttendeeFacingTotalAsync: empty StripeRefundId — returning legacy total ${Total}",
                legacyFallbackTotal);
            return legacyFallbackTotal;
        }

        try
        {
            var workflowLine = await _refundRequestRepository
                .GetWorkflowLineByStripeRefundIdAsync(stripeRefundId, cancellationToken);

            if (workflowLine is null)
            {
                // Legacy direct-Stripe refund (pre-6A.148 CancelRsvp path). The caller's
                // RefundAmount + AddOnRefundAmount formula IS the right answer for that path —
                // it was correct before workflow existed and remains correct now.
                _logger.LogInformation(
                    "[6A.148.W5.6.A] No workflow line for StripeRefundId={Sri} (legacy direct-Stripe refund); using legacy total ${Total}",
                    stripeRefundId, legacyFallbackTotal);
                return legacyFallbackTotal;
            }

            var rr = await _refundRequestRepository.GetByIdAsync(
                workflowLine.RefundRequestId, cancellationToken);
            if (rr is null)
            {
                // Workflow line found but parent RR couldn't be loaded — shouldn't happen
                // (the line's FK constrains the RR to exist) but defensive.
                _logger.LogWarning(
                    "[6A.148.W5.6.A] Workflow line {LineId} found but RR {RrId} not loaded; falling back to legacy total ${Total}",
                    workflowLine.Id, workflowLine.RefundRequestId, legacyFallbackTotal);
                return legacyFallbackTotal;
            }

            // SUM all Refunded line items. Failed lines (Stripe rejected the refund call)
            // are CORRECTLY excluded — the attendee never saw that money come back, so the
            // email's total shouldn't include it. Approved-but-not-yet-Refunded lines
            // also excluded — see the W5.5.D5 timing guarantee in the interface doc
            // comment.
            var workflowTotal = rr.LineItems
                .Where(li => li.Status == RefundLineItemStatus.Refunded
                             && li.ApprovedAmount is not null)
                .Sum(li => li.ApprovedAmount!.Amount);

            var refundedCount = rr.LineItems.Count(li => li.Status == RefundLineItemStatus.Refunded);
            var totalLines = rr.LineItems.Count;

            _logger.LogInformation(
                "[6A.148.W5.6.A] Workflow refund aggregated total ${Total} from {RefundedCount}/{TotalCount} Refunded line(s) — " +
                "StripeRefundId={Sri} RrId={RrId} LegacyFormulaWouldHaveSaid=${Legacy}",
                workflowTotal, refundedCount, totalLines, stripeRefundId, rr.Id, legacyFallbackTotal);

            return workflowTotal;
        }
        catch (Exception ex)
        {
            // Fail-OPEN: a transient DB blip should NEVER silence the completion email.
            // Caller's legacy total is still a valid number (just understated for workflow
            // refunds). Better to send a partial-correct email than to send nothing.
            _logger.LogError(ex,
                "[6A.148.W5.6.A] ComputeAttendeeFacingTotalAsync EXCEPTION for StripeRefundId={Sri}; falling back to legacy total ${Total}",
                stripeRefundId, legacyFallbackTotal);
            return legacyFallbackTotal;
        }
    }
}
