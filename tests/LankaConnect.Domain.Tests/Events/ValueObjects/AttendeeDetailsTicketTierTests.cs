using LankaConnect.Domain.Events.Enums;
using LankaConnect.Domain.Events.ValueObjects;

namespace LankaConnect.Domain.Tests.Events.ValueObjects;

/// <summary>
/// Tests for AttendeeDetails with TicketTierId/TicketTierName support
/// </summary>
public class AttendeeDetailsTicketTierTests
{
    private readonly Guid _tierId = Guid.NewGuid();

    [Fact]
    public void Create_WithTierAssignment_Should_Succeed()
    {
        var result = AttendeeDetails.Create(
            "John Doe", AgeCategory.Adult, Gender.Male,
            ticketTierId: _tierId, ticketTierName: "VIP");

        result.IsSuccess.Should().BeTrue();
        result.Value.TicketTierId.Should().Be(_tierId);
        result.Value.TicketTierName.Should().Be("VIP");
    }

    [Fact]
    public void Create_WithoutTierAssignment_Should_HaveNullTier()
    {
        var result = AttendeeDetails.Create("Jane Doe", AgeCategory.Child);

        result.IsSuccess.Should().BeTrue();
        result.Value.TicketTierId.Should().BeNull();
        result.Value.TicketTierName.Should().BeNull();
    }

    [Fact]
    public void Create_TrimsTicketTierName()
    {
        var result = AttendeeDetails.Create(
            "John Doe", AgeCategory.Adult,
            ticketTierId: _tierId, ticketTierName: "  VIP  ");

        result.IsSuccess.Should().BeTrue();
        result.Value.TicketTierName.Should().Be("VIP");
    }

    [Fact]
    public void Equality_DifferentTiers_ShouldNotBeEqual()
    {
        var tierId1 = Guid.NewGuid();
        var tierId2 = Guid.NewGuid();

        var a1 = AttendeeDetails.Create("John", AgeCategory.Adult, ticketTierId: tierId1, ticketTierName: "VIP").Value;
        var a2 = AttendeeDetails.Create("John", AgeCategory.Adult, ticketTierId: tierId2, ticketTierName: "Basic").Value;

        a1.Should().NotBe(a2);
    }

    [Fact]
    public void Equality_SameTier_ShouldBeEqual()
    {
        var a1 = AttendeeDetails.Create("John", AgeCategory.Adult, ticketTierId: _tierId, ticketTierName: "VIP").Value;
        var a2 = AttendeeDetails.Create("John", AgeCategory.Adult, ticketTierId: _tierId, ticketTierName: "VIP").Value;

        a1.Should().Be(a2);
    }

    [Fact]
    public void BackwardCompatibility_ExistingCreateCallsStillWork()
    {
        // Existing code creates AttendeeDetails without tier params — should still work
        var result = AttendeeDetails.Create("Test User", AgeCategory.Adult, Gender.Female);

        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be("Test User");
        result.Value.AgeCategory.Should().Be(AgeCategory.Adult);
        result.Value.Gender.Should().Be(Gender.Female);
        result.Value.TicketTierId.Should().BeNull();
        result.Value.TicketTierName.Should().BeNull();
    }
}
