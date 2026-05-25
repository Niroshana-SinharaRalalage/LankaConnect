using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using LankaConnect.Application.Events.Services;

namespace LankaConnect.API.Controllers;

/// <summary>
/// Phase 7G — manual trigger for the refund-reconciliation safety net.
///
/// The reconciler also runs automatically every 5 minutes via
/// <c>RefundReconciliationBackgroundService</c>; this endpoint exists so an
/// operator (admin or event organiser) can clear backlog immediately rather
/// than waiting for the next pass — useful during incident response and for
/// post-deploy verification.
///
/// The work itself is identical to the background pass: queries Stripe for
/// each stuck refund's canonical status and completes the DB transition for
/// any refund Stripe reports as "succeeded". Idempotent.
/// </summary>
[ApiController]
[Route("api/admin/refund-reconciliation")]
public class RefundReconciliationController : ControllerBase
{
    private readonly IRefundReconciliationService _reconciliationService;
    private readonly ILogger<RefundReconciliationController> _logger;

    public RefundReconciliationController(
        IRefundReconciliationService reconciliationService,
        ILogger<RefundReconciliationController> logger)
    {
        _reconciliationService = reconciliationService;
        _logger = logger;
    }

    /// <summary>
    /// Triggers a one-shot reconciliation pass and returns the summary
    /// of what was found / fixed / left pending. Authorized for Admin and
    /// EventOrganizer roles — both legitimately need to clear stuck refund
    /// rows during incident response.
    /// </summary>
    /// <param name="batchSize">
    /// Optional max number of stuck registrations to process this run.
    /// If omitted, the configured default (50) applies.
    /// </param>
    /// <param name="ageThresholdMinutes">
    /// Optional grace-period override for this run. Production passes wait
    /// 10 min so the primary webhook gets a fair chance first; operators
    /// triggering manually during incident response can pass <c>0</c> to
    /// reconcile freshly-stuck rows immediately. Negative values clamp to 0.
    /// </param>
    /// <param name="cancellationToken">Cooperative cancellation token.</param>
    [HttpPost("run")]
    [Authorize(Roles = "Admin,AdminManager,EventOrganizer")]
    [ProducesResponseType(typeof(RefundReconciliationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> RunReconciliation(
        [FromQuery] int? batchSize = null,
        [FromQuery] int? ageThresholdMinutes = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "[Phase 7G] Manual reconciliation trigger - User={User}, BatchSize={BatchSize}, AgeThresholdMinutes={AgeThresholdMinutes}",
            User?.Identity?.Name ?? "(anonymous)",
            batchSize?.ToString() ?? "default",
            ageThresholdMinutes?.ToString() ?? "default");

        var result = await _reconciliationService.ReconcileStuckRefundsAsync(
            batchSize, ageThresholdMinutes, cancellationToken);

        // Phase 6A.148 (architect F11): also reconcile stuck Approved refund requests
        // — rows where the approve transaction committed but the post-commit Stripe
        // dispatch never advanced any line item (process crash, transient Stripe error).
        try
        {
            var approvedResult = await _reconciliationService
                .ReconcileStuckApprovedRefundRequestsAsync(ageThresholdMinutes, cancellationToken);
            if (approvedResult.IsSuccess)
                _logger.LogInformation(
                    "[6A.148] Stuck-Approved reconciliation re-dispatched {Count} requests",
                    approvedResult.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[6A.148] Stuck-Approved reconciliation pass failed (non-fatal — legacy reconciler still ran)");
        }

        if (result.IsFailure)
        {
            _logger.LogError(
                "[Phase 7G] Manual reconciliation FAILED - User={User}, Error={Error}",
                User?.Identity?.Name ?? "(anonymous)", result.Error);
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Title = "Reconciliation Failed",
                Status = StatusCodes.Status500InternalServerError,
                Detail = result.Error,
            });
        }

        var r = result.Value;
        _logger.LogInformation(
            "[Phase 7G] Manual reconciliation COMPLETE - User={User}, Scanned={Scanned}, Reconciled={Reconciled}",
            User?.Identity?.Name ?? "(anonymous)", r.ScannedCount, r.ReconciledCount);

        return Ok(new RefundReconciliationResponse(
            ScannedCount: r.ScannedCount,
            ReconciledCount: r.ReconciledCount,
            StillPendingCount: r.StillPendingCount,
            FailedAtStripeCount: r.FailedAtStripeCount,
            MissingRefundIdCount: r.MissingRefundIdCount,
            StripeLookupFailedCount: r.StripeLookupFailedCount,
            Warnings: r.Warnings));
    }
}

/// <summary>
/// Public-facing response shape for the manual reconciliation trigger.
/// Mirrors <see cref="RefundReconciliationResult"/> but lives in the API
/// project so OpenAPI surfaces it as a stable contract.
/// </summary>
public record RefundReconciliationResponse(
    int ScannedCount,
    int ReconciledCount,
    int StillPendingCount,
    int FailedAtStripeCount,
    int MissingRefundIdCount,
    int StripeLookupFailedCount,
    IReadOnlyList<string> Warnings);
