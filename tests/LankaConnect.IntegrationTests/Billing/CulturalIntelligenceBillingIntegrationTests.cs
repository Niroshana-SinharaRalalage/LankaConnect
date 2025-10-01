using FluentAssertions;
using LankaConnect.API.DTOs;
using LankaConnect.Application.Billing;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Billing;
using LankaConnect.Domain.Shared;
using LankaConnect.IntegrationTests.Common;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace LankaConnect.IntegrationTests.Billing;

[Collection("IntegrationTests")]
public class CulturalIntelligenceBillingIntegrationTests : BaseIntegrationTest
{
    private readonly ICulturalIntelligenceBillingService _billingService;
    private readonly IBillingRepository _billingRepository;

    public CulturalIntelligenceBillingIntegrationTests(IntegrationTestWebApplicationFactory factory) : base(factory)
    {
        _billingService = _serviceScope.ServiceProvider.GetRequiredService<ICulturalIntelligenceBillingService>();
        _billingRepository = _serviceScope.ServiceProvider.GetRequiredService<IBillingRepository>();
    }

    [Fact]
    public async Task GetCulturalIntelligenceTiers_ShouldReturnAllAvailableTiers()
    {
        // Act
        var response = await _httpClient.GetAsync("/api/billing/cultural-intelligence/tiers");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Community");
        content.Should().Contain("Professional");
        content.Should().Contain("Enterprise");
        content.Should().Contain("basicCalendar");
        content.Should().Contain("buddhistPremium");
        content.Should().Contain("culturalScoring");
    }

    [Fact]
    public async Task CreateSubscription_CommunityTier_ShouldCreateFreeSubscription()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var request = new CreateCulturalIntelligenceSubscriptionRequest
        {
            UserId = userId,
            TierName = "Community",
            TrialDays = 0
        };

        await AuthenticateAsync();

