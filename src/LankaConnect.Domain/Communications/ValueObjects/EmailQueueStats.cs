namespace LankaConnect.Domain.Communications.ValueObjects;

/// <summary>
/// Value object representing email queue statistics for monitoring and reporting
/// </summary>
public class EmailQueueStats
{
    public int PendingCount { get; init; }
    public int QueuedCount { get; init; }
    public int SendingCount { get; init; }
    public int SentCount { get; init; }
    public int DeliveredCount { get; init; }
    public int FailedCount { get; init; }
    public int RetryableFailedCount { get; init; }
    public DateTime LastUpdated { get; init; }

    public EmailQueueStats()
    {
        LastUpdated = DateTime.UtcNow;
    }

    public EmailQueueStats(
        int pendingCount,
        int queuedCount,
        int sendingCount,
        int sentCount,
        int deliveredCount,
        int failedCount,
        int retryableFailedCount)
    {
        PendingCount = pendingCount;
        QueuedCount = queuedCount;
        SendingCount = sendingCount;
        SentCount = sentCount;
        DeliveredCount = deliveredCount;
        FailedCount = failedCount;
        RetryableFailedCount = retryableFailedCount;
        LastUpdated = DateTime.UtcNow;
    }

    /// <summary>
    /// Total number of emails in all states
    /// </summary>
    public int TotalCount => PendingCount + QueuedCount + SendingCount + SentCount + DeliveredCount + FailedCount;

    /// <summary>
    /// Percentage of successfully delivered emails
    /// </summary>
    public decimal DeliveryRate => TotalCount > 0 ? (decimal)DeliveredCount / TotalCount * 100 : 0;

    /// <summary>
    /// Percentage of failed emails
    /// </summary>
    public decimal FailureRate => TotalCount > 0 ? (decimal)FailedCount / TotalCount * 100 : 0;
}