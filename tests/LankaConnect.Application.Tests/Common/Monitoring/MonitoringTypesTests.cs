using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using LankaConnect.Application.Common.Models.Monitoring;
using LankaConnect.Domain.Common.Monitoring;
using Xunit;

namespace LankaConnect.Application.Tests.Common.Monitoring;

/// <summary>
/// TDD RED Phase: Comprehensive tests for Core Monitoring Types
/// Testing AlertEscalationRequest, EscalationPolicy, MaintenanceWindow, AlertSuppressionPolicy
/// </summary>
public class MonitoringTypesTests
{
    #region AlertEscalationRequest Tests (RED Phase)

    [Fact]
    public void AlertEscalationRequest_Create_ShouldValidateRequiredProperties()
    {
        // Arrange
        var alertId = "alert-vesak-2024-001";
        var escalationReason = "Cultural event traffic spike detected";
        var priority = AlertPriority.CulturalEvent;
        var culturalEventType = CulturalEventType.ReligiousFestival;

        // Act
        var request = AlertEscalationRequest.Create(alertId, escalationReason, priority, culturalEventType);

        // Assert
        request.Should().NotBeNull();
        request.AlertId.Should().Be(alertId);
        request.EscalationReason.Should().Be(escalationReason);
        request.Priority.Should().Be(priority);
        request.CulturalEventType.Should().Be(culturalEventType);
        request.RequestedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        request.IsCulturalEvent.Should().BeTrue();
        request.RequiresImmediateAttention.Should().BeTrue();
    }

    [Fact]
    public void AlertEscalationRequest_Create_ShouldHandleStandardAlert()
    {
        // Arrange
        var alertId = "alert-database-001";
        var escalationReason = "Database connection pool exhaustion";
        var priority = AlertPriority.High;

        // Act
        var request = AlertEscalationRequest.Create(alertId, escalationReason, priority);

        // Assert
        request.Should().NotBeNull();
        request.AlertId.Should().Be(alertId);
        request.Priority.Should().Be(priority);
        request.CulturalEventType.Should().BeNull();
        request.IsCulturalEvent.Should().BeFalse();
        request.RequiresImmediateAttention.Should().BeTrue();
    }

    [Theory]
    [InlineData(AlertPriority.Critical, true)]
    [InlineData(AlertPriority.CulturalEvent, true)]
    [InlineData(AlertPriority.High, true)]
    [InlineData(AlertPriority.Medium, false)]
    [InlineData(AlertPriority.Low, false)]
    public void AlertEscalationRequest_RequiresImmediateAttention_ShouldReturnCorrectValue(AlertPriority priority, bool expected)
    {
        // Arrange & Act
        var request = AlertEscalationRequest.Create("test-alert", "Test reason", priority);

        // Assert
        request.RequiresImmediateAttention.Should().Be(expected);
    }

    #endregion

    #region EscalationPolicy Tests (RED Phase)

    [Fact]
    public void EscalationPolicy_Create_ShouldValidateCulturalEventPolicy()
    {
        // Arrange
        var policyName = "Cultural Event Escalation Policy";
        var levels = new List<EscalationLevel>
        {
            EscalationLevel.Create("L1-Cultural", TimeSpan.FromMinutes(2), new[] { "cultural-team@lankaconnect.com" }),
            EscalationLevel.Create("L2-Engineering", TimeSpan.FromMinutes(5), new[] { "engineering@lankaconnect.com" }),
            EscalationLevel.Create("L3-Management", TimeSpan.FromMinutes(10), new[] { "management@lankaconnect.com" })
        };
        var culturalEventTypes = new[] { CulturalEventType.ReligiousFestival, CulturalEventType.NationalHoliday };

        // Act
        var policy = EscalationPolicy.CreateCultural(policyName, levels, culturalEventTypes);

        // Assert
        policy.Should().NotBeNull();
        policy.PolicyName.Should().Be(policyName);
        policy.EscalationLevels.Should().HaveCount(3);
        policy.SupportedCulturalEvents.Should().HaveCount(2);
        policy.IsCulturalPolicy.Should().BeTrue();
        policy.MaxEscalationTime.Should().Be(TimeSpan.FromMinutes(17)); // 2+5+10
        policy.IsActive.Should().BeTrue();
    }

