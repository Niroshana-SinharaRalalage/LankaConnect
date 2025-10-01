using FluentAssertions;
using LankaConnect.Application.Common.Security;
using LankaConnect.Domain.Common;
using Xunit;

namespace LankaConnect.Application.Tests.Common.Security;

/// <summary>
/// TDD RED Phase: Audit & Access Types Tests
/// Testing comprehensive audit and access pattern types for Cultural Intelligence platform
/// </summary>
public class AuditAccessTypesTests
{
    #region AuditConfiguration Tests (RED Phase)

    [Fact]
    public void AuditConfiguration_Create_ShouldReturnValidConfiguration()
    {
        // Arrange
        var auditScope = AuditScope.CulturalData;
        var retentionPeriod = TimeSpan.FromDays(2555); // 7 years for SOC2
        var complianceStandards = new[] { "SOC2", "GDPR", "CCPA" };

        // Act
        var result = AuditConfiguration.Create(auditScope, retentionPeriod, complianceStandards);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Value.AuditScope.Should().Be(auditScope);
        result.Value.RetentionPeriod.Should().Be(retentionPeriod);
        result.Value.ComplianceStandards.Should().Contain("SOC2");
        result.Value.IsActive.Should().BeTrue();
    }

    [Fact]
    public void AuditConfiguration_WithInvalidRetention_ShouldReturnFailure()
    {
        // Arrange
        var invalidRetention = TimeSpan.FromDays(1); // Too short for compliance

        // Act
        var result = AuditConfiguration.Create(AuditScope.SystemAccess, invalidRetention, new[] { "SOC2" });

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("retention period");
    }

    [Fact]
    public void AuditConfiguration_EnableRealTimeMonitoring_ShouldActivateFeature()
    {
        // Arrange
        var config = AuditConfiguration.Create(AuditScope.CulturalData, TimeSpan.FromDays(365), new[] { "GDPR" }).Value;

        // Act
        var result = config.EnableRealTimeMonitoring(true);

        // Assert
        result.IsSuccess.Should().BeTrue();
        config.RealTimeMonitoringEnabled.Should().BeTrue();
    }

    #endregion

    #region AccessPatternAnalysis Tests (RED Phase)

    [Fact]
    public void AccessPatternAnalysis_Create_ShouldReturnValidAnalysis()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var analysisWindow = TimeSpan.FromHours(24);
        var accessPatterns = new List<AccessPattern>
        {
            new AccessPattern { Resource = "/cultural-data/ceremonies", Timestamp = DateTime.UtcNow.AddHours(-2), AccessType = "READ" },
            new AccessPattern { Resource = "/cultural-data/ceremonies", Timestamp = DateTime.UtcNow.AddHours(-1), AccessType = "READ" }
        };

        // Act
        var result = AccessPatternAnalysis.Create(userId, analysisWindow, accessPatterns);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Value.UserId.Should().Be(userId);
        result.Value.AnalysisWindow.Should().Be(analysisWindow);
        result.Value.TotalAccesses.Should().Be(2);
        result.Value.IsAnomalous.Should().BeFalse(); // Normal pattern
    }

    [Fact]
    public void AccessPatternAnalysis_WithSuspiciousPattern_ShouldDetectAnomaly()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var suspiciousPatterns = new List<AccessPattern>();
        
        // Create rapid-fire access pattern (suspicious)
        for (int i = 0; i < 100; i++)
        {
            suspiciousPatterns.Add(new AccessPattern 
            { 
                Resource = "/cultural-data/sacred-ceremonies", 
                Timestamp = DateTime.UtcNow.AddSeconds(-i), 
                AccessType = "READ" 
            });
        }

        // Act
        var result = AccessPatternAnalysis.Create(userId, TimeSpan.FromMinutes(5), suspiciousPatterns);

        // Assert
        result.Value.IsAnomalous.Should().BeTrue();
        result.Value.AnomalyScore.Should().BeGreaterThan(80); // High anomaly score
        result.Value.TotalAccesses.Should().Be(100);
    }

    #endregion

    #region AccessAuditResult Tests (RED Phase)

    [Fact]
    public void AccessAuditResult_CreateSuccess_ShouldReturnValidResult()
    {
        // Arrange
        var auditSummary = new AccessAuditSummary
        {
            AuditId = Guid.NewGuid(),
            TotalAccessesAudited = 50000,
            ViolationsFound = 3,
            ComplianceScore = 99.4m,
            AuditDurationMs = 15000,
            CulturalDataAccesses = 12000
        };

        // Act
        var result = AccessAuditResult.Success(auditSummary);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.AuditSummary.Should().Be(auditSummary);
        result.IsCompliant.Should().BeTrue(); // >95% compliance
        result.ViolationCount.Should().Be(3);
        result.CompliancePercentage.Should().Be(99.4m);
    }

    [Fact]
    public void AccessAuditResult_WithHighViolations_ShouldIndicateNonCompliance()
    {
        // Arrange
        var nonCompliantSummary = new AccessAuditSummary
        {
            AuditId = Guid.NewGuid(),
            TotalAccessesAudited = 1000,
            ViolationsFound = 150, // 15% violation rate
            ComplianceScore = 85.0m,
            AuditDurationMs = 8000,
            CulturalDataAccesses = 200
        };

        // Act
        var result = AccessAuditResult.Success(nonCompliantSummary);

        // Assert
        result.IsCompliant.Should().BeFalse();
        result.ViolationCount.Should().Be(150);
        result.CompliancePercentage.Should().Be(85.0m);
        result.RequiresImmediateAction.Should().BeTrue();
    }

    [Fact]
    public void AccessAuditResult_CreateFailure_ShouldReturnFailedResult()
    {
        // Arrange
        var error = "Audit system unavailable - cultural data access monitoring failed";

        // Act
        var result = AccessAuditResult.Failure(error);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(error);
        result.IsCompliant.Should().BeFalse();
        result.AuditSummary.Should().BeNull();
    }

    #endregion

    #region Integration Tests (RED Phase)

    [Fact]
    public void AuditSystem_IntegratedWorkflow_ShouldProvideComprehensiveTracking()
    {
        // Arrange
        var auditConfig = AuditConfiguration.Create(
            AuditScope.CulturalData, 
            TimeSpan.FromDays(2555), 
            new[] { "SOC2", "GDPR" }).Value;
        
        var accessPatterns = new List<AccessPattern>
        {
            new AccessPattern { Resource = "/cultural-intelligence/sacred-content", Timestamp = DateTime.UtcNow.AddHours(-1), AccessType = "READ" }
        };
        
        var patternAnalysis = AccessPatternAnalysis.Create(
            Guid.NewGuid(), 
            TimeSpan.FromHours(24), 
            accessPatterns).Value;

        var auditSummary = new AccessAuditSummary
        {
            AuditId = Guid.NewGuid(),
            TotalAccessesAudited = accessPatterns.Count,
            ViolationsFound = 0,
            ComplianceScore = 100.0m,
            AuditDurationMs = 5000,
            CulturalDataAccesses = accessPatterns.Count
        };

        // Act
        var auditResult = AccessAuditResult.Success(auditSummary);
        var configUpdate = auditConfig.EnableRealTimeMonitoring(true);

        // Assert
        auditConfig.IsActive.Should().BeTrue();
        auditConfig.RealTimeMonitoringEnabled.Should().BeTrue();
        patternAnalysis.IsAnomalous.Should().BeFalse();
        auditResult.IsCompliant.Should().BeTrue();
        auditResult.CompliancePercentage.Should().Be(100.0m);
        configUpdate.IsSuccess.Should().BeTrue();
    }

    #endregion
}