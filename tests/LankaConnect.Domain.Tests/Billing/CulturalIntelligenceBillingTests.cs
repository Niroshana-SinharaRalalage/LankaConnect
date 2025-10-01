using FluentAssertions;
using LankaConnect.Domain.Billing;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Shared;
using Xunit;

namespace LankaConnect.Domain.Tests.Billing;

public class CulturalIntelligenceBillingTests
{
    [Fact]
    public void CulturalIntelligenceTier_CreateCommunityTier_ShouldHaveCorrectFreeConfiguration()
    {
        // Arrange & Act
        var communityTier = CulturalIntelligenceTier.CreateCommunityTier();

        // Assert
        communityTier.Name.Value.Should().Be("Community");
        communityTier.BasePrice.IsFree.Should().BeTrue();
        communityTier.BasePrice.Amount.Should().Be(0m);
        communityTier.RequestLimit.Limit.Should().Be(1000);
        communityTier.FeatureAccess.BasicCalendarAccess.Should().BeTrue();
        communityTier.FeatureAccess.BuddhistCalendarPremium.Should().BeFalse();
        communityTier.FeatureAccess.CulturalAppropriatenessScoring.Should().BeFalse();
        communityTier.ServiceLevel.Level.Should().Be("Community");
    }

    [Fact]
    public void CulturalIntelligenceTier_CreateProfessionalTier_ShouldHaveProfessionalFeatures()
    {
        // Arrange & Act
        var professionalTier = CulturalIntelligenceTier.CreateProfessionalTier();

        // Assert
        professionalTier.Name.Value.Should().Be("Professional");
        professionalTier.BasePrice.Amount.Should().Be(99.00m);
        professionalTier.RequestLimit.Limit.Should().Be(25000);
        professionalTier.FeatureAccess.BuddhistCalendarPremium.Should().BeTrue();
        professionalTier.FeatureAccess.HinduCalendarPremium.Should().BeTrue();
        professionalTier.FeatureAccess.AIRecommendations.Should().BeTrue();
        professionalTier.FeatureAccess.CulturalAppropriatenessScoring.Should().BeTrue();
        professionalTier.FeatureAccess.WebhookSupport.Should().BeTrue();
        professionalTier.FeatureAccess.CustomAIModels.Should().BeFalse(); // Enterprise only
        professionalTier.ServiceLevel.Level.Should().Be("Business");
    }

    [Fact]
    public void CulturalIntelligenceTier_CreateEnterpriseTier_ShouldHaveAllFeatures()
    {
        // Arrange & Act
        var enterpriseTier = CulturalIntelligenceTier.CreateEnterpriseTier();

        // Assert
        enterpriseTier.Name.Value.Should().Be("Enterprise");
        enterpriseTier.BasePrice.Amount.Should().Be(999.00m);
        enterpriseTier.RequestLimit.IsUnlimited.Should().BeTrue();
        enterpriseTier.FeatureAccess.CustomAIModels.Should().BeTrue();
        enterpriseTier.FeatureAccess.WhiteLabelLicensing.Should().BeTrue();
        enterpriseTier.FeatureAccess.CulturalConsultingServices.Should().BeTrue();
        enterpriseTier.FeatureAccess.DiasporaAnalytics.Should().BeTrue();
        enterpriseTier.ServiceLevel.Level.Should().Be("Enterprise");
        enterpriseTier.ServiceLevel.DedicatedSupport.Should().BeTrue();
    }

    [Fact]
    public void UsageBasedPricing_Professional_ShouldHaveCorrectRates()
    {
        // Arrange & Act
        var professionalPricing = UsageBasedPricing.CreateProfessional();

        // Assert
        professionalPricing.CulturalAppropriatenessValidation.Amount.Should().Be(0.10m);
        professionalPricing.CulturalAppropriatenessValidation.Unit.Should().Be("validation");
        professionalPricing.DiasporaAnalysis.Amount.Should().Be(0.25m);
        professionalPricing.MultiLanguageTranslation.Amount.Should().Be(0.15m);
        professionalPricing.CulturalConflictResolution.Amount.Should().Be(0.20m);
        professionalPricing.CustomMarketResearch.Amount.Should().Be(2500.00m);
    }

    [Fact]
    public void UsageBasedPricing_Enterprise_ShouldHaveDiscountedRates()
    {
        // Arrange & Act
        var enterprisePricing = UsageBasedPricing.CreateEnterprise();

        // Assert
        enterprisePricing.CulturalAppropriatenessValidation.Amount.Should().Be(0.08m); // Discounted from 0.10m
        enterprisePricing.DiasporaAnalysis.Amount.Should().Be(0.20m); // Discounted from 0.25m
        enterprisePricing.MultiLanguageTranslation.Amount.Should().Be(0.12m); // Discounted from 0.15m
        enterprisePricing.CulturalConflictResolution.Amount.Should().Be(0.15m); // Discounted from 0.20m
        enterprisePricing.CustomMarketResearch.Amount.Should().Be(2000.00m); // Discounted from 2500.00m
    }