    [Fact]
    public void EscalationPolicy_Create_ShouldValidateStandardPolicy()
    {
        // Arrange
        var policyName = "Standard Database Escalation Policy";
        var levels = new List<EscalationLevel>
        {
            EscalationLevel.Create("L1-OnCall", TimeSpan.FromMinutes(5), new[] { "oncall@lankaconnect.com" }),
            EscalationLevel.Create("L2-Lead", TimeSpan.FromMinutes(15), new[] { "team-lead@lankaconnect.com" })
        };

        // Act
        var policy = EscalationPolicy.CreateStandard(policyName, levels);

        // Assert
        policy.Should().NotBeNull();
        policy.PolicyName.Should().Be(policyName);
        policy.EscalationLevels.Should().HaveCount(2);
        policy.SupportedCulturalEvents.Should().BeEmpty();
        policy.IsCulturalPolicy.Should().BeFalse();
        policy.MaxEscalationTime.Should().Be(TimeSpan.FromMinutes(20)); // 5+15
    }

    #endregion

    #region MaintenanceWindow Tests (RED Phase)

    [Fact]
    public void MaintenanceWindow_Create_ShouldValidateMaintenanceWindow()
    {
        // Arrange
        var windowName = "Database Optimization Maintenance";
        var startTime = DateTime.UtcNow.AddHours(2);
        var duration = TimeSpan.FromHours(4);
        var maintenanceType = MaintenanceType.DatabaseOptimization;
        var affectedServices = new[] { "database-cluster", "connection-pool" };

        // Act
        var window = MaintenanceWindow.Create(windowName, startTime, duration, maintenanceType, affectedServices);

        // Assert
        window.Should().NotBeNull();
        window.WindowName.Should().Be(windowName);
        window.StartTime.Should().Be(startTime);
        window.Duration.Should().Be(duration);
        window.EndTime.Should().Be(startTime.Add(duration));
        window.MaintenanceType.Should().Be(maintenanceType);
        window.AffectedServices.Should().HaveCount(2);
        window.IsActive.Should().BeFalse(); // Not started yet
        window.SuppressAlerts.Should().BeTrue();
    }

    [Fact]
    public void MaintenanceWindow_IsActive_ShouldReturnCorrectStatus()
    {
        // Arrange
        var startTime = DateTime.UtcNow.AddMinutes(-10); // Started 10 minutes ago
        var duration = TimeSpan.FromHours(2);
        var window = MaintenanceWindow.Create("Test Window", startTime, duration, MaintenanceType.SystemUpdate, new[] { "service1" });

        // Act & Assert
        window.IsActive.Should().BeTrue();
        window.TimeRemaining.Should().BeLessOrEqualTo(TimeSpan.FromHours(2));
        window.TimeRemaining.Should().BeGreaterThan(TimeSpan.FromMinutes(100));
    }

    [Fact]
    public void MaintenanceWindow_CreateCulturalAware_ShouldAvoidCulturalEvents()
    {
        // Arrange
        var windowName = "Cultural-Aware Maintenance";
        var proposedStart = DateTime.UtcNow.AddDays(1); // Tomorrow
        var duration = TimeSpan.FromHours(6);
        var culturalEvents = new[] { "Vesak Day", "Diwali" };

        // Act
        var window = MaintenanceWindow.CreateCulturalAware(windowName, proposedStart, duration, 
            MaintenanceType.SystemUpgrade, new[] { "cultural-service" }, culturalEvents);

        // Assert
        window.Should().NotBeNull();
        window.WindowName.Should().Be(windowName);
        window.CulturalConsiderations.Should().HaveCount(2);
        window.IsCulturallyAware.Should().BeTrue();
        window.ConflictsWithCulturalEvents.Should().BeFalse();
    }

