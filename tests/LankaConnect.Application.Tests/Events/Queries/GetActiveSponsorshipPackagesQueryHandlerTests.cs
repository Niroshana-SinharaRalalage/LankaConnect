using System.Runtime.CompilerServices;
using FluentAssertions;
using LankaConnect.Application.Events.Queries.GetActiveSponsorshipPackages;
using LankaConnect.Domain.Common;
using LankaConnect.Products.LankaEvents.Domain;
using LankaConnect.Modules.Identity.Domain.DomainEvents;
using LankaConnect.Products.LankaEvents.Domain.Enums;
using LankaConnect.Products.LankaEvents.Domain.Repositories;
using LankaConnect.Products.LankaEvents.Domain.ValueObjects;
using LankaConnect.Domain.Shared.Enums;
using LankaConnect.Domain.Shared.ValueObjects;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace LankaConnect.Application.Tests.Events.Queries;

/// <summary>
/// Phase 6A.157 — public query coverage. Architect-locked: any gate failure
/// returns empty list (never errors) so the FE silently hides the section.
/// </summary>
public class GetActiveSponsorshipPackagesQueryHandlerTests
{
    private readonly Mock<IEventRepository> _eventRepo = new();
    private readonly Mock<ISponsorshipPackageRepository> _packageRepo = new();
    private readonly GetActiveSponsorshipPackagesQueryHandler _sut;
    private static readonly Guid EventId = Guid.NewGuid();

    public GetActiveSponsorshipPackagesQueryHandlerTests()
    {
        _sut = new GetActiveSponsorshipPackagesQueryHandler(
            _eventRepo.Object, _packageRepo.Object,
            Mock.Of<ILogger<GetActiveSponsorshipPackagesQueryHandler>>());
    }

    private static Event MakeEvent(EventStatus status, bool sponsorsEnabled, bool packagesEnabled)
    {
        // Bypass the heavy Event constructor — we only need a few properties read
        // by the query handler (Status, SponsorConfig). RuntimeHelpers is the
        // non-obsolete replacement for FormatterServices.GetUninitializedObject.
        var @event = (Event)RuntimeHelpers.GetUninitializedObject(typeof(Event));
        typeof(LegacyBaseEntity).GetProperty("Id")!.SetValue(@event, EventId);
        typeof(Event).GetProperty("Status")!.SetValue(@event, status);
        var sponsorConfig = sponsorsEnabled
            ? SponsorConfiguration.Create(true, true, true, null, null, true, packagesEnabled).Value
            : SponsorConfiguration.Disabled();
        typeof(Event).GetProperty("SponsorConfig")!.SetValue(@event, sponsorConfig);
        return @event;
    }

    [Fact]
    public async Task Handle_EventNotFound_ReturnsEmpty()
    {
        _eventRepo.Setup(r => r.GetByIdAsync(EventId, It.IsAny<CancellationToken>())).ReturnsAsync((Event?)null);

        var result = await _sut.Handle(new GetActiveSponsorshipPackagesQuery(EventId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_EventDraft_ReturnsEmpty()
    {
        _eventRepo.Setup(r => r.GetByIdAsync(EventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(MakeEvent(EventStatus.Draft, true, true));

        var result = await _sut.Handle(new GetActiveSponsorshipPackagesQuery(EventId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
        _packageRepo.Verify(r => r.GetActiveByEventIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_SponsorsDisabled_ReturnsEmpty()
    {
        _eventRepo.Setup(r => r.GetByIdAsync(EventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(MakeEvent(EventStatus.Published, sponsorsEnabled: false, packagesEnabled: true));

        var result = await _sut.Handle(new GetActiveSponsorshipPackagesQuery(EventId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_PackagesDisabled_ReturnsEmpty()
    {
        _eventRepo.Setup(r => r.GetByIdAsync(EventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(MakeEvent(EventStatus.Published, sponsorsEnabled: true, packagesEnabled: false));

        var result = await _sut.Handle(new GetActiveSponsorshipPackagesQuery(EventId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_AllGatesOpen_ReturnsMappedDtos()
    {
        _eventRepo.Setup(r => r.GetByIdAsync(EventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(MakeEvent(EventStatus.Published, true, true));
        var pkg = SponsorshipPackage.Create(
            EventId, "Gold", "desc", Money.Create(500m, Currency.USD).Value,
            quantityLimit: 10, sortOrder: 1, tier: "Gold",
            perks: new List<string> { "Perk A" }, includedTicketCount: 3).Value;
        _packageRepo.Setup(r => r.GetActiveByEventIdAsync(EventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SponsorshipPackage> { pkg });

        var result = await _sut.Handle(new GetActiveSponsorshipPackagesQuery(EventId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1);
        var dto = result.Value[0];
        dto.Name.Should().Be("Gold");
        dto.Tier.Should().Be("Gold");
        dto.PriceAmount.Should().Be(500m);
        dto.IncludedTicketCount.Should().Be(3);
        dto.RemainingStock.Should().Be(10);
        dto.IsSoldOut.Should().BeFalse();
    }
}
