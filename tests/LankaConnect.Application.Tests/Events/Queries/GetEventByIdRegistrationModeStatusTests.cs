using LankaConnect.Modules.Identity.Contracts; // W4.6.a: ICurrentUserService moved here
using AutoMapper;
using LankaConnect.Modules.Communications.Contracts;
using FluentAssertions;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Common.Mappings;
using LankaConnect.Application.Events.Queries.GetEventById;
using LankaConnect.Domain.Communications;
using LankaConnect.Domain.Communications.Entities;
using LankaConnect.Domain.Events;
using LankaConnect.Domain.Events.Enums;
using LankaConnect.Domain.Events.ValueObjects;
using LankaConnect.Domain.Shared.Enums;
using LankaConnect.Domain.Shared.ValueObjects;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace LankaConnect.Application.Tests.Events.Queries;

/// <summary>
/// Phase 7E paid-B-mode gate (review iteration 1, edit #5) — handler-level integration test.
///
/// Architect-required: the mapper unit tests
/// (<c>EventMappingProfilePaidBModeGateTests</c>) only exercise the mapping rule in
/// isolation. Mapper tests pass while DI / AutoMapper-profile-registration is broken — the
/// handler builds an <see cref="EventDto"/> via <see cref="IMapper.Map{T}"/> AND then
/// re-emits a <c>with</c>-expression that could silently overwrite the new field if a future
/// edit isn't careful. This test wires the real <see cref="EventMappingProfile"/> through a
/// real <see cref="MapperConfiguration"/> and asserts that <see cref="LankaConnect.Application.Events.Common.EventDto.RegistrationModeStatus"/>
/// round-trips end-to-end through <see cref="GetEventByIdQueryHandler"/>.
/// </summary>
public class GetEventByIdRegistrationModeStatusTests
{
    private readonly Mock<IEventRepository> _eventRepository = new();
    private readonly Mock<IRegistrationRepository> _registrationRepository = new();
    private readonly Mock<IEmailGroupQueries> _emailGroupRepository = new();
    private readonly Mock<ICurrentUserService> _currentUserService = new();
    private readonly Mock<ILogger<GetEventByIdQueryHandler>> _logger = new();
    private readonly IMapper _mapper;

    public GetEventByIdRegistrationModeStatusTests()
    {
        var config = new MapperConfiguration(cfg => cfg.AddProfile<EventMappingProfile>());
        _mapper = config.CreateMapper();

        // Anonymous reads — IsAuthenticated=false avoids the user-registration lookup branch.
        _currentUserService.SetupGet(c => c.IsAuthenticated).Returns(false);
        _currentUserService.SetupGet(c => c.UserId).Returns(Guid.Empty);

        // Wave 5.4.d.1: swapped to IEmailGroupQueries; method shape now uses
        // IReadOnlyList<Guid> in + IReadOnlyList<EmailGroupSummaryDto> out.
        _emailGroupRepository
            .Setup(r => r.GetByIdsAsync(It.IsAny<IReadOnlyList<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<EmailGroupSummaryDto>)new List<EmailGroupSummaryDto>());
    }

    private static Event CreateFreeEvent()
    {
        var title = EventTitle.Create("Handler integration test").Value;
        var description = EventDescription.Create("Phase 7E paid-B-mode gate handler test").Value;
        var @event = Event.Create(title, description, DateTime.UtcNow.AddDays(7), DateTime.UtcNow.AddDays(8), Guid.NewGuid(), 100).Value;
        @event.SetAsFreeEvent();
        return @event;
    }

    private static Event CreatePaidEvent()
    {
        var title = EventTitle.Create("Handler integration test").Value;
        var description = EventDescription.Create("Phase 7E paid-B-mode gate handler test").Value;
        var @event = Event.Create(title, description, DateTime.UtcNow.AddDays(7), DateTime.UtcNow.AddDays(8), Guid.NewGuid(), 100).Value;
        @event.SetPricing(Money.Create(50m, Currency.USD).Value);
        return @event;
    }

    private GetEventByIdQueryHandler BuildHandler() => new(
        _eventRepository.Object,
        _registrationRepository.Object,
        _emailGroupRepository.Object,
        _currentUserService.Object,
        _mapper,
        _logger.Object);

    [Fact]
    public async Task Handler_PopulatesRegistrationModeStatus_Active_ForFreeBModeEvent()
    {
        var @event = CreateFreeEvent();
        @event.SetRegistrationMode(RegistrationMode.HeadCountByAge);
        _eventRepository
            .Setup(r => r.GetByIdAsync(@event.Id, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(@event);

        var result = await BuildHandler().Handle(new GetEventByIdQuery(@event.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.RegistrationModeStatus.Should().Be("active",
            "free + HeadCountByAge passes compatibility — handler must propagate the mapper's value");
    }

    [Fact]
    public async Task Handler_PopulatesRegistrationModeStatus_Active_ForPaidSinglePriceBModeEvent()
    {
        // Phase 7E.3b shipped paid B-mode + Stripe checkout. Paid single-price + HeadCountByAge
        // is now "active" through the handler — architect-required integration check that the
        // gate-removal cascades correctly through DI + AutoMapper profile + handler with-expression.
        var @event = CreatePaidEvent();
        @event.SetRegistrationMode(RegistrationMode.HeadCountByAge);
        _eventRepository
            .Setup(r => r.GetByIdAsync(@event.Id, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(@event);

        var result = await BuildHandler().Handle(new GetEventByIdQuery(@event.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.RegistrationModeStatus.Should().Be("active",
            "Phase 7E.3b lifted the PaidHeadCountDeferred gate — paid + HeadCountByAge now passes " +
            "compatibility through the full handler pipeline");
    }

    [Fact]
    public async Task Handler_PopulatesRegistrationModeStatus_Active_ForLegacyDetailedAttendeesEvent()
    {
        // Mode A is always active regardless of paid/free — the legacy default path must keep working.
        var @event = CreatePaidEvent();
        _eventRepository
            .Setup(r => r.GetByIdAsync(@event.Id, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(@event);

        var result = await BuildHandler().Handle(new GetEventByIdQuery(@event.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.RegistrationModeStatus.Should().Be("active");
    }
}
