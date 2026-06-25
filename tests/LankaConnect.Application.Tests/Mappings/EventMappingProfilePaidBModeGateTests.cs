using AutoMapper;
using FluentAssertions;
using LankaConnect.Application.Common.Mappings;
using LankaConnect.Application.Events.Common;
using LankaConnect.Domain.Events;
using LankaConnect.Domain.Users.DomainEvents; // W4.7.a: user-aggregate events moved here
using LankaConnect.Domain.Events.Enums;
using LankaConnect.Domain.Events.ValueObjects;
using LankaConnect.Domain.Shared.Enums;
using LankaConnect.Domain.Shared.ValueObjects;
using Xunit;

namespace LankaConnect.Application.Tests.Mappings;

/// <summary>
/// Phase 7E paid-B-mode gate (review iteration 1, 2026-04-28) — verifies that
/// <see cref="EventMappingProfile"/> populates <see cref="EventDto.RegistrationModeStatus"/>
/// correctly:
/// <list type="bullet">
/// <item>"deferred" when the configured mode + shape currently fails compatibility (paid + B)</item>
/// <item>"active" when compatibility passes (free + B; any Mode A; any Mode C valid shape)</item>
/// </list>
///
/// Architect-required (edit #5): the unit tests here cover the mapper helper directly. A
/// separate handler-level integration test
/// (<c>GetEventByIdQueryHandler_PopulatesRegistrationModeStatus_EndToEnd</c>) catches DI /
/// AutoMapper-profile-registration breaks that the mapper-only unit can miss.
/// </summary>
public class EventMappingProfilePaidBModeGateTests
{
    private readonly IMapper _mapper;

    public EventMappingProfilePaidBModeGateTests()
    {
        var configuration = new MapperConfiguration(cfg => cfg.AddProfile<EventMappingProfile>());
        _mapper = configuration.CreateMapper();
    }

    private static Event CreateFreeEvent(int capacity = 100)
    {
        var title = EventTitle.Create("Mapper test event").Value;
        var description = EventDescription.Create("Phase 7E paid-B-mode gate mapper").Value;
        var start = DateTime.UtcNow.AddDays(7);
        var end = DateTime.UtcNow.AddDays(8);
        var @event = Event.Create(title, description, start, end, Guid.NewGuid(), capacity).Value;
        // Event.Create defaults IsFreeEvent = false; tests that want free must opt in.
        @event.SetAsFreeEvent().IsSuccess.Should().BeTrue();
        return @event;
    }

    private static Event CreatePaidEvent(decimal amount = 50m, int capacity = 100)
    {
        var title = EventTitle.Create("Mapper test event").Value;
        var description = EventDescription.Create("Phase 7E paid-B-mode gate mapper").Value;
        var start = DateTime.UtcNow.AddDays(7);
        var end = DateTime.UtcNow.AddDays(8);
        var @event = Event.Create(title, description, start, end, Guid.NewGuid(), capacity).Value;
        @event.SetPricing(Money.Create(amount, Currency.USD).Value).IsSuccess.Should().BeTrue();
        return @event;
    }

    [Fact]
    public void Mapper_Returns_Active_ForPaidSinglePriceEvent_InHeadCountByAge()
    {
        // Phase 7E.3b shipped paid B-mode. Paid + HeadCountByAge (single-price) is now "active".
        var @event = CreatePaidEvent();
        @event.SetRegistrationMode(RegistrationMode.HeadCountByAge).IsSuccess.Should().BeTrue();

        var dto = _mapper.Map<EventDto>(@event);

        dto.RegistrationModeStatus.Should().Be("active",
            "Phase 7E.3b shipped paid B-mode — paid + HeadCountByAge (single-price) passes compatibility");
    }

    [Theory]
    [InlineData(RegistrationMode.HeadCountOnly)]
    [InlineData(RegistrationMode.HeadCountByAge)]
    [InlineData(RegistrationMode.HeadCountByGender)]
    [InlineData(RegistrationMode.HeadCountByAgeAndGender)]
    public void Mapper_Returns_Active_ForPaidSinglePriceEvent_InAnyBMode(RegistrationMode bMode)
    {
        // Phase 7E.3b: all four B-modes now compatible with paid single-price events.
        var @event = CreatePaidEvent(25m);
        @event.SetRegistrationMode(bMode).IsSuccess.Should().BeTrue();

        var dto = _mapper.Map<EventDto>(@event);

        dto.RegistrationModeStatus.Should().Be("active", $"paid + {bMode} (single-price) ships in 7E.3b");
    }

    [Theory]
    [InlineData(RegistrationMode.HeadCountOnly)]
    [InlineData(RegistrationMode.HeadCountByAge)]
    [InlineData(RegistrationMode.HeadCountByGender)]
    [InlineData(RegistrationMode.HeadCountByAgeAndGender)]
    public void Mapper_Returns_Active_ForFreeEvent_InAnyBMode(RegistrationMode bMode)
    {
        var @event = CreateFreeEvent();
        @event.SetRegistrationMode(bMode).IsSuccess.Should().BeTrue();

        var dto = _mapper.Map<EventDto>(@event);

        dto.RegistrationModeStatus.Should().Be("active", $"free + {bMode} is implemented today (slice 7E.3a)");
    }

    [Fact]
    public void Mapper_Returns_Active_ForLegacy_DetailedAttendees_Event()
    {
        var @event = CreatePaidEvent(30m);
        // Paid + Mode A is the most common pre-7E shape — must remain "active" so the
        // existing per-attendee form keeps rendering.
        @event.RegistrationMode.Should().Be(RegistrationMode.DetailedAttendees,
            "default mode is DetailedAttendees; sanity check");

        var dto = _mapper.Map<EventDto>(@event);

        dto.RegistrationModeStatus.Should().Be("active",
            "paid + DetailedAttendees is the legacy default and must always render the form");
    }

    [Fact]
    public void Mapper_Returns_Active_ForFreeEvent_InNoRegistrationMode()
    {
        var @event = CreateFreeEvent();
        @event.SetRegistrationMode(RegistrationMode.NoRegistration).IsSuccess.Should().BeTrue();

        var dto = _mapper.Map<EventDto>(@event);

        dto.RegistrationModeStatus.Should().Be("active",
            "free + NoRegistration is a valid Mode C event — no compatibility gate applies");
    }
}
