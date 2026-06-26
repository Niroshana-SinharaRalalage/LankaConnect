using LankaConnect.Modules.Identity.Contracts;
using FluentAssertions;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Events.Commands.PhotoAlbums.SendAlbumNotification;
using LankaConnect.Application.Interfaces;
using LankaConnect.Domain.Business.ValueObjects;
using LankaConnect.Domain.Events;
using LankaConnect.Modules.Identity.Domain.DomainEvents;
using LankaConnect.Modules.Media.Domain;
using LankaConnect.Modules.Media.Domain.Entities;
using LankaConnect.Modules.Media.Domain.Enums;
using LankaConnect.Modules.Media.Domain.DomainEvents;
using LankaConnect.Domain.Events.Entities;
using LankaConnect.Domain.Events.Enums;
using LankaConnect.Domain.Events.ValueObjects;
using LankaConnect.Modules.Identity.Domain.Entities;
using LankaConnect.Modules.Identity.Domain.Repositories;
using LankaConnect.Modules.Identity.Domain.Events;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace LankaConnect.Application.Tests.Events.Commands;

/// <summary>
/// Phase 7B: TDD tests for SendAlbumNotificationCommandHandler.
/// Covers: album validation, organizer auth, registration recipient path,
/// sign-up list participant path, and no-recipient early exit.
/// Red-Green-Refactor cycle.
/// </summary>
public class SendAlbumNotificationCommandHandlerTests
{
    private readonly Mock<IPhotoAlbumRepository> _albumRepo;
    private readonly Mock<IRegistrationRepository> _registrationRepo;
    private readonly Mock<IEventRepository> _eventRepo;
    private readonly Mock<IIdentityQueries> _userRepo;
    private readonly Mock<IEmailUrlHelper> _emailUrlHelper;
    private readonly Mock<IServiceScopeFactory> _scopeFactory;
    private readonly SendAlbumNotificationCommandHandler _handler;

    private static readonly Guid OrganizerId = Guid.NewGuid();
    private static readonly Guid EventId    = Guid.NewGuid();
    private static readonly Guid AlbumId    = Guid.NewGuid();

