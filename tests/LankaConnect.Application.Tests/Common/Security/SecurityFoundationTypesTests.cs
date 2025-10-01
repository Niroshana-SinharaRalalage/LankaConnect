using FluentAssertions;
using LankaConnect.Application.Common.Security;
using LankaConnect.Domain.Common;
using Xunit;

namespace LankaConnect.Application.Tests.Common.Security;

/// <summary>
/// TDD RED Phase: Security Foundation Types Tests
/// Testing critical foundation types to resolve 80+ cascading compilation errors
/// Priority: ProtectionLevel (6 refs), SecurityLoadBalancingResult, ScalingOperation
/// </summary>
public class SecurityFoundationTypesTests
{
    #region ProtectionLevel Tests (RED Phase - 6 references)

    [Fact]
    public void ProtectionLevel_BasicLevel_ShouldHaveCorrectProperties()
    {
        // Arrange & Act
        var basicLevel = ProtectionLevel.Basic;
        
        // Assert
        basicLevel.Should().Be(ProtectionLevel.Basic);
        ((int)basicLevel).Should().Be(1);
    }

    [Fact]
    public void ProtectionLevel_EnterpriseLevel_ShouldBeHighestValue()
    {
        // Arrange & Act
        var enterpriseLevel = ProtectionLevel.Enterprise;
        
        // Assert
        enterpriseLevel.Should().Be(ProtectionLevel.Enterprise);
        ((int)enterpriseLevel).Should().BeGreaterThan((int)ProtectionLevel.High);
        ((int)enterpriseLevel).Should().BeGreaterThan((int)ProtectionLevel.Medium);
        ((int)enterpriseLevel).Should().BeGreaterThan((int)ProtectionLevel.Basic);
    }

    [Fact]
    public void ProtectionLevel_AllLevels_ShouldBeInCorrectOrder()
    {
        // Arrange & Act & Assert
        ((int)ProtectionLevel.Basic).Should().BeLessThan((int)ProtectionLevel.Medium);
        ((int)ProtectionLevel.Medium).Should().BeLessThan((int)ProtectionLevel.High);
        ((int)ProtectionLevel.High).Should().BeLessThan((int)ProtectionLevel.Enterprise);
    }

    #endregion

    #region Security Load Balancing Result Tests (RED Phase)

    [Fact]
    public void SecurityLoadBalancingResult_CreateSuccess_ShouldReturnValidResult()
    {
        // Arrange
        var configuration = new SecurityLoadBalancingConfiguration
        {
            LoadBalancingStrategy = LoadBalancingStrategy.CulturalIntelligenceAware,
            SecurityAwareRouting = true,
            CulturalEventLoadDistribution = true,
            DiasporaEngagementOptimization = true,
            ThreatDetectionIntegration = true
        };

        var metrics = new SecurityLoadBalancingMetrics
        {
            TotalRequestsBalanced = 150000,
            SecurityValidationsPerformed = 148500,
            CulturalEventRequestsRouted = 45000,
            DiasporaEngagementRequestsOptimized = 67000,
            AverageSecurityValidationTime = TimeSpan.FromMilliseconds(12),
            LoadDistributionEfficiency = 94.8m
        };
        
        // Act
        var result = SecurityLoadBalancingResult.Create(configuration, metrics);
        
        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Value.IsSecurityOptimized.Should().BeTrue();
        result.Value.CulturalIntelligencePerformance.Should().BeGreaterThan(90m);
        result.Value.DiasporaEngagementEfficiency.Should().BeGreaterThan(85m);
        result.Value.ThreatDetectionIntegration.Should().BeTrue();
        result.Value.OverallSecurityScore.Should().BeGreaterThan(90m);
    }

    [Fact]
    public void SecurityLoadBalancingResult_PoorPerformance_ShouldIndicateIssues()
    {
        // Arrange
        var poorConfiguration = new SecurityLoadBalancingConfiguration
        {
            LoadBalancingStrategy = LoadBalancingStrategy.Basic,
            SecurityAwareRouting = false,
            CulturalEventLoadDistribution = false,
            DiasporaEngagementOptimization = false,
            ThreatDetectionIntegration = false
        };

        var poorMetrics = new SecurityLoadBalancingMetrics
        {
            TotalRequestsBalanced = 50000,
            SecurityValidationsPerformed = 25000, // Only 50% validated
            AverageSecurityValidationTime = TimeSpan.FromMilliseconds(150), // Too slow
            LoadDistributionEfficiency = 45.2m // Poor efficiency
        };
        
        // Act
        var result = SecurityLoadBalancingResult.Create(poorConfiguration, poorMetrics);
        
        // Assert
        result.Value.IsSecurityOptimized.Should().BeFalse();
        result.Value.RequiresOptimization.Should().BeTrue();
        result.Value.SecurityValidationCoverage.Should().BeLessThan(60m);
        result.Value.PerformanceRecommendations.Should().NotBeEmpty();
    }

