using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using LankaConnect.Domain.Common.Monitoring;
using LankaConnect.Domain.Common.Enums;
using LankaConnect.Domain.Common.ValueObjects;
using LankaConnect.Domain.Common;
using Moq;
using Xunit;

namespace LankaConnect.Domain.Tests.Common.Monitoring;

/// <summary>
/// TDD London School tests for canonical ComplianceViolation domain entity.
/// Tests behavior verification, cultural intelligence compliance, and Clean Architecture boundaries.
/// </summary>
public class ComplianceViolationTests
{
    #region RED PHASE - Enhanced ComplianceViolation Tests

    [Fact]
    public void Create_WithValidCulturalParameters_ShouldCreateComplianceViolationWithCulturalContext()
    {
        // Arrange
        var violationId = "CV-2025-001";
        var slaId = "SLA-CULTURAL-001";
        var violationType = ComplianceViolationType.CulturalDataProtection;
        var severity = ViolationSeverity.High;
        var description = "Cultural data access violation during sacred event";
        var culturalContext = new CulturalDataCategory
        {
            CategoryId = "SACRED-CONTENT",
            CulturalSignificance = CulturalSignificance.Sacred,
            RequiredProtectionLevel = DataProtectionLevel.Maximum
        };
        var duration = TimeSpan.FromHours(2);

        // Act & Assert - This should FAIL initially (RED PHASE)
        var action = () => ComplianceViolation.CreateWithCulturalContext(
            violationId, slaId, violationType, severity, description, culturalContext, duration);

        action.Should().NotThrow("Enhanced ComplianceViolation should support cultural context");
    }

    [Fact]
    public void ComplianceViolation_ShouldImplementProperDomainEntity()
    {
        // Arrange
        var violationId = "CV-2025-002";
        var slaId = "SLA-GDPR-001";

        // Act & Assert - This should FAIL initially (RED PHASE)
        var action = () => ComplianceViolation.Create(
            violationId, slaId, ComplianceViolationType.GDPR, ViolationSeverity.Critical,
            "GDPR violation detected", null, TimeSpan.FromMinutes(30));

        var violation = action.Should().NotThrow().Subject;

        // Domain Entity requirements - will be implemented in GREEN phase
        violation.Should().NotBeNull();
        violation.ViolationId.Should().Be(violationId);
        violation.Should().NotBeNull();
    }

    [Theory]
    [InlineData(ComplianceViolationType.GDPR, "GDPR compliance breach")]
    [InlineData(ComplianceViolationType.HIPAA, "HIPAA protected health information exposure")]
    [InlineData(ComplianceViolationType.SOC2, "SOC2 security control failure")]
    [InlineData(ComplianceViolationType.CulturalDataProtection, "Sacred content unauthorized access")]
    public void Create_WithDifferentComplianceTypes_ShouldCreateAppropriateViolation(
        ComplianceViolationType type, string expectedDescription)
    {
        // Act & Assert - This should FAIL initially (RED PHASE)
        var action = () => ComplianceViolation.Create(
            $"CV-{type}", "SLA-001", type, ViolationSeverity.Medium,
            expectedDescription, null, TimeSpan.FromMinutes(15));

        var violation = action.Should().NotThrow().Subject;
        violation.ViolationType.Should().Be(type);
        violation.Description.Should().Be(expectedDescription);
    }

    [Fact]
    public void RequiresExecutiveAttention_WithHighSeverityAndCulturalImpact_ShouldReturnTrue()
    {
        // Arrange - This should FAIL initially (RED PHASE)
        var violation = ComplianceViolation.CreateWithCulturalContext(
            "CV-EXEC-001", "SLA-001", ComplianceViolationType.CulturalDataProtection,
            ViolationSeverity.High, "Sacred event data breach",
            new CulturalDataCategory
            {
                CategoryId = "RELIGIOUS-CEREMONIES",
                CulturalSignificance = CulturalSignificance.Sacred
            },
            TimeSpan.FromHours(1));

        // Act & Assert
        violation.RequiresExecutiveAttention.Should().BeTrue(
            "High severity violations with cultural impact require executive attention");
    }

