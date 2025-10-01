using FluentAssertions;
using LankaConnect.Application.Common.Security;
using LankaConnect.Domain.Common;
using Xunit;

namespace LankaConnect.Application.Tests.Common.Security;

/// <summary>
/// TDD RED Phase: Cross-Region Security & Failover Types Tests
/// Testing comprehensive cross-region security patterns for global Cultural Intelligence platform
/// Expected Error Reduction: 25-35 errors (final sprint to <100 errors total)
/// </summary>
public class CrossRegionSecurityTypesTests
{
    #region Cross-Border Security Result Tests (RED Phase)

    [Fact]
    public void CrossBorderSecurityResult_CreateSuccess_ShouldReturnValidResult()
    {
        // Arrange
        var securityConfiguration = new CrossBorderSecurityConfiguration
        {
            SourceRegion = "North America",
            TargetRegion = "Europe",
            DataClassification = DataClassification.CulturalIntelligence,
            ComplianceStandards = new[] { "GDPR", "CCPA", "SOC2" },
            EncryptionLevel = EncryptionLevel.EnterpriseGrade
        };

        var securityValidation = new CrossBorderSecurityValidation
        {
            DataSovereigntyCompliant = true,
            CrossBorderTransferApproved = true,
            CulturalDataProtectionLevel = CulturalDataProtection.Maximum,
            DiasporaPrivacyCompliance = true,
            DataResidencyRequirementsMet = true
        };
        
        // Act
        var result = CrossBorderSecurityResult.Create(securityConfiguration, securityValidation);
        
        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Value.IsSecureForCrossBorderTransfer.Should().BeTrue();
        result.Value.ComplianceLevel.Should().Be(ComplianceLevel.FullyCompliant);
        result.Value.CulturalIntelligenceProtectionScore.Should().BeGreaterThan(90m);
        result.Value.DiasporaDataSecurityLevel.Should().Be(SecurityLevel.Maximum);
        result.Value.DataSovereigntyCompliance.Should().BeTrue();
    }

    [Fact]
    public void CrossBorderSecurityResult_NonCompliantTransfer_ShouldIndicateIssues()
    {
        // Arrange
        var riskyConfiguration = new CrossBorderSecurityConfiguration
        {
            SourceRegion = "Asia",
            TargetRegion = "North America",
            DataClassification = DataClassification.HighlySensitiveCultural,
            ComplianceStandards = new[] { "LocalRegulationsOnly" }, // Insufficient
            EncryptionLevel = EncryptionLevel.Standard // Too low for cultural data
        };

        var nonCompliantValidation = new CrossBorderSecurityValidation
        {
            DataSovereigntyCompliant = false,
            CrossBorderTransferApproved = false,
            CulturalDataProtectionLevel = CulturalDataProtection.Basic,
            DiasporaPrivacyCompliance = false,
            DataResidencyRequirementsMet = false
        };
        
        // Act
        var result = CrossBorderSecurityResult.Create(riskyConfiguration, nonCompliantValidation);
        
        // Assert
        result.Value.IsSecureForCrossBorderTransfer.Should().BeFalse();
        result.Value.ComplianceLevel.Should().Be(ComplianceLevel.NonCompliant);
        result.Value.RequiresImmediateRemediation.Should().BeTrue();
        result.Value.ComplianceViolations.Should().NotBeEmpty();
        result.Value.SecurityRecommendations.Should().NotBeEmpty();
    }

    #endregion

    #region Regional Failover Security Result Tests (RED Phase)

