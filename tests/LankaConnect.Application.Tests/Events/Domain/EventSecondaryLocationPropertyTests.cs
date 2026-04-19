using FluentAssertions;
using LankaConnect.Domain.Business.ValueObjects;
using LankaConnect.Domain.Events;
using LankaConnect.Domain.Events.DomainEvents;
using LankaConnect.Domain.Events.Enums;
using LankaConnect.Domain.Events.ValueObjects;
using Xunit;

namespace LankaConnect.Application.Tests.Events.Domain;

/// <summary>
/// Unit tests for Event.SecondaryLocation + SetSecondaryLocation / ClearSecondaryLocation.
/// Phase 7C.1: Location Name + Secondary Location feature.
/// </summary>
public class EventSecondaryLocationPropertyTests
{
    private static Event CreateValidEvent()
    {
        var title = EventTitle.Create("Test Event").Value;
        var description = EventDescription.Create("Test Description").Value;
        var startDate = DateTime.UtcNow.AddDays(7);
        var endDate = startDate.AddHours(2);
        var organizerId = Guid.NewGuid();
        return Event.Create(title, description, startDate, endDate, organizerId, 100).Value;
    }

    private static EventSecondaryLocation CreateValidSecondary(
        SecondaryLocationType type = SecondaryLocationType.ParkingLot,
        string name = "North Lot")
    {
        var address = Address.Create("500 Side St", "Los Angeles", "CA", "90001", "USA").Value;
        var location = EventLocation.Create(address, name: name).Value;
        return EventSecondaryLocation.Create(type, location).Value;
    }

    [Fact]
    public void Event_NewlyCreated_SecondaryLocationIsNull()
    {
        var @event = CreateValidEvent();

        @event.SecondaryLocation.Should().BeNull();
        @event.HasSecondaryLocation().Should().BeFalse();
    }

    [Fact]
    public void SetSecondaryLocation_WithValidVO_SetsProperty()
    {
        var @event = CreateValidEvent();
        var secondary = CreateValidSecondary();

        var result = @event.SetSecondaryLocation(secondary);

        result.IsSuccess.Should().BeTrue();
        @event.SecondaryLocation.Should().Be(secondary);
        @event.HasSecondaryLocation().Should().BeTrue();
    }

    [Fact]
    public void SetSecondaryLocation_WithNull_Fails()
    {
        var @event = CreateValidEvent();

        var result = @event.SetSecondaryLocation(null!);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Contains("Secondary location"));
        @event.SecondaryLocation.Should().BeNull();
    }

    [Fact]
    public void SetSecondaryLocation_Replaces_ExistingSecondary()
    {
        var @event = CreateValidEvent();
        var first = CreateValidSecondary(SecondaryLocationType.ParkingLot, "Lot A");
        @event.SetSecondaryLocation(first);

        var second = CreateValidSecondary(SecondaryLocationType.SecondaryVenue, "Annex");
        var result = @event.SetSecondaryLocation(second);

        result.IsSuccess.Should().BeTrue();
        @event.SecondaryLocation.Should().Be(second);
        @event.SecondaryLocation!.Type.Should().Be(SecondaryLocationType.SecondaryVenue);
    }

    [Fact]
    public void ClearSecondaryLocation_WhenSet_RemovesProperty()
    {
        var @event = CreateValidEvent();
        @event.SetSecondaryLocation(CreateValidSecondary());

        var result = @event.ClearSecondaryLocation();

        result.IsSuccess.Should().BeTrue();
        @event.SecondaryLocation.Should().BeNull();
        @event.HasSecondaryLocation().Should().BeFalse();
    }

    [Fact]
    public void ClearSecondaryLocation_WhenAlreadyNull_Succeeds_Idempotent()
    {
        var @event = CreateValidEvent();

        var result = @event.ClearSecondaryLocation();

        result.IsSuccess.Should().BeTrue("clearing an already-absent secondary location should be a no-op");
        @event.SecondaryLocation.Should().BeNull();
    }
}