    [Fact]
    public void IsCulturallySignificant_WithSacredDataCategory_ShouldReturnTrue()
    {
        // Arrange - This should FAIL initially (RED PHASE)
        var culturalCategory = new CulturalDataCategory
        {
            CategoryId = "SACRED-RITUALS",
            CulturalSignificance = CulturalSignificance.Sacred
        };

        var violation = ComplianceViolation.CreateWithCulturalContext(
            "CV-SACRED-001", "SLA-001", ComplianceViolationType.CulturalDataProtection,
            ViolationSeverity.Medium, "Sacred ritual data compromised", culturalCategory,
            TimeSpan.FromMinutes(30));

        // Act & Assert
        violation.IsCulturallySignificant.Should().BeTrue();
        violation.AffectedCulturalCategory.Should().Be(culturalCategory);
    }

    [Fact]
    public void CalculateComplianceImpactScore_WithMultipleFactors_ShouldReturnAccurateScore()
    {
        // Arrange - This should FAIL initially (RED PHASE)
        var violation = ComplianceViolation.CreateWithCulturalContext(
            "CV-IMPACT-001", "SLA-001", ComplianceViolationType.MultiJurisdictional,
            ViolationSeverity.Critical, "Cross-border cultural data violation",
            new CulturalDataCategory
            {
                CategoryId = "DIASPORA-HERITAGE",
                CulturalSignificance = CulturalSignificance.CriticalHeritage,
                RequiredProtectionLevel = DataProtectionLevel.Maximum
            },
            TimeSpan.FromDays(1));

        // Act & Assert
        var impactScore = violation.CalculateComplianceImpactScore();
        impactScore.Should().BeGreaterThan(0.8,
            "Critical violations with cultural heritage impact should have high impact scores");
    }

    [Fact]
    public void GenerateRemediationPlan_ForCulturalViolation_ShouldIncludeCulturalExpertise()
    {
        // Arrange - This should FAIL initially (RED PHASE)
        var violation = ComplianceViolation.CreateWithCulturalContext(
            "CV-REMEDIATION-001", "SLA-001", ComplianceViolationType.CulturalDataProtection,
            ViolationSeverity.High, "Cultural sensitivity violation",
            new CulturalDataCategory
            {
                CategoryId = "TRADITIONAL-PRACTICES",
                CulturalSignificance = CulturalSignificance.High
            },
            TimeSpan.FromHours(4));

        // Act & Assert
        var remediationPlan = violation.GenerateRemediationPlan();
        remediationPlan.Should().NotBeNull();
        remediationPlan.RequiresCulturalExpertConsultation.Should().BeTrue();
        remediationPlan.RemediationSteps.Should().Contain(step =>
            step.StepType == RemediationStepType.CulturalSensitivityReview);
    }

    [Theory]
    [InlineData(ViolationSeverity.Low, false)]
    [InlineData(ViolationSeverity.Medium, false)]
    [InlineData(ViolationSeverity.High, true)]
    [InlineData(ViolationSeverity.Critical, true)]
    public void RequiresImmediateAction_BasedOnSeverity_ShouldReturnCorrectValue(
        ViolationSeverity severity, bool expectedRequiresAction)
    {
        // Arrange - This should FAIL initially (RED PHASE)
        var violation = ComplianceViolation.Create(
            "CV-ACTION-001", "SLA-001", ComplianceViolationType.SOC2,
            severity, "Security control violation", null, TimeSpan.FromMinutes(10));

        // Act & Assert
        violation.RequiresImmediateAction.Should().Be(expectedRequiresAction);
    }

    #endregion

    #region Cultural Intelligence Integration Tests

