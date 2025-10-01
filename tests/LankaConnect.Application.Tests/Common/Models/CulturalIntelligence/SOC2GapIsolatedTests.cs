using System;
using FluentAssertions;
using Xunit;
using LankaConnect.Application.Common.Models.CulturalIntelligence;

namespace LankaConnect.Application.Tests.Common.Models.CulturalIntelligence;

/// <summary>
/// Isolated TDD tests for SOC2Gap to verify the specific compliance type works
/// </summary>
public class SOC2GapIsolatedTests
{
    [Fact]
    public void SOC2Gap_ConstructorWithTwoParameters_ShouldCreateValidGap()
    {
        // Arrange & Act
        var gap = new SOC2Gap("SECURITY", "Security controls not properly implemented");

        // Assert
        gap.Should().NotBeNull();
        gap.GapId.Should().NotBeNullOrEmpty();
        gap.GapCategory.Should().Be("SECURITY");
        gap.Description.Should().Be("Security controls not properly implemented");
        gap.Severity.Should().Be("Critical");
        gap.ResponsibleTeam.Should().Be("Security Team");
        gap.IdentifiedDate.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void SOC2Gap_ParameterlessConstructor_ShouldCreateEmptyGap()
    {
        // Arrange & Act
        var gap = new SOC2Gap();

        // Assert
        gap.Should().NotBeNull();
        gap.GapId.Should().NotBeNull();
        gap.GapCategory.Should().NotBeNull();
        gap.Description.Should().NotBeNull();
        gap.Severity.Should().NotBeNull();
        gap.ResponsibleTeam.Should().NotBeNull();
        gap.ComplianceDetails.Should().NotBeNull();
    }

    [Theory]
    [InlineData("SECURITY", "Critical", "Security Team")]
    [InlineData("AVAILABILITY", "High", "Infrastructure Team")]
    [InlineData("PROCESSING_INTEGRITY", "High", "Development Team")]
    [InlineData("CONFIDENTIALITY", "Critical", "Security Team")]
    [InlineData("PRIVACY", "Critical", "Compliance Team")]
    [InlineData("UNKNOWN", "Medium", "General Team")]
    public void SOC2Gap_Constructor_ShouldMapCategoryToSeverityAndTeam(string category, string expectedSeverity, string expectedTeam)
    {
        // Arrange & Act
        var gap = new SOC2Gap(category, "Test description");

        // Assert
        gap.GapCategory.Should().Be(category);
        gap.Severity.Should().Be(expectedSeverity);
        gap.ResponsibleTeam.Should().Be(expectedTeam);
    }
}