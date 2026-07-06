using LankaConnect.BuildingBlocks.Domain;
using System.Diagnostics.CodeAnalysis;
namespace LankaConnect.Modules.Communications.Domain.Entities;

/// <summary>
/// Phase 6A.89: Domain entity for persisting email metrics to the database.
///
/// Problem Solved:
/// - DefaultEmailMetrics stored all metrics in memory
/// - Azure Container Apps restarts caused complete data loss
/// - Dashboard showed 0 for all metrics after container restart
///
/// Solution:
/// - Persist metrics to database for durability
/// - Aggregate metrics by date and template name
/// - Support historical queries for dashboard
/// </summary>
public class EmailMetricRecord : LegacyBaseEntity
{
    /// <summary>
    /// Date for which these metrics apply (granularity: daily)
    /// </summary>
    public DateOnly MetricDate { get; private set; }

    /// <summary>
    /// Template name (null for global/aggregate metrics)
    /// </summary>
    public string? TemplateName { get; private set; }

    /// <summary>
    /// Total emails sent for this date/template combination
    /// </summary>
    public int TotalSent { get; private set; }

    /// <summary>
    /// Number of successful email sends
    /// </summary>
    public int Successful { get; private set; }

    /// <summary>
    /// Number of failed email sends
    /// </summary>
    public int Failed { get; private set; }

    /// <summary>
    /// Average duration in milliseconds for email sends
    /// </summary>
    public int AverageDurationMs { get; private set; }

    /// <summary>
    /// Total duration in milliseconds (used to calculate average)
    /// </summary>
    public long TotalDurationMs { get; private set; }

    /// <summary>
    /// Number of parameter validation failures
    /// </summary>
    public int ValidationFailures { get; private set; }

    /// <summary>
    /// Number of template not found errors
    /// </summary>
    public int TemplateNotFoundCount { get; private set; }

    // For EF Core
    [SetsRequiredMembers]
    private EmailMetricRecord() { }

    [SetsRequiredMembers]
    private EmailMetricRecord(DateOnly metricDate, string? templateName)
    {
        MetricDate = metricDate;
        TemplateName = templateName;
        TotalSent = 0;
        Successful = 0;
        Failed = 0;
        AverageDurationMs = 0;
        TotalDurationMs = 0;
        ValidationFailures = 0;
        TemplateNotFoundCount = 0;
    }

    /// <summary>
    /// Creates a new metric record for a specific date and template
    /// </summary>
    public static EmailMetricRecord Create(DateOnly metricDate, string? templateName = null)
    {
        return new EmailMetricRecord(metricDate, templateName);
    }

    /// <summary>
    /// Creates a metric record for the global aggregate (no template name)
    /// </summary>
    public static EmailMetricRecord CreateGlobal(DateOnly metricDate)
    {
        return new EmailMetricRecord(metricDate, null);
    }

    /// <summary>
    /// Records an email send with duration and success status
    /// </summary>
    public void RecordEmailSend(int durationMs, bool success)
    {
        TotalSent++;
        TotalDurationMs += durationMs;
        AverageDurationMs = TotalSent > 0 ? (int)(TotalDurationMs / TotalSent) : 0;

        if (success)
            Successful++;
        else
            Failed++;

    }

    /// <summary>
    /// Records a parameter validation failure
    /// </summary>
    public void RecordValidationFailure()
    {
        ValidationFailures++;
    }

    /// <summary>
    /// Records a template not found error
    /// </summary>
    public void RecordTemplateNotFound()
    {
        TemplateNotFoundCount++;
    }

    /// <summary>
    /// Merges metrics from another record (used for batch updates)
    /// </summary>
    public void MergeFrom(int totalSent, int successful, int failed, long totalDurationMs, int validationFailures, int templateNotFoundCount)
    {
        TotalSent += totalSent;
        Successful += successful;
        Failed += failed;
        TotalDurationMs += totalDurationMs;
        ValidationFailures += validationFailures;
        TemplateNotFoundCount += templateNotFoundCount;
        AverageDurationMs = TotalSent > 0 ? (int)(TotalDurationMs / TotalSent) : 0;
    }

    /// <summary>
    /// Success rate as percentage (0-100)
    /// </summary>
    public double SuccessRate => TotalSent > 0 ? (double)Successful / TotalSent * 100 : 0;
}