    [Fact]
    public void ValidateCulturalCompliance_WithGDPRAndCulturalData_ShouldPerformDualValidation()
    {
        // Arrange - This should FAIL initially (RED PHASE)
        var culturalCategory = new CulturalDataCategory
        {
            CategoryId = "EUROPEAN-DIASPORA-DATA",
            CulturalSignificance = CulturalSignificance.High,
            RequiredProtectionLevel = DataProtectionLevel.GDPR
        };

        var violation = ComplianceViolation.CreateWithCulturalContext(
            "CV-DUAL-001", "SLA-GDPR-001", ComplianceViolationType.GDPR,
            ViolationSeverity.Medium, "GDPR violation affecting cultural heritage data",
            culturalCategory, TimeSpan.FromHours(1));

        // Act & Assert
        var complianceValidation = violation.ValidateCulturalCompliance();
        complianceValidation.Should().NotBeNull();
        complianceValidation.GDPRCompliant.Should().BeFalse("Violation indicates non-compliance");
        complianceValidation.CulturalSensitivityCompliant.Should().BeFalse();
        complianceValidation.RequiresDualRemediation.Should().BeTrue();
    }

    [Fact]
    public void TrackViolationHistory_ShouldMaintainTemporalInvariant()
    {
        // Arrange - This should FAIL initially (RED PHASE)
        var violation = ComplianceViolation.Create(
            "CV-HISTORY-001", "SLA-001", ComplianceViolationType.SOC2,
            ViolationSeverity.Medium, "Tracked violation", null, TimeSpan.FromMinutes(30));

        var historyEntry1 = ViolationHistoryEntry.Create("Status updated", ViolationStatus.Detected);
        var historyEntry2 = ViolationHistoryEntry.Create("Under investigation", ViolationStatus.InvestigationInProgress);

        // Act - This should FAIL initially (RED PHASE)
        violation.AddHistoryEntry(historyEntry1);
        violation.AddHistoryEntry(historyEntry2);

        // Assert
        violation.ViolationHistory.Should().HaveCount(2);
        violation.ViolationHistory.Should().BeInAscendingOrder(h => h.Timestamp,
            "History should maintain temporal order");
    }

    #endregion

    #region London School Mockist Tests (Behavior Verification)

    [Fact]
    public void NotifyStakeholders_WhenCriticalCulturalViolationOccurs_ShouldNotifyAllRelevantParties()
    {
        // Arrange - Mock dependencies (London School approach)
        var mockNotificationService = new Mock<ICulturalComplianceNotificationService>();
        var mockAuditLogger = new Mock<IComplianceAuditLogger>();
        var mockCulturalAdvisor = new Mock<ICulturalComplianceAdvisor>();

        var violation = ComplianceViolation.CreateWithCulturalContext(
            "CV-NOTIFY-001", "SLA-001", ComplianceViolationType.CulturalDataProtection,
            ViolationSeverity.Critical, "Critical sacred data breach",
            new CulturalDataCategory
            {
                CategoryId = "SACRED-CEREMONIES",
                CulturalSignificance = CulturalSignificance.Sacred
            },
            TimeSpan.FromHours(2));

        // Act - This should FAIL initially (RED PHASE)
        violation.NotifyStakeholders(mockNotificationService.Object, mockAuditLogger.Object,
            mockCulturalAdvisor.Object);

        // Assert - Verify behavior/interactions (London School focus)
        mockNotificationService.Verify(x => x.NotifyExecutiveTeamAsync(
            It.Is<ComplianceViolation>(v => v.ViolationId == "CV-NOTIFY-001")),
            Times.Once, "Executive team should be notified of critical violations");

        mockNotificationService.Verify(x => x.NotifyCulturalAuthoritiesAsync(
            It.IsAny<CulturalDataCategory>(), It.IsAny<ComplianceViolation>()),
            Times.Once, "Cultural authorities must be notified for sacred data breaches");

        mockAuditLogger.Verify(x => x.LogComplianceViolationAsync(
            It.Is<ComplianceViolation>(v => v.Severity == ViolationSeverity.Critical)),
            Times.Once, "All critical violations must be audit logged");

        mockCulturalAdvisor.Verify(x => x.AssessCulturalImpactAsync(
            It.IsAny<CulturalDataCategory>()),
            Times.Once, "Cultural impact assessment required for cultural violations");
    }

