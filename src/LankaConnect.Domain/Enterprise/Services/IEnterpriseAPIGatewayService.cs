using LankaConnect.Domain.Common;
using LankaConnect.Domain.Enterprise.ValueObjects;
using CulturalIntelligenceEndpoint = LankaConnect.Domain.Enterprise.ValueObjects.CulturalIntelligenceEndpoint;

namespace LankaConnect.Domain.Enterprise.Services;

/// <summary>
/// Enterprise API Gateway Service - manages Fortune 500 API requests with SLA compliance
/// and advanced cultural intelligence processing capabilities
/// </summary>
public interface IEnterpriseAPIGatewayService
{
    #region Authentication & Authorization

    /// <summary>
    /// Validates enterprise API key with advanced security checks
    /// </summary>
    Task<Result<EnterpriseAPIKeyValidation>> ValidateEnterpriseAPIKeyAsync(
        string apiKey, 
        EnterpriseCulturalRequest request);

    /// <summary>
    /// Validates API key access to specific endpoint with IP and geographic restrictions
    /// </summary>
    Task<Result<EndpointAccessValidation>> ValidateEndpointAccessAsync(
        EnterpriseAPIKey apiKey, 
        CulturalIntelligenceEndpoint endpoint, 
        string sourceIP);

    #endregion

    #region SLA Monitoring & Compliance

    /// <summary>
    /// Monitors SLA compliance metrics for enterprise contracts
    /// </summary>
    Task<Result<SLAComplianceMetrics>> MonitorSLAComplianceAsync(
        EnterpriseContract contract, 
        DateTime startDate, 
        DateTime endDate);

    /// <summary>
    /// Records SLA violation incident for enterprise client
    /// </summary>
    Task<Result> RecordSLAViolationAsync(
        EnterpriseClientId clientId, 
        SLAViolation violation);

    /// <summary>
    /// Calculates real-time SLA performance metrics
    /// </summary>
    Task<Result<SLAPerformanceMetrics>> CalculateSLAPerformanceAsync(
        EnterpriseClientId clientId, 
        TimeSpan measurementPeriod);

    #endregion

    #region Request Processing & Rate Limiting

    /// <summary>
    /// Processes enterprise cultural intelligence request with advanced rate limiting
    /// </summary>
    Task<Result<EnterpriseCulturalResponse>> ProcessEnterpriseCulturalRequestAsync(
        EnterpriseCulturalRequest request);

    /// <summary>
    /// Applies sophisticated rate limiting with cultural complexity scoring
    /// </summary>
    Task<Result<RateLimitingDecision>> ApplyAdvancedRateLimitingAsync(
        EnterpriseAPIKey apiKey, 
        EnterpriseCulturalRequest request);

    /// <summary>
    /// Processes high-priority enterprise requests with guaranteed SLA
    /// </summary>
    Task<Result<EnterpriseCulturalResponse>> ProcessPriorityRequestAsync(
        EnterpriseCulturalRequest request, 
        SLARequirements slaRequirements);

    #endregion

    #region Analytics & Reporting

    /// <summary>
    /// Generates enterprise usage analytics for billing and optimization
    /// </summary>
    Task<Result<EnterpriseAnalytics>> GenerateEnterpriseAnalyticsAsync(
        EnterpriseClientId clientId, 
        AnalyticsRequest analyticsRequest);

    /// <summary>
    /// Creates comprehensive enterprise API performance report
    /// </summary>
    Task<Result<EnterprisePerformanceReport>> GeneratePerformanceReportAsync(
        EnterpriseClientId clientId, 
        DateTime startDate, 
        DateTime endDate);

    /// <summary>
    /// Generates cultural intelligence usage insights for optimization
    /// </summary>
    Task<Result<CulturalIntelligenceInsights>> GenerateCulturalUsageInsightsAsync(
        EnterpriseClientId clientId, 
        TimeSpan analysisPeriod);

    #endregion

    #region Health & Monitoring

    /// <summary>
    /// Performs comprehensive enterprise gateway health check
    /// </summary>
    Task<Result<EnterpriseGatewayHealth>> PerformHealthCheckAsync();

    /// <summary>
    /// Validates system capacity for enterprise workloads
    /// </summary>
    Task<Result<CapacityAssessment>> AssessSystemCapacityAsync(
        List<EnterpriseContract> activeContracts);

    /// <summary>
    /// Tests automated failover capabilities for enterprise clients
    /// </summary>
    Task<Result<FailoverTestResult>> TestAutomatedFailoverAsync(
        EnterpriseClientId clientId);

