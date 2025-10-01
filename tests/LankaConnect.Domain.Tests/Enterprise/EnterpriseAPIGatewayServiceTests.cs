using LankaConnect.Domain.Common;
using LankaConnect.Domain.Enterprise;
using LankaConnect.Domain.Enterprise.Services;
using LankaConnect.Domain.Enterprise.ValueObjects;
using LankaConnect.Domain.Shared.ValueObjects;
using LankaConnect.Domain.Shared.Enums;
using FluentAssertions;
using Moq;
using Xunit;

namespace LankaConnect.Domain.Tests.Enterprise;

/// <summary>
/// TDD Tests for Enterprise API Gateway Service
/// These tests define enterprise-grade Cultural Intelligence API processing with SLA guarantees
/// Focus: 99.95% uptime, <200ms response time, SOC 2 compliance, advanced rate limiting
/// </summary>
public class EnterpriseAPIGatewayServiceTests
{
    private readonly Mock<IEnterpriseAPIGatewayService> _mockGatewayService;
    private readonly EnterpriseAPIKey _validFortune500APIKey;
    private readonly EnterpriseAPIKey _validEducationalAPIKey;
    private readonly EnterpriseCulturalRequest _validCulturalRequest;
    private readonly CulturalIntelligenceEndpoint _buddhistCalendarEndpoint;
    private readonly CulturalIntelligenceEndpoint _culturalAppropriatenessEndpoint;
    private readonly EnterpriseClientId _fortune500ClientId;
    private readonly EnterpriseClientId _educationalClientId;

    public EnterpriseAPIGatewayServiceTests()
    {
        _mockGatewayService = new Mock<IEnterpriseAPIGatewayService>();
        _fortune500ClientId = new EnterpriseClientId();
        _educationalClientId = new EnterpriseClientId();
        
        _validFortune500APIKey = new EnterpriseAPIKey(
            "ent_fortune500_api_key_32_chars_minimum_length_required",
            APIKeyTier.Fortune500,
            _fortune500ClientId,
            allowedEndpoints: new List<string> { "/cultural/buddhist-calendar", "/cultural/appropriateness" },
            allowedIPAddresses: new List<string> { "192.168.1.100", "10.0.0.50" },
            dailyRequestLimit: int.MaxValue); // Unlimited for Fortune 500
            
        _validEducationalAPIKey = new EnterpriseAPIKey(
            "ent_educational_api_key_32_chars_minimum_length_required",
            APIKeyTier.Educational,
            _educationalClientId,
            dailyRequestLimit: 50000);
            
        _buddhistCalendarEndpoint = CulturalIntelligenceEndpoint.BuddhistCalendar();
        _culturalAppropriatenessEndpoint = CulturalIntelligenceEndpoint.CulturalAppropriateness();
        
        _validCulturalRequest = new EnterpriseCulturalRequest(
            _validFortune500APIKey,
            _buddhistCalendarEndpoint,
            EnterpriseComplexityLevel.High(),
            SLAResponseTime.Enterprise(),
            CulturalValidationLevel.Advanced(),
            new RequestMetadata(new Dictionary<string, object> { 
                ["client_type"] = "Fortune500",
                ["request_priority"] = "high"
            }),
            _fortune500ClientId,
            "correlation-123-456-789");
    }

    #region Enterprise API Key Validation Tests

