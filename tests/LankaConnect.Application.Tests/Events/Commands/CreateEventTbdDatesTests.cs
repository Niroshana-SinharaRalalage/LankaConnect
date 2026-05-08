using FluentAssertions;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Events.Commands.CreateEvent;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Communications;
using LankaConnect.Domain.Events;
using LankaConnect.Domain.Events.Enums;
using LankaConnect.Domain.Events.Services;
using LankaConnect.Domain.Users;
using LankaConnect.Domain.Users.Enums;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using Email = LankaConnect.Domain.Shared.ValueObjects.Email;

namespace LankaConnect.Application.Tests.Events.Commands;

/// <summary>
/// Phase 8YA.2 — TDD coverage for CreateEventCommand handling of TBD date pair.
///
/// Architect verdict locked 2026-05-08 (Q1=A, Q2=A, Q3=A, Q4=A):
/// - Both dates null → Status starts at Planning (TBD event).
/// - Both dates set → Status starts at Draft (existing behavior).
/// - Mixed (one null, one set) → command rejected by validator (and domain Create
///   would fail anyway as a defence-in-depth).
///
/// These tests pin the contract so future refactors of the command surface
/// can't silently regress to "TBD events impossible" — that bug class was the
/// entire reason for Phase 8YA.
/// </summary>
public class CreateEventTbdDatesTests
{
    private readonly Mock<IEventRepository> _mockEventRepository = new();
    private readonly Mock<IUserRepository> _mockUserRepository = new();
    private readonly Mock<IUnitOfWork> _mockUnitOfWork = new();
    private readonly Mock<IEmailGroupRepository> _mockEmailGroupRepository = new();
    private readonly Mock<IApplicationDbContext> _mockDbContext = new();
    private readonly Mock<IRevenueCalculatorService> _mockRevenueCalculatorService = new();
    private readonly Mock<ITimeZoneLookupService> _mockTimeZoneLookupService = new();
    private readonly Mock<ILogger<CreateEventCommandHandler>> _mockLogger = new();

    private CreateEventCommandHandler CreateHandler() => new(
        _mockEventRepository.Object,
        _mockUserRepository.Object,
        _mockUnitOfWork.Object,
        _mockEmailGroupRepository.Object,
        _mockDbContext.Object,
        _mockRevenueCalculatorService.Object,
        _mockTimeZoneLookupService.Object,
        _mockLogger.Object);

    private User CreateOrganizer(Guid userId)
    {
        var email = Email.Create($"organizer-{userId}@test.com").Value;
        var user = User.Create(email, "Org", "User", UserRole.EventOrganizer).Value;
        typeof(User).GetProperty("Id")?.SetValue(user, userId);
        return user;
    }

    private void SetupHappyPath(Guid organizerId)
    {
        var user = CreateOrganizer(organizerId);
        _mockUserRepository
            .Setup(x => x.GetByIdAsync(organizerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _mockTimeZoneLookupService
            .Setup(x => x.DefaultTimeZoneId)
            .Returns("America/New_York");
        _mockUnitOfWork
            .Setup(x => x.CommitAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Validator: mixed-date pair rejection
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Validator_BothDatesNull_DoesNotFailOnDates()
    {
        var validator = new CreateEventCommandValidator();
        var cmd = new CreateEventCommand(
            Title: "TBD event",
            Description: "Coming soon — date to be confirmed",
            StartDate: null,
            EndDate: null,
            OrganizerId: Guid.NewGuid(),
            Capacity: 100);

        var result = validator.Validate(cmd);

        // The validator should not raise a date-pair error for both-null;
        // mode-resolution etc may still fail but we're asserting no date error here.
        result.Errors.Should().NotContain(e =>
            e.PropertyName.Contains("StartDate") || e.PropertyName.Contains("EndDate"));
    }

    [Fact]
    public void Validator_StartDateOnly_FailsWithMixedDatesError()
    {
        var validator = new CreateEventCommandValidator();
        var cmd = new CreateEventCommand(
            Title: "Mixed dates event",
            Description: "Should be rejected",
            StartDate: DateTime.UtcNow.AddDays(7),
            EndDate: null,
            OrganizerId: Guid.NewGuid(),
            Capacity: 100);

        var result = validator.Validate(cmd);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            (e.PropertyName.Contains("StartDate") || e.PropertyName.Contains("EndDate"))
            && e.ErrorMessage.ToLower().Contains("both"));
    }

    [Fact]
    public void Validator_EndDateOnly_FailsWithMixedDatesError()
    {
        var validator = new CreateEventCommandValidator();
        var cmd = new CreateEventCommand(
            Title: "Mixed dates event",
            Description: "Should be rejected",
            StartDate: null,
            EndDate: DateTime.UtcNow.AddDays(8),
            OrganizerId: Guid.NewGuid(),
            Capacity: 100);

        var result = validator.Validate(cmd);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            (e.PropertyName.Contains("StartDate") || e.PropertyName.Contains("EndDate"))
            && e.ErrorMessage.ToLower().Contains("both"));
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Handler: command flows nullable dates through to Domain
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_BothDatesNull_CreatesPlanningEvent()
    {
        var organizerId = Guid.NewGuid();
        SetupHappyPath(organizerId);

        Event? capturedEvent = null;
        _mockEventRepository
            .Setup(x => x.AddAsync(It.IsAny<Event>(), It.IsAny<CancellationToken>()))
            .Callback<Event, CancellationToken>((e, _) => capturedEvent = e)
            .Returns(Task.CompletedTask);

        var command = new CreateEventCommand(
            Title: "TBD community gathering",
            Description: "Date and venue to be decided",
            StartDate: null,
            EndDate: null,
            OrganizerId: organizerId,
            Capacity: 50);

        var handler = CreateHandler();

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue($"Expected success but got error: {result.Error}");
        capturedEvent.Should().NotBeNull();
        capturedEvent!.Status.Should().Be(EventStatus.Planning);
        capturedEvent.StartDate.Should().BeNull();
        capturedEvent.EndDate.Should().BeNull();
    }

    [Fact]
    public async Task Handle_BothDatesSet_CreatesDraftEvent()
    {
        var organizerId = Guid.NewGuid();
        SetupHappyPath(organizerId);

        Event? capturedEvent = null;
        _mockEventRepository
            .Setup(x => x.AddAsync(It.IsAny<Event>(), It.IsAny<CancellationToken>()))
            .Callback<Event, CancellationToken>((e, _) => capturedEvent = e)
            .Returns(Task.CompletedTask);

        var command = new CreateEventCommand(
            Title: "Dated event",
            Description: "Has a real date",
            StartDate: DateTime.UtcNow.AddDays(7),
            EndDate: DateTime.UtcNow.AddDays(8),
            OrganizerId: organizerId,
            Capacity: 50);

        var handler = CreateHandler();

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue($"Expected success but got error: {result.Error}");
        capturedEvent.Should().NotBeNull();
        capturedEvent!.Status.Should().Be(EventStatus.Draft);
        capturedEvent.StartDate.Should().NotBeNull();
        capturedEvent.EndDate.Should().NotBeNull();
    }
}