    #endregion

    #region AlertSuppressionPolicy Tests (RED Phase)

    [Fact]
    public void AlertSuppressionPolicy_Create_ShouldValidateSuppressionRules()
    {
        // Arrange
        var policyName = "Cultural Event Alert Suppression";
        var suppressionRules = new List<SuppressionRule>
        {
            SuppressionRule.Create("High CPU during festivals", AlertCategory.Performance, 
                CulturalEventType.ReligiousFestival, TimeSpan.FromMinutes(30)),
            SuppressionRule.Create("Memory spike during celebrations", AlertCategory.Resource, 
                CulturalEventType.SeasonalCelebration, TimeSpan.FromMinutes(15))
        };

        // Act
        var policy = AlertSuppressionPolicy.Create(policyName, suppressionRules);

        // Assert
        policy.Should().NotBeNull();
        policy.PolicyName.Should().Be(policyName);
        policy.SuppressionRules.Should().HaveCount(2);
        policy.IsActive.Should().BeTrue();
        policy.HasCulturalRules.Should().BeTrue();
        policy.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void AlertSuppressionPolicy_ShouldSuppressAlert_ShouldReturnCorrectDecision()
    {
        // Arrange
        var rules = new List<SuppressionRule>
        {
            SuppressionRule.Create("Festival suppression", AlertCategory.Performance, 
                CulturalEventType.ReligiousFestival, TimeSpan.FromMinutes(60))
        };
        var policy = AlertSuppressionPolicy.Create("Test Policy", rules);
        var alertContext = new AlertSuppressionContext("alert-001", AlertCategory.Performance, CulturalEventType.ReligiousFestival);

        // Act
        var shouldSuppress = policy.ShouldSuppressAlert(alertContext);

        // Assert
        shouldSuppress.Should().BeTrue();
    }

    [Fact]
    public void AlertSuppressionPolicy_GetActiveSuppressions_ShouldReturnActiveSuppressionsOnly()
    {
        // Arrange
        var rules = new List<SuppressionRule>
        {
            SuppressionRule.Create("Active rule", AlertCategory.Performance, 
                CulturalEventType.ReligiousFestival, TimeSpan.FromMinutes(60)),
            SuppressionRule.Create("Expired rule", AlertCategory.Security, 
                CulturalEventType.NationalHoliday, TimeSpan.FromMinutes(-30)) // Expired
        };
        var policy = AlertSuppressionPolicy.Create("Test Policy", rules);

        // Act
        var activeSuppressions = policy.GetActiveSuppressions();

        // Assert
        activeSuppressions.Should().HaveCount(1);
        activeSuppressions.First().Description.Should().Be("Active rule");
    }

    #endregion
}

/// <summary>
/// Supporting types for comprehensive monitoring tests
/// </summary>
public static class MonitoringTestHelpers
{
    public static AlertEscalationRequest CreateCulturalEventEscalation()
    {
        return AlertEscalationRequest.Create(
            "cultural-alert-001",
            "Vesak Day traffic exceeded cultural intelligence capacity",
            AlertPriority.CulturalEvent,
            CulturalEventType.ReligiousFestival);
    }

    public static EscalationPolicy CreateDefaultCulturalPolicy()
    {
        var levels = new List<EscalationLevel>
        {
            EscalationLevel.Create("L1-Cultural", TimeSpan.FromMinutes(2), new[] { "cultural@lankaconnect.com" }),
            EscalationLevel.Create("L2-Engineering", TimeSpan.FromMinutes(5), new[] { "eng@lankaconnect.com" }),
            EscalationLevel.Create("L3-Management", TimeSpan.FromMinutes(10), new[] { "mgmt@lankaconnect.com" })
        };

        return EscalationPolicy.CreateCultural(
            "Default Cultural Event Policy",
            levels,
            new[] { CulturalEventType.ReligiousFestival, CulturalEventType.NationalHoliday });
    }
}