    [Fact]
    public void CulturalComplexityScore_HighComplexity_ShouldHaveCorrectBillingMultiplier()
    {
        // Arrange
        var complexityFactors = new ComplexityFactor[]
        {
            ComplexityFactor.Create("MultiCulturalContext", 30),
            ComplexityFactor.Create("RealTimeProcessing", 25),
            ComplexityFactor.Create("AdvancedAI", 35)
        };

        // Act
        var complexityScore = new CulturalComplexityScore(90, complexityFactors);

        // Assert
        complexityScore.Score.Should().Be(90);
        complexityScore.BillingMultiplier.Should().Be(2.0m); // Very high complexity
        complexityScore.Factors.Should().HaveCount(3);
    }

    [Fact]
    public void CulturalComplexityScore_MediumComplexity_ShouldHaveCorrectBillingMultiplier()
    {
        // Arrange & Act
        var complexityScore = new CulturalComplexityScore(60, Array.Empty<ComplexityFactor>());

        // Assert
        complexityScore.Score.Should().Be(60);
        complexityScore.BillingMultiplier.Should().Be(1.2m); // Medium complexity
    }

    [Fact]
    public void UsageCost_WithComplexityMultiplier_ShouldCalculateCorrectTotal()
    {
        // Arrange
        var breakdown = CostBreakdown.Create(
            baseAmount: 0.10m,
            complexityMultiplier: 1.5m,
            tierDiscount: 0.0m
        );

        // Act
        var usageCost = new UsageCost(0.10m, 1.5m, Currency.USD(), breakdown);

        // Assert
        usageCost.BaseAmount.Should().Be(0.10m);
        usageCost.ComplexityMultiplier.Should().Be(1.5m);
        usageCost.TotalAmount.Should().Be(0.15m); // 0.10 * 1.5
        usageCost.Currency.Code.Should().Be("USD");
    }

    [Fact]
    public void APIKey_CreationWithExpiration_ShouldTrackExpiry()
    {
        // Arrange
        var userId = UserId.Create();
        var expirationDate = DateTime.UtcNow.AddMonths(12);

        // Act
        var apiKey = new APIKey("test-api-key-123", APIKeyTier.Professional, userId, expirationDate);

        // Assert
        apiKey.Value.Should().Be("test-api-key-123");
        apiKey.Tier.Should().Be(APIKeyTier.Professional);
        apiKey.AssociatedUser.Should().Be(userId);
        apiKey.ExpiresAt.Should().Be(expirationDate);
        apiKey.IsExpired.Should().BeFalse();
    }

    [Fact]
    public void APIKey_ExpiredKey_ShouldReturnTrue()
    {
        // Arrange
        var userId = UserId.Create();
        var pastDate = DateTime.UtcNow.AddDays(-1);

        // Act
        var expiredApiKey = new APIKey("expired-key", APIKeyTier.Community, userId, pastDate);

        // Assert
        expiredApiKey.IsExpired.Should().BeTrue();
    }

    [Fact]
    public void BuddhistCalendarRequest_WithAstronomicalPrecision_ShouldCreateCorrectly()
    {
        // Arrange
        var variations = new CustomCalendarVariation[]
        {
            CustomCalendarVariation.Create("SriLankanBuddhist"),
            CustomCalendarVariation.Create("ThailandBuddhist")
        };
        var requestedDate = DateTime.UtcNow.AddDays(30);

        // Act
        var request = new BuddhistCalendarRequest(
            CalendarPrecisionLevel.AstronomicalPrecision,
            AstronomicalCalculationType.LunarCalculation,
            variations,
            requestedDate);

        // Assert
        request.PrecisionLevel.Should().Be(CalendarPrecisionLevel.AstronomicalPrecision);
        request.CalculationType.Should().Be(AstronomicalCalculationType.LunarCalculation);
        request.Variations.Should().HaveCount(2);
        request.RequestedDate.Should().Be(requestedDate);
    }

