using LankaConnect.Domain.Billing;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Common.Enums;
using LankaConnect.Domain.Common.Monitoring;
using LankaConnect.Domain.Shared.ValueObjects;
using LankaConnect.Domain.Shared;
using LankaConnect.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;
using MediatR;

namespace LankaConnect.Application.Billing;

/// <summary>
/// Application service for Cultural Intelligence billing operations
/// Implements Stripe integration for advanced billing scenarios
/// </summary>
public class CulturalIntelligenceBillingService : ICulturalIntelligenceBillingService
{
    private readonly IStripePaymentService _stripeService;
    private readonly IBillingRepository _billingRepository;
    private readonly ICulturalCalendarService _culturalCalendarService;
    private readonly ICulturalAppropriatenessService _appropriatenessService;
    private readonly IDiasporaAnalyticsService _diasporaAnalyticsService;
    private readonly IUsageTrackingService _usageTrackingService;
    private readonly IMediator _mediator;
    private readonly ILogger<CulturalIntelligenceBillingService> _logger;

    public CulturalIntelligenceBillingService(
        IStripePaymentService stripeService,
        IBillingRepository billingRepository,
        ICulturalCalendarService culturalCalendarService,
        ICulturalAppropriatenessService appropriatenessService,
        IDiasporaAnalyticsService diasporaAnalyticsService,
        IUsageTrackingService usageTrackingService,
        IMediator mediator,
        ILogger<CulturalIntelligenceBillingService> logger)
    {
        _stripeService = stripeService ?? throw new ArgumentNullException(nameof(stripeService));
        _billingRepository = billingRepository ?? throw new ArgumentNullException(nameof(billingRepository));
        _culturalCalendarService = culturalCalendarService ?? throw new ArgumentNullException(nameof(culturalCalendarService));
        _appropriatenessService = appropriatenessService ?? throw new ArgumentNullException(nameof(appropriatenessService));
        _diasporaAnalyticsService = diasporaAnalyticsService ?? throw new ArgumentNullException(nameof(diasporaAnalyticsService));
        _usageTrackingService = usageTrackingService ?? throw new ArgumentNullException(nameof(usageTrackingService));
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result> CreateCulturalSubscriptionAsync(
        UserId userId, 
        CulturalIntelligenceTier tier, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Creating cultural intelligence subscription for user {UserId} with tier {TierName}", 
                userId.Value, tier.Name.Value);

            // Validate user eligibility
            var userValidation = await ValidateUserEligibilityAsync(userId, cancellationToken);
            if (userValidation.IsFailure)
            {
                return Result.Failure($"User validation failed: {userValidation.Error}");
            }

            // Create Stripe subscription if not free tier
            if (!tier.BasePrice.IsFree)
            {
                var stripeResult = await _stripeService.CreateSubscriptionAsync(new CreateStripeSubscriptionRequest
                {
                    UserId = userId,
                    PriceAmount = tier.BasePrice.Amount,
                    Currency = tier.BasePrice.Currency.Code,
                    TierName = tier.Name.Value,
                    Features = MapTierFeatures(tier.FeatureAccess)
                }, cancellationToken);

                if (stripeResult.IsFailure)
                {
                    _logger.LogError("Failed to create Stripe subscription: {Error}", stripeResult.Error);
                    return Result.Failure($"Payment setup failed: {stripeResult.Error}");
                }
            }

            // Create billing record
            var billingSubscription = CulturalIntelligenceSubscription.Create(
                CulturalIntelligenceSubscriptionId.New(),
                userId,
                tier,
                DateTime.UtcNow,
                DateTime.UtcNow.AddMonths(1));

            var saveResult = await _billingRepository.SaveSubscriptionAsync(billingSubscription, cancellationToken);
            if (saveResult.IsFailure)
            {
                _logger.LogError("Failed to save billing subscription: {Error}", saveResult.Error);
                return Result.Failure($"Subscription creation failed: {saveResult.Error}");
            }

            // Generate API key for the subscription
            var apiKey = new APIKey(
                GenerateApiKey(userId, tier.Name.Value),
                MapTierToAPIKeyTier(tier.Name.Value),
                userId,
                DateTime.UtcNow.AddYears(1));

            await _billingRepository.SaveAPIKeyAsync(apiKey, cancellationToken);

            _logger.LogInformation("Successfully created cultural intelligence subscription for user {UserId}", userId.Value);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating cultural intelligence subscription for user {UserId}", userId.Value);
            return Result.Failure($"Subscription creation failed: {ex.Message}");
        }
    }

    public async Task<Result> ProcessCulturalAPIUsageAsync(
        APIKey apiKey, 
        CulturalIntelligenceRequest request, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Processing cultural API usage for key {ApiKey}", apiKey.Value);

            // Validate API key
            if (apiKey.IsExpired)
            {
                return Result.Failure("API key has expired");
            }

            var subscription = await _billingRepository.GetSubscriptionByUserIdAsync(apiKey.AssociatedUser, cancellationToken);
            if (subscription == null)
            {
                return Result.Failure("No active subscription found");
            }

            // Check rate limits
            var rateLimitCheck = await CheckRateLimitsAsync(apiKey, subscription.Tier, cancellationToken);
            if (rateLimitCheck.IsFailure)
            {
                return rateLimitCheck;
            }

            // Calculate usage cost
            var usageCost = CalculateUsageCost(request, subscription.Tier);

            // Track usage
            var usage = new CulturalAPIUsage(
                apiKey,
                ConvertAPIEndpointToCulturalIntelligenceEndpoint(request.Endpoint),
                usageCost,
                request.ComplexityScore,
                ConvertRequestMetadataToUsageMetadata(request.Metadata));

            await _usageTrackingService.TrackUsageAsync(usage, cancellationToken);

            // Charge for usage if applicable
            if (usageCost.TotalAmount > 0 && !subscription.Tier.BasePrice.IsFree)
            {
                var chargeResult = await _stripeService.ChargeUsageAsync(new ChargeUsageRequest
                {
                    UserId = apiKey.AssociatedUser,
                    Amount = usageCost.TotalAmount,
                    Currency = usageCost.Currency.Code,
                    Description = $"Cultural Intelligence API usage - {request.Endpoint.Path}",
                    Metadata = new Dictionary<string, string>
                    {
                        ["endpoint"] = request.Endpoint.Path,
                        ["complexity_score"] = request.ComplexityScore.Score.ToString(),
                        ["api_key"] = apiKey.Value
                    }
                }, cancellationToken);

                if (chargeResult.IsFailure)
                {
                    _logger.LogError("Failed to charge for usage: {Error}", chargeResult.Error);
                    return Result.Failure($"Usage billing failed: {chargeResult.Error}");
                }
            }

            _logger.LogInformation("Successfully processed cultural API usage for {Endpoint}", request.Endpoint.Path);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing cultural API usage");
            return Result.Failure($"Usage processing failed: {ex.Message}");
        }
    }

    public async Task<Result> ProcessBuddhistCalendarPremiumUsageAsync(
        APIKey apiKey, 
        BuddhistCalendarRequest request, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Processing Buddhist calendar premium usage");

            var subscription = await _billingRepository.GetSubscriptionByUserIdAsync(apiKey.AssociatedUser, cancellationToken);
            if (subscription == null || !subscription.Tier.FeatureAccess.BuddhistCalendarPremium)
            {
                return Result.Failure("Buddhist calendar premium features not available for this subscription");
            }

            // Calculate premium pricing based on precision level
            var premiumCost = CalculateBuddhistCalendarPremiumCost(request, subscription.Tier);

            if (premiumCost.TotalAmount > 0)
            {
                var chargeResult = await _stripeService.ChargeUsageAsync(new ChargeUsageRequest
                {
                    UserId = apiKey.AssociatedUser,
                    Amount = premiumCost.TotalAmount,
                    Currency = premiumCost.Currency.Code,
                    Description = $"Buddhist Calendar Premium - {request.PrecisionLevel} precision",
                    Metadata = new Dictionary<string, string>
                    {
                        ["feature"] = "buddhist_calendar_premium",
                        ["precision_level"] = request.PrecisionLevel.ToString(),
                        ["calculation_type"] = request.CalculationType.ToString(),
                        ["variations_count"] = request.Variations.Length.ToString()
                    }
                }, cancellationToken);

                if (chargeResult.IsFailure)
                {
                    return Result.Failure($"Buddhist calendar premium billing failed: {chargeResult.Error}");
                }
            }

            // Process the actual calendar request
            await _culturalCalendarService.ProcessBuddhistCalendarRequestAsync(request, cancellationToken);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing Buddhist calendar premium usage");
            return Result.Failure($"Buddhist calendar premium processing failed: {ex.Message}");
        }
    }

    public async Task<Result> ProcessCulturalAppropriatenesScoringAsync(
        APIKey apiKey, 
        CulturalAppropriatenessRequest request, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Processing cultural appropriateness scoring");

            var subscription = await _billingRepository.GetSubscriptionByUserIdAsync(apiKey.AssociatedUser, cancellationToken);
            if (subscription == null || !subscription.Tier.FeatureAccess.CulturalAppropriatenessScoring)
            {
                return Result.Failure("Cultural appropriateness scoring not available for this subscription");
            }

            // Calculate pricing based on complexity and real-time requirements
            var scoringCost = CalculateCulturalAppropriatenessCost(request, subscription.Tier);

            if (scoringCost.TotalAmount > 0)
            {
                var chargeResult = await _stripeService.ChargeUsageAsync(new ChargeUsageRequest
                {
                    UserId = apiKey.AssociatedUser,
                    Amount = scoringCost.TotalAmount,
                    Currency = scoringCost.Currency.Code,
                    Description = $"Cultural Appropriateness Scoring - {request.ComplexityLevel}",
                    Metadata = new Dictionary<string, string>
                    {
                        ["feature"] = "cultural_appropriateness_scoring",
                        ["complexity_level"] = request.ComplexityLevel.ToString(),
                        ["contexts_count"] = request.Contexts.Length.ToString(),
                        ["real_time"] = request.RealTimeModeration.IsEnabled.ToString()
                    }
                }, cancellationToken);

                if (chargeResult.IsFailure)
                {
                    return Result.Failure($"Cultural appropriateness scoring billing failed: {chargeResult.Error}");
                }
            }

            // Process the actual appropriateness scoring
            await _appropriatenessService.ProcessAppropriatenessRequestAsync(request, cancellationToken);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing cultural appropriateness scoring");
            return Result.Failure($"Cultural appropriateness scoring failed: {ex.Message}");
        }
    }

    public async Task<Result> ProcessDiasporaAnalyticsUsageAsync(
        APIKey apiKey, 
        DiasporaAnalyticsRequest request, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Processing diaspora analytics usage");

            var subscription = await _billingRepository.GetSubscriptionByUserIdAsync(apiKey.AssociatedUser, cancellationToken);
            if (subscription == null || !subscription.Tier.FeatureAccess.DiasporaAnalytics)
            {
                return Result.Failure("Diaspora analytics not available for this subscription");
            }

            // Calculate pricing based on analytics type and complexity
            var analyticsCost = CalculateDiasporaAnalyticsCost(request, subscription.Tier);

            if (analyticsCost.TotalAmount > 0)
            {
                var chargeResult = await _stripeService.ChargeUsageAsync(new ChargeUsageRequest
                {
                    UserId = apiKey.AssociatedUser,
                    Amount = analyticsCost.TotalAmount,
                    Currency = analyticsCost.Currency.Code,
                    Description = $"Diaspora Analytics - {request.AnalyticsType}",
                    Metadata = new Dictionary<string, string>
                    {
                        ["feature"] = "diaspora_analytics",
                        ["analytics_type"] = request.AnalyticsType.ToString(),
                        ["regions_count"] = request.Regions.Length.ToString(),
                        ["segments_count"] = request.Segments.Length.ToString(),
                        ["timeframe"] = request.Timeframe.Description
                    }
                }, cancellationToken);

                if (chargeResult.IsFailure)
                {
                    return Result.Failure($"Diaspora analytics billing failed: {chargeResult.Error}");
                }
            }

            // Process the actual analytics request
            await _diasporaAnalyticsService.ProcessAnalyticsRequestAsync(request, cancellationToken);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing diaspora analytics");
            return Result.Failure($"Diaspora analytics processing failed: {ex.Message}");
        }
    }

    public async Task<Result> ProcessEnterpriseContractAsync(
        EnterpriseClient client, 
        CulturalServicesContract contract, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Processing enterprise contract for client {ClientId}", client.Id);

            if (!client.IsContractActive())
            {
                return Result.Failure("Enterprise contract is not currently active");
            }

            // Create Stripe subscription for enterprise contract
            var stripeResult = await _stripeService.CreateEnterpriseSubscriptionAsync(new CreateEnterpriseSubscriptionRequest
            {
                ClientId = client.Id,
                ContractValue = contract.TotalValue.Amount,
                Currency = contract.TotalValue.Currency.Code,
                PaymentSchedule = contract.PaymentSchedule,
                Services = contract.Services,
                ConsultingHours = contract.ConsultingHours,
                WhiteLabelLicensing = contract.WhiteLabelLicensing
            }, cancellationToken);

            if (stripeResult.IsFailure)
            {
                _logger.LogError("Failed to create enterprise subscription: {Error}", stripeResult.Error);
                return Result.Failure($"Enterprise contract setup failed: {stripeResult.Error}");
            }

            // Save contract to repository
            await _billingRepository.SaveEnterpriseContractAsync(contract, cancellationToken);

            _logger.LogInformation("Successfully processed enterprise contract for client {ClientId}", client.Id);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing enterprise contract for client {ClientId}", client.Id);
            return Result.Failure($"Enterprise contract processing failed: {ex.Message}");
        }
    }

    public async Task<Result> ProcessPartnershipRevenueAsync(
        PartnershipId partnershipId, 
        RevenueShare revenueShare, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Processing partnership revenue for partnership {PartnershipId}", partnershipId.Value);

            // Get partnership details
            var partnership = await _billingRepository.GetPartnershipAsync(partnershipId, cancellationToken);
            if (partnership == null)
            {
                return Result.Failure("Partnership not found");
            }

            // Calculate total revenue for the period
            var totalRevenue = await _billingRepository.GetPartnershipRevenueAsync(partnershipId, cancellationToken);

            // Calculate revenue share including authenticity bonuses
            var shareAmount = revenueShare.CalculateShare(totalRevenue);

            // Process payout via Stripe
            var payoutResult = await _stripeService.CreatePartnerPayoutAsync(new CreatePartnerPayoutRequest
            {
                PartnershipId = partnershipId.Value,
                Amount = shareAmount,
                Currency = "USD", // Default to USD for partnerships
                Description = $"Revenue share payment - {DateTime.UtcNow:yyyy-MM}",
                Metadata = new Dictionary<string, string>
                {
                    ["partnership_id"] = partnershipId.Value.ToString(),
                    ["total_revenue"] = totalRevenue.ToString("F2"),
                    ["share_percentage"] = revenueShare.Percentage.ToString("F2"),
                    ["authenticity_bonus"] = revenueShare.AuthenticityBonus.BonusPercentage.ToString("F2")
                }
            }, cancellationToken);

            if (payoutResult.IsFailure)
            {
                _logger.LogError("Failed to create partner payout: {Error}", payoutResult.Error);
                return Result.Failure($"Partnership payout failed: {payoutResult.Error}");
            }

            _logger.LogInformation("Successfully processed partnership revenue for partnership {PartnershipId}", partnershipId.Value);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing partnership revenue for partnership {PartnershipId}", partnershipId.Value);
            return Result.Failure($"Partnership revenue processing failed: {ex.Message}");
        }
    }

    public async Task<Result<BillingAnalytics>> GetCulturalRevenueAnalyticsAsync(
        TimeRange timeRange, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Generating cultural revenue analytics for period {StartDate} to {EndDate}", 
                timeRange.StartDate, timeRange.EndDate);

            var revenueMetrics = await _billingRepository.GetRevenueMetricsAsync(timeRange, cancellationToken);
            var usageMetrics = await _billingRepository.GetUsageMetricsAsync(timeRange, cancellationToken);
            var customerMetrics = await _billingRepository.GetCustomerMetricsAsync(timeRange, cancellationToken);
            var culturalMetrics = await _billingRepository.GetCulturalFeatureMetricsAsync(timeRange, cancellationToken);

            var analytics = new BillingAnalytics(
                revenueMetrics,
                usageMetrics,
                customerMetrics,
                culturalMetrics,
                timeRange);

            return Result<BillingAnalytics>.Success(analytics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating cultural revenue analytics");
            return Result<BillingAnalytics>.Failure($"Analytics generation failed: {ex.Message}");
        }
    }

    // Private helper methods
    private async Task<Result> ValidateUserEligibilityAsync(UserId userId, CancellationToken cancellationToken)
    {
        // Implement user validation logic
        var existingSubscription = await _billingRepository.GetSubscriptionByUserIdAsync(userId, cancellationToken);
        if (existingSubscription != null && existingSubscription.IsActive)
        {
            return Result.Failure("User already has an active subscription");
        }

        return Result.Success();
    }

    private async Task<Result> CheckRateLimitsAsync(APIKey apiKey, CulturalIntelligenceTier tier, CancellationToken cancellationToken)
    {
        var currentUsage = await _usageTrackingService.GetCurrentMonthlyUsageAsync(apiKey.AssociatedUser, cancellationToken);
        
        if (!tier.RequestLimit.IsUnlimited && currentUsage >= tier.RequestLimit.Limit)
        {
            return Result.Failure($"Monthly request limit of {tier.RequestLimit.Limit} exceeded");
        }

        return Result.Success();
    }

    private UsageCost CalculateUsageCost(CulturalIntelligenceRequest request, CulturalIntelligenceTier tier)
    {
        var baseAmount = GetBaseAmountForEndpoint(ConvertAPIEndpointToCulturalIntelligenceEndpoint(request.Endpoint));
        var complexityMultiplier = request.ComplexityScore.BillingMultiplier;
        var tierMultiplier = GetTierMultiplier(tier.Name.Value);

        var finalMultiplier = complexityMultiplier * tierMultiplier;

        var breakdown = CostBreakdown.Create(baseAmount, finalMultiplier, 0.0m);

        return new UsageCost(baseAmount, finalMultiplier, Currency.USD(), breakdown);
    }

    private UsageCost CalculateBuddhistCalendarPremiumCost(BuddhistCalendarRequest request, CulturalIntelligenceTier tier)
    {
        var baseAmount = request.PrecisionLevel switch
        {
            CalendarPrecisionLevel.Basic => 0.05m,
            CalendarPrecisionLevel.Advanced => 0.10m,
            CalendarPrecisionLevel.AstronomicalPrecision => 0.20m,
            CalendarPrecisionLevel.CustomVariation => 0.30m,
            _ => 0.05m
        };

        // Additional charges for variations
        var variationCharges = request.Variations.Length * 0.05m;
        var totalBase = baseAmount + variationCharges;

        var tierMultiplier = GetTierMultiplier(tier.Name.Value);
        var breakdown = CostBreakdown.Create(totalBase, tierMultiplier, 0.0m);

        return new UsageCost(totalBase, tierMultiplier, Currency.USD(), breakdown);
    }

    private UsageCost CalculateCulturalAppropriatenessCost(CulturalAppropriatenessRequest request, CulturalIntelligenceTier tier)
    {
        var baseAmount = request.ComplexityLevel switch
        {
            ValidationComplexityLevel.Basic => tier.UsagePricing.CulturalAppropriatenessValidation.Amount,
            ValidationComplexityLevel.Advanced => tier.UsagePricing.CulturalAppropriatenessValidation.Amount * 1.5m,
            ValidationComplexityLevel.MultiCultural => tier.UsagePricing.CulturalAppropriatenessValidation.Amount * 2.5m,
            ValidationComplexityLevel.RealTime => tier.UsagePricing.CulturalAppropriatenessValidation.Amount * 3.0m,
            _ => tier.UsagePricing.CulturalAppropriatenessValidation.Amount
        };

        // Additional charges for multiple contexts
        var contextMultiplier = Math.Max(1.0m, request.Contexts.Length * 0.5m);
        var realTimeMultiplier = request.RealTimeModeration.IsEnabled ? 1.5m : 1.0m;

        var totalMultiplier = contextMultiplier * realTimeMultiplier;
        var breakdown = CostBreakdown.Create(baseAmount, totalMultiplier, 0.0m);

        return new UsageCost(baseAmount, totalMultiplier, Currency.USD(), breakdown);
    }

    private UsageCost CalculateDiasporaAnalyticsCost(DiasporaAnalyticsRequest request, CulturalIntelligenceTier tier)
    {
        var baseAmount = request.AnalyticsType switch
        {
            AnalyticsType.GeographicClustering => tier.UsagePricing.DiasporaAnalysis.Amount,
            AnalyticsType.CommunityEngagement => tier.UsagePricing.DiasporaAnalysis.Amount * 0.8m,
            AnalyticsType.CulturalTrendPrediction => tier.UsagePricing.DiasporaAnalysis.Amount * 2.0m,
            AnalyticsType.CustomMarketResearch => tier.UsagePricing.CustomMarketResearch.Amount,
            _ => tier.UsagePricing.DiasporaAnalysis.Amount
        };

        // Additional charges for regions and segments
        var complexityMultiplier = 1.0m + (request.Regions.Length * 0.1m) + (request.Segments.Length * 0.05m);
        var timeframeMultiplier = request.Timeframe.Months > 6 ? 1.5m : 1.0m;

        var totalMultiplier = complexityMultiplier * timeframeMultiplier;
        var breakdown = CostBreakdown.Create(baseAmount, totalMultiplier, 0.0m);

        return new UsageCost(baseAmount, totalMultiplier, Currency.USD(), breakdown);
    }

    private decimal GetBaseAmountForEndpoint(LankaConnect.Domain.Billing.BillingEndpoint endpoint)
    {
        return endpoint.Category switch
        {
            EndpointCategory.BuddhistCalendar => 0.05m,
            EndpointCategory.HinduCalendar => 0.05m,
            EndpointCategory.CulturalAppropriateness => 0.10m,
            EndpointCategory.DiasporaAnalytics => 0.25m,
            EndpointCategory.MultiLanguage => 0.15m,
            EndpointCategory.CulturalConsulting => 1.00m,
            _ => 0.05m
        };
    }

    private decimal GetTierMultiplier(string tierName)
    {
        return tierName.ToLower() switch
        {
            "community" => 0.0m,    // Free tier
            "professional" => 1.0m,  // Standard pricing
            "enterprise" => 0.8m,    // 20% discount
            "custom" => 0.7m,        // 30% discount
            _ => 1.0m
        };
    }

    private APIKeyTier MapTierToAPIKeyTier(string tierName)
    {
        return tierName.ToLower() switch
        {
            "community" => APIKeyTier.Community,
            "professional" => APIKeyTier.Professional,
            "enterprise" => APIKeyTier.Enterprise,
            "custom" => APIKeyTier.Custom,
            _ => APIKeyTier.Community
        };
    }

    private Dictionary<string, bool> MapTierFeatures(CulturalFeatureAccess featureAccess)
    {
        return new Dictionary<string, bool>
        {
            ["basic_calendar"] = featureAccess.BasicCalendarAccess,
            ["buddhist_premium"] = featureAccess.BuddhistCalendarPremium,
            ["hindu_premium"] = featureAccess.HinduCalendarPremium,
            ["ai_recommendations"] = featureAccess.AIRecommendations,
            ["cultural_scoring"] = featureAccess.CulturalAppropriatenessScoring,
            ["diaspora_analytics"] = featureAccess.DiasporaAnalytics,
            ["multi_language"] = featureAccess.MultiLanguageSupport,
            ["webhooks"] = featureAccess.WebhookSupport,
            ["custom_ai"] = featureAccess.CustomAIModels,
            ["white_label"] = featureAccess.WhiteLabelLicensing,
            ["consulting"] = featureAccess.CulturalConsultingServices
        };
    }

    private string GenerateApiKey(UserId userId, string tierName)
    {
        var prefix = tierName.ToLower() switch
        {
            "community" => "lc_comm",
            "professional" => "lc_prof",
            "enterprise" => "lc_ent",
            "custom" => "lc_cust",
            _ => "lc_comm"
        };

        return $"{prefix}_{userId.Value:N}_{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}";
    }

    private LankaConnect.Domain.Billing.BillingEndpoint ConvertAPIEndpointToCulturalIntelligenceEndpoint(APIEndpoint apiEndpoint)
    {
        return apiEndpoint.Category switch
        {
            EndpointCategory.BuddhistCalendar => LankaConnect.Domain.Billing.BillingEndpoint.BuddhistCalendar(apiEndpoint.Path),
            EndpointCategory.HinduCalendar => LankaConnect.Domain.Billing.BillingEndpoint.HinduCalendar(apiEndpoint.Path),
            EndpointCategory.CulturalAppropriateness => LankaConnect.Domain.Billing.BillingEndpoint.CulturalAppropriateness(apiEndpoint.Path),
            EndpointCategory.DiasporaAnalytics => LankaConnect.Domain.Billing.BillingEndpoint.DiasporaAnalytics(apiEndpoint.Path),
            EndpointCategory.MultiLanguage => LankaConnect.Domain.Billing.BillingEndpoint.MultiLanguage(apiEndpoint.Path),
            _ => LankaConnect.Domain.Billing.BillingEndpoint.BuddhistCalendar(apiEndpoint.Path)
        };
    }

    private UsageMetadata ConvertRequestMetadataToUsageMetadata(RequestMetadata requestMetadata)
    {
        return UsageMetadata.Create(
            requestMetadata.RequestId.ToString(),
            requestMetadata.ClientId?.ToString() ?? string.Empty
        );
    }
}