    #endregion

    #region Scaling Operation Tests (RED Phase)

    [Fact]
    public void ScalingOperation_CreateSuccess_ShouldReturnValidOperation()
    {
        // Arrange
        var operationType = ScalingOperationType.CulturalEventAutoScaling;
        var targetResources = new ScalingTargetResources
        {
            CPUCores = 16,
            MemoryGB = 64,
            StorageGB = 1000,
            NetworkBandwidthMbps = 10000,
            CulturalIntelligenceProcessingUnits = 8
        };

        var scalingTriggers = new ScalingTriggers
        {
            CPUThreshold = 75.0m,
            MemoryThreshold = 80.0m,
            CulturalEventLoadThreshold = 70.0m,
            DiasporaEngagementSpike = 85.0m,
            NetworkThroughputThreshold = 90.0m
        };
        
        // Act
        var result = ScalingOperation.Create(operationType, targetResources, scalingTriggers);
        
        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Value.OperationType.Should().Be(operationType);
        result.Value.IsValidConfiguration.Should().BeTrue();
        result.Value.EstimatedExecutionTime.Should().BeGreaterThan(TimeSpan.Zero);
        result.Value.CulturalIntelligenceReady.Should().BeTrue();
        result.Value.DiasporaEngagementSupported.Should().BeTrue();
        result.Value.SecurityCompliant.Should().BeTrue();
    }

    [Fact]
    public void ScalingOperation_InvalidConfiguration_ShouldReturnFailure()
    {
        // Arrange
        var invalidTargetResources = new ScalingTargetResources
        {
            CPUCores = 0, // Invalid
            MemoryGB = -10, // Invalid
            CulturalIntelligenceProcessingUnits = -1 // Invalid
        };

        var invalidTriggers = new ScalingTriggers
        {
            CPUThreshold = 150.0m, // Invalid > 100%
            CulturalEventLoadThreshold = -5.0m // Invalid negative
        };
        
        // Act
        var result = ScalingOperation.Create(ScalingOperationType.Manual, invalidTargetResources, invalidTriggers);
        
        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().NotBeEmpty();
        result.Errors.Should().Contain(e => e.Contains("CPU cores"));
        result.Errors.Should().Contain(e => e.Contains("threshold"));
    }

    #endregion

    #region Security Maintenance Protocol Tests (RED Phase)

    [Fact]
    public void SecurityMaintenanceProtocol_CreateSuccess_ShouldReturnValidProtocol()
    {
        // Arrange
        var protocolType = SecurityMaintenanceType.CulturalIntelligenceSecurityAudit;
        var maintenanceSchedule = new SecurityMaintenanceSchedule
        {
            Frequency = MaintenanceFrequency.Weekly,
            MaintenanceWindow = new TimeSpan(2, 0, 0, 0), // 2 days
            CulturalEventExclusion = true,
            DiasporaEngagementMinimalImpact = true,
            AutomaticRollback = true
        };

        var securityChecks = new SecurityMaintenanceChecks
        {
            VulnerabilityScanning = true,
            PenetrationTesting = true,
            CulturalDataIntegrityValidation = true,
            DiasporaPrivacyCompliance = true,
            CrossRegionSecurityValidation = true,
            ThreatDetectionTesting = true
        };
        
        // Act
        var result = SecurityMaintenanceProtocol.Create(protocolType, maintenanceSchedule, securityChecks);
        
        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Value.ProtocolType.Should().Be(protocolType);
        result.Value.IsComprehensive.Should().BeTrue();
        result.Value.CulturalIntelligenceProtected.Should().BeTrue();
        result.Value.DiasporaEngagementSafe.Should().BeTrue();
        result.Value.ComplianceLevel.Should().Be(ComplianceLevel.FullyCompliant);
        result.Value.EstimatedMaintenanceDuration.Should().BeLessThan(TimeSpan.FromHours(8));
    }

    #endregion

    #region Disaster Recovery Procedure Tests (RED Phase)

    [Fact]
    public void DisasterRecoveryProcedure_CreateSuccess_ShouldReturnValidProcedure()
    {
        // Arrange
        var recoveryType = DisasterRecoveryType.CulturalIntelligenceSystemFailure;
        var recoveryConfiguration = new DisasterRecoveryConfiguration
        {
            RecoveryTimeObjective = TimeSpan.FromMinutes(30),
            RecoveryPointObjective = TimeSpan.FromMinutes(5),
            CulturalDataRecoveryPriority = RecoveryPriority.Critical,
            DiasporaEngagementContinuity = true,
            CrossRegionFailoverEnabled = true,
            AutomaticRecoveryTriggers = true
        };

        var recoveryResources = new DisasterRecoveryResources
        {
            BackupDataCenters = 3,
            CulturalIntelligenceBackupSystems = 2,
            DiasporaEngagementBackupCapacity = 100,
            CrossRegionConnectivity = true,
            EmergencyScalingCapability = true
        };
        
        // Act
        var result = DisasterRecoveryProcedure.Create(recoveryType, recoveryConfiguration, recoveryResources);
        
        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Value.RecoveryType.Should().Be(recoveryType);
        result.Value.MeetsRecoveryObjectives.Should().BeTrue();
        result.Value.CulturalDataProtected.Should().BeTrue();
        result.Value.DiasporaEngagementContinuityScore.Should().BeGreaterThan(95m);
        result.Value.CrossRegionFailoverReady.Should().BeTrue();
        result.Value.EstimatedRecoveryTime.Should().BeLessOrEqualTo(TimeSpan.FromMinutes(30));
    }

