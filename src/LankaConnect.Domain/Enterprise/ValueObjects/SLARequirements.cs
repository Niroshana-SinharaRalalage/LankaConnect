using LankaConnect.Domain.Common;

namespace LankaConnect.Domain.Enterprise.ValueObjects;

/// <summary>
/// Service Level Agreement requirements for enterprise clients
/// Defines uptime, response time, and performance guarantees
/// </summary>
public class SLARequirements : ValueObject
{
    public double UptimeGuaranteePercentage { get; }
    public TimeSpan MaxResponseTime { get; }
    public int MaxRequestsPerSecond { get; }
    public TimeSpan SupportResponseTime { get; }
    public bool DedicatedSupport { get; }
    public bool AutomatedFailover { get; }
    public string SeverityLevels { get; }

    public SLARequirements(
        double uptimeGuaranteePercentage,
        TimeSpan maxResponseTime,
        int maxRequestsPerSecond,
        TimeSpan supportResponseTime,
        bool dedicatedSupport = true,
        bool automatedFailover = true,
        string severityLevels = "Critical,High,Medium,Low")
    {
        if (uptimeGuaranteePercentage < 95.0 || uptimeGuaranteePercentage > 100.0)
            throw new ArgumentException("Uptime guarantee must be between 95% and 100%.", nameof(uptimeGuaranteePercentage));
            
        if (maxResponseTime <= TimeSpan.Zero)
            throw new ArgumentException("Max response time must be positive.", nameof(maxResponseTime));
            
        if (maxRequestsPerSecond <= 0)
            throw new ArgumentException("Max requests per second must be positive.", nameof(maxRequestsPerSecond));
            
        if (supportResponseTime <= TimeSpan.Zero)
            throw new ArgumentException("Support response time must be positive.", nameof(supportResponseTime));

        UptimeGuaranteePercentage = uptimeGuaranteePercentage;
        MaxResponseTime = maxResponseTime;
        MaxRequestsPerSecond = maxRequestsPerSecond;
        SupportResponseTime = supportResponseTime;
        DedicatedSupport = dedicatedSupport;
        AutomatedFailover = automatedFailover;
        SeverityLevels = severityLevels ?? throw new ArgumentNullException(nameof(severityLevels));
    }

    /// <summary>
    /// Creates enterprise-grade SLA for Fortune 500 companies (99.95% uptime)
    /// </summary>
    public static SLARequirements CreateEnterpriseSLA()
        => new(
            uptimeGuaranteePercentage: 99.95,
            maxResponseTime: TimeSpan.FromMilliseconds(200),
            maxRequestsPerSecond: 10000,
            supportResponseTime: TimeSpan.FromHours(4),
            dedicatedSupport: true,
            automatedFailover: true,
            severityLevels: "Critical,High,Medium,Low");

    /// <summary>
    /// Creates premium SLA for mid-market companies (99.9% uptime)
    /// </summary>
    public static SLARequirements CreatePremiumSLA()
        => new(
            uptimeGuaranteePercentage: 99.9,
            maxResponseTime: TimeSpan.FromMilliseconds(500),
            maxRequestsPerSecond: 5000,
            supportResponseTime: TimeSpan.FromHours(8),
            dedicatedSupport: false,
            automatedFailover: true,
            severityLevels: "High,Medium,Low");

    /// <summary>
    /// Creates standard SLA for small to medium businesses (99.5% uptime)
    /// </summary>
    public static SLARequirements CreateStandardSLA()
        => new(
            uptimeGuaranteePercentage: 99.5,
            maxResponseTime: TimeSpan.FromMilliseconds(1000),
            maxRequestsPerSecond: 1000,
            supportResponseTime: TimeSpan.FromHours(24),
            dedicatedSupport: false,
            automatedFailover: false,
            severityLevels: "Medium,Low");

    public bool IsEnterprise => UptimeGuaranteePercentage >= 99.95;
    public bool IsPremium => UptimeGuaranteePercentage >= 99.9;
    public bool RequiresRealTimeMonitoring => MaxResponseTime <= TimeSpan.FromMilliseconds(300);

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return UptimeGuaranteePercentage;
        yield return MaxResponseTime;
        yield return MaxRequestsPerSecond;
        yield return SupportResponseTime;
        yield return DedicatedSupport;
        yield return AutomatedFailover;
        yield return SeverityLevels;
    }
}