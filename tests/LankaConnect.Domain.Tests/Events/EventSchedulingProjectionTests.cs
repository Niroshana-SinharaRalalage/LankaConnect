using LankaConnect.Products.LankaEvents.Domain;
using LankaConnect.Products.LankaEvents.Domain.ValueObjects;
using LankaConnect.Products.LankaEvents.Domain.Enums;

namespace LankaConnect.Domain.Tests.Events;

/// <summary>
/// Wave 4.8.b (2026-06-26): pinning tests for Event.cs composing the Scheduling.Domain
/// reusable primitives (ScheduledOccurrence + CapacityRule) as read-only projections.
/// Storage stays on the inline fields; these tests verify the projections stay in sync.
/// </summary>
public class EventSchedulingProjectionTests
{
    private static Event MakeEvent(DateTime? start, DateTime? end, int capacity = 100, string? tz = "America/New_York")
    {
        var result = Event.Create(
            title: EventTitle.Create("Test Event").Value,
            description: EventDescription.Create("Test event description for scheduling projection tests").Value,
            startDate: start ?? DateTime.UtcNow.AddDays(7),
            endDate: end ?? DateTime.UtcNow.AddDays(7).AddHours(2),
            organizerId: Guid.NewGuid(),
            capacity: capacity,
            location: null,
            category: EventCategory.Cultural);
        return result.Value;
    }

    [Fact]
    public void Occurrence_ProjectsInlineFields()
    {
        var start = DateTime.UtcNow.AddDays(7);
        var end = start.AddHours(2);
        var evt = MakeEvent(start, end);

        evt.Occurrence.StartDate.Should().Be(start);
        evt.Occurrence.EndDate.Should().Be(end);
        evt.Occurrence.TimeZoneId.Should().BeNull(); // Event.Create does not set TimeZoneId by default
        evt.Occurrence.HasCommittedSchedule.Should().BeTrue();
    }

    [Fact]
    public void CapacityRule_ProjectsCapacityInt()
    {
        var evt = MakeEvent(null, null, capacity: 250);

        evt.CapacityRule.Should().NotBeNull();
        evt.CapacityRule!.Total.Should().Be(250);
        evt.CapacityRule.HasRoomFor(currentlyReserved: 200, additional: 50).Should().BeTrue();
        evt.CapacityRule.HasRoomFor(currentlyReserved: 250, additional: 1).Should().BeFalse();
    }

    [Fact]
    public void Occurrence_DerivesInvariantsFromInlineFields()
    {
        // Reflects the existing Event factory invariant — Event.Create rejects EndDate<StartDate
        // BEFORE the Occurrence projection is computed, so any Event instance has a valid Occurrence.
        var now = DateTime.UtcNow;
        var evt = MakeEvent(now.AddHours(1), now.AddHours(3));

        evt.Occurrence.IsUpcoming(now).Should().BeTrue();
        evt.Occurrence.IsPast(now).Should().BeFalse();
    }
}
