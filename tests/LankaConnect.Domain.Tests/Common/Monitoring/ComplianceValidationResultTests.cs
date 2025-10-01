using FluentAssertions;
using LankaConnect.Domain.Common.Monitoring;
using LankaConnect.Domain.Common.Enums;
using LankaConnect.Domain.Common.ValueObjects;

namespace LankaConnect.Domain.Tests.Common.Monitoring;

/// <summary>
/// TDD RED PHASE: Tests for ComplianceValidationResult (20 references - Tier 1 Priority)
/// These tests establish the contract for Fortune 500 compliance validation
/// </summary>
public class ComplianceValidationResultTests
{
    [Test]
    public void ComplianceValidationResult_ShouldHaveRequiredProperties()
    {
        // Arrange
        var violations = new List<ComplianceViolation>
        {
            new ComplianceViolation("GDPR-001", "Personal data not encrypted", ComplianceSeverity.High),
            new ComplianceViolation("SOC2-015", "Audit trail incomplete", ComplianceSeverity.Medium)
        };

        var complianceMetrics = new ComplianceMetrics(
            overallScore: 85.5,
            gdprCompliance: 90.0,
            soc2Compliance: 88.0,
            sriLankanDataProtectionCompliance: 82.0
        );

        // Act
        var result = new ComplianceValidationResult(
            isCompliant: false,
            overallComplianceScore: 85.5,
            violations: violations,
            complianceMetrics: complianceMetrics,
            validationTimestamp: DateTime.UtcNow,
            validationContext: "Cultural Data Processing"
        );

        // Assert
        result.IsCompliant.Should().BeFalse();
        result.OverallComplianceScore.Should().Be(85.5);
        result.Violations.Should().HaveCount(2);
        result.ComplianceMetrics.Should().NotBeNull();
        result.ValidationTimestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        result.ValidationContext.Should().Be("Cultural Data Processing");
    }

    [Test]
    public void ComplianceValidationResult_ShouldValidateFullCompliance()
    {
        // Arrange & Act
        var result = new ComplianceValidationResult(
            isCompliant: true,
            overallComplianceScore: 100.0,
            violations: new List<ComplianceViolation>(),
            complianceMetrics: new ComplianceMetrics(100.0, 100.0, 100.0, 100.0),
            validationTimestamp: DateTime.UtcNow,
            validationContext: "Sri Lankan Cultural Heritage Protection"
        );

        // Assert
        result.IsCompliant.Should().BeTrue();
        result.OverallComplianceScore.Should().Be(100.0);
        result.Violations.Should().BeEmpty();
        result.HasCriticalViolations().Should().BeFalse();
        result.IsFortuneReadyCompliance().Should().BeTrue();
    }

    [Test]
    public void ComplianceValidationResult_ShouldIdentifyCriticalViolations()
    {
        // Arrange
        var criticalViolations = new List<ComplianceViolation>
        {
            new ComplianceViolation("SEC-001", "Cultural data exposed in logs", ComplianceSeverity.Critical),
            new ComplianceViolation("GDPR-005", "Data retention policy violated", ComplianceSeverity.High)
        };

        // Act
        var result = new ComplianceValidationResult(
            isCompliant: false,
            overallComplianceScore: 45.0,
            violations: criticalViolations,
            complianceMetrics: new ComplianceMetrics(45.0, 50.0, 40.0, 45.0),
            validationTimestamp: DateTime.UtcNow,
            validationContext: "Emergency Security Audit"
        );

        // Assert
        result.HasCriticalViolations().Should().BeTrue();
        result.GetCriticalViolations().Should().HaveCount(1);
        result.IsFortuneReadyCompliance().Should().BeFalse();
        result.RequiresImmediateAction().Should().BeTrue();
    }