    [Fact]
    public void CulturalAppropriatenessRequest_WithMultipleCultures_ShouldCreateCorrectly()
    {
        // Arrange
        var content = ContentToValidate.Create("Test cultural content", ContentType.Text);
        var contexts = new CulturalContext[]
        {
            CulturalContext.Buddhist(),
            CulturalContext.Hindu(),
            CulturalContext.Christian()
        };

        // Act
        var request = new CulturalAppropriatenessRequest(
            content,
            contexts,
            ValidationComplexityLevel.MultiCultural,
            RealTimeModeration.Enabled());

        // Assert
        request.Content.Should().Be(content);
        request.Contexts.Should().HaveCount(3);
        request.ComplexityLevel.Should().Be(ValidationComplexityLevel.MultiCultural);
        request.RealTimeModeration.IsEnabled.Should().BeTrue();
    }

    [Fact]
    public void DiasporaAnalyticsRequest_WithGeographicClustering_ShouldCreateCorrectly()
    {
        // Arrange
        var regions = new GeographicRegion[]
        {
            GeographicRegion.Create("North America"),
            GeographicRegion.Create("Europe"),
            GeographicRegion.Create("Australia")
        };
        var segments = new CommunitySegment[]
        {
            CommunitySegment.Create("Young Professionals"),
            CommunitySegment.Create("Students")
        };

        // Act
        var request = new DiasporaAnalyticsRequest(
            AnalyticsType.GeographicClustering,
            regions,
            segments,
            PredictionTimeframe.SixMonths());

        // Assert
        request.AnalyticsType.Should().Be(AnalyticsType.GeographicClustering);
        request.Regions.Should().HaveCount(3);
        request.Segments.Should().HaveCount(2);
        request.Timeframe.Should().Be(PredictionTimeframe.SixMonths());
    }

    [Fact]
    public void RevenueShare_CalculateShare_ShouldIncludeAuthenticityBonus()
    {
        // Arrange
        var authenticityBonus = CulturalAuthenticityBonus.Create(5.0m, "High cultural accuracy");
        var revenueShare = new RevenueShare(
            percentage: 75.0m, // 75% share
            minimumAmount: 100.0m,
            maximumAmount: 10000.0m,
            authenticityBonus);

        var totalRevenue = 1000.0m;

        // Act
        var calculatedShare = revenueShare.CalculateShare(totalRevenue);

        // Assert
        // Base share: 1000 * 0.75 = 750
        // Authenticity bonus: 1000 * 0.05 = 50
        // Total: 750 + 50 = 800
        calculatedShare.Should().Be(800.0m);
    }

    [Fact]
    public void EnterpriseClient_IsContractActive_ShouldReturnCorrectStatus()
    {
        // Arrange
        var clientId = new EnterpriseClientId();
        var companyName = CompanyName.Create("Cultural Intelligence Corp");
        var contactInfo = ContactInfo.Create("contact@cultural.com", "+1234567890");
        var culturalRequirements = CulturalRequirements.Create(
            new string[] { "Buddhist", "Hindu" },
            new string[] { "Sinhala", "Tamil", "English" }
        );
        var customPricing = CustomPricing.Create(5000.0m, "Custom enterprise pricing");

        var startDate = DateTime.UtcNow.AddDays(-30);
        var endDate = DateTime.UtcNow.AddDays(335); // ~11 months from now

        // Act
        var client = new EnterpriseClient(
            clientId,
            companyName,
            contactInfo,
            culturalRequirements,
            customPricing,
            startDate,
            endDate);

        // Assert
        client.IsContractActive().Should().BeTrue();
        client.CompanyName.Should().Be(companyName);
        client.CustomPricing.Should().Be(customPricing);
    }

    [Theory]
    [InlineData("", typeof(ArgumentException))]
    [InlineData(null, typeof(ArgumentException))]
    public void TierName_InvalidValue_ShouldThrowException(string invalidName, Type exceptionType)
    {
        // Act & Assert
        Action act = () => new TierName(invalidName);
        act.Should().Throw<Exception>().And.Should().BeOfType(exceptionType);
    }

    [Theory]
    [InlineData(-1.0, typeof(ArgumentException))]
    [InlineData(-100.0, typeof(ArgumentException))]
    public void MonthlyPrice_NegativeAmount_ShouldThrowException(decimal invalidAmount, Type exceptionType)
    {
        // Act & Assert
        Action act = () => MonthlyPrice.Create(invalidAmount);
        act.Should().Throw<Exception>().And.Should().BeOfType(exceptionType);
    }

    [Theory]
    [InlineData(0, typeof(ArgumentException))]
    [InlineData(-1, typeof(ArgumentException))]
    public void APIRequestLimit_InvalidLimit_ShouldThrowException(int invalidLimit, Type exceptionType)
    {
        // Act & Assert
        Action act = () => APIRequestLimit.Create(invalidLimit);
        act.Should().Throw<Exception>().And.Should().BeOfType(exceptionType);
    }

