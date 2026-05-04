using LankaConnect.Application.Events.Services;
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
            return;
        }

        var r = result.Value;
        if (r.ScannedCount == 0)
        {
            // Quiet path — most passes find nothing stuck.
            _logger.LogDebug(
                "[Phase 7G] Reconciliation pass — nothing stuck");
            return;
        }

        _logger.LogInformation(
            "[Phase 7G] Reconciliation pass — Scanned={Scanned}, Reconciled={Reconciled}, StillPending={Pending}, FailedAtStripe={Failed}, MissingRefundId={Missing}, StripeLookupFailed={LookupFailed}, Warnings={WarningCount}",
            r.ScannedCount, r.ReconciledCount, r.StillPendingCount,
            r.FailedAtStripeCount, r.MissingRefundIdCount, r.StripeLookupFailedCount, r.Warnings.Count);

        foreach (var w in r.Warnings)
        {
            _logger.LogWarning("[Phase 7G] {Warning}", w);
        }
    }
}
