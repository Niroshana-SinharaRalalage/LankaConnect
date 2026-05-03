namespace LankaConnect.Infrastructure.BackgroundServices;

/// <summary>
/// Phase 7G — settings for the durable refund-reconciliation safety net.
/// Distinct from the unfinished <see cref="RefundCleanupSettings"/> stub
/// (Phase 6A.93, never shipped) — this one drives an active reconciler that
/// queries Stripe and completes the DB transition for any refund whose
/// <c>charge.refunded</c> webhook went missing.
/// </summary>
public class RefundReconciliationSettings
{
    public const string SectionName = "RefundReconciliation";

    /// <summary>
    /// Whether the background reconciler is enabled. Set <c>false</c> to disable
    /// without removing the wiring (useful for incident response).
    /// Default: <c>true</c>.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// How often to check for stuck refunds, in minutes.
    /// Default: 5 minutes — frequent enough that buyers don't see "Refund in
    /// Progress" linger long after the actual money returned, infrequent enough
    /// to keep Stripe API call volume bounded.
    /// </summary>
    public int IntervalMinutes { get; set; } = 5;

    /// <summary>
    /// Grace period before the reconciler touches a row, in minutes. Gives the
    /// primary <c>charge.refunded</c> webhook a fair chance to arrive on its own.
    /// Default: 10 minutes.
    /// </summary>
    public int AgeThresholdMinutes { get; set; } = 10;

    /// <summary>
    /// Max number of stuck refunds reconciled per pass. Bounds the Stripe API
    /// call burst when a backlog has accumulated.
    /// Default: 50.
    /// </summary>
    public int BatchSize { get; set; } = 50;

    /// <summary>
    /// Initial delay before the reconciler runs its first pass after the API
    /// container starts. Lets the rest of DI finish warming up.
    /// Default: 30 seconds.
    /// </summary>
    public int InitialDelaySeconds { get; set; } = 30;
}
