using LankaConnect.Products.LankaEvents.Domain.Enums;
using LankaConnect.Products.LankaEvents.Domain.ValueObjects;

namespace LankaConnect.Domain.Tests.Events.ValueObjects;

/// <summary>
/// Phase 8 S8.1 — tests for AttendeeDetails seat-binding support. The
/// SeatId/SeatLabel fields existed since Phase 2A foundation but were
/// vestigial — never populated. S8.1 introduces a value-object-style
/// `WithSeat` instance method so application handlers can bind a seat
/// to an existing AttendeeDetails without re-specifying every other
/// field on the 7-arg <see cref="AttendeeDetails.Create"/> factory.
/// </summary>
public class AttendeeDetailsSeatTests
{
    private static AttendeeDetails CreateBase()
    {
        var result = AttendeeDetails.Create(
            "John Doe",
            AgeCategory.Adult,
            Gender.Male,
            ticketTierId: Guid.NewGuid(),
            ticketTierName: "VIP");
        result.IsSuccess.Should().BeTrue();
        return result.Value;
    }

    [Fact]
    public void Create_WithSeatFields_Should_Succeed_AndPopulateBoth()
    {
        var seatId = Guid.NewGuid();

        var result = AttendeeDetails.Create(
            "Jane Doe",
            AgeCategory.Adult,
            Gender.Female,
            seatId: seatId,
            seatLabel: "A1");

        result.IsSuccess.Should().BeTrue();
        result.Value.SeatId.Should().Be(seatId);
        result.Value.SeatLabel.Should().Be("A1");
    }

    [Fact]
    public void Create_WithSeatLabelWithWhitespace_Should_Trim()
    {
        var result = AttendeeDetails.Create(
            "Jane Doe", AgeCategory.Adult, Gender.Female,
            seatId: Guid.NewGuid(), seatLabel: "  A1  ");

        result.IsSuccess.Should().BeTrue();
        result.Value.SeatLabel.Should().Be("A1");
    }

    [Fact]
    public void WithSeat_HappyPath_Should_ReturnNewInstance_WithSeatBound()
    {
        var original = CreateBase();
        var seatId = Guid.NewGuid();

        var result = original.WithSeat(seatId, "B7");

        result.IsSuccess.Should().BeTrue();
        result.Value.SeatId.Should().Be(seatId);
        result.Value.SeatLabel.Should().Be("B7");
        // All other fields preserved
        result.Value.Name.Should().Be(original.Name);
        result.Value.AgeCategory.Should().Be(original.AgeCategory);
        result.Value.Gender.Should().Be(original.Gender);
        result.Value.TicketTierId.Should().Be(original.TicketTierId);
        result.Value.TicketTierName.Should().Be(original.TicketTierName);
    }

    [Fact]
    public void WithSeat_Should_ReturnNewInstance_NotMutateOriginal()
    {
        var original = CreateBase();
        original.SeatId.Should().BeNull();
        original.SeatLabel.Should().BeNull();

        var seatId = Guid.NewGuid();
        var bound = original.WithSeat(seatId, "C3").Value;

        // Original is immutable
        original.SeatId.Should().BeNull();
        original.SeatLabel.Should().BeNull();
        bound.SeatId.Should().Be(seatId);
        bound.SeatLabel.Should().Be("C3");
    }

    [Fact]
    public void WithSeat_TrimsSeatLabelWhitespace()
    {
        var original = CreateBase();

        var bound = original.WithSeat(Guid.NewGuid(), "  D9  ").Value;

        bound.SeatLabel.Should().Be("D9");
    }

    [Fact]
    public void WithSeat_RejectsEmptySeatId()
    {
        var original = CreateBase();

        var result = original.WithSeat(Guid.Empty, "A1");

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Seat");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void WithSeat_RejectsEmptyOrWhitespaceSeatLabel(string? bad)
    {
        var original = CreateBase();

        var result = original.WithSeat(Guid.NewGuid(), bad!);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Seat");
    }

    [Fact]
    public void WithSeat_OnAlreadyBoundSeat_AllowsRebind()
    {
        // Use case: webhook retries / reconciliation re-applies seat assignment.
        // No invariant enforced here at the value-object level — the aggregate-level
        // Registration.ConfirmSeatAssignments enforces idempotency.
        var first = CreateBase().WithSeat(Guid.NewGuid(), "A1").Value;
        var newSeatId = Guid.NewGuid();

        var second = first.WithSeat(newSeatId, "B2");

        second.IsSuccess.Should().BeTrue();
        second.Value.SeatId.Should().Be(newSeatId);
        second.Value.SeatLabel.Should().Be("B2");
    }
}
