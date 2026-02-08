namespace LankaConnect.Infrastructure.BackgroundServices;

/// <summary>
/// Phase 6A.93: Settings for the RefundStatusCleanupJob background service.
/// Monitors registrations stuck in RefundRequested status for too long.
/// </summary>
public class RefundCleanupSettings
{
    public const string SectionName = "RefundCleanupSettings";

    /// <summary>
    /// How often to check for stale refund requests (in minutes).
    /// Default: 60 minutes (1 hour)
    /// </summary>
    public int CheckIntervalInMinutes { get; set; } = 60;

    /// <summary>
    /// Number of hours after which a RefundRequested registration is considered stale.
    /// Default: 72 hours (3 days) - Stripe typically processes refunds within 5-10 business days
    /// </summary>
    public int StaleThresholdInHours { get; set; } = 72;

    /// <summary>
    /// Number of hours after which a stale refund becomes critical and requires immediate action.
    /// Default: 168 hours (7 days)
    /// </summary>
    public int CriticalThresholdInHours { get; set; } = 168;

    /// <summary>
    /// Whether to send alert emails to admins when stale refunds are detected.
    /// Default: true
    /// </summary>
    public bool SendAdminAlerts { get; set; } = true;

    /// <summary>
    /// Admin email address(es) to receive stale refund alerts (comma-separated).
    /// </summary>
    public string AdminEmailAddresses { get; set; } = string.Empty;

    /// <summary>
    /// Whether the cleanup job is enabled.
    /// Set to false to disable the background job.
    /// Default: true
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Initial delay before the job starts processing (in seconds).
    /// Allows the application to fully initialize.
    /// Default: 30 seconds
    /// </summary>
    public int InitialDelayInSeconds { get; set; } = 30;
}
