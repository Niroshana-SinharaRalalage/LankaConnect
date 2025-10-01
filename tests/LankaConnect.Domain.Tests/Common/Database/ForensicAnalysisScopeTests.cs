using FluentAssertions;
using LankaConnect.Domain.Common.Database;

namespace LankaConnect.Domain.Tests.Common.Database;

public class ForensicAnalysisScopeTests
{
    [Fact]
    public void ForensicAnalysisScope_Should_Have_Required_Values()
    {
        // Arrange & Act
        var scopeValues = Enum.GetValues<ForensicAnalysisScope>();
        
        // Assert
        scopeValues.Should().NotBeEmpty();
        scopeValues.Should().Contain(ForensicAnalysisScope.SystemWide);
        scopeValues.Should().Contain(ForensicAnalysisScope.CulturalData);
    }

    [Theory]
    [InlineData(ForensicAnalysisScope.SystemWide)]
    [InlineData(ForensicAnalysisScope.CulturalData)]
    [InlineData(ForensicAnalysisScope.UserData)]
    public void ForensicAnalysisScope_Values_Should_Be_Valid(ForensicAnalysisScope scope)
    {
        // Arrange & Act & Assert
        scope.Should().BeDefined();
        ((int)scope).Should().BeGreaterOrEqualTo(0);
    }
}