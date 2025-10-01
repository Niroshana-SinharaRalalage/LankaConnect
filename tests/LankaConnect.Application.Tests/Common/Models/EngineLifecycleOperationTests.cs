using FluentAssertions;
using LankaConnect.Application.Common.Models.ConnectionPool;

namespace LankaConnect.Application.Tests.Common.Models;

public class EngineLifecycleOperationTests
{
    [Fact]
    public void EngineLifecycleOperation_Should_Exist_And_Be_Instantiable()
    {
        // Arrange & Act
        var operation = new EngineLifecycleOperation();
        
        // Assert
        operation.Should().NotBeNull();
    }

    [Fact]
    public void EngineLifecycleOperation_Should_Have_Required_Properties()
    {
        // Arrange & Act
        var operation = new EngineLifecycleOperation();
        
        // Assert
        operation.Should().NotBeNull();
        operation.GetType().GetProperty("OperationType").Should().NotBeNull();
        operation.GetType().GetProperty("OperationId").Should().NotBeNull();
    }
}