    [Fact]
    public void EscalateViolation_WhenRemediationTimeline exceeded_ShouldFollowEscalationProtocol()
    {
        // Arrange - Mock collaboration objects
        var mockEscalationService = new Mock<IViolationEscalationService>();
        var mockComplianceManager = new Mock<IComplianceManager>();

        var violation = ComplianceViolation.Create(
            "CV-ESCALATE-001", "SLA-001", ComplianceViolationType.GDPR,
            ViolationSeverity.High, "Overdue GDPR violation", null, TimeSpan.FromDays(2));

        // Act - This should FAIL initially (RED PHASE)
        violation.EscalateViolation(mockEscalationService.Object, mockComplianceManager.Object);

        // Assert - Verify proper collaboration sequence
        mockEscalationService.Verify(x => x.CreateEscalationTicketAsync(
            It.Is<ComplianceViolation>(v => v.ViolationId == "CV-ESCALATE-001")),
            Times.Once, "Escalation ticket should be created");

        mockComplianceManager.Verify(x => x.AssignSeniorAnalystAsync(
            It.IsAny<string>()),
            Times.Once, "Senior analyst should be assigned to escalated violations");

        // Verify call order
        var expectedCallOrder = new List<string>
        {
            nameof(IViolationEscalationService.CreateEscalationTicketAsync),
            nameof(IComplianceManager.AssignSeniorAnalystAsync)
        };

        // This verification should FAIL initially (RED PHASE)
        mockEscalationService.Invocations.Select(i => i.Method.Name)
            .Concat(mockComplianceManager.Invocations.Select(i => i.Method.Name))
            .Should().ContainInOrder(expectedCallOrder);
    }

    #endregion

    #region Domain Event Tests

    [Fact]
    public void Create_ComplianceViolation_ShouldRaiseDomainEvent()
    {
        // Arrange - This should FAIL initially (RED PHASE)
        var violation = ComplianceViolation.Create(
            "CV-EVENT-001", "SLA-001", ComplianceViolationType.SOC2,
            ViolationSeverity.Medium, "SOC2 control failure", null, TimeSpan.FromMinutes(45));

        // Act & Assert
        violation.DomainEvents.Should().ContainSingle(e => e is ComplianceViolationDetectedEvent);

        var domainEvent = violation.DomainEvents.OfType<ComplianceViolationDetectedEvent>().First();
        domainEvent.ViolationId.Should().Be("CV-EVENT-001");
        domainEvent.Severity.Should().Be(ViolationSeverity.Medium);
    }

    [Fact]
    public void ResolveViolation_ShouldRaiseResolutionEvent()
    {
        // Arrange - This should FAIL initially (RED PHASE)
        var violation = ComplianceViolation.Create(
            "CV-RESOLVE-001", "SLA-001", ComplianceViolationType.HIPAA,
            ViolationSeverity.Low, "HIPAA minor violation", null, TimeSpan.FromMinutes(10));

        var resolutionDetails = new ViolationResolutionDetails
        {
            ResolvedBy = "compliance.officer@lanka-connect.com",
            ResolutionMethod = ResolutionMethod.ProcessImprovement,
            ResolutionNotes = "Updated access controls"
        };

        // Act - This should FAIL initially (RED PHASE)
        violation.ResolveViolation(resolutionDetails);

        // Assert
        violation.Status.Should().Be(ViolationStatus.Resolved);
        violation.DomainEvents.Should().Contain(e => e is ComplianceViolationResolvedEvent);
    }

    #endregion
}

#region Supporting Types for Tests (Will be implemented in GREEN phase)

public interface ICulturalComplianceNotificationService
{
    Task NotifyExecutiveTeamAsync(ComplianceViolation violation);
    Task NotifyCulturalAuthoritiesAsync(CulturalDataCategory category, ComplianceViolation violation);
}

public interface IComplianceAuditLogger
{
    Task LogComplianceViolationAsync(ComplianceViolation violation);
}

public interface ICulturalComplianceAdvisor
{
    Task<CulturalImpactAssessment> AssessCulturalImpactAsync(CulturalDataCategory category);
}

public interface IViolationEscalationService
{
    Task<string> CreateEscalationTicketAsync(ComplianceViolation violation);
}

public interface IComplianceManager
{
    Task AssignSeniorAnalystAsync(string violationId);
}

#endregion