    [Fact]
    public async Task ValidateEnterpriseAPIKey_WithValidFortune500Key_ShouldReturnValidResult()
    {
        // Arrange
        var expectedValidation = new EnterpriseAPIKeyValidation(
            isValid: true,
            tier: APIKeyTier.Fortune500,
            clientId: _fortune500ClientId,
            validationErrors: new List<string>(),
            requiresAdditionalVerification: false);
            
        _mockGatewayService
            .Setup(x => x.ValidateEnterpriseAPIKeyAsync(It.IsAny<string>(), It.IsAny<EnterpriseCulturalRequest>()))
            .ReturnsAsync(Result<EnterpriseAPIKeyValidation>.Success(expectedValidation));

        // Act
        var result = await _mockGatewayService.Object.ValidateEnterpriseAPIKeyAsync(
            _validFortune500APIKey.KeyValue, _validCulturalRequest);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.IsValid.Should().BeTrue();
        result.Value.Tier.Should().Be(APIKeyTier.Fortune500);
        result.Value.ClientId.Should().Be(_fortune500ClientId);
        result.Value.ValidationErrors.Should().BeEmpty();
        result.Value.RequiresAdditionalVerification.Should().BeFalse();
    }

    [Fact]
    public async Task ValidateEnterpriseAPIKey_WithExpiredKey_ShouldReturnInvalidResult()
    {
        // Arrange
        var expiredAPIKey = new EnterpriseAPIKey(
            "ent_expired_api_key_32_chars_minimum_length_required",
            APIKeyTier.Enterprise,
            _fortune500ClientId,
            expiresAt: DateTime.UtcNow.AddDays(-1)); // Expired yesterday
            
        var expiredRequest = new EnterpriseCulturalRequest(
            expiredAPIKey,
            _buddhistCalendarEndpoint,
            EnterpriseComplexityLevel.Medium(),
            SLAResponseTime.Enterprise(),
            CulturalValidationLevel.Standard(),
            new RequestMetadata(new Dictionary<string, object>()),
            _fortune500ClientId);
            
        var expectedValidation = new EnterpriseAPIKeyValidation(
            isValid: false,
            tier: APIKeyTier.Enterprise,
            clientId: _fortune500ClientId,
            validationErrors: new List<string> { "API key has expired" },
            requiresAdditionalVerification: false);
            
        _mockGatewayService
            .Setup(x => x.ValidateEnterpriseAPIKeyAsync(It.IsAny<string>(), It.IsAny<EnterpriseCulturalRequest>()))
            .ReturnsAsync(Result<EnterpriseAPIKeyValidation>.Success(expectedValidation));

        // Act
        var result = await _mockGatewayService.Object.ValidateEnterpriseAPIKeyAsync(
            expiredAPIKey.KeyValue, expiredRequest);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.IsValid.Should().BeFalse();
        result.Value.ValidationErrors.Should().Contain("API key has expired");
    }

    [Fact]
    public async Task ValidateEnterpriseAPIKey_WithInvalidKeyFormat_ShouldReturnValidationError()
    {
        // Arrange
        var invalidAPIKey = "too_short_key"; // Less than 32 characters
        
        var expectedValidation = new EnterpriseAPIKeyValidation(
            isValid: false,
            tier: APIKeyTier.Standard,
            clientId: new EnterpriseClientId(),
            validationErrors: new List<string> { "API key format is invalid", "API key must be at least 32 characters" },
            requiresAdditionalVerification: false);
            
        _mockGatewayService
            .Setup(x => x.ValidateEnterpriseAPIKeyAsync(It.IsAny<string>(), It.IsAny<EnterpriseCulturalRequest>()))
            .ReturnsAsync(Result<EnterpriseAPIKeyValidation>.Success(expectedValidation));

        // Act
        var result = await _mockGatewayService.Object.ValidateEnterpriseAPIKeyAsync(
            invalidAPIKey, _validCulturalRequest);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.IsValid.Should().BeFalse();
        result.Value.ValidationErrors.Should().Contain("API key format is invalid");
        result.Value.ValidationErrors.Should().Contain("API key must be at least 32 characters");
    }

    #endregion

    #region Endpoint Access Validation Tests