    [Fact]
    public void RegionalFailoverSecurityResult_CreateSuccess_ShouldReturnValidResult()
    {
        // Arrange
        var failoverConfiguration = new RegionalFailoverConfiguration
        {
            PrimaryRegion = "Europe",
            FailoverRegion = "North America",
            FailoverTriggerThreshold = 95m, // 95% availability threshold
            CulturalIntelligenceFailoverEnabled = true,
            DiasporaEngagementContinuity = true,
            MaximumFailoverTime = TimeSpan.FromMinutes(5)
        };

        var securityValidation = new RegionalFailoverSecurityValidation
        {
            CrossRegionSecurityMaintained = true,
            DataIntegrityDuringFailover = true,
            CulturalDataConsistencyValidated = true,
            ComplianceRequirementsInFailoverRegion = true,
            EncryptionInTransit = true,
            EncryptionAtRest = true
        };
        
        // Act
        var result = RegionalFailoverSecurityResult.Create(failoverConfiguration, securityValidation);
        
        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Value.IsFailoverSecure.Should().BeTrue();
        result.Value.SecurityMaintainedDuringFailover.Should().BeTrue();
        result.Value.CulturalIntelligenceContinuitySecure.Should().BeTrue();
        result.Value.DiasporaEngagementSecurityLevel.Should().Be(SecurityLevel.High);
        result.Value.ComplianceInFailoverRegion.Should().BeTrue();
        result.Value.DataIntegrityScore.Should().BeGreaterThan(95m);
    }

    [Fact]
    public void RegionalFailoverSecurityResult_InsecureFailover_ShouldIndicateRisks()
    {
        // Arrange
        var insecureConfiguration = new RegionalFailoverConfiguration
        {
            PrimaryRegion = "Asia",
            FailoverRegion = "Unregulated Region",
            FailoverTriggerThreshold = 50m, // Too low
            CulturalIntelligenceFailoverEnabled = false, // Cultural data not protected
            DiasporaEngagementContinuity = false,
            MaximumFailoverTime = TimeSpan.FromHours(2) // Too slow
        };

        var insecureValidation = new RegionalFailoverSecurityValidation
        {
            CrossRegionSecurityMaintained = false,
            DataIntegrityDuringFailover = false,
            CulturalDataConsistencyValidated = false,
            ComplianceRequirementsInFailoverRegion = false,
            EncryptionInTransit = false,
            EncryptionAtRest = true
        };
        
        // Act
        var result = RegionalFailoverSecurityResult.Create(insecureConfiguration, insecureValidation);
        
        // Assert
        result.Value.IsFailoverSecure.Should().BeFalse();
        result.Value.SecurityMaintainedDuringFailover.Should().BeFalse();
        result.Value.CulturalIntelligenceContinuitySecure.Should().BeFalse();
        result.Value.RequiresSecurityUpgrade.Should().BeTrue();
        result.Value.SecurityRisks.Should().NotBeEmpty();
        result.Value.ComplianceInFailoverRegion.Should().BeFalse();
    }

    #endregion

    #region Cross-Region Incident Response Result Tests (RED Phase)

    [Fact]
    public void CrossRegionIncidentResponseResult_CreateSuccess_ShouldReturnValidResult()
    {
        // Arrange
        var incidentConfiguration = new CrossRegionIncidentConfiguration
        {
            IncidentType = SecurityIncidentType.CulturalDataBreach,
            AffectedRegions = new[] { "Europe", "North America", "Asia-Pacific" },
            IncidentSeverity = IncidentSeverity.High,
            CulturalIntelligenceDataInvolved = true,
            DiasporaCommunitiesAffected = 3,
            ResponseTimeRequirement = TimeSpan.FromMinutes(15)
        };

        var responseMetrics = new IncidentResponseMetrics
        {
            InitialResponseTime = TimeSpan.FromMinutes(12),
            CrossRegionCoordinationTime = TimeSpan.FromMinutes(8),
            ContainmentTime = TimeSpan.FromMinutes(45),
            CulturalDataProtectionActivated = true,
            DiasporaNotificationsSent = true,
            ComplianceAuthoritiesNotified = true,
            SecurityMeasuresImplemented = 8
        };
        
        // Act
        var result = CrossRegionIncidentResponseResult.Create(incidentConfiguration, responseMetrics);
        
        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Value.ResponseTimeCompliance.Should().BeTrue(); // 12 min < 15 min
        result.Value.CrossRegionCoordinationEffective.Should().BeTrue();
        result.Value.CulturalDataProtectionAdequate.Should().BeTrue();
        result.Value.DiasporaCommunitiesProtected.Should().BeTrue();
        result.Value.ComplianceNotificationCompliant.Should().BeTrue();
        result.Value.IncidentContainmentScore.Should().BeGreaterThan(85m);
    }

