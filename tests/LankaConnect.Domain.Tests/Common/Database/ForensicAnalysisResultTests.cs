using FluentAssertions;
using LankaConnect.Domain.Common.Database;

namespace LankaConnect.Domain.Tests.Common.Database;

public class ForensicAnalysisResultTests
{
    [Fact]
    public void ForensicAnalysisResult_Should_Be_Instantiable()
    {
        // Arrange & Act
        var result = new ForensicAnalysisResult();
        
        // Assert
        result.Should().NotBeNull();
    }

    [Fact]
    public void ForensicAnalysisResult_Should_Have_Required_Properties()
    {
        // Arrange & Act
        var result = new ForensicAnalysisResult();
        
        // Assert
        result.Should().NotBeNull();
        result.GetType().GetProperty("AnalysisId").Should().NotBeNull();
        result.GetType().GetProperty("CompromisedDataCount").Should().NotBeNull();
        result.GetType().GetProperty("SecurityRiskLevel").Should().NotBeNull();
    }
}