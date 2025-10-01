using FluentAssertions;
using LankaConnect.Application.Common.Models.ConnectionPool;

namespace LankaConnect.Application.Tests.Common.Models;

public class ScalingPredictionResultTests
{
    [Fact]
    public void ScalingPredictionResult_Should_Exist_And_Be_Instantiable()
    {
        // Arrange & Act
        var result = new ScalingPredictionResult();
        
        // Assert
        result.Should().NotBeNull();
    }

    [Fact]
    public void ScalingPredictionResult_Should_Have_Required_Properties()
    {
        // Arrange & Act
        var result = new ScalingPredictionResult();
        
        // Assert
        result.Should().NotBeNull();
        result.GetType().GetProperty("RecommendedInstanceCount").Should().NotBeNull();
        result.GetType().GetProperty("PredictionConfidence").Should().NotBeNull();
    }
}