    [Fact]
    public void CrossRegionIncidentResponseResult_SlowResponse_ShouldIndicateInadequacy()
    {
        // Arrange
        var criticalIncident = new CrossRegionIncidentConfiguration
        {
            IncidentType = SecurityIncidentType.CulturalIntelligenceSystemBreach,
            AffectedRegions = new[] { "Global" },
            IncidentSeverity = IncidentSeverity.Critical,
            CulturalIntelligenceDataInvolved = true,
            DiasporaCommunitiesAffected = 15, // Many communities affected
            ResponseTimeRequirement = TimeSpan.FromMinutes(5) // Very strict for critical
        };

        var slowResponseMetrics = new IncidentResponseMetrics
        {
            InitialResponseTime = TimeSpan.FromMinutes(25), // Too slow
            CrossRegionCoordinationTime = TimeSpan.FromMinutes(60), // Very slow
            ContainmentTime = TimeSpan.FromHours(4), // Extremely slow
            CulturalDataProtectionActivated = false, // Failed to protect
            DiasporaNotificationsSent = false, // Failed to notify
            ComplianceAuthoritiesNotified = true,
            SecurityMeasuresImplemented = 2 // Insufficient measures
        };
        
        // Act
        var result = CrossRegionIncidentResponseResult.Create(criticalIncident, slowResponseMetrics);
        
        // Assert
        result.Value.ResponseTimeCompliance.Should().BeFalse();
        result.Value.CrossRegionCoordinationEffective.Should().BeFalse();
        result.Value.CulturalDataProtectionAdequate.Should().BeFalse();
        result.Value.DiasporaCommunitiesProtected.Should().BeFalse();
        result.Value.RequiresImmediateImprovement.Should().BeTrue();
        result.Value.IncidentContainmentScore.Should().BeLessThan(50m);
    }

    #endregion

    #region Inter-Region Optimization Result Tests (RED Phase)

    [Fact]
    public void InterRegionOptimizationResult_CreateSuccess_ShouldReturnValidResult()
    {
        // Arrange
        var optimizationConfiguration = new InterRegionOptimizationConfiguration
        {
            OptimizationScope = OptimizationScope.GlobalCulturalIntelligence,
            OptimizationTargets = new[]
            {
                "Cultural Event Latency Optimization",
                "Diaspora Engagement Response Time",
                "Cross-Border Data Transfer Speed"
            },
            PerformanceTargets = new InterRegionPerformanceTargets
            {
                MaxCrossBorderLatency = TimeSpan.FromMilliseconds(200),
                CulturalIntelligenceProcessingLatency = TimeSpan.FromMilliseconds(150),
                DiasporaEngagementResponseTime = TimeSpan.FromMilliseconds(300)
            }
        };

        var optimizationResults = new InterRegionOptimizationData
        {
            AchievedCrossBorderLatency = TimeSpan.FromMilliseconds(180),
            CulturalIntelligenceLatency = TimeSpan.FromMilliseconds(140),
            DiasporaEngagementLatency = TimeSpan.FromMilliseconds(280),
            ThroughputImprovement = 35.5m,
            CulturalEventProcessingGain = 28.3m,
            DiasporaEngagementOptimization = 31.2m
        };
        
        // Act
        var result = InterRegionOptimizationResult.Create(optimizationConfiguration, optimizationResults);
        
        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Value.OptimizationTargetsMet.Should().BeTrue();
        result.Value.CrossBorderLatencyOptimized.Should().BeTrue();
        result.Value.CulturalIntelligencePerformanceGain.Should().BeGreaterThan(25m);
        result.Value.DiasporaEngagementImprovement.Should().BeGreaterThan(25m);
        result.Value.OverallOptimizationScore.Should().BeGreaterThan(80m);
        result.Value.IsProductionReady.Should().BeTrue();
    }

    #endregion

    #region Data Transfer Request Tests (RED Phase)

