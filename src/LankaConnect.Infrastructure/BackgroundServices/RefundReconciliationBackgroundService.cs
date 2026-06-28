using LankaConnect.Products.LankaEvents.Application.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LankaConnect.Infrastructure.BackgroundServices;

/// <summary>
/// Phase 7G — the durable safety net for the "stuck refund" bug class.
/// Runs every <see cref="RefundReconciliationSettings.IntervalMinutes"/>,
/// hands work off to <see cref="IRefundReconciliationService"/>, and logs
/// the per-pass summary. Exception-resilient: a single failed pass doesn't
/// crash the host or stop future passes.
/// Mirrors the pattern of <see cref="SeatHoldCleanupService"/>.
/// </summary>
public class RefundReconciliationBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly RefundReconciliationSettings _settings;
    private readonly ILogger<RefundReconciliationBackgroundService> _logger;

    public RefundReconciliationBackgroundService(
        IServiceScopeFactory scopeFactory,
        IOptions<RefundReconciliationSettings> settings,
        ILogger<RefundReconciliationBackgroundService> logger)
    {
        _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
        _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_settings.Enabled)
        {
            _logger.LogInformation(
                "[Phase 7G] RefundReconciliationBackgroundService is disabled via settings — skipping");
            return;
        }

        var interval = TimeSpan.FromMinutes(Math.Max(1, _settings.IntervalMinutes));
        var initialDelay = TimeSpan.FromSeconds(Math.Max(0, _settings.InitialDelaySeconds));

        _logger.LogInformation(
            "[Phase 7G] RefundReconciliationBackgroundService starting — InitialDelay={InitialDelay}s, Interval={Interval}, BatchSize={BatchSize}, AgeThresholdMinutes={AgeThresholdMinutes}",
            initialDelay.TotalSeconds, interval, _settings.BatchSize, _settings.AgeThresholdMinutes);

        try
        {
            if (initialDelay > TimeSpan.Zero)
            {
                await Task.Delay(initialDelay, stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            return; // shutting down before we ever ran
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ReconcileOnceAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // Expected during shutdown
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "[Phase 7G] RefundReconciliationBackgroundService pass FAULTED — will retry next interval");
            }

            try
            {
                await Task.Delay(interval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }

        _logger.LogInformation("[Phase 7G] RefundReconciliationBackgroundService stopped");
    }

    private async Task ReconcileOnceAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var reconciler = scope.ServiceProvider.GetRequiredService<IRefundReconciliationService>();

        var result = await reconciler.ReconcileStuckRefundsAsync(
            batchSize: _settings.BatchSize,
            ageThresholdMinutes: _settings.AgeThresholdMinutes,
            cancellationToken: cancellationToken);

        if (result.IsFailure)
        {
            _logger.LogWarning(
                "[Phase 7G] Reconciliation pass returned failure — Error={Error}",
                result.Error);
            // Continue to 6A.148 passes anyway — they're independent of the 7G result and
            // their own try/catch isolates their failures. The W5.5.D6.5 stuck-Cancelled
            // sweep needs to run on every tick regardless of 7G outcome.
        }
        else
        {
            var r = result.Value;
            if (r.ScannedCount > 0)
            {
                _logger.LogInformation(
                    "[Phase 7G] Reconciliation pass — Scanned={Scanned}, Reconciled={Reconciled}, StillPending={Pending}, FailedAtStripe={Failed}, MissingRefundId={Missing}, StripeLookupFailed={LookupFailed}, Warnings={WarningCount}",
                    r.ScannedCount, r.ReconciledCount, r.StillPendingCount,
                    r.FailedAtStripeCount, r.MissingRefundIdCount, r.StripeLookupFailedCount, r.Warnings.Count);

                foreach (var w in r.Warnings)
                {
                    _logger.LogWarning("[Phase 7G] {Warning}", w);
                }
            }
            else
            {
                _logger.LogDebug(
                    "[Phase 7G] Reconciliation pass — nothing stuck");
            }
        }

        // Phase 6A.148 (architect F11): also sweep stuck-Approved RR rows.
        try
        {
            var approvedResult = await reconciler.ReconcileStuckApprovedRefundRequestsAsync(
                ageThresholdMinutes: _settings.AgeThresholdMinutes,
                cancellationToken: cancellationToken);
            if (approvedResult.IsSuccess && approvedResult.Value > 0)
                _logger.LogInformation(
                    "[6A.148 RECON] Re-dispatched {Count} stuck Approved refund requests",
                    approvedResult.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[6A.148 RECON] ReconcileStuckApprovedRefundRequestsAsync threw — swallowed");
        }

        // Phase 6A.148.W5.5.D6.5: heal stuck Cancelled-with-refunded-ticket registrations
        // (Bug 1 safety net). Same cadence as the Approved sweep; both are bounded-batch.
        try
        {
            var healedResult = await reconciler.ReconcileStuckCancelledWithRefundedTicketAsync(
                ageThresholdMinutes: _settings.AgeThresholdMinutes,
                cancellationToken: cancellationToken);
            if (healedResult.IsSuccess && healedResult.Value > 0)
                _logger.LogInformation(
                    "[6A.148.W5.5.D6.5 RECON] Healed {Count} stuck Cancelled→Refunded registrations",
                    healedResult.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[6A.148.W5.5.D6.5 RECON] ReconcileStuckCancelledWithRefundedTicketAsync threw — swallowed");
        }
    }
}