    #endregion

    #region Security & Compliance

    /// <summary>
    /// Validates SOC 2 Type II compliance for cultural data protection
    /// </summary>
    Task<Result<ComplianceValidation>> ValidateSOC2ComplianceAsync(
        EnterpriseCulturalRequest request);

    /// <summary>
    /// Performs GDPR compliance check for diaspora community data
    /// </summary>
    Task<Result<GDPRComplianceCheck>> ValidateGDPRComplianceAsync(
        EnterpriseCulturalRequest request, 
        string dataSubjectRegion);

    /// <summary>
    /// Audits cultural data access and processing for enterprise clients
    /// </summary>
    Task<Result<CulturalDataAudit>> AuditCulturalDataAccessAsync(
        EnterpriseClientId clientId, 
        DateTime auditStartDate, 
        DateTime auditEndDate);

    #endregion

    #region Disaster Recovery

    /// <summary>
    /// Initiates disaster recovery procedures for enterprise clients
    /// </summary>
    Task<Result<DisasterRecoveryInitiation>> InitiateDisasterRecoveryAsync(
        DisasterRecoveryTrigger trigger);

    /// <summary>
    /// Synchronizes Buddhist/Hindu calendar data across regions for disaster recovery
    /// </summary>
    Task<Result<CalendarDataSynchronization>> SynchronizeCulturalCalendarDataAsync(
        List<string> targetRegions);

    /// <summary>
    /// Validates data integrity for cultural intelligence systems post-recovery
    /// </summary>
    Task<Result<DataIntegrityValidation>> ValidateCulturalDataIntegrityAsync();

    #endregion
}

#region Supporting Value Objects and DTOs

public class EnterpriseAPIKeyValidation : ValueObject
{
    public bool IsValid { get; }
    public APIKeyTier Tier { get; }
    public EnterpriseClientId ClientId { get; }
    public List<string> ValidationErrors { get; }
    public DateTime ValidationTimestamp { get; }
    public bool RequiresAdditionalVerification { get; }

    public EnterpriseAPIKeyValidation(
        bool isValid, 
        APIKeyTier tier, 
        EnterpriseClientId clientId, 
        List<string>? validationErrors = null,
        bool requiresAdditionalVerification = false)
    {
        IsValid = isValid;
        Tier = tier;
        ClientId = clientId;
        ValidationErrors = validationErrors ?? new List<string>();
        ValidationTimestamp = DateTime.UtcNow;
        RequiresAdditionalVerification = requiresAdditionalVerification;
    }

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return IsValid;
        yield return Tier;
        yield return ClientId;
        yield return RequiresAdditionalVerification;
    }
}

public class EndpointAccessValidation : ValueObject
{
    public bool HasAccess { get; }
    public string? DenialReason { get; }
    public bool RequiresIPWhitelisting { get; }
    public bool RequiresGeographicValidation { get; }
    public List<string> AllowedRegions { get; }

    public EndpointAccessValidation(
        bool hasAccess, 
        string? denialReason = null,
        bool requiresIPWhitelisting = false,
        bool requiresGeographicValidation = false,
        List<string>? allowedRegions = null)
    {
        HasAccess = hasAccess;
        DenialReason = denialReason ?? string.Empty;
        RequiresIPWhitelisting = requiresIPWhitelisting;
        RequiresGeographicValidation = requiresGeographicValidation;
        AllowedRegions = allowedRegions ?? new List<string>();
    }

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return HasAccess;
        yield return DenialReason ?? string.Empty;
        yield return RequiresIPWhitelisting;
        yield return RequiresGeographicValidation;
    }
}

public class SLAComplianceMetrics : ValueObject
{
    public double UptimePercentage { get; }
    public double AverageResponseTime { get; }
    public int TotalRequests { get; }
    public int SuccessfulRequests { get; }
    public int SLAViolations { get; }
    public bool MeetsRequirements { get; }
    public List<SLAMetric> DetailedMetrics { get; }
    public DateTime MeasurementStartDate { get; }
    public DateTime MeasurementEndDate { get; }

    public SLAComplianceMetrics(
        double uptimePercentage,
        double averageResponseTime,
        int totalRequests,
        int successfulRequests,
        int slaViolations,
        List<SLAMetric> detailedMetrics,
        DateTime measurementStartDate,
        DateTime measurementEndDate)
    {
        UptimePercentage = uptimePercentage;
        AverageResponseTime = averageResponseTime;
        TotalRequests = totalRequests;
        SuccessfulRequests = successfulRequests;
        SLAViolations = slaViolations;
        MeetsRequirements = CalculateCompliance();
        DetailedMetrics = detailedMetrics ?? new List<SLAMetric>();
        MeasurementStartDate = measurementStartDate;
        MeasurementEndDate = measurementEndDate;
    }

