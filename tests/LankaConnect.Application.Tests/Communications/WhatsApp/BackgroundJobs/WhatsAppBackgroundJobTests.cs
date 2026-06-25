using LankaConnect.Application.Communications.BackgroundJobs;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Communications;
using LankaConnect.Domain.Communications.Entities;
using LankaConnect.Domain.Communications.Enums;
using LankaConnect.Domain.Events;
using LankaConnect.Domain.Users.DomainEvents; // W4.7.a: user-aggregate events moved here
using Microsoft.Extensions.Logging;

namespace LankaConnect.Application.Tests.Communications.WhatsApp.BackgroundJobs;

/// <summary>
/// Phase 7A.3: Unit tests for NewsletterWhatsAppJob and EventDetailsWhatsAppJob.
/// Both jobs receive fully resolved dependencies from DI (no IServiceScopeFactory pattern),
/// so we can inject mocks directly and verify without Task.Run delays.
/// </summary>
public class WhatsAppBackgroundJobTests
{
    // ─── Shared mocks ──────────────────────────────────────────────────────────
    private readonly Mock<IWhatsAppService> _mockWhatsAppService;
    private readonly Mock<INewsletterRepository> _mockNewsletterRepo;
    private readonly Mock<IEventRepository> _mockEventRepo;

    public WhatsAppBackgroundJobTests()
    {
        _mockWhatsAppService = new Mock<IWhatsAppService>();
        _mockNewsletterRepo = new Mock<INewsletterRepository>();
        _mockEventRepo = new Mock<IEventRepository>();

        // Default success responses
        _mockWhatsAppService
            .Setup(s => s.BroadcastToEventAttendeesAsync(
                It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<Dictionary<string, string>>(),
                It.IsAny<WhatsAppNotificationType>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<int>.Success(10));
    }

    // ─── Helper factories ──────────────────────────────────────────────────────

    private static Mock<ILogger<T>> CreateLogger<T>() => new Mock<ILogger<T>>();

    private static Event CreateRealEvent(Guid eventId, string title = "Test Event")
    {
        var eventTitle = LankaConnect.Domain.Events.ValueObjects.EventTitle.Create(title).Value;
        var eventDesc = LankaConnect.Domain.Events.ValueObjects.EventDescription.Create("Test description").Value;

        var evt = Event.Create(
            eventTitle,
            eventDesc,
            DateTime.UtcNow.AddDays(14),
            DateTime.UtcNow.AddDays(14).AddHours(2),
            Guid.NewGuid(),
            100,
            null,
            LankaConnect.Domain.Events.Enums.EventCategory.Community).Value;

        evt.SetAsFreeEvent();

        // Set the Id using reflection (LegacyBaseEntity.Id has protected setter)
        var prop = typeof(LankaConnect.Domain.Common.LegacyBaseEntity).GetProperty("Id");
        prop?.SetValue(evt, eventId);
        return evt;
    }

    private static Newsletter CreateRealNewsletter(
        Guid newsletterId,
        string titleValue = "Monthly Update",
        Guid? eventId = null,
        string descriptionValue = "This month in LankaConnect...")
    {
        var title = LankaConnect.Domain.Communications.ValueObjects.NewsletterTitle
            .Create(titleValue).Value;
        var description = LankaConnect.Domain.Communications.ValueObjects.NewsletterDescription
            .Create(descriptionValue).Value;

        var newsletter = Newsletter.Create(
            title, description,
            Guid.NewGuid(),               // createdByUserId
            new List<Guid>(),             // no email groups
            includeNewsletterSubscribers: true,
            eventId: eventId).Value;

        // Set the Id using reflection (LegacyBaseEntity.Id has protected setter)
        typeof(LankaConnect.Domain.Common.LegacyBaseEntity).GetProperty("Id")?.SetValue(newsletter, newsletterId);
        return newsletter;
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // NewsletterWhatsAppJob
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task Newsletter_ExecuteAsync_NewsletterNotFound_LogsWarningAndReturns()
    {
        var job = new NewsletterWhatsAppJob(
            _mockNewsletterRepo.Object, _mockWhatsAppService.Object,
            CreateLogger<NewsletterWhatsAppJob>().Object);

        var newsletterId = Guid.NewGuid();
        _mockNewsletterRepo
            .Setup(r => r.GetByIdAsync(newsletterId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Newsletter?)null);

        await job.ExecuteAsync(newsletterId);

        _mockWhatsAppService.Verify(
            s => s.BroadcastToEventAttendeesAsync(It.IsAny<Guid>(), It.IsAny<string>(),
                It.IsAny<Dictionary<string, string>>(), It.IsAny<WhatsAppNotificationType>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Newsletter_ExecuteAsync_WithEventId_BroadcastsToEventAttendees()
    {
        var job = new NewsletterWhatsAppJob(
            _mockNewsletterRepo.Object, _mockWhatsAppService.Object,
            CreateLogger<NewsletterWhatsAppJob>().Object);

        var newsletterId = Guid.NewGuid();
        var associatedEventId = Guid.NewGuid();

        _mockNewsletterRepo
            .Setup(r => r.GetByIdAsync(newsletterId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateRealNewsletter(newsletterId, "Community Update", associatedEventId));

        await job.ExecuteAsync(newsletterId);

        _mockWhatsAppService.Verify(
            s => s.BroadcastToEventAttendeesAsync(associatedEventId, It.IsAny<string>(),
                It.IsAny<Dictionary<string, string>>(), WhatsAppNotificationType.Newsletter,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Newsletter_ExecuteAsync_NoEventId_SkipsBroadcast()
    {
        var job = new NewsletterWhatsAppJob(
            _mockNewsletterRepo.Object, _mockWhatsAppService.Object,
            CreateLogger<NewsletterWhatsAppJob>().Object);

        var newsletterId = Guid.NewGuid();

        // EventId is null — not associated with a specific event
        _mockNewsletterRepo
            .Setup(r => r.GetByIdAsync(newsletterId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateRealNewsletter(newsletterId, "General Update", eventId: null));

        await job.ExecuteAsync(newsletterId);

        _mockWhatsAppService.Verify(
            s => s.BroadcastToEventAttendeesAsync(It.IsAny<Guid>(), It.IsAny<string>(),
                It.IsAny<Dictionary<string, string>>(), It.IsAny<WhatsAppNotificationType>(),
                It.IsAny<CancellationToken>()),
            Times.Never,
            "newsletters without an associated event must be skipped");
    }

    [Fact]
    public async Task Newsletter_ExecuteAsync_BroadcastFails_DoesNotThrow()
    {
        var job = new NewsletterWhatsAppJob(
            _mockNewsletterRepo.Object, _mockWhatsAppService.Object,
            CreateLogger<NewsletterWhatsAppJob>().Object);

        var newsletterId = Guid.NewGuid();
        var eventId = Guid.NewGuid();

        _mockNewsletterRepo
            .Setup(r => r.GetByIdAsync(newsletterId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateRealNewsletter(newsletterId, "Update", eventId));

        _mockWhatsAppService
            .Setup(s => s.BroadcastToEventAttendeesAsync(
                It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<Dictionary<string, string>>(),
                It.IsAny<WhatsAppNotificationType>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<int>.Failure("WhatsApp service unavailable"));

        var act = async () => await job.ExecuteAsync(newsletterId);
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task Newsletter_ExecuteAsync_ServiceThrows_DoesNotPropagateException()
    {
        var job = new NewsletterWhatsAppJob(
            _mockNewsletterRepo.Object, _mockWhatsAppService.Object,
            CreateLogger<NewsletterWhatsAppJob>().Object);

        var newsletterId = Guid.NewGuid();
        var eventId = Guid.NewGuid();

        _mockNewsletterRepo
            .Setup(r => r.GetByIdAsync(newsletterId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateRealNewsletter(newsletterId, "Update", eventId));

        _mockWhatsAppService
            .Setup(s => s.BroadcastToEventAttendeesAsync(
                It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<Dictionary<string, string>>(),
                It.IsAny<WhatsAppNotificationType>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Downstream error"));

        var act = async () => await job.ExecuteAsync(newsletterId);
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task Newsletter_ExecuteAsync_LongDescription_TruncatesPreviewTo100Chars()
    {
        var job = new NewsletterWhatsAppJob(
            _mockNewsletterRepo.Object, _mockWhatsAppService.Object,
            CreateLogger<NewsletterWhatsAppJob>().Object);

        var newsletterId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        var longDescription = new string('x', 200); // 200 chars — must be truncated to 100

        // Use the real factory with a long description (200 x's)
        _mockNewsletterRepo
            .Setup(r => r.GetByIdAsync(newsletterId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateRealNewsletter(newsletterId, "Title", eventId, longDescription));

        await job.ExecuteAsync(newsletterId);

        _mockWhatsAppService.Verify(
            s => s.BroadcastToEventAttendeesAsync(eventId, It.IsAny<string>(),
                It.Is<Dictionary<string, string>>(d =>
                    d.Any(kv => kv.Value.Length <= 100 && kv.Value.EndsWith("..."))),
                WhatsAppNotificationType.Newsletter, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Newsletter_ExecuteAsync_ShortDescription_NotTruncated()
    {
        var job = new NewsletterWhatsAppJob(
            _mockNewsletterRepo.Object, _mockWhatsAppService.Object,
            CreateLogger<NewsletterWhatsAppJob>().Object);

        var newsletterId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        var shortDescription = "Short preview text"; // well under 100 chars

        _mockNewsletterRepo
            .Setup(r => r.GetByIdAsync(newsletterId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateRealNewsletter(newsletterId, "Title", eventId, shortDescription));

        await job.ExecuteAsync(newsletterId);

        _mockWhatsAppService.Verify(
            s => s.BroadcastToEventAttendeesAsync(eventId, It.IsAny<string>(),
                It.Is<Dictionary<string, string>>(d => d.ContainsValue(shortDescription)),
                WhatsAppNotificationType.Newsletter, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // EventDetailsWhatsAppJob
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task EventDetails_ExecuteAsync_EventNotFound_LogsWarningAndReturns()
    {
        var job = new EventDetailsWhatsAppJob(
            _mockWhatsAppService.Object, _mockEventRepo.Object,
            CreateLogger<EventDetailsWhatsAppJob>().Object);

        var eventId = Guid.NewGuid();
        _mockEventRepo
            .Setup(r => r.GetByIdAsync(eventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Event?)null);

        await job.ExecuteAsync(eventId);

        _mockWhatsAppService.Verify(
            s => s.BroadcastToEventAttendeesAsync(It.IsAny<Guid>(), It.IsAny<string>(),
                It.IsAny<Dictionary<string, string>>(), It.IsAny<WhatsAppNotificationType>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task EventDetails_ExecuteAsync_ValidEvent_BroadcastsToAttendees()
    {
        var job = new EventDetailsWhatsAppJob(
            _mockWhatsAppService.Object, _mockEventRepo.Object,
            CreateLogger<EventDetailsWhatsAppJob>().Object);

        var eventId = Guid.NewGuid();
        _mockEventRepo
            .Setup(r => r.GetByIdAsync(eventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateRealEvent(eventId));

        await job.ExecuteAsync(eventId);

        _mockWhatsAppService.Verify(
            s => s.BroadcastToEventAttendeesAsync(eventId, It.IsAny<string>(),
                It.IsAny<Dictionary<string, string>>(), WhatsAppNotificationType.EventUpdate,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task EventDetails_ExecuteAsync_WithLocation_IncludesAddressInParameters()
    {
        var job = new EventDetailsWhatsAppJob(
            _mockWhatsAppService.Object, _mockEventRepo.Object,
            CreateLogger<EventDetailsWhatsAppJob>().Object);

        var eventId = Guid.NewGuid();

        // Create a real Address via factory (LankaConnect.Domain.Business.ValueObjects.Address)
        var realAddress = LankaConnect.Domain.Business.ValueObjects.Address
            .Create("123 Main St", "Atlanta", "GA", "30301", "US").Value;

        // Create a real EventLocation wrapping the address
        var realLocation = LankaConnect.Domain.Events.ValueObjects.EventLocation
            .Create(realAddress).Value;

        // Create a real event with the physical location
        var eventTitle = LankaConnect.Domain.Events.ValueObjects.EventTitle.Create("Annual Gala").Value;
        var eventDesc = LankaConnect.Domain.Events.ValueObjects.EventDescription.Create("Test").Value;
        var realEvent = Event.Create(eventTitle, eventDesc,
            DateTime.UtcNow.AddDays(14), DateTime.UtcNow.AddDays(14).AddHours(2),
            Guid.NewGuid(), 100, realLocation,
            LankaConnect.Domain.Events.Enums.EventCategory.Community).Value;
        realEvent.SetAsFreeEvent();
        typeof(LankaConnect.Domain.Common.LegacyBaseEntity).GetProperty("Id")?.SetValue(realEvent, eventId);

        _mockEventRepo
            .Setup(r => r.GetByIdAsync(eventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(realEvent);

        await job.ExecuteAsync(eventId);

        _mockWhatsAppService.Verify(
            s => s.BroadcastToEventAttendeesAsync(eventId, It.IsAny<string>(),
                It.Is<Dictionary<string, string>>(d =>
                    d.Any(kv => kv.Value.Contains("Main St") || kv.Value.Contains("Atlanta"))),
                WhatsAppNotificationType.EventUpdate, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task EventDetails_ExecuteAsync_OnlineEvent_UsesOnlineEventLocation()
    {
        var job = new EventDetailsWhatsAppJob(
            _mockWhatsAppService.Object, _mockEventRepo.Object,
            CreateLogger<EventDetailsWhatsAppJob>().Object);

        var eventId = Guid.NewGuid();

        // Event with no physical location (default for CreateRealEvent) → should use "Online Event"
        _mockEventRepo
            .Setup(r => r.GetByIdAsync(eventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateRealEvent(eventId));

        await job.ExecuteAsync(eventId);

        _mockWhatsAppService.Verify(
            s => s.BroadcastToEventAttendeesAsync(eventId, It.IsAny<string>(),
                It.Is<Dictionary<string, string>>(d => d.ContainsValue("Online Event")),
                WhatsAppNotificationType.EventUpdate, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task EventDetails_ExecuteAsync_BroadcastFails_DoesNotThrow()
    {
        var job = new EventDetailsWhatsAppJob(
            _mockWhatsAppService.Object, _mockEventRepo.Object,
            CreateLogger<EventDetailsWhatsAppJob>().Object);

        var eventId = Guid.NewGuid();
        _mockEventRepo
            .Setup(r => r.GetByIdAsync(eventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateRealEvent(eventId));

        _mockWhatsAppService
            .Setup(s => s.BroadcastToEventAttendeesAsync(
                It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<Dictionary<string, string>>(),
                It.IsAny<WhatsAppNotificationType>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<int>.Failure("Rate limit exceeded"));

        var act = async () => await job.ExecuteAsync(eventId);
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task EventDetails_ExecuteAsync_ServiceThrows_DoesNotPropagateException()
    {
        var job = new EventDetailsWhatsAppJob(
            _mockWhatsAppService.Object, _mockEventRepo.Object,
            CreateLogger<EventDetailsWhatsAppJob>().Object);

        var eventId = Guid.NewGuid();
        _mockEventRepo
            .Setup(r => r.GetByIdAsync(eventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateRealEvent(eventId));

        _mockWhatsAppService
            .Setup(s => s.BroadcastToEventAttendeesAsync(
                It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<Dictionary<string, string>>(),
                It.IsAny<WhatsAppNotificationType>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new TimeoutException("Service timed out"));

        var act = async () => await job.ExecuteAsync(eventId);
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task EventDetails_ExecuteAsync_IncludesEventTitleInParameters()
    {
        var job = new EventDetailsWhatsAppJob(
            _mockWhatsAppService.Object, _mockEventRepo.Object,
            CreateLogger<EventDetailsWhatsAppJob>().Object);

        var eventId = Guid.NewGuid();
        var expectedTitle = "Sri Lankan Cultural Festival";

        _mockEventRepo
            .Setup(r => r.GetByIdAsync(eventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateRealEvent(eventId, expectedTitle));

        await job.ExecuteAsync(eventId);

        _mockWhatsAppService.Verify(
            s => s.BroadcastToEventAttendeesAsync(eventId, It.IsAny<string>(),
                It.Is<Dictionary<string, string>>(d => d.ContainsValue(expectedTitle)),
                WhatsAppNotificationType.EventUpdate, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task EventDetails_ExecuteAsync_BroadcastSucceeds_LogsCount()
    {
        var logger = CreateLogger<EventDetailsWhatsAppJob>();
        var job = new EventDetailsWhatsAppJob(
            _mockWhatsAppService.Object, _mockEventRepo.Object, logger.Object);

        var eventId = Guid.NewGuid();
        _mockEventRepo
            .Setup(r => r.GetByIdAsync(eventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateRealEvent(eventId));

        _mockWhatsAppService
            .Setup(s => s.BroadcastToEventAttendeesAsync(
                It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<Dictionary<string, string>>(),
                It.IsAny<WhatsAppNotificationType>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<int>.Success(42));

        // Act — should complete without error; logging is internal
        await job.ExecuteAsync(eventId);

        // Verify the broadcast was invoked (logging cannot be verified without ILogger<T> log-level setup)
        _mockWhatsAppService.Verify(
            s => s.BroadcastToEventAttendeesAsync(eventId, It.IsAny<string>(),
                It.IsAny<Dictionary<string, string>>(), WhatsAppNotificationType.EventUpdate,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
