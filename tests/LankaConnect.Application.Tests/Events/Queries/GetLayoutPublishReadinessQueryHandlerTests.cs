using FluentAssertions;
using LankaConnect.Application.Events.Queries.GetLayoutPublishReadiness;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events;
using LankaConnect.Modules.Identity.Domain.DomainEvents;
using LankaConnect.Domain.Events.Entities;
using LankaConnect.Domain.Events.Enums;
using LankaConnect.Domain.Events.Repositories;
using LankaConnect.Domain.Shared.Enums;
using LankaConnect.Domain.Shared.ValueObjects;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace LankaConnect.Application.Tests.Events.Queries;

/// <summary>
/// Slice S4 — handler-level tests for the publish-readiness query. Validates
/// repository wiring, template-vs-event-attached branching, and DTO projection.
/// Domain enumeration logic is covered separately in <see cref="LankaConnect.Domain.Tests"/>.
/// </summary>
public class GetLayoutPublishReadinessQueryHandlerTests
{
    private readonly Mock<IVenueLayoutRepository> _layoutRepo = new();
    private readonly Mock<IEventRepository> _eventRepo = new();
    private readonly GetLayoutPublishReadinessQueryHandler _sut;

    public GetLayoutPublishReadinessQueryHandlerTests()
    {
        _sut = new GetLayoutPublishReadinessQueryHandler(
            _layoutRepo.Object,
            _eventRepo.Object,
            Mock.Of<ILogger<GetLayoutPublishReadinessQueryHandler>>());
    }

    [Fact]
    public async Task Handle_Should_Return_NotFound_When_Layout_Missing()
    {
        var layoutId = Guid.NewGuid();
        _layoutRepo.Setup(r => r.GetWithZonesAndSeatsAsync(layoutId, It.IsAny<CancellationToken>()))
                   .ReturnsAsync((VenueLayout?)null);

        var result = await _sut.Handle(new GetLayoutPublishReadinessQuery(layoutId), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.ErrorKind.Should().Be(ErrorKind.NotFound);
    }

    [Fact]
    public async Task Handle_Should_Return_Empty_Report_For_Template_Layout()
    {
        var ownerId = Guid.NewGuid();
        var template = VenueLayout.Create(
            "My Template", LayoutType.Theater, ownerId,
            eventId: null, isTemplate: true).Value;
        var zone = template.AddZone("VIP", "#FF0000", 1).Value;
        template.GenerateTheaterSeats(zone.Id, rows: 2, seatsPerRow: 5);

        _layoutRepo.Setup(r => r.GetWithZonesAndSeatsAsync(template.Id, It.IsAny<CancellationToken>()))
                   .ReturnsAsync(template);

        var result = await _sut.Handle(new GetLayoutPublishReadinessQuery(template.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        // Template has no event tiers — no warnings/blockers about tiers.
        // Zone is unmapped + has seats, so the domain method correctly raises ZoneUnmapped.
        result.Value.Blockers.Should().Contain(b => b.Code == "ZoneUnmapped");
        result.Value.TierSummary.Should().BeEmpty();
        _eventRepo.Verify(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_Should_Load_Event_Tiers_For_Event_Attached_Layout_And_Project_Tier_Summary()
    {
        var organiserId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        var layout = VenueLayout.Create(
            "Event Layout", LayoutType.Theater, organiserId, eventId).Value;
        var zone = layout.AddZone("VIP", "#FF0000", 1).Value;
        layout.GenerateTheaterSeats(zone.Id, rows: 2, seatsPerRow: 5); // 10 seats

        var price = Money.Create(100m, Currency.USD).Value;
        var tier = TicketTier.Create(eventId, "VIP", "VIP section", price, null, null, 30, 10, 1).Value;
        tier.AssignToZone(zone.Id);

        var @event = CreateEventWithTier(eventId, organiserId, tier);

        _layoutRepo.Setup(r => r.GetWithZonesAndSeatsAsync(layout.Id, It.IsAny<CancellationToken>()))
                   .ReturnsAsync(layout);
        _eventRepo.Setup(r => r.GetByIdAsync(eventId, false, It.IsAny<CancellationToken>()))
                  .ReturnsAsync(@event);

        var result = await _sut.Handle(new GetLayoutPublishReadinessQuery(layout.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.IsPublishReady.Should().BeTrue();
        result.Value.TierSummary.Should().ContainSingle(s =>
            s.TierId == tier.Id
            && s.TierName == "VIP"
            && s.TierCapacity == 30
            && s.MappedZones.Count == 1
            && s.TotalEnabledSeats == 10);
    }

    [Fact]
    public async Task Handle_Should_Project_Issue_Codes_As_Strings()
    {
        var organiserId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        var layout = VenueLayout.Create("L", LayoutType.Theater, organiserId, eventId).Value;
        var zone = layout.AddZone("Unmapped", "#FF0000", 1).Value;
        layout.GenerateTheaterSeats(zone.Id, rows: 1, seatsPerRow: 3);

        var price = Money.Create(50m, Currency.USD).Value;
        var tier = TicketTier.Create(eventId, "Basic", "...", price, null, null, 50, 10, 1).Value;
        var @event = CreateEventWithTier(eventId, organiserId, tier);

        _layoutRepo.Setup(r => r.GetWithZonesAndSeatsAsync(layout.Id, It.IsAny<CancellationToken>()))
                   .ReturnsAsync(layout);
        _eventRepo.Setup(r => r.GetByIdAsync(eventId, false, It.IsAny<CancellationToken>()))
                  .ReturnsAsync(@event);

        var result = await _sut.Handle(new GetLayoutPublishReadinessQuery(layout.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Blockers.Should().Contain(b => b.Code == "ZoneUnmapped");
        result.Value.Warnings.Should().Contain(w => w.Code == "TierWithoutMapping");
    }

    private static Event CreateEventWithTier(Guid eventId, Guid organiserId, TicketTier tier)
    {
        var title = LankaConnect.Domain.Events.ValueObjects.EventTitle.Create("Test Event").Value;
        var description = LankaConnect.Domain.Events.ValueObjects.EventDescription
            .Create("Description").Value;
        var @event = Event.Create(
            title, description,
            startDate: DateTime.UtcNow.AddDays(7),
            endDate: DateTime.UtcNow.AddDays(7).AddHours(2),
            organizerId: organiserId,
            capacity: 500).Value;
        @event.SetTicketingMode(TicketingMode.Tiered);

        // Force the tier into the event via the private backing field — there's
        // no public AddTicketTier on Event today.
        var tiersField = typeof(Event).GetField("_ticketTiers",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("_ticketTiers field not found");
        var list = (List<TicketTier>)tiersField.GetValue(@event)!;
        list.Add(tier);

        // Force the Id for cross-aggregate linkage assertions.
        var idProp = typeof(LankaConnect.Domain.Common.LegacyBaseEntity).GetProperty("Id")!;
        idProp.SetValue(@event, eventId);
        return @event;
    }
}
