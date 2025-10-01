using FluentAssertions;
using LankaConnect.Application.Common.Models.Backup;

namespace LankaConnect.Application.Tests.Common.Models;

public class BackupScheduleResultTests
{
    [Fact]
    public void BackupScheduleResult_Should_Be_Instantiable()
    {
        // Arrange & Act
        var result = new BackupScheduleResult();
        
        // Assert
        result.Should().NotBeNull();
    }

    [Fact]
    public void BackupScheduleResult_Should_Have_Required_Properties()
    {
        // Arrange & Act
        var result = new BackupScheduleResult();
        
        // Assert
        result.Should().NotBeNull();
        result.GetType().GetProperty("ScheduleId").Should().NotBeNull();
        result.GetType().GetProperty("IsSuccess").Should().NotBeNull();
        result.GetType().GetProperty("CulturalEventConsiderations").Should().NotBeNull();
    }
}