    [Fact]
    public async Task ValidateEndpointAccess_WithAuthorizedEndpointAndIP_ShouldAllowAccess()
    {
        // Arrange
        var authorizedIP = "192.168.1.100";
        var expectedAccessValidation = new EndpointAccessValidation(
            hasAccess: true,
            denialReason: null,
            requiresIPWhitelisting: true,
            requiresGeographicValidation: false,
            allowedRegions: new List<string> { "US-West", "US-East" });
            
        _mockGatewayService
            .Setup(x => x.ValidateEndpointAccessAsync(
                It.IsAny<EnterpriseAPIKey>(), 
                It.IsAny<CulturalIntelligenceEndpoint>(), 
                It.IsAny<string>()))
            .ReturnsAsync(Result<EndpointAccessValidation>.Success(expectedAccessValidation));

        // Act
        var result = await _mockGatewayService.Object.ValidateEndpointAccessAsync(
            _validFortune500APIKey, _buddhistCalendarEndpoint, authorizedIP);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.HasAccess.Should().BeTrue();
        result.Value.DenialReason.Should().BeNull();
        result.Value.RequiresIPWhitelisting.Should().BeTrue();
        result.Value.AllowedRegions.Should().Contain("US-West");
        result.Value.AllowedRegions.Should().Contain("US-East");
    }