    #endregion

    #region ML Threat Detection Configuration Tests (RED Phase)

    [Fact]
    public void MLThreatDetectionConfiguration_CreateSuccess_ShouldReturnValidConfiguration()
    {
        // Arrange
        var detectionScope = ThreatDetectionScope.CulturalIntelligencePlatform;
        var mlConfiguration = new MLThreatDetectionSettings
        {
            ModelAccuracy = 96.5m,
            FalsePositiveRate = 2.1m,
            DetectionLatency = TimeSpan.FromMilliseconds(50),
            CulturalPatternAnalysisEnabled = true,
            DiasporaEngagementAnomalyDetection = true,
            RealTimeProcessing = true,
            AutomaticMitigation = true
        };

        var threatCategories = new ThreatDetectionCategories
        {
            CulturalDataExfiltration = true,
            DiasporaIdentityTheft = true,
            CommunityEngagementManipulation = true,
            CrossBorderSecurityViolations = true,
            CulturalIntelligenceSystemIntrusion = true
        };
        
        // Act
        var result = MLThreatDetectionConfiguration.Create(detectionScope, mlConfiguration, threatCategories);
        
        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Value.DetectionScope.Should().Be(detectionScope);
        result.Value.IsHighAccuracy.Should().BeTrue(); // >95% accuracy
        result.Value.CulturalPatternProtection.Should().BeTrue();
        result.Value.DiasporaSecurityLevel.Should().Be(SecurityLevel.High);
        result.Value.RealTimeCapable.Should().BeTrue();
        result.Value.ComprehensiveThreatCoverage.Should().BeTrue();
    }

    #endregion

    #region Integration Tests (RED Phase)

    [Fact]
    public void SecurityFoundationSystem_IntegratedWorkflow_ShouldProvideComprehensiveSecurity()
    {
        // Arrange
        var protectionLevel = ProtectionLevel.Enterprise;

        var loadBalancingResult = SecurityLoadBalancingResult.Create(
            new SecurityLoadBalancingConfiguration { SecurityAwareRouting = true },
            new SecurityLoadBalancingMetrics { LoadDistributionEfficiency = 95m });

        var scalingOperation = ScalingOperation.Create(
            ScalingOperationType.CulturalEventAutoScaling,
            new ScalingTargetResources { CPUCores = 16, CulturalIntelligenceProcessingUnits = 8 },
            new ScalingTriggers { CulturalEventLoadThreshold = 70m });

        var maintenanceProtocol = SecurityMaintenanceProtocol.Create(
            SecurityMaintenanceType.CulturalIntelligenceSecurityAudit,
            new SecurityMaintenanceSchedule { CulturalEventExclusion = true },
            new SecurityMaintenanceChecks { CulturalDataIntegrityValidation = true });
        
        // Act
        var systemSecurityScore = CalculateFoundationSecurityScore(
            protectionLevel,
            loadBalancingResult.Value,
            scalingOperation.Value,
            maintenanceProtocol.Value);
        
        // Assert
        systemSecurityScore.Should().BeGreaterThan(90.0m);
        protectionLevel.Should().Be(ProtectionLevel.Enterprise);
        loadBalancingResult.Value.IsSecurityOptimized.Should().BeTrue();
        scalingOperation.Value.SecurityCompliant.Should().BeTrue();
        maintenanceProtocol.Value.CulturalIntelligenceProtected.Should().BeTrue();
    }

    #endregion

    private decimal CalculateFoundationSecurityScore(
        ProtectionLevel protection,
        SecurityLoadBalancingResult loadBalancing,
        ScalingOperation scaling,
        SecurityMaintenanceProtocol maintenance)
    {
        var protectionScore = (int)protection * 25m; // Enterprise = 4 * 25 = 100
        var loadBalancingScore = loadBalancing.IsSecurityOptimized ? 90m : 60m;
        var scalingScore = scaling.SecurityCompliant ? 85m : 50m;
        var maintenanceScore = maintenance.IsComprehensive ? 88m : 70m;

        return (protectionScore + loadBalancingScore + scalingScore + maintenanceScore) / 4;
    }
}