    [Test]
    public void ComplianceValidationResult_ShouldValidateSriLankanDataProtection()
    {
        // Arrange
        var culturalViolations = new List<ComplianceViolation>
        {
            new ComplianceViolation("SLDP-001", "Buddhist sacred text not properly protected", ComplianceSeverity.High),
            new ComplianceViolation("SLDP-002", "Tamil cultural data anonymization required", ComplianceSeverity.Medium)
        };

        var culturalMetrics = new ComplianceMetrics(
            overallScore: 78.0,
            gdprCompliance: 85.0,
            soc2Compliance: 82.0,
            sriLankanDataProtectionCompliance: 68.0 // Below acceptable threshold
        );

        // Act
        var result = new ComplianceValidationResult(
            isCompliant: false,
            overallComplianceScore: 78.0,
            violations: culturalViolations,
            complianceMetrics: culturalMetrics,
            validationTimestamp: DateTime.UtcNow,
            validationContext: "Cultural Heritage Compliance Review"
        );

        // Assert
        result.ComplianceMetrics.SriLankanDataProtectionCompliance.Should().Be(68.0);
        result.MeetsSriLankanCulturalStandards().Should().BeFalse(); // Below 70% threshold
        result.GetCulturalViolations().Should().HaveCount(2);
        result.RequiresCulturalSensitivityReview().Should().BeTrue();
    }

    [Test]
    public void ComplianceValidationResult_ShouldGenerateActionPlan()
    {
        // Arrange
        var violations = new List<ComplianceViolation>
        {
            new ComplianceViolation("GDPR-001", "Consent not recorded", ComplianceSeverity.High),
            new ComplianceViolation("SOC2-003", "Change management incomplete", ComplianceSeverity.Medium),
            new ComplianceViolation("CULT-001", "Cultural sensitivity not validated", ComplianceSeverity.Low)
        };

        var result = new ComplianceValidationResult(
            isCompliant: false,
            overallComplianceScore: 72.0,
            violations: violations,
            complianceMetrics: new ComplianceMetrics(72.0, 75.0, 70.0, 71.0),
            validationTimestamp: DateTime.UtcNow,
            validationContext: "Quarterly Compliance Review"
        );

        // Act
        var actionPlan = result.GenerateActionPlan();

        // Assert
        actionPlan.Should().NotBeNull();
        actionPlan.PriorityActions.Should().HaveCount(1); // High severity
        actionPlan.StandardActions.Should().HaveCount(1); // Medium severity
        actionPlan.MinorActions.Should().HaveCount(1);    // Low severity
        actionPlan.EstimatedResolutionTime.Should().BeGreaterThan(TimeSpan.Zero);
    }

    [Test]
    public void ComplianceValidationResult_ShouldRequireValidComplianceScore()
    {
        // Arrange & Act
        var createWithInvalidScore = () => new ComplianceValidationResult(
            isCompliant: true,
            overallComplianceScore: 150.0, // Invalid score > 100
            violations: new List<ComplianceViolation>(),
            complianceMetrics: new ComplianceMetrics(100.0, 100.0, 100.0, 100.0),
            validationTimestamp: DateTime.UtcNow,
            validationContext: "Test"
        );

        // Assert
        createWithInvalidScore.Should().Throw<ArgumentException>()
            .WithMessage("Compliance score must be between 0.0 and 100.0");
    }

    [Test]
    public void ComplianceValidationResult_ShouldValidateConsistency()
    {
        // Arrange & Act
        var createInconsistentResult = () => new ComplianceValidationResult(
            isCompliant: true,    // Claiming compliance
            overallComplianceScore: 100.0,
            violations: new List<ComplianceViolation> // But has violations
            {
                new ComplianceViolation("TEST-001", "Test violation", ComplianceSeverity.Low)
            },
            complianceMetrics: new ComplianceMetrics(100.0, 100.0, 100.0, 100.0),
            validationTimestamp: DateTime.UtcNow,
            validationContext: "Inconsistent Test"
        );

        // Assert
        createInconsistentResult.Should().Throw<InvalidOperationException>()
            .WithMessage("Cannot be compliant with existing violations");
    }
}

/// <summary>
/// Supporting value objects for compliance validation
/// </summary>
public record ComplianceViolation(string Code, string Description, ComplianceSeverity Severity);

public record ComplianceMetrics(
    double OverallScore,
    double GdprCompliance,
    double Soc2Compliance,
    double SriLankanDataProtectionCompliance
);

public record ComplianceActionPlan(
    IReadOnlyList<string> PriorityActions,
    IReadOnlyList<string> StandardActions,
    IReadOnlyList<string> MinorActions,
    TimeSpan EstimatedResolutionTime
);

public enum ComplianceSeverity
{
    Low,
    Medium,
    High,
    Critical
}