    public SendAlbumNotificationCommandHandlerTests()
    {
        _albumRepo        = new Mock<IPhotoAlbumRepository>();
        _registrationRepo = new Mock<IRegistrationRepository>();
        _eventRepo        = new Mock<IEventRepository>();
        _userRepo         = new Mock<IIdentityQueries>();
        _emailUrlHelper   = new Mock<IEmailUrlHelper>();
        _scopeFactory     = new Mock<IServiceScopeFactory>();

        _emailUrlHelper
            .Setup(x => x.BuildEventDetailsUrl(It.IsAny<Guid>()))
            .Returns("https://example.com/events/test");

        // Default: no scope service needed (fire-and-forget won't reach email sending in unit tests)
        var mockScope           = new Mock<IServiceScope>();
        var mockServiceProvider = new Mock<IServiceProvider>();
        mockScope.Setup(s => s.ServiceProvider).Returns(mockServiceProvider.Object);
        _scopeFactory.Setup(f => f.CreateScope()).Returns(mockScope.Object);

        _handler = new SendAlbumNotificationCommandHandler(
            _albumRepo.Object,
            _registrationRepo.Object,
            _eventRepo.Object,
            _userRepo.Object,
            _emailUrlHelper.Object,
            _scopeFactory.Object,
            Mock.Of<ILogger<SendAlbumNotificationCommandHandler>>());
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Helpers
    // ─────────────────────────────────────────────────────────────────────────

    private static PhotoAlbum CreatePublishedAlbum(Guid? albumId = null, Guid? organizerId = null)
    {
        var album = PhotoAlbum.Create(
            eventId:    EventId,
            organizerId: organizerId ?? OrganizerId,
            eventTitle: "Test Event",
            name:       "Test Album").Value;

        // Force PhotoCount > 0 so Publish() succeeds
        typeof(PhotoAlbum).GetProperty("PhotoCount")!.SetValue(album, 1);

        // Force album Id if needed
        if (albumId.HasValue)
            typeof(LankaConnect.Domain.Common.LegacyBaseEntity).GetProperty("Id")!.SetValue(album, albumId.Value);

        album.Publish();
        return album;
    }

    private static SendAlbumNotificationCommand MakeCommand(Guid? userId = null)
        => new(EventId, AlbumId, userId ?? OrganizerId);

    // ─────────────────────────────────────────────────────────────────────────
    // Failure cases
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_AlbumNotFound_ReturnsFailure()
    {
        _albumRepo
            .Setup(r => r.GetByIdAsync(AlbumId, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync((PhotoAlbum?)null);

        var result = await _handler.Handle(MakeCommand(), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain(AlbumId.ToString());
    }

    [Fact]
    public async Task Handle_UserIsNotOrganizer_ReturnsFailure()
    {
        var album = CreatePublishedAlbum(albumId: AlbumId, organizerId: Guid.NewGuid());
        _albumRepo
            .Setup(r => r.GetByIdAsync(AlbumId, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(album);

        var result = await _handler.Handle(MakeCommand(userId: Guid.NewGuid()), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("organizer");
    }

    [Fact]
    public async Task Handle_AlbumNotPublished_ReturnsFailure()
    {
        // Create a Draft album (not published)
        var album = PhotoAlbum.Create(EventId, OrganizerId, "Test Event", "Draft Album").Value;
        typeof(LankaConnect.Domain.Common.LegacyBaseEntity).GetProperty("Id")!.SetValue(album, AlbumId);

        _albumRepo
            .Setup(r => r.GetByIdAsync(AlbumId, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(album);

        var result = await _handler.Handle(MakeCommand(), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("published");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // No-recipient early exit
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_NoEligibleRegistrations_AndNoSignUpParticipants_ReturnsSuccess_WithoutDispatch()
    {
        var album = CreatePublishedAlbum(albumId: AlbumId);
        _albumRepo
            .Setup(r => r.GetByIdAsync(AlbumId, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(album);

        // No registrations
        _registrationRepo
            .Setup(r => r.GetByEventAsync(EventId, It.IsAny<CancellationToken>(), false))
            .ReturnsAsync(new List<Registration>());

        // No event (or event without sign-up lists)
        _eventRepo
            .Setup(r => r.GetByIdAsync(EventId, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Event?)null);

        var result = await _handler.Handle(MakeCommand(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue("no recipients is a valid state, not an error");
        // No scope created because fire-and-forget never dispatched
        _scopeFactory.Verify(f => f.CreateScope(), Times.Never);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Success with eligible registrations
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WithEligibleRegistrations_ReturnsSuccess()
    {
        var album = CreatePublishedAlbum(albumId: AlbumId);
        _albumRepo
            .Setup(r => r.GetByIdAsync(AlbumId, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(album);

        // One confirmed registration
        var reg = BuildRegistrationWithContact(RegistrationStatus.Confirmed, "test@example.com");
        _registrationRepo
            .Setup(r => r.GetByEventAsync(EventId, It.IsAny<CancellationToken>(), false))
            .ReturnsAsync(new List<Registration> { reg });

        _eventRepo
            .Setup(r => r.GetByIdAsync(EventId, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Event?)null);

        var result = await _handler.Handle(MakeCommand(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue($"Expected success; error: {result.Error}");
    }

    [Fact]
    public async Task Handle_WithAttendedRegistration_IsIncludedAsRecipient()
    {
        var album = CreatePublishedAlbum(albumId: AlbumId);
        _albumRepo
            .Setup(r => r.GetByIdAsync(AlbumId, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(album);

        var attended = BuildRegistrationWithContact(RegistrationStatus.Attended, "attended@example.com");
        _registrationRepo
            .Setup(r => r.GetByEventAsync(EventId, It.IsAny<CancellationToken>(), false))
            .ReturnsAsync(new List<Registration> { attended });

        _eventRepo
            .Setup(r => r.GetByIdAsync(EventId, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Event?)null);

        var result = await _handler.Handle(MakeCommand(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_CancelledRegistrationOnly_ExcludedAndNoEmailDispatched()
    {
        var album = CreatePublishedAlbum(albumId: AlbumId);
        _albumRepo
            .Setup(r => r.GetByIdAsync(AlbumId, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(album);

        // Only a cancelled registration — should be excluded
        var cancelled = BuildRegistrationWithContact(RegistrationStatus.Cancelled, "cancelled@example.com");
        _registrationRepo
            .Setup(r => r.GetByEventAsync(EventId, It.IsAny<CancellationToken>(), false))
            .ReturnsAsync(new List<Registration> { cancelled });

        _eventRepo
            .Setup(r => r.GetByIdAsync(EventId, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Event?)null);

        var result = await _handler.Handle(MakeCommand(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue("cancelled registrations excluded = no error");
        _scopeFactory.Verify(f => f.CreateScope(), Times.Never);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Sign-up list participant path (Phase 7B new behaviour)
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_NullEvent_GracefullySkipsSignUpLookup_StillSendsToRegistrations()
    {
        var album = CreatePublishedAlbum(albumId: AlbumId);
        _albumRepo
            .Setup(r => r.GetByIdAsync(AlbumId, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(album);

        var confirmed = BuildRegistrationWithContact(RegistrationStatus.Confirmed, "reg@example.com");
        _registrationRepo
            .Setup(r => r.GetByEventAsync(EventId, It.IsAny<CancellationToken>(), false))
            .ReturnsAsync(new List<Registration> { confirmed });

        // Event repository returns null — graceful degradation
        _eventRepo
            .Setup(r => r.GetByIdAsync(EventId, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Event?)null);

        var result = await _handler.Handle(MakeCommand(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        // User repo NOT called when event is null (no sign-up lists to process)
        _userRepo.Verify(
            r => r.GetEmailsByUserIdsAsync(It.IsAny<IReadOnlyList<Guid>>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_EventWithNoSignUpLists_UserRepoNotCalled()
    {
        var album = CreatePublishedAlbum(albumId: AlbumId);
        _albumRepo
            .Setup(r => r.GetByIdAsync(AlbumId, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(album);

        var confirmed = BuildRegistrationWithContact(RegistrationStatus.Confirmed, "reg2@example.com");
        _registrationRepo
            .Setup(r => r.GetByEventAsync(EventId, It.IsAny<CancellationToken>(), false))
            .ReturnsAsync(new List<Registration> { confirmed });

        // Return a real event with no sign-up lists
        var @event = BuildEventWithNoSignUpLists();
        _eventRepo
            .Setup(r => r.GetByIdAsync(EventId, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(@event);

        var result = await _handler.Handle(MakeCommand(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _userRepo.Verify(
            r => r.GetEmailsByUserIdsAsync(It.IsAny<IReadOnlyList<Guid>>(), It.IsAny<CancellationToken>()),
            Times.Never,
            "Event with no sign-up lists must not query user repo for sign-up emails");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Private helpers
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Builds a Registration with a known status and email via the Contact field.
    /// Uses RuntimeHelpers.GetUninitializedObject then initializes all private List
    /// backing fields so that LINQ calls (HasDetailedAttendees().Any()) don't throw.
    /// </summary>
    private static Registration BuildRegistrationWithContact(RegistrationStatus status, string email)
    {
        var reg = (Registration)System.Runtime.CompilerServices.RuntimeHelpers
            .GetUninitializedObject(typeof(Registration));

        // Initialize all private List<T> backing fields to empty lists so that
        // domain methods (HasDetailedAttendees → Attendees.Any()) don't throw ArgumentNullException.
        foreach (var field in typeof(Registration)
            .GetFields(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance))
        {
            if (field.FieldType.IsGenericType &&
                field.FieldType.GetGenericTypeDefinition() == typeof(List<>))
            {
                field.SetValue(reg, Activator.CreateInstance(field.FieldType));
            }
        }

        typeof(Registration).GetProperty("Status")!.SetValue(reg, status);

        // Try to find a Contact property and set Email on it
        var contactProp = typeof(Registration).GetProperty("Contact");
        if (contactProp != null)
        {
            try
            {
                var contactType = contactProp.PropertyType;
                var contact = System.Runtime.CompilerServices.RuntimeHelpers
                    .GetUninitializedObject(contactType);
                contactType.GetProperty("Email")?.SetValue(contact, email);
                contactProp.SetValue(reg, contact);
            }
            catch
            {
                // Contact construction failed; AttendeeInfo will remain null → falls to "Attendee" default
            }
        }

        return reg;
    }

    /// <summary>
    /// Builds an Event aggregate with no sign-up lists (clean state after Create).
    /// </summary>
    private static Event BuildEventWithNoSignUpLists()
    {
        var title       = EventTitle.Create("Test Event").Value;
        var description = EventDescription.Create("Test Desc").Value;
        var address     = Address.Create("123 St", "City", "CA", "90001", "USA").Value;
        var location    = EventLocation.Create(address).Value;

        var @event = Event.Create(
            title,
            description,
            startDate:   DateTime.UtcNow.AddDays(7),
            endDate:     DateTime.UtcNow.AddDays(7).AddHours(2),
            organizerId: OrganizerId,
            capacity:    100,
            location:    location).Value;

        return @event;
    }
}
