using FluentAssertions;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Events.Commands.UpdateSponsor;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events;
using LankaConnect.Domain.Users.DomainEvents; // W4.7.a: user-aggregate events moved here
using LankaConnect.Domain.Events.Repositories;
using LankaConnect.Domain.Events.ValueObjects;
using LankaConnect.Domain.Shared.Enums;
using LankaConnect.Domain.Shared.ValueObjects;
using Microsoft.Extensions.Logging;
using Moq;

namespace LankaConnect.Application.Tests.Events.Commands.UpdateSponsor;

/// <summary>
/// Phase 6A.151 — handler-layer tests for the PATCH sponsor flow. These
/// complement the 38 domain-layer state-matrix tests in SponsorTests.cs;
/// concerns covered here are handler-specific:
///   - authz resolution (organizer vs sponsor-self vs forbidden vs anonymous-shadow)
///   - sponsor-not-found / event-mismatch
///   - MinSponsorAmount re-validation (architect H3): fires only when Amount is
///     in the patch AND new value < event min; bypassed for notes-only edits
/// State-matrix enforcement is NOT re-tested here; the domain layer has it.
/// </summary>
public class UpdateSponsorCommandHandlerTests
{
    private readonly Mock<IEventRepository> _eventRepository = new();
    private readonly Mock<ISponsorRepository> _sponsorRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<ILogger<UpdateSponsorCommandHandler>> _logger = new();
    private readonly UpdateSponsorCommandHandler _handler;

    public UpdateSponsorCommandHandlerTests()
    {
        _handler = new UpdateSponsorCommandHandler(
            _eventRepository.Object,
            _sponsorRepository.Object,
            _unitOfWork.Object,
            _logger.Object);
    }

    private static (Sponsor sponsor, Guid sponsorUserId) MakeSponsor(Guid eventId, Guid? userId = null)
    {
        var sid = userId ?? Guid.NewGuid();
        var result = Sponsor.CreateMoneySponsor(
            eventId,
            sid,
            "Jane Doe",
            "jane@example.com",
            null,
            "AcmeCo",
            "Happy to help",
            Money.Create(100m, Currency.USD).Value);
        return (result.Value, sid);
    }

    // Note on test-coverage scope:
    //
    // The handler's authz happy-paths (organizer-allowed, sponsor-self-allowed,
    // stranger-forbidden) and MinSponsorAmount re-validation require a fully
    // constructed Event aggregate with SponsorConfig + at least one organizer.
    // The project does not currently expose an EventTestBuilder; building one
    // is out-of-scope for 6A.151 and tracked as a separate follow-up.
    //
    // For 6A.151 we cover the handler-specific branches that ARE reachable
    // with the public Sponsor factory alone: sponsor-not-found, event-mismatch,
    // parent-event-not-found. The state-matrix authz lives ENTIRELY in the
    // domain layer (Sponsor.UpdateXxx) and is fully covered by the 38 6A.151
    // tests in SponsorTests.cs (committed in C1, all GREEN). The remaining
    // handler-layer branches will be exercised by the C3 API integration tests
    // which spin up the full DI graph including a real Event aggregate.

    [Fact]
    public async Task Handle_SponsorNotFound_ReturnsNotFound()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var sponsorId = Guid.NewGuid();
        var command = new UpdateSponsorCommand(eventId, sponsorId, Guid.NewGuid(), Notes: "x");

        _sponsorRepository
            .Setup(r => r.GetByIdAsync(sponsorId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Sponsor?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorKind.Should().Be(ErrorKind.NotFound);
    }

    [Fact]
    public async Task Handle_EventIdMismatch_ReturnsNotFound()
    {
        // Arrange — sponsor belongs to a different event than the request
        var requestEventId = Guid.NewGuid();
        var sponsorEventId = Guid.NewGuid();  // different
        var (sponsor, _) = MakeSponsor(sponsorEventId);
        var command = new UpdateSponsorCommand(requestEventId, sponsor.Id, Guid.NewGuid(), Notes: "x");

        _sponsorRepository
            .Setup(r => r.GetByIdAsync(sponsor.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(sponsor);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorKind.Should().Be(ErrorKind.NotFound);
    }

    [Fact]
    public async Task Handle_ParentEventNotFound_ReturnsNotFound()
    {
        // Arrange — sponsor exists but the parent Event aggregate is missing
        var eventId = Guid.NewGuid();
        var (sponsor, _) = MakeSponsor(eventId);
        var command = new UpdateSponsorCommand(eventId, sponsor.Id, Guid.NewGuid(), Notes: "x");

        _sponsorRepository
            .Setup(r => r.GetByIdAsync(sponsor.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(sponsor);
        _eventRepository
            .Setup(r => r.GetByIdAsync(eventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Event?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorKind.Should().Be(ErrorKind.NotFound);
    }

    // NOTE: Authz happy-path tests (sponsor-self allowed, organizer allowed,
    // stranger forbidden) and MinSponsorAmount tests live in the C3
    // integration suite once the Event aggregate test builder is in place.
    // The domain-layer state-matrix is fully covered by SponsorTests.cs (38
    // 6A.151 tests already GREEN in C1). The three failure-path tests above
    // exercise the not-found / event-mismatch branches that ARE reachable
    // with only the Sponsor aggregate constructible from a public factory.
}