    private bool CalculateCompliance()
    {
        return UptimePercentage >= 99.95 && AverageResponseTime <= 200 && SLAViolations == 0;
    }

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return UptimePercentage;
        yield return AverageResponseTime;
        yield return TotalRequests;
        yield return SLAViolations;
        yield return MeasurementStartDate;
        yield return MeasurementEndDate;
    }
}

public class SLAMetric : ValueObject
{
    public string MetricName { get; }
    public double Value { get; }
    public string Unit { get; }
    public bool IsWithinSLA { get; }
    public DateTime Timestamp { get; }

    public SLAMetric(string metricName, double value, string unit, bool isWithinSLA)
    {
        MetricName = metricName;
        Value = value;
        Unit = unit;
        IsWithinSLA = isWithinSLA;
        Timestamp = DateTime.UtcNow;
    }

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return MetricName;
        yield return Value;
        yield return Unit;
        yield return IsWithinSLA;
    }
}

public class SLAViolation : ValueObject
{
    public SLAViolationType ViolationType { get; }
    public string Description { get; }
    public DateTime OccurredAt { get; }
    public TimeSpan Duration { get; }
    public SLAViolationSeverity Severity { get; }
    public string ImpactDescription { get; }
    public bool RequiresCustomerNotification { get; }

    public SLAViolation(
        SLAViolationType violationType,
        string description,
        DateTime occurredAt,
        TimeSpan duration,
        SLAViolationSeverity severity,
        string impactDescription,
        bool requiresCustomerNotification = true)
    {
        ViolationType = violationType;
        Description = description;
        OccurredAt = occurredAt;
        Duration = duration;
        Severity = severity;
        ImpactDescription = impactDescription;
        RequiresCustomerNotification = requiresCustomerNotification;
    }

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return ViolationType;
        yield return Description;
        yield return OccurredAt;
        yield return Duration;
        yield return Severity;
    }
}

public enum SLAViolationType
{
    ResponseTimeExceeded,
    UptimeBelow99_95,
    CapacityLimitReached,
    SecurityIncident,
    DataLoss,
    ServiceUnavailable
}

public enum SLAViolationSeverity
{
    Low,
    Medium,
    High,
    Critical
}

public class EnterpriseCulturalResponse : ValueObject
{
    public string ResponseData { get; }
    public TimeSpan ProcessingTime { get; }
    public CulturalValidationResult ValidationResult { get; }
    public List<string> Warnings { get; }
    public bool SLACompliant { get; }
    public DateTime Timestamp { get; }
    public string CorrelationId { get; }
    public int ProcessingCost { get; }

    public EnterpriseCulturalResponse(
        string responseData,
        TimeSpan processingTime,
        CulturalValidationResult validationResult,
        List<string>? warnings = null,
        bool slaCompliant = true,
        string? correlationId = null,
        int processingCost = 0)
    {
        ResponseData = responseData;
        ProcessingTime = processingTime;
        ValidationResult = validationResult;
        Warnings = warnings ?? new List<string>();
        SLACompliant = slaCompliant;
        Timestamp = DateTime.UtcNow;
        CorrelationId = correlationId ?? Guid.NewGuid().ToString();
        ProcessingCost = processingCost;
    }

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return ResponseData;
        yield return ProcessingTime;
        yield return ValidationResult;
        yield return SLACompliant;
        yield return CorrelationId;
    }
}

public class CulturalValidationResult : ValueObject
{
    public bool IsValid { get; }
    public double ConfidenceScore { get; }
    public List<string> ValidationNotes { get; }
    public bool RequiredHumanReview { get; }
    public bool PassedCommunityValidation { get; }

    public CulturalValidationResult(
        bool isValid,
        double confidenceScore,
        List<string>? validationNotes = null,
        bool requiredHumanReview = false,
        bool passedCommunityValidation = false)
    {
        IsValid = isValid;
        ConfidenceScore = Math.Max(0, Math.Min(1, confidenceScore));
        ValidationNotes = validationNotes ?? new List<string>();
        RequiredHumanReview = requiredHumanReview;
        PassedCommunityValidation = passedCommunityValidation;
    }

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return IsValid;
        yield return ConfidenceScore;
        yield return RequiredHumanReview;
        yield return PassedCommunityValidation;
    }
}

#endregion