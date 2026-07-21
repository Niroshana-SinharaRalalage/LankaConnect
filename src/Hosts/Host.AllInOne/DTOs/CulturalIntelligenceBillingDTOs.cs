using System.ComponentModel.DataAnnotations;

namespace LankaConnect.API.DTOs;

/// <summary>
/// Request to create a Cultural Intelligence subscription
/// </summary>
public class CreateCulturalIntelligenceSubscriptionRequest
{
    [Required]
    public Guid UserId { get; set; }

    [Required]
    [StringLength(50)]
    public string TierName { get; set; } = string.Empty;

    [StringLength(100)]
    public string? PromoCode { get; set; }

    [Range(0, 365)]
    public int TrialDays { get; set; } = 0;

    public Dictionary<string, string>? Metadata { get; set; }
}

/// <summary>
/// Request to process Cultural Intelligence API usage
/// </summary>
public class ProcessCulturalIntelligenceUsageRequest
{
    [Required]
    [StringLength(200)]
    public string ApiKey { get; set; } = string.Empty;

    [Required]
    public Guid UserId { get; set; }

    [Required]
    [StringLength(500)]
    public string Endpoint { get; set; } = string.Empty;

    [Range(0, 100)]
    public int ComplexityScore { get; set; } = 50;

    [StringLength(100)]
    public string? ClientId { get; set; }

    public Dictionary<string, object>? AdditionalMetadata { get; set; }
}

/// <summary>
/// Request to process Buddhist Calendar premium usage
/// </summary>
public class ProcessBuddhistCalendarUsageRequest
{
    [Required]
    [StringLength(200)]
    public string ApiKey { get; set; } = string.Empty;

    [Required]
    public Guid UserId { get; set; }

    [Required]
    [StringLength(50)]
    public string PrecisionLevel { get; set; } = "Basic";

    [Required]
    [StringLength(50)]
    public string CalculationType { get; set; } = "LunarCalculation";

    public string[]? Variations { get; set; }