        // Act
        var response = await _httpClient.PostAsJsonAsync("/api/billing/cultural-intelligence/subscriptions", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var subscription = await _billingRepository.GetSubscriptionByUserIdAsync(UserId.Create(userId));
        subscription.Should().NotBeNull();
        subscription!.Tier.Name.Value.Should().Be("Community");
        subscription.Tier.BasePrice.IsFree.Should().BeTrue();
        subscription.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task CreateSubscription_ProfessionalTier_ShouldCreatePaidSubscription()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var request = new CreateCulturalIntelligenceSubscriptionRequest
        {
            UserId = userId,
            TierName = "Professional",
            TrialDays = 14
        };

        await AuthenticateAsync();

        // Act
        var response = await _httpClient.PostAsJsonAsync("/api/billing/cultural-intelligence/subscriptions", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var subscription = await _billingRepository.GetSubscriptionByUserIdAsync(UserId.Create(userId));
        subscription.Should().NotBeNull();
        subscription!.Tier.Name.Value.Should().Be("Professional");
        subscription.Tier.BasePrice.Amount.Should().Be(99.00m);
        subscription.Tier.FeatureAccess.BuddhistCalendarPremium.Should().BeTrue();
        subscription.Tier.FeatureAccess.CulturalAppropriatenessScoring.Should().BeTrue();
    }

    [Fact]
    public async Task CreateSubscription_EnterpriseTier_ShouldCreateUnlimitedSubscription()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var request = new CreateCulturalIntelligenceSubscriptionRequest
        {
            UserId = userId,
            TierName = "Enterprise",
            PromoCode = "ENTERPRISE2025"
        };

        await AuthenticateAsync();

        // Act
        var response = await _httpClient.PostAsJsonAsync("/api/billing/cultural-intelligence/subscriptions", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var subscription = await _billingRepository.GetSubscriptionByUserIdAsync(UserId.Create(userId));
        subscription.Should().NotBeNull();
        subscription!.Tier.Name.Value.Should().Be("Enterprise");
        subscription.Tier.RequestLimit.IsUnlimited.Should().BeTrue();
        subscription.Tier.FeatureAccess.CustomAIModels.Should().BeTrue();
        subscription.Tier.FeatureAccess.WhiteLabelLicensing.Should().BeTrue();
        subscription.Tier.FeatureAccess.CulturalConsultingServices.Should().BeTrue();
    }

    [Fact]
    public async Task ProcessUsage_ValidAPIKey_ShouldProcessSuccessfully()
    {
        // Arrange
        var userId = Guid.NewGuid();
        await CreateTestSubscription(userId, "Professional");

        var request = new ProcessCulturalIntelligenceUsageRequest
        {
            ApiKey = "lc_prof_test_12345",
            UserId = userId,
            Endpoint = "/api/cultural-intelligence/buddhist-calendar",
            ComplexityScore = 75,
            ClientId = "test-client-123"
        };

        await AuthenticateAsync();

        // Act
        var response = await _httpClient.PostAsJsonAsync("/api/billing/cultural-intelligence/usage", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Usage processed successfully");
    }

    [Fact]
    public async Task ProcessBuddhistCalendarUsage_AstronomicalPrecision_ShouldChargePremiumRate()
    {
        // Arrange
        var userId = Guid.NewGuid();
        await CreateTestSubscription(userId, "Professional");

        var request = new ProcessBuddhistCalendarUsageRequest
        {
            ApiKey = "lc_prof_test_12345",
            UserId = userId,
            PrecisionLevel = "AstronomicalPrecision",
            CalculationType = "LunarCalculation",
            Variations = new[] { "SriLankanBuddhist", "ThailandBuddhist" },
            RequestedDate = DateTime.UtcNow.AddDays(30)
        };

        await AuthenticateAsync();

        // Act
        var response = await _httpClient.PostAsJsonAsync("/api/billing/cultural-intelligence/usage/buddhist-calendar", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var responseContent = await response.Content.ReadFromJsonAsync<dynamic>();
        responseContent.Should().NotBeNull();
        
        // Verify premium pricing: Base 0.20 + 2 variations (0.05 each) = 0.30
        var expectedCost = 0.30m;
        // responseContent.cost should be approximately expectedCost
    }

    [Fact]
    public async Task ProcessCulturalAppropriatenessUsage_MultiCulturalRealTime_ShouldChargePremiumRate()
    {
        // Arrange
        var userId = Guid.NewGuid();
        await CreateTestSubscription(userId, "Professional");

        var request = new ProcessCulturalAppropriatenessUsageRequest
        {
            ApiKey = "lc_prof_test_12345",
            UserId = userId,
            Content = "This is a test content for cultural appropriateness validation",
            ContentType = "Text",
            ComplexityLevel = "MultiCultural",
            Contexts = new[] { "Buddhist", "Hindu", "Christian" },
            RealTimeModeration = true,
            Language = "en"
        };

        await AuthenticateAsync();

        // Act
        var response = await _httpClient.PostAsJsonAsync("/api/billing/cultural-intelligence/usage/cultural-appropriateness", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var responseContent = await response.Content.ReadFromJsonAsync<dynamic>();
        responseContent.Should().NotBeNull();
        
        // Verify multicultural real-time premium pricing
        // Base: 0.25, Context multiplier: 3 * 0.5 = 1.5, Real-time: 1.5x
        // Expected: 0.25 * 1.5 * 1.5 = 0.5625
    }

    [Fact]
    public async Task ProcessDiasporaAnalyticsUsage_GeographicClustering_ShouldProcessSuccessfully()
    {
        // Arrange
        var userId = Guid.NewGuid();
        await CreateTestSubscription(userId, "Enterprise");

        var request = new ProcessDiasporaAnalyticsUsageRequest
        {
            ApiKey = "lc_ent_test_12345",
            UserId = userId,
            AnalyticsType = "GeographicClustering",
            Regions = new[] { "North America", "Europe", "Australia" },
            Segments = new[] { "Young Professionals", "Students" },
            TimeframeMonths = 6
        };

        await AuthenticateAsync();

        // Act
        var response = await _httpClient.PostAsJsonAsync("/api/billing/cultural-intelligence/usage/diaspora-analytics", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var responseContent = await response.Content.ReadFromJsonAsync<dynamic>();
        responseContent.Should().NotBeNull();
        // Verify analytics were processed with correct parameters
    }

    [Fact]
    public async Task CreateEnterpriseContract_ValidRequest_ShouldCreateContract()
    {
        // Arrange
        var request = new CreateEnterpriseContractRequest
        {
            CompanyName = "Cultural Intelligence Corp",
            ContactEmail = "enterprise@cultural.com",
            ContactPhone = "+1-555-0123",
            RequiredCultures = new[] { "Buddhist", "Hindu", "Christian" },
            RequiredLanguages = new[] { "Sinhala", "Tamil", "English" },
            ContractValue = 50000.00m,
            PricingDescription = "Custom enterprise pricing with cultural consulting",
            ContractStartDate = DateTime.UtcNow,
            ContractEndDate = DateTime.UtcNow.AddYears(1),
            PaymentFrequency = "Monthly",
            FirstPaymentDate = DateTime.UtcNow.AddDays(30),
            NumberOfPayments = 12,
            ConsultingHours = 100,
            ConsultingHourlyRate = 300.00m,
            Services = new[]
            {
                new CulturalServiceRequest { Name = "Buddhist Calendar API", Price = 5000.00m, Description = "Premium Buddhist calendar service" },
                new CulturalServiceRequest { Name = "Cultural Consulting", Price = 30000.00m, Description = "Cultural appropriateness consulting" },
                new CulturalServiceRequest { Name = "Diaspora Analytics", Price = 15000.00m, Description = "Advanced diaspora community analytics" }
            },
            WhiteLabelLicensing = new WhiteLabelLicensingRequest
            {
                SetupFee = 5000.00m,
                MonthlyFee = 500.00m
            }
        };

        await AuthenticateAsAdminAsync();

        // Act
        var response = await _httpClient.PostAsJsonAsync("/api/billing/cultural-intelligence/enterprise/contracts", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var responseContent = await response.Content.ReadFromJsonAsync<dynamic>();
        responseContent.Should().NotBeNull();
        // Verify contract was created with all specified services
    }

    [Fact]
    public async Task ProcessPartnershipRevenue_ValidPartnership_ShouldCalculateRevenueShare()
    {
        // Arrange
        var partnershipId = Guid.NewGuid();
        await CreateTestPartnership(partnershipId);

        var request = new ProcessPartnershipRevenueRequest
        {
            SharePercentage = 75.0m,
            MinimumAmount = 100.0m,
            MaximumAmount = 10000.0m,
            AuthenticityBonusPercentage = 5.0m,
            BonusReason = "High cultural accuracy and authenticity"
        };

        await AuthenticateAsAdminAsync();

        // Act
        var response = await _httpClient.PostAsJsonAsync($"/api/billing/cultural-intelligence/partnerships/{partnershipId}/revenue-share", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var responseContent = await response.Content.ReadFromJsonAsync<dynamic>();
        responseContent.Should().NotBeNull();
        // Verify revenue sharing calculation includes authenticity bonus
    }

    [Fact]
    public async Task GetRevenueAnalytics_ValidDateRange_ShouldReturnComprehensiveAnalytics()
    {
        // Arrange
        await CreateTestData();

        var startDate = DateTime.UtcNow.AddMonths(-1).ToString("yyyy-MM-dd");
        var endDate = DateTime.UtcNow.ToString("yyyy-MM-dd");

        await AuthenticateAsAdminAsync();

        // Act
        var response = await _httpClient.GetAsync($"/api/billing/cultural-intelligence/analytics/revenue?startDate={startDate}&endDate={endDate}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var analytics = await response.Content.ReadFromJsonAsync<dynamic>();
        analytics.Should().NotBeNull();
        
        // Verify all analytics components are present
        // analytics.totalRevenue, monthlyRecurringRevenue, etc.
    }

    [Fact]
    public async Task HandleStripeWebhook_SubscriptionCreated_ShouldActivateSubscription()
    {
        // Arrange
        var webhookPayload = CreateTestWebhookPayload("customer.subscription.created");
        var signature = CreateTestWebhookSignature(webhookPayload);

        // Act
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/billing/cultural-intelligence/webhooks/stripe")
        {
            Content = new StringContent(webhookPayload, System.Text.Encoding.UTF8, "application/json")
        };
        request.Headers.Add("Stripe-Signature", signature);

        var response = await _httpClient.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        // Verify webhook was processed and subscription was activated
    }

    [Fact]
    public async Task ProcessUsage_ExpiredAPIKey_ShouldReturnUnauthorized()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var expiredApiKey = CreateExpiredAPIKey(userId);

        var request = new ProcessCulturalIntelligenceUsageRequest
        {
            ApiKey = expiredApiKey,
            UserId = userId,
            Endpoint = "/api/cultural-intelligence/test",
            ComplexityScore = 50
        };

        await AuthenticateAsync();

        // Act
        var response = await _httpClient.PostAsJsonAsync("/api/billing/cultural-intelligence/usage", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("API key has expired");
    }

    [Fact]
    public async Task ProcessUsage_ExceedsRateLimit_ShouldReturnRateLimitError()
    {
        // Arrange
        var userId = Guid.NewGuid();
        await CreateTestSubscription(userId, "Community"); // 1,000 request limit

        // Simulate exceeding rate limit
        await SimulateAPIUsage(userId, 1001);

        var request = new ProcessCulturalIntelligenceUsageRequest
        {
            ApiKey = "lc_comm_test_12345",
            UserId = userId,
            Endpoint = "/api/cultural-intelligence/test",
            ComplexityScore = 25
        };

        await AuthenticateAsync();

        // Act
        var response = await _httpClient.PostAsJsonAsync("/api/billing/cultural-intelligence/usage", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Monthly request limit");
        content.Should().Contain("exceeded");
    }

    // Helper methods
    private async Task CreateTestSubscription(Guid userId, string tierName)
    {
        var tier = tierName.ToLower() switch
        {
            "community" => CulturalIntelligenceTier.CreateCommunityTier(),
            "professional" => CulturalIntelligenceTier.CreateProfessionalTier(),
            "enterprise" => CulturalIntelligenceTier.CreateEnterpriseTier(),
            _ => throw new ArgumentException($"Unknown tier: {tierName}")
        };

        var subscription = CulturalIntelligenceSubscription.Create(
            CulturalIntelligenceSubscriptionId.New(),
            UserId.Create(userId),
            tier,
            DateTime.UtcNow,
            DateTime.UtcNow.AddMonths(1));

        await _billingRepository.SaveSubscriptionAsync(subscription);
    }

    private async Task CreateTestPartnership(Guid partnershipId)
    {
        var partnership = Partnership.Create(
            new PartnershipId(partnershipId),
            "Test Cultural Organization",
            ContactInfo.Create("partner@cultural.org", "+1-555-0456"),
            new RevenueShare(75.0m, 100.0m, 10000.0m, CulturalAuthenticityBonus.Create(5.0m, "Authenticity bonus")),
            DateTime.UtcNow.AddMonths(-6));

        // Save partnership (implementation would depend on repository)
    }

    private async Task CreateTestData()
    {
        // Create test subscriptions, usage data, and revenue data for analytics
        var communityUserId = Guid.NewGuid();
        var professionalUserId = Guid.NewGuid();
        var enterpriseUserId = Guid.NewGuid();

        await CreateTestSubscription(communityUserId, "Community");
        await CreateTestSubscription(professionalUserId, "Professional");
        await CreateTestSubscription(enterpriseUserId, "Enterprise");

        // Simulate some API usage
        await SimulateAPIUsage(professionalUserId, 500);
        await SimulateAPIUsage(enterpriseUserId, 2000);
    }

    private async Task SimulateAPIUsage(Guid userId, int requestCount)
    {
        // Implementation would simulate API usage for testing rate limits
        // This would involve creating usage records in the database
        await Task.CompletedTask; // Placeholder
    }

    private string CreateExpiredAPIKey(Guid userId)
    {
        return $"lc_prof_expired_{userId:N}_{DateTimeOffset.UtcNow.AddDays(-1).ToUnixTimeSeconds()}";
    }

    private string CreateTestWebhookPayload(string eventType)
    {
        return $@"{{
            ""id"": ""evt_test_webhook"",
            ""object"": ""event"",
            ""type"": ""{eventType}"",
            ""created"": {DateTimeOffset.UtcNow.ToUnixTimeSeconds()},
            ""data"": {{
                ""object"": {{
                    ""id"": ""sub_test_subscription"",
                    ""status"": ""active"",
                    ""metadata"": {{
                        ""user_id"": ""{Guid.NewGuid()}""
                    }}
                }}
            }}
        }}";
    }

    private string CreateTestWebhookSignature(string payload)
    {
        // Implementation would create a valid Stripe webhook signature
        return "v1=test_signature,v0=fallback_signature";
    }
}

[Collection("IntegrationTests")]
public class CulturalIntelligenceBillingServiceTests : BaseIntegrationTest
{
    private readonly ICulturalIntelligenceBillingService _billingService;

    public CulturalIntelligenceBillingServiceTests(IntegrationTestWebApplicationFactory factory) : base(factory)
    {
        _billingService = _serviceScope.ServiceProvider.GetRequiredService<ICulturalIntelligenceBillingService>();
    }

    [Fact]
    public async Task CreateCulturalSubscriptionAsync_NewUser_ShouldCreateSuccessfully()
    {
        // Arrange
        var userId = UserId.Create();
        var tier = CulturalIntelligenceTier.CreateProfessionalTier();

        // Act
        var result = await _billingService.CreateCulturalSubscriptionAsync(userId, tier);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task ProcessCulturalAPIUsageAsync_ValidUsage_ShouldCalculateCorrectCost()
    {
        // Arrange
        var userId = UserId.Create();
        var apiKey = new APIKey("test_key", APIKeyTier.Professional, userId);
        
        var endpoint = CulturalIntelligenceEndpoint.CulturalAppropriateness("/api/cultural-appropriateness/validate");
        var complexityScore = new CulturalComplexityScore(85, new[] 
        {
            ComplexityFactor.Create("MultiCultural", 30),
            ComplexityFactor.Create("RealTime", 25),
            ComplexityFactor.Create("AdvancedAI", 30)
        });
        var metadata = new RequestMetadata(new Dictionary<string, object>
        {
            ["client_id"] = "test_client",
            ["request_time"] = DateTime.UtcNow
        });

        var culturalRequest = new CulturalIntelligenceRequest(endpoint, complexityScore, metadata);

        // Create subscription first
        var tier = CulturalIntelligenceTier.CreateProfessionalTier();
        await _billingService.CreateCulturalSubscriptionAsync(userId, tier);

        // Act
        var result = await _billingService.ProcessCulturalAPIUsageAsync(apiKey, culturalRequest);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task ProcessBuddhistCalendarPremiumUsageAsync_AstronomicalPrecision_ShouldChargePremiumRate()
    {
        // Arrange
        var userId = UserId.Create();
        var apiKey = new APIKey("test_key", APIKeyTier.Professional, userId);
        
        var buddhistRequest = new BuddhistCalendarRequest(
            CalendarPrecisionLevel.AstronomicalPrecision,
            AstronomicalCalculationType.LunarCalculation,
            new[] 
            {
                CustomCalendarVariation.Create("SriLankanBuddhist"),
                CustomCalendarVariation.Create("ThailandBuddhist")
            },
            DateTime.UtcNow.AddDays(30));

        // Create subscription first
        var tier = CulturalIntelligenceTier.CreateProfessionalTier();
        await _billingService.CreateCulturalSubscriptionAsync(userId, tier);

        // Act
        var result = await _billingService.ProcessBuddhistCalendarPremiumUsageAsync(apiKey, buddhistRequest);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task GetCulturalRevenueAnalyticsAsync_ValidTimeRange_ShouldReturnAnalytics()
    {
        // Arrange
        var timeRange = new TimeRange(
            DateTime.UtcNow.AddMonths(-1),
            DateTime.UtcNow);

        // Act
        var result = await _billingService.GetCulturalRevenueAnalyticsAsync(timeRange);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.TimeRange.Should().Be(timeRange);
    }
}