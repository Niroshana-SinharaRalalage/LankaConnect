using FluentAssertions;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Products.LankaEvents.Application.Queries.GetEventIcs;
using LankaConnect.Products.LankaEvents.Domain;
using LankaConnect.Modules.Identity.Domain.DomainEvents;
using LankaConnect.Products.LankaEvents.Domain.ValueObjects;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace LankaConnect.Application.Tests.Events.Queries;

/// <summary>
/// Phase 8YA.2 — TBD-aware ICS export.
///
/// An iCalendar feed must have DTSTART + DTEND. There is no representation
/// of "Date TBD" in the .ics format itself, so the only correct response
/// for a TBD event is a Result.Failure surfaced by the controller as 422
/// Unprocessable Entity (architect-locked: "Return 422 / 404 if dates are TBD").
/// </summary>
public class GetEventIcsQueryHandlerTbdTests
{
    private readonly Mock<IEventRepository> _eventRepository = new();
    private readonly Mock<ILogger<GetEventIcsQueryHandler>> _logger = new();

    private GetEventIcsQueryHandler CreateHandler() =>
        new(_eventRepository.Object, _logger.Object);

    private static Event CreatePublishedTbdEvent()
    {
        var ev = Event.Create(
            EventTitle.Create("TBD event for ICS").Value,
            EventDescription.Create("Date to be confirmed").Value,
            startDate: null,
            endDate: null,
            organizerId: Guid.NewGuid(),
            capacity: 50).Value;
        ev.Publish();
        return ev;
    }

    [Fact]
    public async Task Handle_TbdEvent_ReturnsFailureWithDateTbdMessage()
    {
        var tbdEvent = CreatePublishedTbdEvent();
        _eventRepository
            .Setup(r => r.GetByIdAsync(tbdEvent.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tbdEvent);

        var result = await CreateHandler().Handle(
            new GetEventIcsQuery(tbdEvent.Id), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.ToLower().Should().Contain("date",
            because: "the failure message should explain dates aren't confirmed");
    }

    [Fact]
    public async Task Handle_DatedEvent_ReturnsSuccessWithIcsContent()
    {
        // Control case — proves the failure path doesn't accidentally trip on dated events.
        var datedEvent = Event.Create(
            EventTitle.Create("Dated event").Value,
            EventDescription.Create("Has dates").Value,
            DateTime.UtcNow.AddDays(7),
            DateTime.UtcNow.AddDays(8),
            Guid.NewGuid(),
            capacity: 50).Value;
        datedEvent.Publish();

        _eventRepository
            .Setup(r => r.GetByIdAsync(datedEvent.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(datedEvent);

        var result = await CreateHandler().Handle(
            new GetEventIcsQuery(datedEvent.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue($"Expected success but got: {result.Error}");
        result.Value.Should().Contain("DTSTART:");
        result.Value.Should().Contain("DTEND:");
    }
}