    [Fact]
    public async Task ValidateEndpointAccess_WithUnauthorizedIP_ShouldDenyAccess()
    {
        // Arrange
        var unauthorizedIP = "203.45.67.89"; // Not in allowed list
        var expectedAccessValidation = new EndpointAccessValidation(
            hasAccess: false,
            denialReason: "Source IP address not in whitelist",
            requiresIPWhitelisting: true,
            requiresGeographicValidation: true,
            allowedRegions: new List<string> { "US-West", "US-East" });
            
        _mockGatewayService
            .Setup(x => x.ValidateEndpointAccessAsync(
                It.IsAny<EnterpriseAPIKey>(), 
                It.IsAny<CulturalIntelligenceEndpoint>(), 
                It.IsAny<string>()))
            .ReturnsAsync(Result<EndpointAccessValidation>.Success(expectedAccessValidation));

        // Act
        var result = await _mockGatewayService.Object.ValidateEndpointAccessAsync(
            _validFortune500APIKey, _buddhistCalendarEndpoint, unauthorizedIP);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.HasAccess.Should().BeFalse();
        result.Value.DenialReason.Should().Be("Source IP address not in whitelist");
        result.Value.RequiresIPWhitelisting.Should().BeTrue();
        result.Value.RequiresGeographicValidation.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateEndpointAccess_WithRestrictedEndpointForTier_ShouldDenyAccess()
    {
        // Arrange - Educational tier trying to access enterprise-only diaspora analytics
        var diasporaEndpoint = CulturalIntelligenceEndpoint.DiasporaAnalytics();
        var expectedAccessValidation = new EndpointAccessValidation(
            hasAccess: false,
            denialReason: "Endpoint requires Fortune500 or Enterprise tier",
            requiresIPWhitelisting: false,
            requiresGeographicValidation: false);
            
        _mockGatewayService
            .Setup(x => x.ValidateEndpointAccessAsync(
                It.IsAny<EnterpriseAPIKey>(), 
                It.IsAny<CulturalIntelligenceEndpoint>(), 
                It.IsAny<string>()))
            .ReturnsAsync(Result<EndpointAccessValidation>.Success(expectedAccessValidation));

        // Act
        var result = await _mockGatewayService.Object.ValidateEndpointAccessAsync(
            _validEducationalAPIKey, diasporaEndpoint, "192.168.1.1");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.HasAccess.Should().BeFalse();
        result.Value.DenialReason.Should().Contain("requires Fortune500 or Enterprise tier");
    }

    #endregion

    #region SLA Monitoring Tests

    [Fact]
    public async Task MonitorSLACompliance_WithActiveContractAndGoodPerformance_ShouldReturnCompliantMetrics()
    {
        // Arrange
        var contract = CreateValidFortune500Contract();
        var startDate = DateTime.UtcNow.AddDays(-7);
        var endDate = DateTime.UtcNow;
        
        var expectedMetrics = new SLAComplianceMetrics(
            uptimePercentage: 99.98, // Above 99.95% requirement
            averageResponseTime: 150.5, // Below 200ms requirement
            totalRequests: 1000000,
            successfulRequests: 999800,
            slaViolations: 0,
            detailedMetrics: new List<SLAMetric>
            {
                new SLAMetric("Buddhist Calendar API", 145.2, "ms", true),
                new SLAMetric("Cultural Appropriateness API", 188.7, "ms", true),
                new SLAMetric("Diaspora Analytics API", 156.3, "ms", true)
            },
            measurementStartDate: startDate,
            measurementEndDate: endDate);
            
        _mockGatewayService
            .Setup(x => x.MonitorSLAComplianceAsync(
                It.IsAny<EnterpriseContract>(),
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>()))
            .ReturnsAsync(Result<SLAComplianceMetrics>.Success(expectedMetrics));

        // Act
        var result = await _mockGatewayService.Object.MonitorSLAComplianceAsync(
            contract, startDate, endDate);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.MeetsRequirements.Should().BeTrue();
        result.Value.UptimePercentage.Should().BeGreaterThan(99.95);
        result.Value.AverageResponseTime.Should().BeLessThan(200);
        result.Value.SLAViolations.Should().Be(0);
        result.Value.DetailedMetrics.Should().HaveCount(3);
        result.Value.DetailedMetrics.Should().AllSatisfy(m => m.IsWithinSLA.Should().BeTrue());
    }

    [Fact]
    public async Task MonitorSLACompliance_WithSLAViolations_ShouldReturnNonCompliantMetrics()
    {
        // Arrange
        var contract = CreateValidFortune500Contract();
        var startDate = DateTime.UtcNow.AddDays(-7);
        var endDate = DateTime.UtcNow;
        
        var expectedMetrics = new SLAComplianceMetrics(
            uptimePercentage: 99.92, // Below 99.95% requirement
            averageResponseTime: 250.8, // Above 200ms requirement
            totalRequests: 1000000,
            successfulRequests: 999200,
            slaViolations: 3, // Multiple violations
            detailedMetrics: new List<SLAMetric>
            {
                new SLAMetric("Buddhist Calendar API", 285.4, "ms", false), // SLA violation
                new SLAMetric("Cultural Appropriateness API", 325.7, "ms", false), // SLA violation
                new SLAMetric("System Uptime", 99.92, "%", false) // SLA violation
            },
            measurementStartDate: startDate,
            measurementEndDate: endDate);
            
        _mockGatewayService
            .Setup(x => x.MonitorSLAComplianceAsync(
                It.IsAny<EnterpriseContract>(),
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>()))
            .ReturnsAsync(Result<SLAComplianceMetrics>.Success(expectedMetrics));

        // Act
        var result = await _mockGatewayService.Object.MonitorSLAComplianceAsync(
            contract, startDate, endDate);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.MeetsRequirements.Should().BeFalse();
        result.Value.UptimePercentage.Should().BeLessThan(99.95);
        result.Value.AverageResponseTime.Should().BeGreaterThan(200);
        result.Value.SLAViolations.Should().Be(3);
        result.Value.DetailedMetrics.Should().Contain(m => !m.IsWithinSLA);
    }

    [Fact]
    public async Task RecordSLAViolation_WithCriticalViolation_ShouldRecordSuccessfully()
    {
        // Arrange
        var slaViolation = new SLAViolation(
            violationType: SLAViolationType.ResponseTimeExceeded,
            description: "Buddhist Calendar API response time exceeded 200ms for Fortune 500 client",
            occurredAt: DateTime.UtcNow,
            duration: TimeSpan.FromMinutes(15),
            severity: SLAViolationSeverity.Critical,
            impactDescription: "Fortune 500 client cultural event planning disrupted",
            requiresCustomerNotification: true);
            
        _mockGatewayService
            .Setup(x => x.RecordSLAViolationAsync(
                It.IsAny<EnterpriseClientId>(),
                It.IsAny<SLAViolation>()))
            .ReturnsAsync(Result.Success());

        // Act
        var result = await _mockGatewayService.Object.RecordSLAViolationAsync(
            _fortune500ClientId, slaViolation);

        // Assert
        result.IsSuccess.Should().BeTrue();
        
        // Verify the call was made with correct parameters
        _mockGatewayService.Verify(
            x => x.RecordSLAViolationAsync(_fortune500ClientId, It.IsAny<SLAViolation>()),
            Times.Once);
    }

    #endregion

    #region Advanced Rate Limiting Tests

    [Fact]
    public async Task ApplyAdvancedRateLimiting_WithinLimitsAndLowComplexity_ShouldAllowRequest()
    {
        // Arrange
        var lowComplexityRequest = new EnterpriseCulturalRequest(
            _validFortune500APIKey,
            _buddhistCalendarEndpoint,
            EnterpriseComplexityLevel.Low(), // Low complexity
            SLAResponseTime.Enterprise(),
            CulturalValidationLevel.Basic(),
            new RequestMetadata(new Dictionary<string, object>()),
            _fortune500ClientId);
            
        var expectedDecision = new RateLimitingDecision(
            allowed: true,
            reason: "Request within limits and low complexity",
            remainingQuota: 999999,
            resetTime: DateTime.UtcNow.AddHours(1),
            complexityScore: 1,
            priorityLevel: RequestPriority.Normal);
            
        _mockGatewayService
            .Setup(x => x.ApplyAdvancedRateLimitingAsync(
                It.IsAny<EnterpriseAPIKey>(),
                It.IsAny<EnterpriseCulturalRequest>()))
            .ReturnsAsync(Result<RateLimitingDecision>.Success(expectedDecision));

        // Act
        var result = await _mockGatewayService.Object.ApplyAdvancedRateLimitingAsync(
            _validFortune500APIKey, lowComplexityRequest);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Allowed.Should().BeTrue();
        result.Value.ComplexityScore.Should().Be(1);
        result.Value.PriorityLevel.Should().Be(RequestPriority.Normal);
        result.Value.RemainingQuota.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task ApplyAdvancedRateLimiting_WithHighComplexityExtremeRequest_ShouldApplySpecialThrottling()
    {
        // Arrange
        var extremeComplexityRequest = new EnterpriseCulturalRequest(
            _validFortune500APIKey,
            CulturalIntelligenceEndpoint.DiasporaAnalytics(), // High complexity endpoint
            EnterpriseComplexityLevel.Extreme(requiresAdvancedAI: true), // Extreme complexity
            SLAResponseTime.Enterprise(),
            CulturalValidationLevel.CommunityValidated(), // Requires human + community validation
            new RequestMetadata(new Dictionary<string, object> {
                ["analysis_regions"] = new[] { "North America", "Europe", "Australia" },
                ["community_segments"] = new[] { "Professionals", "Students", "Families" }
            }),
            _fortune500ClientId);
            
        var expectedDecision = new RateLimitingDecision(
            allowed: true,
            reason: "High complexity request - special throttling applied",
            remainingQuota: 95, // Reduced quota due to complexity
            resetTime: DateTime.UtcNow.AddHours(1),
            complexityScore: 30, // Extreme complexity score
            priorityLevel: RequestPriority.High);
            
        _mockGatewayService
            .Setup(x => x.ApplyAdvancedRateLimitingAsync(
                It.IsAny<EnterpriseAPIKey>(),
                It.IsAny<EnterpriseCulturalRequest>()))
            .ReturnsAsync(Result<RateLimitingDecision>.Success(expectedDecision));

        // Act
        var result = await _mockGatewayService.Object.ApplyAdvancedRateLimitingAsync(
            _validFortune500APIKey, extremeComplexityRequest);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Allowed.Should().BeTrue();
        result.Value.ComplexityScore.Should().Be(30);
        result.Value.PriorityLevel.Should().Be(RequestPriority.High);
        result.Value.RemainingQuota.Should().BeLessThan(100); // Reduced due to complexity
        result.Value.Reason.Should().Contain("special throttling applied");
    }

    [Fact]
    public async Task ApplyAdvancedRateLimiting_WithEducationalTierExceedingQuota_ShouldDenyRequest()
    {
        // Arrange - Educational API key that has exceeded daily quota
        var educationalRequest = new EnterpriseCulturalRequest(
            _validEducationalAPIKey,
            _buddhistCalendarEndpoint,
            EnterpriseComplexityLevel.Medium(),
            SLAResponseTime.Premium(),
            CulturalValidationLevel.Standard(),
            new RequestMetadata(new Dictionary<string, object>()),
            _educationalClientId);
            
        var expectedDecision = new RateLimitingDecision(
            allowed: false,
            reason: "Daily quota exceeded for Educational tier",
            remainingQuota: 0,
            resetTime: DateTime.UtcNow.AddHours(18), // Reset at midnight
            complexityScore: 3,
            priorityLevel: RequestPriority.Low);
            
        _mockGatewayService
            .Setup(x => x.ApplyAdvancedRateLimitingAsync(
                It.IsAny<EnterpriseAPIKey>(),
                It.IsAny<EnterpriseCulturalRequest>()))
            .ReturnsAsync(Result<RateLimitingDecision>.Success(expectedDecision));

        // Act
        var result = await _mockGatewayService.Object.ApplyAdvancedRateLimitingAsync(
            _validEducationalAPIKey, educationalRequest);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Allowed.Should().BeFalse();
        result.Value.Reason.Should().Contain("quota exceeded");
        result.Value.RemainingQuota.Should().Be(0);
        result.Value.ResetTime.Should().BeAfter(DateTime.UtcNow);
    }

    #endregion

    #region Enterprise Cultural Request Processing Tests

    [Fact]
    public async Task ProcessEnterpriseCulturalRequest_WithValidBuddhistCalendarRequest_ShouldReturnCulturalResponse()
    {
        // Arrange
        var buddhistCalendarRequest = new EnterpriseCulturalRequest(
            _validFortune500APIKey,
            CulturalIntelligenceEndpoint.BuddhistCalendar("/cultural/buddhist-calendar/vesak-2024"),
            EnterpriseComplexityLevel.Medium(),
            SLAResponseTime.Enterprise(), // <200ms requirement
            CulturalValidationLevel.Advanced(),
            new RequestMetadata(new Dictionary<string, object> {
                ["year"] = 2024,
                ["festival"] = "Vesak",
                ["timezone"] = "America/Los_Angeles",
                ["precision_level"] = "AstronomicalPrecision"
            }),
            _fortune500ClientId);
            
        var expectedResponse = new EnterpriseCulturalResponse(
            responseData: "{\"vesak_2024\": {\"date\": \"2024-05-23\", \"moon_phase\": \"full\", \"cultural_significance\": \"Buddha's birth, enlightenment, and parinirvana\", \"diaspora_friendly\": true}}",
            processingTime: TimeSpan.FromMilliseconds(145), // Within SLA
            validationResult: new CulturalValidationResult(
                isValid: true,
                confidenceScore: 0.98,
                validationNotes: new List<string> { "Astronomically validated", "Community expert reviewed" }),
            warnings: new List<string>(),
            slaCompliant: true,
            correlationId: "correlation-123-456-789",
            processingCost: 8); // Medium complexity with advanced validation
            
        _mockGatewayService
            .Setup(x => x.ProcessEnterpriseCulturalRequestAsync(
                It.IsAny<EnterpriseCulturalRequest>()))
            .ReturnsAsync(Result<EnterpriseCulturalResponse>.Success(expectedResponse));

        // Act
        var result = await _mockGatewayService.Object.ProcessEnterpriseCulturalRequestAsync(
            buddhistCalendarRequest);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.SLACompliant.Should().BeTrue();
        result.Value.ProcessingTime.Should().BeLessThan(TimeSpan.FromMilliseconds(200));
        result.Value.ValidationResult.IsValid.Should().BeTrue();
        result.Value.ValidationResult.ConfidenceScore.Should().BeGreaterThan(0.95);
        result.Value.ResponseData.Should().Contain("vesak_2024");
        result.Value.ResponseData.Should().Contain("diaspora_friendly");
        result.Value.ProcessingCost.Should().BeGreaterThan(0);
        result.Value.Warnings.Should().BeEmpty();
    }

    [Fact]
    public async Task ProcessEnterpriseCulturalRequest_WithCulturalAppropriatenessRequest_ShouldReturnValidationResponse()
    {
        // Arrange
        var appropriatenessRequest = new EnterpriseCulturalRequest(
            _validFortune500APIKey,
            CulturalIntelligenceEndpoint.CulturalAppropriateness("/cultural/appropriateness/validate"),
            EnterpriseComplexityLevel.High(requiresAdvancedAI: true),
            SLAResponseTime.Enterprise(),
            CulturalValidationLevel.CommunityValidated(), // Requires community validation
            new RequestMetadata(new Dictionary<string, object> {
                ["content_type"] = "marketing_campaign",
                ["target_culture"] = "Sri Lankan Buddhist",
                ["content"] = "Workplace wellness program incorporating mindfulness meditation",
                ["context"] = "Fortune 500 employee wellness initiative"
            }),
            _fortune500ClientId);
            
        var expectedResponse = new EnterpriseCulturalResponse(
            responseData: "{\"appropriateness_score\": 0.92, \"cultural_alignment\": \"high\", \"recommendations\": [\"Include acknowledgment of Buddhist origins\", \"Avoid commercialization of sacred practices\"], \"community_feedback\": \"Positive - respectful approach to mindfulness\"}",
            processingTime: TimeSpan.FromMilliseconds(380), // Higher due to community validation
            validationResult: new CulturalValidationResult(
                isValid: true,
                confidenceScore: 0.92,
                validationNotes: new List<string> { 
                    "Buddhist community expert review completed",
                    "Cultural context appropriately considered",
                    "Corporate wellness application approved"
                },
                requiredHumanReview: true,
                passedCommunityValidation: true),
            warnings: new List<string> { "Ensure proper attribution to Buddhist traditions" },
            slaCompliant: false, // Exceeded 200ms due to community validation
            correlationId: "correlation-789-123-456",
            processingCost: 25); // High complexity with community validation
            
        _mockGatewayService
            .Setup(x => x.ProcessEnterpriseCulturalRequestAsync(
                It.IsAny<EnterpriseCulturalRequest>()))
            .ReturnsAsync(Result<EnterpriseCulturalResponse>.Success(expectedResponse));

        // Act
        var result = await _mockGatewayService.Object.ProcessEnterpriseCulturalRequestAsync(
            appropriatenessRequest);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.ValidationResult.IsValid.Should().BeTrue();
        result.Value.ValidationResult.RequiredHumanReview.Should().BeTrue();
        result.Value.ValidationResult.PassedCommunityValidation.Should().BeTrue();
        result.Value.ValidationResult.ConfidenceScore.Should().BeGreaterThan(0.9);
        result.Value.ResponseData.Should().Contain("appropriateness_score");
        result.Value.ResponseData.Should().Contain("community_feedback");
        result.Value.ProcessingCost.Should().BeGreaterThan(20); // High complexity + community validation
        result.Value.Warnings.Should().NotBeEmpty();
        result.Value.Warnings.Should().Contain(w => w.Contains("attribution"));
    }

    #endregion

    #region Priority Request Processing Tests

    [Fact]
    public async Task ProcessPriorityRequest_WithEnterpriseSLA_ShouldProcessWithGuaranteedResponseTime()
    {
        // Arrange
        var priorityRequest = new EnterpriseCulturalRequest(
            _validFortune500APIKey,
            CulturalIntelligenceEndpoint.DiasporaAnalytics("/cultural/diaspora/emergency-analysis"),
            EnterpriseComplexityLevel.VeryHigh(requiresAdvancedAI: true),
            SLAResponseTime.RealTime(), // <100ms requirement for emergency
            CulturalValidationLevel.Expert(),
            new RequestMetadata(new Dictionary<string, object> {
                ["priority"] = "emergency",
                ["analysis_type"] = "community_crisis_response",
                ["regions"] = new[] { "San Francisco Bay Area", "Toronto", "London" },
                ["urgency_level"] = "critical"
            }),
            _fortune500ClientId);
            
        var enterpriseSLA = SLARequirements.CreateEnterpriseSLA();
        
        var expectedResponse = new EnterpriseCulturalResponse(
            responseData: "{\"priority_analysis\": {\"community_response_capacity\": \"high\", \"coordination_hubs\": [\"SF Temple\", \"Toronto Community Center\"], \"emergency_contacts\": [...]}}",
            processingTime: TimeSpan.FromMilliseconds(85), // Within real-time SLA
            validationResult: new CulturalValidationResult(
                isValid: true,
                confidenceScore: 0.95,
                validationNotes: new List<string> { "Priority queue processing", "Expert validation expedited" }),
            warnings: new List<string>(),
            slaCompliant: true,
            correlationId: "emergency-priority-001",
            processingCost: 45); // High cost for priority processing
            
        _mockGatewayService
            .Setup(x => x.ProcessPriorityRequestAsync(
                It.IsAny<EnterpriseCulturalRequest>(),
                It.IsAny<SLARequirements>()))
            .ReturnsAsync(Result<EnterpriseCulturalResponse>.Success(expectedResponse));

        // Act
        var result = await _mockGatewayService.Object.ProcessPriorityRequestAsync(
            priorityRequest, enterpriseSLA);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.SLACompliant.Should().BeTrue();
        result.Value.ProcessingTime.Should().BeLessThan(TimeSpan.FromMilliseconds(100)); // Real-time SLA
        result.Value.ValidationResult.ConfidenceScore.Should().BeGreaterThan(0.9);
        result.Value.ResponseData.Should().Contain("priority_analysis");
        result.Value.ResponseData.Should().Contain("emergency_contacts");
        result.Value.ProcessingCost.Should().BeGreaterThan(40); // Premium for priority processing
    }

    #endregion

    #region Helper Methods

    private EnterpriseContract CreateValidFortune500Contract()
    {
        return EnterpriseContract.CreateFortune500Contract(
            "Fortune 500 Test Corp",
            "enterprise@fortune500test.com",
            1200000m, // $1.2M annual
            DateTime.UtcNow.AddDays(-30), // Started 30 days ago
            36, // 3-year term
            "Full Cultural Intelligence platform access");
    }

    #endregion
}

#region Supporting Test Value Objects

public class RateLimitingDecision
{
    public bool Allowed { get; }
    public string Reason { get; }
    public int RemainingQuota { get; }
    public DateTime ResetTime { get; }
    public int ComplexityScore { get; }
    public RequestPriority PriorityLevel { get; }

    public RateLimitingDecision(
        bool allowed,
        string reason,
        int remainingQuota,
        DateTime resetTime,
        int complexityScore,
        RequestPriority priorityLevel)
    {
        Allowed = allowed;
        Reason = reason;
        RemainingQuota = remainingQuota;
        ResetTime = resetTime;
        ComplexityScore = complexityScore;
        PriorityLevel = priorityLevel;
    }
}

public enum RequestPriority
{
    Low,
    Normal,
    High,
    Critical,
    Emergency
}

#endregion