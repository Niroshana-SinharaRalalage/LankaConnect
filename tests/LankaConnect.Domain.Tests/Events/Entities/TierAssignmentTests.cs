using LankaConnect.Domain.Events.Entities;
using LankaConnect.Domain.Events.Enums;

namespace LankaConnect.Domain.Tests.Events.Entities;

public class TierAssignmentTests
{
    private readonly Guid _tierId = Guid.NewGuid();
    private readonly Guid _zoneId = Guid.NewGuid();
    private readonly Guid _tableId = Guid.NewGuid();

    [Fact]
    public void Create_WithValidZoneAssignment_Should_Return_Success()
    {
        // Act
        var result = TierAssignment.Create(_tierId, AssignableKind.Zone, _zoneId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var assignment = result.Value;
        assignment.TierId.Should().Be(_tierId);
        assignment.AssignableKind.Should().Be(AssignableKind.Zone);
        assignment.AssignableId.Should().Be(_zoneId);
        assignment.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Create_WithValidTableAssignment_Should_Return_Success()
    {
        // Act
        var result = TierAssignment.Create(_tierId, AssignableKind.Table, _tableId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.AssignableKind.Should().Be(AssignableKind.Table);
        result.Value.AssignableId.Should().Be(_tableId);
    }

    [Fact]
    public void Create_WithEmptyTierId_Should_Fail()
    {
        // Act
        var result = TierAssignment.Create(Guid.Empty, AssignableKind.Zone, _zoneId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Tier ID");
    }

    [Fact]
    public void Create_WithEmptyAssignableId_Should_Fail()
    {
        // Act
        var result = TierAssignment.Create(_tierId, AssignableKind.Zone, Guid.Empty);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Assignable ID");
    }

    [Fact]
    public void Create_Returns_DistinctInstances_For_Same_Inputs()
    {
        // Arrange — two assignments with identical keys; entity equality is value-based (composite key)
        var a = TierAssignment.Create(_tierId, AssignableKind.Zone, _zoneId).Value;
        var b = TierAssignment.Create(_tierId, AssignableKind.Zone, _zoneId).Value;

        // Assert — they are two separate objects, but EF will reject duplicates at the PK level
        a.Should().NotBeSameAs(b);
        a.TierId.Should().Be(b.TierId);
        a.AssignableKind.Should().Be(b.AssignableKind);
        a.AssignableId.Should().Be(b.AssignableId);
    }
}
