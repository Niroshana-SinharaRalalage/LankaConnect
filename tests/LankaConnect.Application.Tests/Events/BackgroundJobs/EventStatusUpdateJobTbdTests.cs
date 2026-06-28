using FluentAssertions;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Events.BackgroundJobs;
using LankaConnect.Domain.Common;
using LankaConnect.Products.LankaEvents.Domain;
using LankaConnect.Modules.Identity.Domain.DomainEvents;
using LankaConnect.Products.LankaEvents.Domain.Enums;
using LankaConnect.Products.LankaEvents.Domain.ValueObjects;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace LankaConnect.Application.Tests.Events.BackgroundJobs;

/// <summary>
/// Phase 8YA.2 — TBD-aware filtering in <see cref="EventStatusUpdateJob"/>.
///
/// Q1=A allows TBD events to be Published. The status job auto-transitions
/// Published → Active when StartDate ≤ now and Active → Completed when
/// EndDate &lt; now. TBD events have null dates and must NOT be transitioned
/// — they sit in Published until SetDates fills in the dates.
///
/// These tests pin the filter so a future refactor can't silently transition
/// a Published-TBD event to Active by treating null dates as "long-past".
/// </summary>
public class EventStatusUpdateJobTbdTests
{
    private readonly Mock<IEventRepository> _eventRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<ILogger<EventStatusUpdateJob>> _logger = new();

    private EventStatusUpdateJob CreateJob() =>
        new(_eventRepository.Object, _unitOfWork.Object, _logger.Object);

    private static Event CreatePublishedTbdEvent()
    {
        var ev = Event.Create(
            EventTitle.Create("TBD published event").Value,
            EventDescription.Create("Will be scheduled later").Value,
            startDate: null,
            endDate: null,
            organizerId: Guid.NewGuid(),
            capacity: 100).Value;
        ev.Publish();
        return ev;
    }

    private static Event CreatePublishedDatedEvent(DateTime start, DateTime end)
    {
        // Domain.Create rejects past dates; build with future dates then rewrite via
        // reflection to simulate "event has already started" — exactly what the job
        // tests for. Reflection only used in tests; production code goes through
        // SetDates which validates.
        var ev = Event.Create(
            EventTitle.Create("Dated published event").Value,
            EventDescription.Create("Has real dates").Value,
            DateTime.UtcNow.AddDays(7), DateTime.UtcNow.AddDays(8),
            organizerId: Guid.NewGuid(),
            capacity: 100).Value;
        ev.Publish();

        var startProp = typeof(Event).GetProperty(nameof(Event.StartDate));
        var endProp = typeof(Event).GetProperty(nameof(Event.EndDate));
        startProp!.SetValue(ev, (DateTime?)DateTime.SpecifyKind(start, DateTimeKind.Utc));
        endProp!.SetValue(ev, (DateTime?)DateTime.SpecifyKind(end, DateTimeKind.Utc));
        return ev;
    }

    [Fact]
    public async Task ExecuteAsync_PublishedTbdEvent_NotTransitionedToActive()
    {
        // Arrange
        var tbdEvent = CreatePublishedTbdEvent();
        _eventRepository
            .Setup(r => r.GetEventsByStatusAsync(EventStatus.Published, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { tbdEvent });
        _eventRepository
            .Setup(r => r.GetEventsByStatusAsync(EventStatus.Active, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<Event>());

        // Act
        await CreateJob().ExecuteAsync();

        // Assert: TBD event is left in Published. The job should NOT have called
        // ActivateEvent on it (which would have failed the no-StartDate guard
        // anyway, but we want the filter upstream to prevent the noisy WARN log).
        tbdEvent.Status.Should().Be(EventStatus.Published);
        _unitOfWork.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_PublishedDatedEventStarted_TransitionsToActive()
    {
        // Arrange — control event with real dates, start time in the past
        var datedEvent = CreatePublishedDatedEvent(
            DateTime.UtcNow.AddHours(-1), DateTime.UtcNow.AddHours(2));
        _eventRepository
            .Setup(r => r.GetEventsByStatusAsync(EventStatus.Published, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { datedEvent });
        _eventRepository
            .Setup(r => r.GetEventsByStatusAsync(EventStatus.Active, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<Event>());

        // Act
        await CreateJob().ExecuteAsync();

        // Assert: dated event transitions normally — proves the filter doesn't
        // kill the happy path.
        datedEvent.Status.Should().Be(EventStatus.Active);
        _unitOfWork.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_TbdAndDatedMixedBatch_OnlyDatedTransitions()
    {
        // Arrange — both events Published. Only the dated one should transition.
        var tbdEvent = CreatePublishedTbdEvent();
        var datedEvent = CreatePublishedDatedEvent(
            DateTime.UtcNow.AddHours(-1), DateTime.UtcNow.AddHours(2));
        _eventRepository
            .Setup(r => r.GetEventsByStatusAsync(EventStatus.Published, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { tbdEvent, datedEvent });
        _eventRepository
            .Setup(r => r.GetEventsByStatusAsync(EventStatus.Active, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<Event>());

        // Act
        await CreateJob().ExecuteAsync();

        // Assert
        tbdEvent.Status.Should().Be(EventStatus.Published);
        datedEvent.Status.Should().Be(EventStatus.Active);
    }
}