    [Theory]
    [InlineData(-1, typeof(ArgumentException))]
    [InlineData(101, typeof(ArgumentException))]
    public void CulturalComplexityScore_InvalidScore_ShouldThrowException(int invalidScore, Type exceptionType)
    {
        // Act & Assert
        Action act = () => new CulturalComplexityScore(invalidScore, Array.Empty<ComplexityFactor>());
        act.Should().Throw<Exception>().And.Should().BeOfType(exceptionType);
    }

    [Fact]
    public void CulturalIntelligenceEndpoint_BuddhistCalendar_ShouldHaveCorrectCategory()
    {
        // Act
        var endpoint = CulturalIntelligenceEndpoint.BuddhistCalendar("/api/buddhist-calendar/premium");

        // Assert
        endpoint.Path.Should().Be("/api/buddhist-calendar/premium");
        endpoint.Category.Should().Be(EndpointCategory.BuddhistCalendar);
        endpoint.BillingComplexity.Should().Be(BillingComplexity.Medium);
    }

    [Fact]
    public void CulturalIntelligenceEndpoint_CulturalAppropriateness_ShouldHaveHighComplexity()
    {
        // Act
        var endpoint = CulturalIntelligenceEndpoint.CulturalAppropriateness("/api/cultural-appropriateness/validate");

        // Assert
        endpoint.Path.Should().Be("/api/cultural-appropriateness/validate");
        endpoint.Category.Should().Be(EndpointCategory.CulturalAppropriateness);
        endpoint.BillingComplexity.Should().Be(BillingComplexity.High);
    }

    [Fact]
    public void CulturalIntelligenceEndpoint_EventRecommendations_ShouldHaveHighComplexity()
    {
        // Act
        var endpoint = CulturalIntelligenceEndpoint.EventRecommendations("/api/events/recommendations");

        // Assert
        endpoint.Path.Should().Be("/api/events/recommendations");
        endpoint.Category.Should().Be(EndpointCategory.EventRecommendations);
        endpoint.BillingComplexity.Should().Be(BillingComplexity.High);
    }

    [Fact]
    public void CulturalIntelligenceEndpoint_CulturalContent_ShouldHaveMediumComplexity()
    {
        // Act
        var endpoint = CulturalIntelligenceEndpoint.CulturalContent("/api/cultural-content/generate");

        // Assert
        endpoint.Path.Should().Be("/api/cultural-content/generate");
        endpoint.Category.Should().Be(EndpointCategory.CulturalContent);
        endpoint.BillingComplexity.Should().Be(BillingComplexity.Medium);
    }

    [Fact]
    public void CulturalIntelligenceEndpoint_BusinessDirectory_ShouldHaveLowComplexity()
    {
        // Act
        var endpoint = CulturalIntelligenceEndpoint.BusinessDirectory("/api/business/directory");

        // Assert
        endpoint.Path.Should().Be("/api/business/directory");
        endpoint.Category.Should().Be(EndpointCategory.BusinessDirectory);
        endpoint.BillingComplexity.Should().Be(BillingComplexity.Low);
    }

    [Fact]
    public void CulturalIntelligenceEndpoint_CommunityEngagement_ShouldHaveHighComplexity()
    {
        // Act
        var endpoint = CulturalIntelligenceEndpoint.CommunityEngagement("/api/community/engagement");

        // Assert
        endpoint.Path.Should().Be("/api/community/engagement");
        endpoint.Category.Should().Be(EndpointCategory.CommunityEngagement);
        endpoint.BillingComplexity.Should().Be(BillingComplexity.High);
    }

    [Fact]
    public void CulturalAPIUsage_Creation_ShouldSetTimestampAutomatically()
    {
        // Arrange
        var beforeCreation = DateTime.UtcNow.AddSeconds(-1);
        var apiKey = new APIKey("test-key", APIKeyTier.Professional, UserId.Create());
        var endpoint = CulturalIntelligenceEndpoint.BuddhistCalendar("/test");
        var cost = new UsageCost(0.10m, 1.0m, Currency.USD(), CostBreakdown.Create(0.10m, 1.0m, 0.0m));
        var complexityScore = new CulturalComplexityScore(50, Array.Empty<ComplexityFactor>());
        var metadata = UsageMetadata.Create("Test usage");

        // Act
        var usage = new CulturalAPIUsage(apiKey, endpoint, cost, complexityScore, metadata);
        var afterCreation = DateTime.UtcNow.AddSeconds(1);

        // Assert
        usage.Timestamp.Should().BeAfter(beforeCreation);
        usage.Timestamp.Should().BeBefore(afterCreation);
        usage.ApiKey.Should().Be(apiKey);
        usage.Cost.Should().Be(cost);
    }
}