    [Required]
    public DateTime RequestedDate { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Request to process Cultural Appropriateness scoring
/// </summary>
public class ProcessCulturalAppropriatenessUsageRequest
{
    [Required]
    [StringLength(200)]
    public string ApiKey { get; set; } = string.Empty;

    [Required]
    public Guid UserId { get; set; }

    [Required]
    [StringLength(10000)]
    public string Content { get; set; } = string.Empty;

    [Required]
    [StringLength(50)]
    public string ContentType { get; set; } = "Text";

    [Required]
    [StringLength(50)]
    public string ComplexityLevel { get; set; } = "Basic";

    public string[]? Contexts { get; set; }

    public bool RealTimeModeration { get; set; } = false;

    [StringLength(10)]
    public string Language { get; set; } = "en";
}

/// <summary>
/// Request to process Diaspora Analytics usage
/// </summary>
public class ProcessDiasporaAnalyticsUsageRequest
{
    [Required]
    [StringLength(200)]
    public string ApiKey { get; set; } = string.Empty;

    [Required]
    public Guid UserId { get; set; }

    [Required]
    [StringLength(50)]
    public string AnalyticsType { get; set; } = "GeographicClustering";

    public string[]? Regions { get; set; }

    public string[]? Segments { get; set; }

    [Range(1, 24)]
    public int TimeframeMonths { get; set; } = 6;
}

/// <summary>
/// Request to create an enterprise contract
/// </summary>
public class CreateEnterpriseContractRequest
{
    [Required]
    [StringLength(200)]
    public string CompanyName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [StringLength(200)]
    public string ContactEmail { get; set; } = string.Empty;

    [Required]
    [StringLength(50)]
    public string ContactPhone { get; set; } = string.Empty;

    [Required]
    public string[] RequiredCultures { get; set; } = Array.Empty<string>();

    [Required]
    public string[] RequiredLanguages { get; set; } = Array.Empty<string>();

    [Required]
    [Range(0.01, 1000000)]
    public decimal ContractValue { get; set; }

    [Required]
    [StringLength(500)]
    public string PricingDescription { get; set; } = string.Empty;

    [Required]
    public DateTime ContractStartDate { get; set; }

    [Required]
    public DateTime ContractEndDate { get; set; }

    [Required]
    [StringLength(50)]
    public string PaymentFrequency { get; set; } = "Monthly";

    [Required]
    public DateTime FirstPaymentDate { get; set; }

    [Required]
    [Range(1, 120)]
    public int NumberOfPayments { get; set; } = 12;

    [Required]
    [Range(0, 1000)]
    public int ConsultingHours { get; set; } = 40;

    [Required]
    [Range(0.01, 10000)]
    public decimal ConsultingHourlyRate { get; set; } = 250.00m;

    [Required]
    public CulturalServiceRequest[] Services { get; set; } = Array.Empty<CulturalServiceRequest>();

    public WhiteLabelLicensingRequest? WhiteLabelLicensing { get; set; }
}

/// <summary>
/// Cultural service for enterprise contracts
/// </summary>
public class CulturalServiceRequest
{
    [Required]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [Range(0.01, 100000)]
    public decimal Price { get; set; }

    [StringLength(1000)]
    public string Description { get; set; } = string.Empty;
}

/// <summary>
/// White label licensing configuration
/// </summary>
public class WhiteLabelLicensingRequest
{
    [Required]
    [Range(0, 50000)]
    public decimal SetupFee { get; set; } = 5000.00m;

    [Required]
    [Range(0, 10000)]
    public decimal MonthlyFee { get; set; } = 500.00m;
}

/// <summary>
/// Request to process partnership revenue sharing
/// </summary>
public class ProcessPartnershipRevenueRequest
{
    [Required]
    [Range(0.01, 100)]
    public decimal SharePercentage { get; set; } = 75.0m;

    [Required]
    [Range(0, 100000)]
    public decimal MinimumAmount { get; set; } = 100.0m;

    [Required]
    [Range(0, 100000)]
    public decimal MaximumAmount { get; set; } = 10000.0m;

    [Range(0, 20)]
    public decimal AuthenticityBonusPercentage { get; set; } = 5.0m;

    [StringLength(500)]
    public string BonusReason { get; set; } = "Cultural authenticity and accuracy bonus";
}

/// <summary>
/// Request to get revenue analytics
/// </summary>
public class GetRevenueAnalyticsRequest
{
    [Required]
    public DateTime StartDate { get; set; } = DateTime.UtcNow.AddMonths(-1);

    [Required]
    public DateTime EndDate { get; set; } = DateTime.UtcNow;

    [StringLength(50)]
    public string? FilterByTier { get; set; }

    [StringLength(50)]
    public string? FilterByFeature { get; set; }

    public bool IncludeUsageMetrics { get; set; } = true;

    public bool IncludeCustomerMetrics { get; set; } = true;

    public bool IncludeCulturalMetrics { get; set; } = true;
}

/// <summary>
/// Response for Cultural Intelligence tier information
/// </summary>
public class CulturalIntelligenceTierResponse
{
    public string Name { get; set; } = string.Empty;
    public PriceResponse Price { get; set; } = new();
    public RequestLimitResponse RequestLimit { get; set; } = new();
    public CulturalFeaturesResponse Features { get; set; } = new();
    public UsagePricingResponse UsagePricing { get; set; } = new();
    public SLAResponse SLA { get; set; } = new();
}

public class PriceResponse
{
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "USD";
    public bool IsFree { get; set; }
}

public class RequestLimitResponse
{
    public int Limit { get; set; }
    public bool IsUnlimited { get; set; }
}

public class CulturalFeaturesResponse
{
    public bool BasicCalendar { get; set; }
    public bool BuddhistPremium { get; set; }
    public bool HinduPremium { get; set; }
    public bool AIRecommendations { get; set; }
    public bool CulturalScoring { get; set; }
    public bool DiasporaAnalytics { get; set; }
    public bool MultiLanguage { get; set; }
    public bool Webhooks { get; set; }
    public bool CustomAI { get; set; }
    public bool WhiteLabel { get; set; }
    public bool Consulting { get; set; }
}

public class UsagePricingResponse
{
    public decimal CulturalValidation { get; set; }
    public decimal DiasporaAnalysis { get; set; }
    public decimal MultiLanguageTranslation { get; set; }
    public decimal CulturalConflictResolution { get; set; }
    public decimal CustomMarketResearch { get; set; }
}

public class SLAResponse
{
    public string Level { get; set; } = string.Empty;
    public double ResponseTimeHours { get; set; }
    public decimal UptimeGuarantee { get; set; }
    public bool DedicatedSupport { get; set; }
}

/// <summary>
/// Response for revenue analytics
/// </summary>
public class RevenueAnalyticsResponse
{
    public decimal TotalRevenue { get; set; }
    public decimal MonthlyRecurringRevenue { get; set; }
    public decimal AverageRevenuePerUser { get; set; }
    public int TotalSubscriptions { get; set; }
    public UsageAnalyticsResponse Usage { get; set; } = new();
    public CustomerAnalyticsResponse Customer { get; set; } = new();
    public CulturalAnalyticsResponse Cultural { get; set; } = new();
}

public class UsageAnalyticsResponse
{
    public long TotalAPIRequests { get; set; }
    public long BuddhistCalendarRequests { get; set; }
    public long CulturalAppropriatenessRequests { get; set; }
    public long DiasporaAnalyticsRequests { get; set; }
    public decimal AverageComplexityScore { get; set; }
}

public class CustomerAnalyticsResponse
{
    public int Community { get; set; }
    public int Professional { get; set; }
    public int Enterprise { get; set; }
    public int Custom { get; set; }
    public decimal ChurnRate { get; set; }
}

public class CulturalAnalyticsResponse
{
    public Dictionary<string, long> FeatureUsage { get; set; } = new();
    public Dictionary<string, decimal> FeatureRevenue { get; set; } = new();
    public Dictionary<string, decimal> PopularityScores { get; set; } = new();
}

/// <summary>
/// Response for usage processing
/// </summary>
public class UsageProcessingResponse
{
    public string Message { get; set; } = string.Empty;
    public string Endpoint { get; set; } = string.Empty;
    public decimal Cost { get; set; }
    public string Currency { get; set; } = "USD";
    public int ComplexityScore { get; set; }
    public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;
    public Dictionary<string, object>? Metadata { get; set; }
}

/// <summary>
/// Response for Buddhist Calendar usage
/// </summary>
public class BuddhistCalendarUsageResponse
{
    public string Message { get; set; } = string.Empty;
    public string PrecisionLevel { get; set; } = string.Empty;
    public string CalculationType { get; set; } = string.Empty;
    public int VariationsCount { get; set; }
    public decimal Cost { get; set; }
    public string Currency { get; set; } = "USD";
    public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Response for Cultural Appropriateness usage
/// </summary>
public class CulturalAppropriatenessUsageResponse
{
    public string Message { get; set; } = string.Empty;
    public string ComplexityLevel { get; set; } = string.Empty;
    public int ContextsAnalyzed { get; set; }
    public bool RealTimeModeration { get; set; }
    public decimal Cost { get; set; }
    public string Currency { get; set; } = "USD";
    public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Response for Diaspora Analytics usage
/// </summary>
public class DiasporaAnalyticsUsageResponse
{
    public string Message { get; set; } = string.Empty;
    public string AnalyticsType { get; set; } = string.Empty;
    public int RegionsAnalyzed { get; set; }
    public int SegmentsAnalyzed { get; set; }
    public int TimeframeMonths { get; set; }
    public decimal Cost { get; set; }
    public string Currency { get; set; } = "USD";
    public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Response for enterprise contract creation
/// </summary>
public class EnterpriseContractResponse
{
    public string Message { get; set; } = string.Empty;
    public Guid ContractId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public decimal ContractValue { get; set; }
    public int ConsultingHours { get; set; }
    public decimal ConsultingHourlyRate { get; set; }
    public bool WhiteLabelIncluded { get; set; }
    public DateTime ContractStartDate { get; set; }
    public DateTime ContractEndDate { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Response for partnership revenue processing
/// </summary>
public class PartnershipRevenueResponse
{
    public string Message { get; set; } = string.Empty;
    public Guid PartnershipId { get; set; }
    public decimal SharePercentage { get; set; }
    public decimal AuthenticityBonusPercentage { get; set; }
    public decimal TotalShareAmount { get; set; }
    public string Currency { get; set; } = "USD";
    public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Error response for API failures
/// </summary>
public class BillingErrorResponse
{
    public string Error { get; set; } = string.Empty;
    public string? Details { get; set; }
    public int StatusCode { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string? RequestId { get; set; }
}

/// <summary>
/// Success response for API operations
/// </summary>
public class BillingSuccessResponse
{
    public string Message { get; set; } = string.Empty;
    public object? Data { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string? RequestId { get; set; }
}