namespace LankaConnect.Application.Common.Models.Performance;

/// <summary>
/// Configuration for database performance alerting rules with cultural intelligence
/// </summary>
public class AlertingRuleConfiguration
{
    /// <summary>
    /// Unique identifier for the alerting rule
    /// </summary>
    public Guid RuleId { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Name of the alerting rule
    /// </summary>
    public string RuleName { get; set; } = string.Empty;

    /// <summary>
    /// Metric to monitor (CPU, Memory, Connections, Response_Time, Cultural_Load)
    /// </summary>
    public string MetricType { get; set; } = string.Empty;

    /// <summary>
    /// Threshold value that triggers the alert
    /// </summary>
    public decimal ThresholdValue { get; set; }

    /// <summary>
    /// Comparison operator (GreaterThan, LessThan, Equals)
    /// </summary>
    public string ComparisonOperator { get; set; } = "GreaterThan";

    /// <summary>
    /// Duration the threshold must be breached before alerting
    /// </summary>
    public TimeSpan Duration { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Severity level of the alert (Low, Medium, High, Critical)
    /// </summary>
    public string SeverityLevel { get; set; } = "Medium";

    /// <summary>
    /// Whether this rule is active
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Cultural events that modify alert thresholds
    /// </summary>
    public Dictionary<string, decimal> CulturalEventThresholds { get; set; } = new();

    /// <summary>
    /// Notification channels for this rule
    /// </summary>
    public List<string> NotificationChannels { get; set; } = new();
}