    [Fact]
    public void DataTransferRequest_CreateSuccess_ShouldReturnValidRequest()
    {
        // Arrange
        var transferScope = DataTransferScope.CulturalIntelligenceData;
        var sourceRegion = "Europe";
        var targetRegion = "North America";
        var dataClassification = DataClassification.CulturalIntelligence;
        var transferReason = "Diaspora Community Service Expansion";
        var complianceRequirements = new[] { "GDPR Article 49", "CCPA", "SOC2 Type II" };
        
        // Act
        var result = DataTransferRequest.Create(
            transferScope,
            sourceRegion,
            targetRegion,
            dataClassification,
            transferReason,
            complianceRequirements);
        
        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Value.TransferScope.Should().Be(transferScope);
        result.Value.SourceRegion.Should().Be(sourceRegion);
        result.Value.TargetRegion.Should().Be(targetRegion);
        result.Value.IsComplianceValidated.Should().BeTrue();
        result.Value.IsCulturalDataProtected.Should().BeTrue();
        result.Value.TransferApprovalStatus.Should().Be(ApprovalStatus.Approved);
        result.Value.RequiredSecurityMeasures.Should().NotBeEmpty();
    }

    [Fact]
    public void DataTransferRequest_HighRiskTransfer_ShouldRequireAdditionalApproval()
    {
        // Arrange
        var highRiskScope = DataTransferScope.SensitiveCulturalData;
        var sourceRegion = "Regulated Region";
        var targetRegion = "Low Regulation Region";
        var sensitiveClassification = DataClassification.HighlySensitiveCultural;
        var vagueReason = "General Operations";
        var insufficientCompliance = new[] { "Local Standards Only" };
        
        // Act
        var result = DataTransferRequest.Create(
            highRiskScope,
            sourceRegion,
            targetRegion,
            sensitiveClassification,
            vagueReason,
            insufficientCompliance);
        
        // Assert
        result.Value.TransferApprovalStatus.Should().Be(ApprovalStatus.RequiresAdditionalApproval);
        result.Value.RequiresManualReview.Should().BeTrue();
        result.Value.RiskLevel.Should().Be(TransferRiskLevel.High);
        result.Value.AdditionalSecurityMeasuresRequired.Should().NotBeEmpty();
        result.Value.ComplianceGaps.Should().NotBeEmpty();
    }

    #endregion

    #region Integration Tests (RED Phase)

    [Fact]
    public void CrossRegionSecuritySystem_IntegratedWorkflow_ShouldProvideComprehensiveSecurity()
    {
        // Arrange
        var crossBorderSecurity = CrossBorderSecurityResult.Create(
            new CrossBorderSecurityConfiguration { DataClassification = DataClassification.CulturalIntelligence },
            new CrossBorderSecurityValidation { DataSovereigntyCompliant = true });

        var failoverSecurity = RegionalFailoverSecurityResult.Create(
            new RegionalFailoverConfiguration { CulturalIntelligenceFailoverEnabled = true },
            new RegionalFailoverSecurityValidation { CrossRegionSecurityMaintained = true });

        var incidentResponse = CrossRegionIncidentResponseResult.Create(
            new CrossRegionIncidentConfiguration { IncidentType = SecurityIncidentType.CulturalDataBreach },
            new IncidentResponseMetrics { InitialResponseTime = TimeSpan.FromMinutes(10) });
        
        // Act
        var systemSecurityScore = CalculateCrossRegionSecurityScore(
            crossBorderSecurity.Value,
            failoverSecurity.Value,
            incidentResponse.Value);
        
        // Assert
        systemSecurityScore.Should().BeGreaterThan(85.0m);
        crossBorderSecurity.Value.IsSecureForCrossBorderTransfer.Should().BeTrue();
        failoverSecurity.Value.IsFailoverSecure.Should().BeTrue();
        incidentResponse.Value.CulturalDataProtectionAdequate.Should().BeTrue();
    }

    #endregion

    private decimal CalculateCrossRegionSecurityScore(
        CrossBorderSecurityResult crossBorder,
        RegionalFailoverSecurityResult failover,
        CrossRegionIncidentResponseResult incident)
    {
        var crossBorderScore = crossBorder.IsSecureForCrossBorderTransfer ? 90m : 60m;
        var failoverScore = failover.IsFailoverSecure ? 85m : 50m;
        var incidentScore = incident.ResponseTimeCompliance ? 88m : 65m;

        return (crossBorderScore + failoverScore + incidentScore) / 3;
    }
}