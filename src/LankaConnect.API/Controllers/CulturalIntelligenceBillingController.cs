using LankaConnect.Application.Billing;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Billing;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace LankaConnect.API.Controllers;

/// <summary>
/// API Controller for Cultural Intelligence billing and subscription management
/// Handles tiered pricing, usage tracking, and enterprise contracts
/// </summary>
[ApiController]
[Route("api/billing/cultural-intelligence")]
[EnableRateLimiting("CulturalIntelligencePolicy")]
public class CulturalIntelligenceBillingController : ControllerBase
{
    private readonly ICulturalIntelligenceBillingService _billingService;
    private readonly IStripeWebhookHandler _webhookHandler;
    private readonly IStripePaymentService _stripeService;
    private readonly ILogger<CulturalIntelligenceBillingController> _logger;

    public CulturalIntelligenceBillingController(
        ICulturalIntelligenceBillingService billingService,
        IStripeWebhookHandler webhookHandler,
        IStripePaymentService stripeService,
        ILogger<CulturalIntelligenceBillingController> logger)
    {
        _billingService = billingService ?? throw new ArgumentNullException(nameof(billingService));
        _webhookHandler = webhookHandler ?? throw new ArgumentNullException(nameof(webhookHandler));
        _stripeService = stripeService ?? throw new ArgumentNullException(nameof(stripeService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Create a Cultural Intelligence subscription
    /// </summary>
    [HttpPost("subscriptions")]
    [Authorize]
    public async Task<IActionResult> CreateSubscription([FromBody] CreateCulturalIntelligenceSubscriptionRequest request)
    {
        try
        {
            _logger.LogInformation("Creating Cultural Intelligence subscription for user {UserId} with tier {TierName}",
                request.UserId, request.TierName);

            var userId = UserId.Create(request.UserId);
            var tier = MapRequestToTier(request);

            var result = await _billingService.CreateCulturalSubscriptionAsync(userId, tier);

            if (result.IsFailure)
            {
                _logger.LogWarning("Failed to create subscription: {Error}", result.Error);
                return BadRequest(new { error = result.Error });
            }

            return Ok(new { message = "Subscription created successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating Cultural Intelligence subscription");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Process API usage billing for Cultural Intelligence features
    /// </summary>
    [HttpPost("usage")]
    [Authorize]
    public async Task<IActionResult> ProcessUsage([FromBody] ProcessCulturalIntelligenceUsageRequest request)
    {
        try
        {
            _logger.LogInformation("Processing Cultural Intelligence usage for API key {ApiKey}", request.ApiKey);

            var apiKey = new APIKey(request.ApiKey, APIKeyTier.Professional, UserId.Create(request.UserId));
            var culturalRequest = MapToCulturalIntelligenceRequest(request);

            var result = await _billingService.ProcessCulturalAPIUsageAsync(apiKey, culturalRequest);

            if (result.IsFailure)
            {
                _logger.LogWarning("Failed to process usage: {Error}", result.Error);
                return BadRequest(new { error = result.Error });
            }

            return Ok(new { message = "Usage processed successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing Cultural Intelligence usage");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Process Buddhist Calendar premium feature usage
    /// </summary>
    [HttpPost("usage/buddhist-calendar")]
    [Authorize]
    public async Task<IActionResult> ProcessBuddhistCalendarUsage([FromBody] ProcessBuddhistCalendarUsageRequest request)
    {
        try
        {
            _logger.LogInformation("Processing Buddhist Calendar premium usage for API key {ApiKey}", request.ApiKey);

            var apiKey = new APIKey(request.ApiKey, APIKeyTier.Professional, UserId.Create(request.UserId));
            var buddhistRequest = MapToBuddhistCalendarRequest(request);

            var result = await _billingService.ProcessBuddhistCalendarPremiumUsageAsync(apiKey, buddhistRequest);

            if (result.IsFailure)
            {
                _logger.LogWarning("Failed to process Buddhist calendar usage: {Error}", result.Error);
                return BadRequest(new { error = result.Error });
            }

            return Ok(new 
            { 
                message = "Buddhist calendar premium usage processed successfully",
                precisionLevel = request.PrecisionLevel,
                cost = CalculateBuddhistCalendarCost(request)
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing Buddhist Calendar premium usage");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Process Cultural Appropriateness scoring usage
    /// </summary>
    [HttpPost("usage/cultural-appropriateness")]
    [Authorize]
    public async Task<IActionResult> ProcessCulturalAppropriatenessUsage([FromBody] ProcessCulturalAppropriatenessUsageRequest request)
    {
        try
        {
            _logger.LogInformation("Processing Cultural Appropriateness scoring for API key {ApiKey}", request.ApiKey);

            var apiKey = new APIKey(request.ApiKey, APIKeyTier.Professional, UserId.Create(request.UserId));
            var appropriatenessRequest = MapToCulturalAppropriatenessRequest(request);

            var result = await _billingService.ProcessCulturalAppropriatenesScoringAsync(apiKey, appropriatenessRequest);

            if (result.IsFailure)
            {
                _logger.LogWarning("Failed to process cultural appropriateness usage: {Error}", result.Error);
                return BadRequest(new { error = result.Error });
            }

            return Ok(new 
            { 
                message = "Cultural appropriateness scoring processed successfully",
                complexityLevel = request.ComplexityLevel,
                contextsAnalyzed = request.Contexts?.Length ?? 0,
                cost = CalculateCulturalAppropriatenessCost(request)
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing Cultural Appropriateness scoring");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Process Diaspora Analytics usage
    /// </summary>
    [HttpPost("usage/diaspora-analytics")]
    [Authorize]
    public async Task<IActionResult> ProcessDiasporaAnalyticsUsage([FromBody] ProcessDiasporaAnalyticsUsageRequest request)
    {
        try
        {
            _logger.LogInformation("Processing Diaspora Analytics usage for API key {ApiKey}", request.ApiKey);

            var apiKey = new APIKey(request.ApiKey, APIKeyTier.Enterprise, UserId.Create(request.UserId));
            var analyticsRequest = MapToDiasporaAnalyticsRequest(request);

            var result = await _billingService.ProcessDiasporaAnalyticsUsageAsync(apiKey, analyticsRequest);

            if (result.IsFailure)
            {
                _logger.LogWarning("Failed to process diaspora analytics usage: {Error}", result.Error);
                return BadRequest(new { error = result.Error });
            }

            return Ok(new 
            { 
                message = "Diaspora analytics processed successfully",
                analyticsType = request.AnalyticsType,
                regionsAnalyzed = request.Regions?.Length ?? 0,
                segmentsAnalyzed = request.Segments?.Length ?? 0,
                cost = CalculateDiasporaAnalyticsCost(request)
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing Diaspora Analytics");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Create enterprise contract with cultural consulting services
    /// </summary>
    [HttpPost("enterprise/contracts")]
    [Authorize(Roles = "Admin,EnterpriseAdmin")]
    public async Task<IActionResult> CreateEnterpriseContract([FromBody] CreateEnterpriseContractRequest request)
    {
        try
        {
            _logger.LogInformation("Creating enterprise contract for client {CompanyName}", request.CompanyName);

            var enterpriseClient = MapToEnterpriseClient(request);
            var contract = MapToCulturalServicesContract(request);

            var result = await _billingService.ProcessEnterpriseContractAsync(enterpriseClient, contract);

            if (result.IsFailure)
            {
                _logger.LogWarning("Failed to create enterprise contract: {Error}", result.Error);
                return BadRequest(new { error = result.Error });
            }

            return Ok(new 
            { 
                message = "Enterprise contract created successfully",
                contractId = contract.ContractId.Value,
                contractValue = contract.TotalValue.Amount,
                consultingHours = contract.ConsultingHours.IncludedHours,
                whiteLabelIncluded = contract.WhiteLabelLicensing?.IsIncluded ?? false
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating enterprise contract");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Process partnership revenue sharing
    /// </summary>
    [HttpPost("partnerships/{partnershipId}/revenue-share")]
    [Authorize(Roles = "Admin,FinanceAdmin")]
    public async Task<IActionResult> ProcessPartnershipRevenue(
        Guid partnershipId, 
        [FromBody] ProcessPartnershipRevenueRequest request)
    {
        try
        {
            _logger.LogInformation("Processing partnership revenue for partnership {PartnershipId}", partnershipId);

            var partnershipIdValue = new PartnershipId(partnershipId);
            var revenueShare = MapToRevenueShare(request);

            var result = await _billingService.ProcessPartnershipRevenueAsync(partnershipIdValue, revenueShare);

            if (result.IsFailure)
            {
                _logger.LogWarning("Failed to process partnership revenue: {Error}", result.Error);
                return BadRequest(new { error = result.Error });
            }

            return Ok(new 
            { 
                message = "Partnership revenue processed successfully",
                partnershipId = partnershipId,
                sharePercentage = request.SharePercentage,
                authenticityBonus = request.AuthenticityBonusPercentage
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing partnership revenue");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Get Cultural Intelligence revenue analytics
    /// </summary>
    [HttpGet("analytics/revenue")]
    [Authorize(Roles = "Admin,FinanceAdmin")]
    public async Task<IActionResult> GetRevenueAnalytics([FromQuery] GetRevenueAnalyticsRequest request)
    {
        try
        {
            _logger.LogInformation("Generating revenue analytics for period {StartDate} to {EndDate}", 
                request.StartDate, request.EndDate);

            var timeRange = new TimeRange(request.StartDate, request.EndDate);
            var result = await _billingService.GetCulturalRevenueAnalyticsAsync(timeRange);

            if (result.IsFailure)
            {
                _logger.LogWarning("Failed to generate revenue analytics: {Error}", result.Error);
                return BadRequest(new { error = result.Error });
            }

            var analytics = result.Value;
            return Ok(new
            {
                totalRevenue = analytics.RevenueMetrics.TotalRevenue,
                monthlyRecurringRevenue = analytics.RevenueMetrics.MonthlyRecurringRevenue,
                averageRevenuePerUser = analytics.RevenueMetrics.AverageRevenuePerUser,
                totalSubscriptions = analytics.RevenueMetrics.TotalSubscriptions,
                totalAPIRequests = analytics.UsageMetrics.TotalAPIRequests,
                buddhistCalendarRequests = analytics.UsageMetrics.BuddhistCalendarRequests,
                culturalAppropriatenessRequests = analytics.UsageMetrics.CulturalAppropriatenessRequests,
                diasporaAnalyticsRequests = analytics.UsageMetrics.DiasporaAnalyticsRequests,
                customerBreakdown = new
                {
                    community = analytics.CustomerMetrics.CommunityTierUsers,
                    professional = analytics.CustomerMetrics.ProfessionalTierUsers,
                    enterprise = analytics.CustomerMetrics.EnterpriseTierUsers,
                    custom = analytics.CustomerMetrics.CustomTierUsers
                },
                churnRate = analytics.CustomerMetrics.ChurnRate,
                featureUsage = analytics.CulturalMetrics.FeatureUsage,
                featureRevenue = analytics.CulturalMetrics.FeatureRevenue
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating revenue analytics");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Stripe webhook endpoint for billing events
    /// </summary>
    [HttpPost("webhooks/stripe")]
    [AllowAnonymous]
    public async Task<IActionResult> HandleStripeWebhook()
    {
        try
        {
            var payload = await new StreamReader(Request.Body).ReadToEndAsync();
            var signature = Request.Headers["Stripe-Signature"].FirstOrDefault();

            if (string.IsNullOrEmpty(signature))
            {
                _logger.LogWarning("Missing Stripe signature in webhook request");
                return BadRequest("Missing signature");
            }

            var webhookResult = await _stripeService.ProcessWebhookAsync(payload, signature);
            
            if (webhookResult.IsFailure)
            {
                _logger.LogError("Failed to process Stripe webhook: {Error}", webhookResult.Error);
                return BadRequest(new { error = webhookResult.Error });
            }

            var webhookEvent = webhookResult.Value;
            var handlingResult = await _webhookHandler.HandleWebhookEventAsync(webhookEvent);

            if (handlingResult.IsFailure)
            {
                _logger.LogError("Failed to handle webhook event: {Error}", handlingResult.Error);
                return StatusCode(500, new { error = handlingResult.Error });
            }

            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing Stripe webhook");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Get available Cultural Intelligence tiers and pricing
    /// </summary>
    [HttpGet("tiers")]
    [AllowAnonymous]
    public IActionResult GetCulturalIntelligenceTiers()
    {
        try
        {
            var communityTier = CulturalIntelligenceTier.CreateCommunityTier();
            var professionalTier = CulturalIntelligenceTier.CreateProfessionalTier();
            var enterpriseTier = CulturalIntelligenceTier.CreateEnterpriseTier();

            return Ok(new
            {
                tiers = new[]
                {
                    MapTierToResponse(communityTier),
                    MapTierToResponse(professionalTier),
                    MapTierToResponse(enterpriseTier)
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving Cultural Intelligence tiers");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    // Private helper methods
    private CulturalIntelligenceTier MapRequestToTier(CreateCulturalIntelligenceSubscriptionRequest request)
    {
        return request.TierName.ToLower() switch
        {
            "community" => CulturalIntelligenceTier.CreateCommunityTier(),
            "professional" => CulturalIntelligenceTier.CreateProfessionalTier(),
            "enterprise" => CulturalIntelligenceTier.CreateEnterpriseTier(),
            _ => throw new ArgumentException($"Unknown tier: {request.TierName}")
        };
    }

    private CulturalIntelligenceRequest MapToCulturalIntelligenceRequest(ProcessCulturalIntelligenceUsageRequest request)
    {
        var endpoint = CulturalIntelligenceEndpoint.BuddhistCalendar(request.Endpoint);
        var complexityScore = new CulturalComplexityScore(request.ComplexityScore, Array.Empty<ComplexityFactor>());
        var metadata = new RequestMetadata(new Dictionary<string, object>
        {
            ["client_id"] = request.ClientId ?? "",
            ["request_time"] = DateTime.UtcNow
        });

        return new CulturalIntelligenceRequest(endpoint, complexityScore, metadata);
    }

    private BuddhistCalendarRequest MapToBuddhistCalendarRequest(ProcessBuddhistCalendarUsageRequest request)
    {
        var precisionLevel = Enum.Parse<CalendarPrecisionLevel>(request.PrecisionLevel);
        var calculationType = Enum.Parse<AstronomicalCalculationType>(request.CalculationType);
        var variations = request.Variations?.Select(v => CustomCalendarVariation.Create(v)).ToArray() ?? Array.Empty<CustomCalendarVariation>();

        return new BuddhistCalendarRequest(precisionLevel, calculationType, variations, request.RequestedDate);
    }

    private CulturalAppropriatenessRequest MapToCulturalAppropriatenessRequest(ProcessCulturalAppropriatenessUsageRequest request)
    {
        var content = ContentToValidate.Create(request.Content, Enum.Parse<ContentType>(request.ContentType));
        var contexts = request.Contexts?.Select(c => CulturalContext.Buddhist(c)).ToArray() ?? Array.Empty<CulturalContext>();
        var complexityLevel = Enum.Parse<ValidationComplexityLevel>(request.ComplexityLevel);
        var realTimeModeration = request.RealTimeModeration ? RealTimeModeration.Enabled() : RealTimeModeration.Disabled();

        return new CulturalAppropriatenessRequest(content, contexts, complexityLevel, realTimeModeration);
    }

    private DiasporaAnalyticsRequest MapToDiasporaAnalyticsRequest(ProcessDiasporaAnalyticsUsageRequest request)
    {
        var analyticsType = Enum.Parse<AnalyticsType>(request.AnalyticsType);
        var regions = request.Regions?.Select(r => GeographicRegion.Create(r)).ToArray() ?? Array.Empty<GeographicRegion>();
        var segments = request.Segments?.Select(s => CommunitySegment.Create(s)).ToArray() ?? Array.Empty<CommunitySegment>();
        var timeframe = request.TimeframeMonths switch
        {
            3 => PredictionTimeframe.ThreeMonths(),
            6 => PredictionTimeframe.SixMonths(),
            12 => PredictionTimeframe.OneYear(),
            _ => PredictionTimeframe.SixMonths()
        };

        return new DiasporaAnalyticsRequest(analyticsType, regions, segments, timeframe);
    }

    private EnterpriseClient MapToEnterpriseClient(CreateEnterpriseContractRequest request)
    {
        var clientId = new EnterpriseClientId();
        var companyName = CompanyName.Create(request.CompanyName);
        var contactInfo = ContactInfo.Create(request.ContactEmail, request.ContactPhone);
        var culturalRequirements = CulturalRequirements.Create(request.RequiredCultures, request.RequiredLanguages);
        var customPricing = CustomPricing.Create(request.ContractValue, request.PricingDescription);

        return new EnterpriseClient(
            clientId,
            companyName,
            contactInfo,
            culturalRequirements,
            customPricing,
            request.ContractStartDate,
            request.ContractEndDate);
    }

    private CulturalServicesContract MapToCulturalServicesContract(CreateEnterpriseContractRequest request)
    {
        var contractId = new ContractId();
        var services = request.Services.Select(s => new CulturalService(s.Name, s.Price, s.Description)).ToArray();
        var contractValue = new ContractValue(request.ContractValue, Currency.USD());
        var paymentSchedule = new PaymentSchedule(
            Enum.Parse<PaymentFrequency>(request.PaymentFrequency),
            request.FirstPaymentDate,
            request.NumberOfPayments);
        var consultingHours = new CulturalConsultingHours(request.ConsultingHours, request.ConsultingHourlyRate);
        var whiteLabelLicensing = request.WhiteLabelLicensing != null 
            ? new WhiteLabelLicensing(true, request.WhiteLabelLicensing.SetupFee, request.WhiteLabelLicensing.MonthlyFee)
            : null;

        return new CulturalServicesContract(contractId, services, contractValue, paymentSchedule, consultingHours, whiteLabelLicensing);
    }

    private RevenueShare MapToRevenueShare(ProcessPartnershipRevenueRequest request)
    {
        var authenticityBonus = CulturalAuthenticityBonus.Create(
            request.AuthenticityBonusPercentage,
            "Cultural authenticity bonus");

        return new RevenueShare(
            request.SharePercentage,
            request.MinimumAmount,
            request.MaximumAmount,
            authenticityBonus);
    }

    private object MapTierToResponse(CulturalIntelligenceTier tier)
    {
        return new
        {
            name = tier.Name.Value,
            price = new
            {
                amount = tier.BasePrice.Amount,
                currency = tier.BasePrice.Currency.Code,
                isFree = tier.BasePrice.IsFree
            },
            requestLimit = new
            {
                limit = tier.RequestLimit.Limit,
                isUnlimited = tier.RequestLimit.IsUnlimited
            },
            features = new
            {
                basicCalendar = tier.FeatureAccess.BasicCalendarAccess,
                buddhistPremium = tier.FeatureAccess.BuddhistCalendarPremium,
                hinduPremium = tier.FeatureAccess.HinduCalendarPremium,
                aiRecommendations = tier.FeatureAccess.AIRecommendations,
                culturalScoring = tier.FeatureAccess.CulturalAppropriatenessScoring,
                diasporaAnalytics = tier.FeatureAccess.DiasporaAnalytics,
                multiLanguage = tier.FeatureAccess.MultiLanguageSupport,
                webhooks = tier.FeatureAccess.WebhookSupport,
                customAI = tier.FeatureAccess.CustomAIModels,
                whiteLabel = tier.FeatureAccess.WhiteLabelLicensing,
                consulting = tier.FeatureAccess.CulturalConsultingServices
            },
            usagePricing = new
            {
                culturalValidation = tier.UsagePricing.CulturalAppropriatenessValidation.Amount,
                diasporaAnalysis = tier.UsagePricing.DiasporaAnalysis.Amount,
                multiLanguageTranslation = tier.UsagePricing.MultiLanguageTranslation.Amount,
                culturalConflictResolution = tier.UsagePricing.CulturalConflictResolution.Amount,
                customMarketResearch = tier.UsagePricing.CustomMarketResearch.Amount
            },
            sla = new
            {
                level = tier.ServiceLevel.Level,
                responseTime = tier.ServiceLevel.ResponseTime.TotalHours,
                uptimeGuarantee = tier.ServiceLevel.UptimeGuarantee,
                dedicatedSupport = tier.ServiceLevel.DedicatedSupport
            }
        };
    }

    private decimal CalculateBuddhistCalendarCost(ProcessBuddhistCalendarUsageRequest request)
    {
        var baseCost = request.PrecisionLevel switch
        {
            "Basic" => 0.05m,
            "Advanced" => 0.10m,
            "AstronomicalPrecision" => 0.20m,
            "CustomVariation" => 0.30m,
            _ => 0.05m
        };

        var variationCost = (request.Variations?.Length ?? 0) * 0.05m;
        return baseCost + variationCost;
    }

    private decimal CalculateCulturalAppropriatenessCost(ProcessCulturalAppropriatenessUsageRequest request)
    {
        var baseCost = request.ComplexityLevel switch
        {
            "Basic" => 0.10m,
            "Advanced" => 0.15m,
            "MultiCultural" => 0.25m,
            "RealTime" => 0.30m,
            _ => 0.10m
        };

        var contextMultiplier = Math.Max(1.0m, (request.Contexts?.Length ?? 1) * 0.5m);
        var realTimeMultiplier = request.RealTimeModeration ? 1.5m : 1.0m;

        return baseCost * contextMultiplier * realTimeMultiplier;
    }

    private decimal CalculateDiasporaAnalyticsCost(ProcessDiasporaAnalyticsUsageRequest request)
    {
        var baseCost = request.AnalyticsType switch
        {
            "GeographicClustering" => 0.25m,
            "CommunityEngagement" => 0.20m,
            "CulturalTrendPrediction" => 0.50m,
            "CustomMarketResearch" => 2500.00m,
            _ => 0.25m
        };

        var complexityMultiplier = 1.0m + ((request.Regions?.Length ?? 0) * 0.1m) + ((request.Segments?.Length ?? 0) * 0.05m);
        var timeframeMultiplier = request.TimeframeMonths > 6 ? 1.5m : 1.0m;

        return baseCost * complexityMultiplier * timeframeMultiplier;
    }
}