using LankaConnect.Modules.Identity.Contracts;
using LankaConnect.BuildingBlocks.Application.Common;
using LankaConnect.Modules.Forms.Domain;
using LankaConnect.Modules.Forms.Domain.Entities;
using LankaConnect.Modules.Forms.Domain.Enums;
using LankaConnect.Modules.Forms.Domain.DomainEvents;
using LankaConnect.Modules.Forms.Domain.Repositories;
using LankaConnect.BuildingBlocks.Application.Common.Interfaces;
using LankaConnect.Products.LankaEvents.Application.EventHandlers;
using LankaConnect.Modules.Payments.Application.EventHandlers; // W4.4.c.3: PaymentCompletedWhatsAppHandler moved here
using LankaConnect.Modules.Forms.Application.EventHandlers; // W5.3d.2: FormResponseWhatsAppHandler moved here
using LankaConnect.BuildingBlocks.Domain;
using LankaConnect.Modules.Communications.Domain.Enums;
using LankaConnect.Products.LankaEvents.Domain;
using LankaConnect.Modules.Identity.Domain.DomainEvents;
using LankaConnect.Products.LankaEvents.Domain.DomainEvents;
using LankaConnect.Products.LankaEvents.Domain.Entities;
using LankaConnect.Products.LankaEvents.Domain.Repositories;
using LankaConnect.Modules.Identity.Domain.Entities;
using LankaConnect.Modules.Identity.Domain.Repositories;
using LankaConnect.Modules.Identity.Domain.Events;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace LankaConnect.Application.Tests.Events.WhatsAppHandlers;

/// <summary>
/// Phase 7A.3: Unit tests for all 11 WhatsApp event notification handlers.
/// Tests cover fire-and-forget pattern behaviour: immediate Task.CompletedTask return,
/// null-guard early exits, and background task WhatsApp sends.
/// All handlers use IServiceScopeFactory to create a DI scope inside Task.Run.
/// </summary>
public class WhatsAppEventHandlerTests
{
    // ─── Shared DI-scope scaffolding ───────────────────────────────────────────
    private readonly Mock<IServiceScopeFactory> _mockScopeFactory;
    private readonly Mock<IServiceScope> _mockScope;
    private readonly Mock<IServiceProvider> _mockServiceProvider;
    private readonly Mock<IWhatsAppService> _mockWhatsAppService;
    private readonly Mock<IIdentityQueries> _mockIdentityQueries;
    private readonly Mock<IUserRepository> _mockUserRepo;
    private readonly Mock<IEventRepository> _mockEventRepo;
    private readonly Mock<IRegistrationRepository> _mockRegistrationRepo;
    private readonly Mock<IAddOnDefinitionRepository> _mockAddOnRepo;
    private readonly Mock<IFormResponseRepository> _mockFormResponseRepo;
    private readonly Mock<IFormRepository> _mockEventFormRepo;
    // Phase 6A.148.W5.6.A: WhatsApp RefundCompleted handler now resolves IRefundTotalCalculator
    // inside its Task.Run scope. Default mock returns the legacy fallback verbatim so existing
    // tests' expected dollar values stay valid.
    private readonly Mock<LankaConnect.Products.LankaEvents.Application.Services.IRefundTotalCalculator> _mockRefundTotalCalculator;

    public WhatsAppEventHandlerTests()
    {
        _mockScopeFactory = new Mock<IServiceScopeFactory>();
        _mockScope = new Mock<IServiceScope>();
        _mockServiceProvider = new Mock<IServiceProvider>();
        _mockWhatsAppService = new Mock<IWhatsAppService>();
        _mockIdentityQueries = new Mock<IIdentityQueries>();
        _mockUserRepo = new Mock<IUserRepository>();
        _mockEventRepo = new Mock<IEventRepository>();
        _mockRegistrationRepo = new Mock<IRegistrationRepository>();
        _mockAddOnRepo = new Mock<IAddOnDefinitionRepository>();
        _mockFormResponseRepo = new Mock<IFormResponseRepository>();
        _mockEventFormRepo = new Mock<IFormRepository>();
        _mockRefundTotalCalculator = new Mock<LankaConnect.Products.LankaEvents.Application.Services.IRefundTotalCalculator>();
        _mockRefundTotalCalculator
            .Setup(c => c.ComputeAttendeeFacingTotalAsync(
                It.IsAny<string>(), It.IsAny<decimal>(), It.IsAny<CancellationToken>()))
            .Returns<string, decimal, CancellationToken>((_, fallback, _) => Task.FromResult(fallback));

        // Wire scope factory → scope → service provider
        _mockScope.Setup(s => s.ServiceProvider).Returns(_mockServiceProvider.Object);
        _mockScopeFactory.Setup(f => f.CreateScope()).Returns(_mockScope.Object);

        // Default service provider registrations
        _mockServiceProvider
            .Setup(sp => sp.GetService(typeof(IWhatsAppService)))
            .Returns(_mockWhatsAppService.Object);
        _mockServiceProvider
            .Setup(sp => sp.GetService(typeof(IUserRepository)))
            .Returns(_mockUserRepo.Object);
        _mockServiceProvider
            .Setup(sp => sp.GetService(typeof(IIdentityQueries)))
            .Returns(_mockIdentityQueries.Object);
        _mockServiceProvider
            .Setup(sp => sp.GetService(typeof(IEventRepository)))
            .Returns(_mockEventRepo.Object);
        _mockServiceProvider
            .Setup(sp => sp.GetService(typeof(IRegistrationRepository)))
            .Returns(_mockRegistrationRepo.Object);
        _mockServiceProvider
            .Setup(sp => sp.GetService(typeof(IAddOnDefinitionRepository)))
            .Returns(_mockAddOnRepo.Object);
        _mockServiceProvider
            .Setup(sp => sp.GetService(typeof(IFormResponseRepository)))
            .Returns(_mockFormResponseRepo.Object);
        _mockServiceProvider
            .Setup(sp => sp.GetService(typeof(IFormRepository)))
            .Returns(_mockEventFormRepo.Object);
        _mockServiceProvider
            .Setup(sp => sp.GetService(typeof(LankaConnect.Products.LankaEvents.Application.Services.IRefundTotalCalculator)))
            .Returns(_mockRefundTotalCalculator.Object);

        // Default WhatsApp success responses
        _mockWhatsAppService
            .Setup(s => s.SendTemplateMessageAsync(
                It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<Dictionary<string, string>>(),
                It.IsAny<WhatsAppNotificationType>(), It.IsAny<Guid?>(), It.IsAny<Guid?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<WhatsAppSendResult>.Success(WhatsAppSendResult.Sent(Guid.NewGuid(), "acs-msg-001")));

        _mockWhatsAppService
            .Setup(s => s.SendTemplateMessageToPhoneAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Dictionary<string, string>>(),
                It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<WhatsAppSendResult>.Success(WhatsAppSendResult.Sent(Guid.NewGuid(), "acs-msg-002")));

        _mockWhatsAppService
            .Setup(s => s.BroadcastToEventAttendeesAsync(
                It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<Dictionary<string, string>>(),
                It.IsAny<WhatsAppNotificationType>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<int>.Success(5));
    }

    // ─── Helper factories ──────────────────────────────────────────────────────

    /// <summary>Creates a real User domain object with a specific Id set via reflection.</summary>
    private static UserSummaryDto CreateRealUser(Guid userId, string firstName = "Niro", string lastName = "Perera")
    {
        return new UserSummaryDto(
            Id: userId,
            Email: $"{userId}@test.com",
            FirstName: firstName,
            LastName: lastName,
            DisplayName: $"{firstName} {lastName}",
            Role: UserRoleDto.GeneralUser,
            Status: UserStatusDto.Active,
            EmailVerified: true,
            CreatedAt: System.DateTime.UtcNow,
            UpdatedAt: null);
    }

    /// <summary>Creates a real Event domain object with a specific Id set via reflection.</summary>
    private static Event CreateRealEvent(Guid eventId, string title = "Test Event")
    {
        var eventTitle = LankaConnect.Products.LankaEvents.Domain.ValueObjects.EventTitle.Create(title).Value;
        var eventDesc = LankaConnect.Products.LankaEvents.Domain.ValueObjects.EventDescription.Create("Test description").Value;

        var evt = Event.Create(
            eventTitle,
            eventDesc,
            DateTime.UtcNow.AddDays(7),
            DateTime.UtcNow.AddDays(7).AddHours(2),
            Guid.NewGuid(),
            100,
            null,
            LankaConnect.Products.LankaEvents.Domain.Enums.EventCategory.Community).Value;

        evt.SetAsFreeEvent();
        SetEntityId(evt, eventId);
        return evt;
    }

    /// <summary>Sets Id via reflection (works for both legacy LegacyBaseEntity and BB.Domain.Entity&lt;TId&gt;).</summary>
    private static void SetEntityId(object entity, Guid id)
    {
        var prop = entity.GetType().GetProperty("Id");
        prop?.SetValue(entity, id);
    }

    /// <summary>
    /// Creates a Registration via EF Core's private parameterless constructor and leaves Contact null.
    /// Used to simulate the "no phone number" path in AnonymousRegistrationWhatsAppHandler.
    /// </summary>
    private static LankaConnect.Products.LankaEvents.Domain.Registration CreateRealRegistrationWithNoContact(Guid eventId)
    {
        var regType = typeof(LankaConnect.Products.LankaEvents.Domain.Registration);
        var ctor = regType.GetConstructor(
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance,
            null, Type.EmptyTypes, null);

        var registration = (LankaConnect.Products.LankaEvents.Domain.Registration)ctor!.Invoke(null);
        // Contact property has private setter — leave null (default)
        SetPrivateProperty(registration, "EventId", eventId);
        return registration;
    }

    /// <summary>
    /// Creates a Registration with a real RegistrationContact (with a valid phone number).
    /// </summary>
    private static LankaConnect.Products.LankaEvents.Domain.Registration CreateRealRegistrationWithPhone(
        Guid eventId, string email, string phoneNumber,
        string? whatsAppPhoneNumber = null, bool whatsAppOptedIn = false)
    {
        var regType = typeof(LankaConnect.Products.LankaEvents.Domain.Registration);
        var ctor = regType.GetConstructor(
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance,
            null, Type.EmptyTypes, null);

        var registration = (LankaConnect.Products.LankaEvents.Domain.Registration)ctor!.Invoke(null);
        SetPrivateProperty(registration, "EventId", eventId);

        // Phase 7A.6D: Pass WhatsApp phone + opt-in flag to RegistrationContact
        var contact = LankaConnect.Products.LankaEvents.Domain.ValueObjects.RegistrationContact
            .Create(email, phoneNumber, null, whatsAppPhoneNumber, whatsAppOptedIn).Value;
        SetPrivateProperty(registration, "Contact", contact);

        var attendee = LankaConnect.Products.LankaEvents.Domain.ValueObjects.AttendeeDetails
            .Create("Guest User", LankaConnect.Products.LankaEvents.Domain.Enums.AgeCategory.Adult, null).Value;

        // _attendees is a private List<AttendeeDetails> field
        var attendeesField = regType.GetField("_attendees",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var attendeesList = (System.Collections.Generic.List<LankaConnect.Products.LankaEvents.Domain.ValueObjects.AttendeeDetails>)
            attendeesField!.GetValue(registration)!;
        attendeesList.Add(attendee);

        return registration;
    }

    private static void SetPrivateProperty(object obj, string propertyName, object? value)
    {
        var prop = obj.GetType().GetProperty(propertyName,
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic |
            System.Reflection.BindingFlags.Instance);
        prop?.SetValue(obj, value);
    }

    private static Mock<ILogger<T>> CreateLogger<T>() => new Mock<ILogger<T>>();

    // ═══════════════════════════════════════════════════════════════════════════
    // 1. RegistrationConfirmedWhatsAppHandler
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task RegistrationConfirmed_Handle_ReturnsImmediately()
    {
        var handler = new RegistrationConfirmedWhatsAppHandler(
            _mockScopeFactory.Object, CreateLogger<RegistrationConfirmedWhatsAppHandler>().Object);

        var domainEvent = new RegistrationConfirmedEvent(Guid.NewGuid(), Guid.NewGuid(), 2, DateTime.UtcNow);
        var notification = new DomainEventNotification<RegistrationConfirmedEvent>(domainEvent);

        var task = handler.Handle(notification, CancellationToken.None);
        task.IsCompleted.Should().BeTrue("fire-and-forget handler must return Task.CompletedTask synchronously");
        await task;
    }

    [Fact]
    public async Task RegistrationConfirmed_Handle_UserNotFound_LogsWarning()
    {
        var logger = CreateLogger<RegistrationConfirmedWhatsAppHandler>();
        var handler = new RegistrationConfirmedWhatsAppHandler(_mockScopeFactory.Object, logger.Object);

        var attendeeId = Guid.NewGuid();
        _mockIdentityQueries.Setup(r => r.GetUserByIdAsync(attendeeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserSummaryDto?)null);

        var domainEvent = new RegistrationConfirmedEvent(Guid.NewGuid(), attendeeId, 1, DateTime.UtcNow);
        await handler.Handle(new DomainEventNotification<RegistrationConfirmedEvent>(domainEvent), CancellationToken.None);
        await Task.Delay(2000); // Allow Task.Run to complete (500ms for CI runners)

        _mockWhatsAppService.Verify(
            s => s.SendTemplateMessageAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<Dictionary<string, string>>(),
                It.IsAny<WhatsAppNotificationType>(), It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task RegistrationConfirmed_Handle_EventNotFound_LogsWarning()
    {
        var logger = CreateLogger<RegistrationConfirmedWhatsAppHandler>();
        var handler = new RegistrationConfirmedWhatsAppHandler(_mockScopeFactory.Object, logger.Object);

        var attendeeId = Guid.NewGuid();
        var eventId = Guid.NewGuid();

        _mockIdentityQueries.Setup(r => r.GetUserByIdAsync(attendeeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateRealUser(attendeeId));
        _mockEventRepo.Setup(r => r.GetByIdAsync(eventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Event?)null);

        var domainEvent = new RegistrationConfirmedEvent(eventId, attendeeId, 1, DateTime.UtcNow);
        await handler.Handle(new DomainEventNotification<RegistrationConfirmedEvent>(domainEvent), CancellationToken.None);
        await Task.Delay(2000);

        _mockWhatsAppService.Verify(
            s => s.SendTemplateMessageAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<Dictionary<string, string>>(),
                It.IsAny<WhatsAppNotificationType>(), It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task RegistrationConfirmed_Handle_ValidData_SendsWhatsApp()
    {
        var handler = new RegistrationConfirmedWhatsAppHandler(
            _mockScopeFactory.Object, CreateLogger<RegistrationConfirmedWhatsAppHandler>().Object);

        var attendeeId = Guid.NewGuid();
        var eventId = Guid.NewGuid();

        _mockIdentityQueries.Setup(r => r.GetUserByIdAsync(attendeeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateRealUser(attendeeId));
        _mockEventRepo.Setup(r => r.GetByIdAsync(eventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateRealEvent(eventId));

        var domainEvent = new RegistrationConfirmedEvent(eventId, attendeeId, 2, DateTime.UtcNow);
        await handler.Handle(new DomainEventNotification<RegistrationConfirmedEvent>(domainEvent), CancellationToken.None);
        await Task.Delay(3000); // Increased for CI runner stability (fire-and-forget Task.Run)

        _mockWhatsAppService.Verify(
            s => s.SendTemplateMessageAsync(attendeeId, It.IsAny<string>(), It.IsAny<Dictionary<string, string>>(),
                WhatsAppNotificationType.EventRegistration, eventId, It.IsAny<Guid?>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // 2. PaymentCompletedWhatsAppHandler
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task PaymentCompleted_Handle_NoUserId_ReturnsImmediatelyWithoutTask()
    {
        var handler = new PaymentCompletedWhatsAppHandler(
            _mockScopeFactory.Object, CreateLogger<PaymentCompletedWhatsAppHandler>().Object);

        var domainEvent = new PaymentCompletedEvent(
            Guid.NewGuid(), Guid.NewGuid(), null, "anon@test.com",
            "pi_test", 50m, 1, DateTime.UtcNow);

        var task = handler.Handle(new DomainEventNotification<PaymentCompletedEvent>(domainEvent), CancellationToken.None);
        task.IsCompleted.Should().BeTrue();
        await task;
        await Task.Delay(50);

        _mockScopeFactory.Verify(f => f.CreateScope(), Times.Never,
            "scope must not be created when there is no UserId");
    }

    [Fact]
    public async Task PaymentCompleted_Handle_UserNotFound_LogsWarning()
    {
        var handler = new PaymentCompletedWhatsAppHandler(
            _mockScopeFactory.Object, CreateLogger<PaymentCompletedWhatsAppHandler>().Object);

        var userId = Guid.NewGuid();
        _mockIdentityQueries.Setup(r => r.GetUserByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserSummaryDto?)null);

        var domainEvent = new PaymentCompletedEvent(
            Guid.NewGuid(), Guid.NewGuid(), userId, "user@test.com",
            "pi_test", 50m, 1, DateTime.UtcNow);

        await handler.Handle(new DomainEventNotification<PaymentCompletedEvent>(domainEvent), CancellationToken.None);
        await Task.Delay(2000);

        _mockWhatsAppService.Verify(
            s => s.SendTemplateMessageAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<Dictionary<string, string>>(),
                It.IsAny<WhatsAppNotificationType>(), It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task PaymentCompleted_Handle_EventNotFound_LogsWarning()
    {
        var handler = new PaymentCompletedWhatsAppHandler(
            _mockScopeFactory.Object, CreateLogger<PaymentCompletedWhatsAppHandler>().Object);

        var userId = Guid.NewGuid();
        var eventId = Guid.NewGuid();

        _mockIdentityQueries.Setup(r => r.GetUserByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateRealUser(userId));
        _mockEventRepo.Setup(r => r.GetByIdAsync(eventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Event?)null);

        var domainEvent = new PaymentCompletedEvent(
            eventId, Guid.NewGuid(), userId, "user@test.com",
            "pi_test", 50m, 1, DateTime.UtcNow);

        await handler.Handle(new DomainEventNotification<PaymentCompletedEvent>(domainEvent), CancellationToken.None);
        await Task.Delay(2000);

        _mockWhatsAppService.Verify(
            s => s.SendTemplateMessageAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<Dictionary<string, string>>(),
                It.IsAny<WhatsAppNotificationType>(), It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task PaymentCompleted_Handle_ValidData_SendsWhatsApp()
    {
        var handler = new PaymentCompletedWhatsAppHandler(
            _mockScopeFactory.Object, CreateLogger<PaymentCompletedWhatsAppHandler>().Object);

        var userId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        var registrationId = Guid.NewGuid();

        _mockIdentityQueries.Setup(r => r.GetUserByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateRealUser(userId));
        _mockEventRepo.Setup(r => r.GetByIdAsync(eventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateRealEvent(eventId));

        var domainEvent = new PaymentCompletedEvent(
            eventId, registrationId, userId, "user@test.com",
            "pi_test", 75m, 2, DateTime.UtcNow);

        await handler.Handle(new DomainEventNotification<PaymentCompletedEvent>(domainEvent), CancellationToken.None);
        await Task.Delay(2000);

        _mockWhatsAppService.Verify(
            s => s.SendTemplateMessageAsync(userId, It.IsAny<string>(), It.IsAny<Dictionary<string, string>>(),
                WhatsAppNotificationType.Payment, eventId, registrationId, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // 3. EventCancelledWhatsAppHandler
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task EventCancelled_Handle_ReturnsImmediately()
    {
        var handler = new EventCancelledWhatsAppHandler(
            _mockScopeFactory.Object, CreateLogger<EventCancelledWhatsAppHandler>().Object);

        var domainEvent = new EventCancelledEvent(Guid.NewGuid(), "Venue issue", DateTime.UtcNow);
        var task = handler.Handle(new DomainEventNotification<EventCancelledEvent>(domainEvent), CancellationToken.None);
        task.IsCompleted.Should().BeTrue();
        await task;
    }

    [Fact]
    public async Task EventCancelled_Handle_EventNotFound_LogsWarning()
    {
        var handler = new EventCancelledWhatsAppHandler(
            _mockScopeFactory.Object, CreateLogger<EventCancelledWhatsAppHandler>().Object);

        var eventId = Guid.NewGuid();
        _mockEventRepo.Setup(r => r.GetByIdAsync(eventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Event?)null);

        var domainEvent = new EventCancelledEvent(eventId, "Venue issue", DateTime.UtcNow);
        await handler.Handle(new DomainEventNotification<EventCancelledEvent>(domainEvent), CancellationToken.None);
        await Task.Delay(2000);

        _mockWhatsAppService.Verify(
            s => s.BroadcastToEventAttendeesAsync(It.IsAny<Guid>(), It.IsAny<string>(),
                It.IsAny<Dictionary<string, string>>(), It.IsAny<WhatsAppNotificationType>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task EventCancelled_Handle_ValidEvent_BroadcastsToAttendees()
    {
        var handler = new EventCancelledWhatsAppHandler(
            _mockScopeFactory.Object, CreateLogger<EventCancelledWhatsAppHandler>().Object);

        var eventId = Guid.NewGuid();
        _mockEventRepo.Setup(r => r.GetByIdAsync(eventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateRealEvent(eventId));

        var domainEvent = new EventCancelledEvent(eventId, "Venue issue", DateTime.UtcNow);
        await handler.Handle(new DomainEventNotification<EventCancelledEvent>(domainEvent), CancellationToken.None);
        await Task.Delay(2000);

        _mockWhatsAppService.Verify(
            s => s.BroadcastToEventAttendeesAsync(eventId, It.IsAny<string>(),
                It.IsAny<Dictionary<string, string>>(), WhatsAppNotificationType.EventCancellation,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // 4. RegistrationCancelledWhatsAppHandler
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task RegistrationCancelled_Handle_ReturnsImmediately()
    {
        var handler = new RegistrationCancelledWhatsAppHandler(
            _mockScopeFactory.Object, CreateLogger<RegistrationCancelledWhatsAppHandler>().Object);

        var domainEvent = new RegistrationCancelledEvent(Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow);
        var task = handler.Handle(new DomainEventNotification<RegistrationCancelledEvent>(domainEvent), CancellationToken.None);
        task.IsCompleted.Should().BeTrue();
        await task;
    }

    [Fact]
    public async Task RegistrationCancelled_Handle_UserNotFound_LogsWarning()
    {
        var handler = new RegistrationCancelledWhatsAppHandler(
            _mockScopeFactory.Object, CreateLogger<RegistrationCancelledWhatsAppHandler>().Object);

        var attendeeId = Guid.NewGuid();
        _mockIdentityQueries.Setup(r => r.GetUserByIdAsync(attendeeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserSummaryDto?)null);

        var domainEvent = new RegistrationCancelledEvent(Guid.NewGuid(), attendeeId, DateTime.UtcNow);
        await handler.Handle(new DomainEventNotification<RegistrationCancelledEvent>(domainEvent), CancellationToken.None);
        await Task.Delay(2000);

        _mockWhatsAppService.Verify(
            s => s.SendTemplateMessageAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<Dictionary<string, string>>(),
                It.IsAny<WhatsAppNotificationType>(), It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task RegistrationCancelled_Handle_EventNotFound_LogsWarning()
    {
        var handler = new RegistrationCancelledWhatsAppHandler(
            _mockScopeFactory.Object, CreateLogger<RegistrationCancelledWhatsAppHandler>().Object);

        var attendeeId = Guid.NewGuid();
        var eventId = Guid.NewGuid();

        _mockIdentityQueries.Setup(r => r.GetUserByIdAsync(attendeeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateRealUser(attendeeId));
        _mockEventRepo.Setup(r => r.GetByIdAsync(eventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Event?)null);

        var domainEvent = new RegistrationCancelledEvent(eventId, attendeeId, DateTime.UtcNow);
        await handler.Handle(new DomainEventNotification<RegistrationCancelledEvent>(domainEvent), CancellationToken.None);
        await Task.Delay(2000);

        _mockWhatsAppService.Verify(
            s => s.SendTemplateMessageAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<Dictionary<string, string>>(),
                It.IsAny<WhatsAppNotificationType>(), It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task RegistrationCancelled_Handle_ValidData_SendsWhatsApp()
    {
        var handler = new RegistrationCancelledWhatsAppHandler(
            _mockScopeFactory.Object, CreateLogger<RegistrationCancelledWhatsAppHandler>().Object);

        var attendeeId = Guid.NewGuid();
        var eventId = Guid.NewGuid();

        _mockIdentityQueries.Setup(r => r.GetUserByIdAsync(attendeeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateRealUser(attendeeId));
        _mockEventRepo.Setup(r => r.GetByIdAsync(eventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateRealEvent(eventId));

        var domainEvent = new RegistrationCancelledEvent(eventId, attendeeId, DateTime.UtcNow);
        await handler.Handle(new DomainEventNotification<RegistrationCancelledEvent>(domainEvent), CancellationToken.None);
        await Task.Delay(3000); // Increased for CI runner stability (fire-and-forget Task.Run)

        _mockWhatsAppService.Verify(
            s => s.SendTemplateMessageAsync(attendeeId, It.IsAny<string>(), It.IsAny<Dictionary<string, string>>(),
                WhatsAppNotificationType.EventRegistration, eventId, It.IsAny<Guid?>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // 5. UserCommittedToSignUpWhatsAppHandler
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task UserCommittedToSignUp_Handle_ReturnsImmediately()
    {
        var handler = new UserCommittedToSignUpWhatsAppHandler(
            _mockScopeFactory.Object, CreateLogger<UserCommittedToSignUpWhatsAppHandler>().Object);

        var domainEvent = new UserCommittedToSignUpEvent(
            Guid.NewGuid(), Guid.NewGuid(), "Food plates", 5, null, DateTime.UtcNow);
        var task = handler.Handle(new DomainEventNotification<UserCommittedToSignUpEvent>(domainEvent), CancellationToken.None);
        task.IsCompleted.Should().BeTrue();
        await task;
    }

    [Fact]
    public async Task UserCommittedToSignUp_Handle_UserNotFound_LogsWarning()
    {
        var handler = new UserCommittedToSignUpWhatsAppHandler(
            _mockScopeFactory.Object, CreateLogger<UserCommittedToSignUpWhatsAppHandler>().Object);

        var userId = Guid.NewGuid();
        _mockIdentityQueries.Setup(r => r.GetUserByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserSummaryDto?)null);

        var domainEvent = new UserCommittedToSignUpEvent(
            Guid.NewGuid(), userId, "Food plates", 5, null, DateTime.UtcNow);
        await handler.Handle(new DomainEventNotification<UserCommittedToSignUpEvent>(domainEvent), CancellationToken.None);
        await Task.Delay(2000);

        _mockWhatsAppService.Verify(
            s => s.SendTemplateMessageAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<Dictionary<string, string>>(),
                It.IsAny<WhatsAppNotificationType>(), It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task UserCommittedToSignUp_Handle_EventNotFound_LogsWarning()
    {
        var handler = new UserCommittedToSignUpWhatsAppHandler(
            _mockScopeFactory.Object, CreateLogger<UserCommittedToSignUpWhatsAppHandler>().Object);

        var userId = Guid.NewGuid();
        var signUpListId = Guid.NewGuid();

        _mockIdentityQueries.Setup(r => r.GetUserByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateRealUser(userId));
        _mockEventRepo.Setup(r => r.GetEventBySignUpListIdAsync(signUpListId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Event?)null);

        var domainEvent = new UserCommittedToSignUpEvent(
            signUpListId, userId, "Food plates", 5, null, DateTime.UtcNow);
        await handler.Handle(new DomainEventNotification<UserCommittedToSignUpEvent>(domainEvent), CancellationToken.None);
        await Task.Delay(2000);

        _mockWhatsAppService.Verify(
            s => s.SendTemplateMessageAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<Dictionary<string, string>>(),
                It.IsAny<WhatsAppNotificationType>(), It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task UserCommittedToSignUp_Handle_SlotsBased_SendsWithSlotCount()
    {
        var handler = new UserCommittedToSignUpWhatsAppHandler(
            _mockScopeFactory.Object, CreateLogger<UserCommittedToSignUpWhatsAppHandler>().Object);

        var userId = Guid.NewGuid();
        var signUpListId = Guid.NewGuid();
        var eventId = Guid.NewGuid();

        _mockIdentityQueries.Setup(r => r.GetUserByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateRealUser(userId));

        _mockEventRepo.Setup(r => r.GetEventBySignUpListIdAsync(signUpListId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateRealEvent(eventId));

        // PhysicalQuantity=null, SlotsClaimed=3 — slot-based item
        var domainEvent = new UserCommittedToSignUpEvent(
            signUpListId, userId, "Volunteer slot", null, 3, DateTime.UtcNow);
        await handler.Handle(new DomainEventNotification<UserCommittedToSignUpEvent>(domainEvent), CancellationToken.None);
        await Task.Delay(2000);

        _mockWhatsAppService.Verify(
            s => s.SendTemplateMessageAsync(userId, It.IsAny<string>(),
                It.Is<Dictionary<string, string>>(d => d.ContainsValue("3")),
                WhatsAppNotificationType.SignupCommitment, eventId, It.IsAny<Guid?>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // 6. CommitmentUpdatedWhatsAppHandler
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task CommitmentUpdated_Handle_ReturnsImmediately()
    {
        var handler = new CommitmentUpdatedWhatsAppHandler(
            _mockScopeFactory.Object, CreateLogger<CommitmentUpdatedWhatsAppHandler>().Object);

        var domainEvent = new CommitmentUpdatedEvent(
            Guid.NewGuid(), Guid.NewGuid(), 5, 8, null, null, "Food plates", DateTime.UtcNow);
        var task = handler.Handle(new DomainEventNotification<CommitmentUpdatedEvent>(domainEvent), CancellationToken.None);
        task.IsCompleted.Should().BeTrue();
        await task;
    }

    [Fact]
    public async Task CommitmentUpdated_Handle_UserNotFound_LogsWarning()
    {
        var handler = new CommitmentUpdatedWhatsAppHandler(
            _mockScopeFactory.Object, CreateLogger<CommitmentUpdatedWhatsAppHandler>().Object);

        var userId = Guid.NewGuid();
        _mockIdentityQueries.Setup(r => r.GetUserByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserSummaryDto?)null);

        var domainEvent = new CommitmentUpdatedEvent(
            Guid.NewGuid(), userId, 5, 8, null, null, "Food plates", DateTime.UtcNow);
        await handler.Handle(new DomainEventNotification<CommitmentUpdatedEvent>(domainEvent), CancellationToken.None);
        await Task.Delay(2000);

        _mockWhatsAppService.Verify(
            s => s.SendTemplateMessageAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<Dictionary<string, string>>(),
                It.IsAny<WhatsAppNotificationType>(), It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task CommitmentUpdated_Handle_EventNotFound_LogsWarning()
    {
        var handler = new CommitmentUpdatedWhatsAppHandler(
            _mockScopeFactory.Object, CreateLogger<CommitmentUpdatedWhatsAppHandler>().Object);

        var userId = Guid.NewGuid();
        var signUpItemId = Guid.NewGuid();

        _mockIdentityQueries.Setup(r => r.GetUserByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateRealUser(userId));
        _mockEventRepo.Setup(r => r.GetEventBySignUpItemIdAsync(signUpItemId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Event?)null);

        var domainEvent = new CommitmentUpdatedEvent(
            signUpItemId, userId, 5, 8, null, null, "Food plates", DateTime.UtcNow);
        await handler.Handle(new DomainEventNotification<CommitmentUpdatedEvent>(domainEvent), CancellationToken.None);
        await Task.Delay(2000);

        _mockWhatsAppService.Verify(
            s => s.SendTemplateMessageAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<Dictionary<string, string>>(),
                It.IsAny<WhatsAppNotificationType>(), It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task CommitmentUpdated_Handle_ValidData_SendsWhatsApp()
    {
        var handler = new CommitmentUpdatedWhatsAppHandler(
            _mockScopeFactory.Object, CreateLogger<CommitmentUpdatedWhatsAppHandler>().Object);

        var userId = Guid.NewGuid();
        var signUpItemId = Guid.NewGuid();
        var eventId = Guid.NewGuid();

        _mockIdentityQueries.Setup(r => r.GetUserByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateRealUser(userId));
        _mockEventRepo.Setup(r => r.GetEventBySignUpItemIdAsync(signUpItemId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateRealEvent(eventId));

        var domainEvent = new CommitmentUpdatedEvent(
            signUpItemId, userId, 5, 8, null, null, "Food plates", DateTime.UtcNow);
        await handler.Handle(new DomainEventNotification<CommitmentUpdatedEvent>(domainEvent), CancellationToken.None);
        await Task.Delay(2000);

        _mockWhatsAppService.Verify(
            s => s.SendTemplateMessageAsync(userId, It.IsAny<string>(), It.IsAny<Dictionary<string, string>>(),
                WhatsAppNotificationType.SignupCommitment, It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // 7. CommitmentCancelledWhatsAppHandler
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task CommitmentCancelled_Handle_ReturnsImmediately()
    {
        var handler = new CommitmentCancelledWhatsAppHandler(
            _mockScopeFactory.Object, CreateLogger<CommitmentCancelledWhatsAppHandler>().Object);

        var domainEvent = new CommitmentCancelledEvent(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Food plates", 5, null);
        var task = handler.Handle(new DomainEventNotification<CommitmentCancelledEvent>(domainEvent), CancellationToken.None);
        task.IsCompleted.Should().BeTrue();
        await task;
    }

    [Fact]
    public async Task CommitmentCancelled_Handle_UserNotFound_LogsWarning()
    {
        var handler = new CommitmentCancelledWhatsAppHandler(
            _mockScopeFactory.Object, CreateLogger<CommitmentCancelledWhatsAppHandler>().Object);

        var userId = Guid.NewGuid();
        _mockIdentityQueries.Setup(r => r.GetUserByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserSummaryDto?)null);

        var domainEvent = new CommitmentCancelledEvent(
            Guid.NewGuid(), Guid.NewGuid(), userId, Guid.NewGuid(), "Food plates", 5, null);
        await handler.Handle(new DomainEventNotification<CommitmentCancelledEvent>(domainEvent), CancellationToken.None);
        await Task.Delay(2000);

        _mockWhatsAppService.Verify(
            s => s.SendTemplateMessageAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<Dictionary<string, string>>(),
                It.IsAny<WhatsAppNotificationType>(), It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task CommitmentCancelled_Handle_EventNotFound_LogsWarning()
    {
        var handler = new CommitmentCancelledWhatsAppHandler(
            _mockScopeFactory.Object, CreateLogger<CommitmentCancelledWhatsAppHandler>().Object);

        var userId = Guid.NewGuid();
        var signUpListId = Guid.NewGuid();

        _mockIdentityQueries.Setup(r => r.GetUserByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateRealUser(userId));
        _mockEventRepo.Setup(r => r.GetEventBySignUpListIdAsync(signUpListId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Event?)null);

        var domainEvent = new CommitmentCancelledEvent(
            Guid.NewGuid(), Guid.NewGuid(), userId, signUpListId, "Food plates", 5, null);
        await handler.Handle(new DomainEventNotification<CommitmentCancelledEvent>(domainEvent), CancellationToken.None);
        await Task.Delay(2000);

        _mockWhatsAppService.Verify(
            s => s.SendTemplateMessageAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<Dictionary<string, string>>(),
                It.IsAny<WhatsAppNotificationType>(), It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task CommitmentCancelled_Handle_ValidData_SendsWhatsApp()
    {
        var handler = new CommitmentCancelledWhatsAppHandler(
            _mockScopeFactory.Object, CreateLogger<CommitmentCancelledWhatsAppHandler>().Object);

        var userId = Guid.NewGuid();
        var signUpListId = Guid.NewGuid();
        var eventId = Guid.NewGuid();

        _mockIdentityQueries.Setup(r => r.GetUserByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateRealUser(userId));
        _mockEventRepo.Setup(r => r.GetEventBySignUpListIdAsync(signUpListId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateRealEvent(eventId));

        var domainEvent = new CommitmentCancelledEvent(
            Guid.NewGuid(), Guid.NewGuid(), userId, signUpListId, "Food plates", 5, null);
        await handler.Handle(new DomainEventNotification<CommitmentCancelledEvent>(domainEvent), CancellationToken.None);
        await Task.Delay(2000);

        _mockWhatsAppService.Verify(
            s => s.SendTemplateMessageAsync(userId, It.IsAny<string>(), It.IsAny<Dictionary<string, string>>(),
                WhatsAppNotificationType.SignupCommitment, It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // 8. RefundRequestedWhatsAppHandler
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task RefundRequested_Handle_NoUserId_SkipsForAnonymous()
    {
        var handler = new RefundRequestedWhatsAppHandler(
            _mockScopeFactory.Object, CreateLogger<RefundRequestedWhatsAppHandler>().Object);

        var domainEvent = new RefundRequestedEvent(
            Guid.NewGuid(), Guid.NewGuid(), null, "anon@test.com",
            "pi_test", 50m, DateTime.UtcNow, 0m);

        var task = handler.Handle(new DomainEventNotification<RefundRequestedEvent>(domainEvent), CancellationToken.None);
        task.IsCompleted.Should().BeTrue();
        await task;
        await Task.Delay(50);

        _mockScopeFactory.Verify(f => f.CreateScope(), Times.Never);
    }

    [Fact]
    public async Task RefundRequested_Handle_UserNotFound_LogsWarning()
    {
        var handler = new RefundRequestedWhatsAppHandler(
            _mockScopeFactory.Object, CreateLogger<RefundRequestedWhatsAppHandler>().Object);

        var userId = Guid.NewGuid();
        _mockIdentityQueries.Setup(r => r.GetUserByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserSummaryDto?)null);

        var domainEvent = new RefundRequestedEvent(
            Guid.NewGuid(), Guid.NewGuid(), userId, "user@test.com",
            "pi_test", 50m, DateTime.UtcNow, 0m);

        await handler.Handle(new DomainEventNotification<RefundRequestedEvent>(domainEvent), CancellationToken.None);
        await Task.Delay(2000);

        _mockWhatsAppService.Verify(
            s => s.SendTemplateMessageAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<Dictionary<string, string>>(),
                It.IsAny<WhatsAppNotificationType>(), It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task RefundRequested_Handle_ValidData_SendsWhatsApp()
    {
        var handler = new RefundRequestedWhatsAppHandler(
            _mockScopeFactory.Object, CreateLogger<RefundRequestedWhatsAppHandler>().Object);

        var userId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        var registrationId = Guid.NewGuid();

        _mockIdentityQueries.Setup(r => r.GetUserByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateRealUser(userId));
        _mockEventRepo.Setup(r => r.GetByIdAsync(eventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateRealEvent(eventId));

        var domainEvent = new RefundRequestedEvent(
            eventId, registrationId, userId, "user@test.com",
            "pi_test", 75m, DateTime.UtcNow, 10m);

        await handler.Handle(new DomainEventNotification<RefundRequestedEvent>(domainEvent), CancellationToken.None);
        await Task.Delay(2000);

        _mockWhatsAppService.Verify(
            s => s.SendTemplateMessageAsync(userId, It.IsAny<string>(), It.IsAny<Dictionary<string, string>>(),
                WhatsAppNotificationType.Refund, eventId, registrationId, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task RefundRequested_Handle_EventNotFound_StillSendsWithFallbackTitle()
    {
        // RefundRequestedHandler does NOT bail when event is null; it uses "Event" as fallback title
        var handler = new RefundRequestedWhatsAppHandler(
            _mockScopeFactory.Object, CreateLogger<RefundRequestedWhatsAppHandler>().Object);

        var userId = Guid.NewGuid();
        var eventId = Guid.NewGuid();

        _mockIdentityQueries.Setup(r => r.GetUserByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateRealUser(userId));
        _mockEventRepo.Setup(r => r.GetByIdAsync(eventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Event?)null);

        var domainEvent = new RefundRequestedEvent(
            eventId, Guid.NewGuid(), userId, "user@test.com",
            "pi_test", 50m, DateTime.UtcNow, 0m);

        await handler.Handle(new DomainEventNotification<RefundRequestedEvent>(domainEvent), CancellationToken.None);
        await Task.Delay(2000);

        // Handler still calls SendTemplateMessageAsync (uses null-coalescing fallback title)
        _mockWhatsAppService.Verify(
            s => s.SendTemplateMessageAsync(userId, It.IsAny<string>(),
                It.Is<Dictionary<string, string>>(d => d.ContainsValue("Event")),
                WhatsAppNotificationType.Refund, eventId, It.IsAny<Guid?>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // 9. RefundCompletedWhatsAppHandler
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task RefundCompleted_Handle_NoUserId_SkipsForAnonymous()
    {
        var handler = new RefundCompletedWhatsAppHandler(
            _mockScopeFactory.Object, CreateLogger<RefundCompletedWhatsAppHandler>().Object);

        var domainEvent = new RefundCompletedEvent(
            Guid.NewGuid(), Guid.NewGuid(), null, "anon@test.com",
            "re_test_refund", 50m, DateTime.UtcNow, 0m);

        var task = handler.Handle(new DomainEventNotification<RefundCompletedEvent>(domainEvent), CancellationToken.None);
        task.IsCompleted.Should().BeTrue();
        await task;
        await Task.Delay(50);

        _mockScopeFactory.Verify(f => f.CreateScope(), Times.Never);
    }

    [Fact]
    public async Task RefundCompleted_Handle_UserNotFound_LogsWarning()
    {
        var handler = new RefundCompletedWhatsAppHandler(
            _mockScopeFactory.Object, CreateLogger<RefundCompletedWhatsAppHandler>().Object);

        var userId = Guid.NewGuid();
        _mockIdentityQueries.Setup(r => r.GetUserByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserSummaryDto?)null);

        var domainEvent = new RefundCompletedEvent(
            Guid.NewGuid(), Guid.NewGuid(), userId, "user@test.com",
            "re_test", 50m, DateTime.UtcNow, 0m);

        await handler.Handle(new DomainEventNotification<RefundCompletedEvent>(domainEvent), CancellationToken.None);
        await Task.Delay(2000);

        _mockWhatsAppService.Verify(
            s => s.SendTemplateMessageAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<Dictionary<string, string>>(),
                It.IsAny<WhatsAppNotificationType>(), It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task RefundCompleted_Handle_ValidData_SendsWhatsApp()
    {
        var handler = new RefundCompletedWhatsAppHandler(
            _mockScopeFactory.Object, CreateLogger<RefundCompletedWhatsAppHandler>().Object);

        var userId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        var registrationId = Guid.NewGuid();

        _mockIdentityQueries.Setup(r => r.GetUserByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateRealUser(userId));
        _mockEventRepo.Setup(r => r.GetByIdAsync(eventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateRealEvent(eventId));

        var domainEvent = new RefundCompletedEvent(
            eventId, registrationId, userId, "user@test.com",
            "re_completed", 75m, DateTime.UtcNow, 10m);

        await handler.Handle(new DomainEventNotification<RefundCompletedEvent>(domainEvent), CancellationToken.None);
        await Task.Delay(2000);

        _mockWhatsAppService.Verify(
            s => s.SendTemplateMessageAsync(userId, It.IsAny<string>(), It.IsAny<Dictionary<string, string>>(),
                WhatsAppNotificationType.Refund, eventId, registrationId, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // 10. EventPublishedWhatsAppHandler
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task EventPublished_Handle_ReturnsImmediately()
    {
        var handler = new EventPublishedWhatsAppHandler(
            _mockScopeFactory.Object, CreateLogger<EventPublishedWhatsAppHandler>().Object);

        var domainEvent = new EventPublishedEvent(Guid.NewGuid(), DateTime.UtcNow, Guid.NewGuid());
        var task = handler.Handle(new DomainEventNotification<EventPublishedEvent>(domainEvent), CancellationToken.None);
        task.IsCompleted.Should().BeTrue();
        await task;
    }

    [Fact]
    public async Task EventPublished_Handle_EventNotFound_LogsWarning()
    {
        var handler = new EventPublishedWhatsAppHandler(
            _mockScopeFactory.Object, CreateLogger<EventPublishedWhatsAppHandler>().Object);

        var eventId = Guid.NewGuid();
        _mockEventRepo.Setup(r => r.GetByIdAsync(eventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Event?)null);

        var domainEvent = new EventPublishedEvent(eventId, DateTime.UtcNow, Guid.NewGuid());
        await handler.Handle(new DomainEventNotification<EventPublishedEvent>(domainEvent), CancellationToken.None);
        await Task.Delay(2000);

        _mockWhatsAppService.Verify(
            s => s.BroadcastToEventAttendeesAsync(It.IsAny<Guid>(), It.IsAny<string>(),
                It.IsAny<Dictionary<string, string>>(), It.IsAny<WhatsAppNotificationType>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task EventPublished_Handle_ValidEvent_BroadcastsToAllOptedInUsers()
    {
        var handler = new EventPublishedWhatsAppHandler(
            _mockScopeFactory.Object, CreateLogger<EventPublishedWhatsAppHandler>().Object);

        var eventId = Guid.NewGuid();
        _mockEventRepo.Setup(r => r.GetByIdAsync(eventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateRealEvent(eventId));

        var domainEvent = new EventPublishedEvent(eventId, DateTime.UtcNow, Guid.NewGuid());
        await handler.Handle(new DomainEventNotification<EventPublishedEvent>(domainEvent), CancellationToken.None);
        await Task.Delay(2000);

        _mockWhatsAppService.Verify(
            s => s.BroadcastToEventAttendeesAsync(eventId, It.IsAny<string>(),
                It.IsAny<Dictionary<string, string>>(), WhatsAppNotificationType.NewEvent,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // 11. AnonymousRegistrationWhatsAppHandler
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task AnonymousRegistration_Handle_ReturnsImmediately()
    {
        var handler = new AnonymousRegistrationWhatsAppHandler(
            _mockScopeFactory.Object, CreateLogger<AnonymousRegistrationWhatsAppHandler>().Object);

        var domainEvent = new AnonymousRegistrationConfirmedEvent(
            Guid.NewGuid(), "guest@test.com", 2, DateTime.UtcNow);
        var task = handler.Handle(new DomainEventNotification<AnonymousRegistrationConfirmedEvent>(domainEvent), CancellationToken.None);
        task.IsCompleted.Should().BeTrue();
        await task;
    }

    [Fact]
    public async Task AnonymousRegistration_Handle_EventNotFound_LogsWarning()
    {
        var handler = new AnonymousRegistrationWhatsAppHandler(
            _mockScopeFactory.Object, CreateLogger<AnonymousRegistrationWhatsAppHandler>().Object);

        var eventId = Guid.NewGuid();
        _mockEventRepo.Setup(r => r.GetByIdAsync(eventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Event?)null);

        var domainEvent = new AnonymousRegistrationConfirmedEvent(
            eventId, "guest@test.com", 2, DateTime.UtcNow);
        await handler.Handle(new DomainEventNotification<AnonymousRegistrationConfirmedEvent>(domainEvent), CancellationToken.None);
        await Task.Delay(2000);

        _mockWhatsAppService.Verify(
            s => s.SendTemplateMessageToPhoneAsync(It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<Dictionary<string, string>>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task AnonymousRegistration_Handle_NoPhoneNumber_SkipsSend()
    {
        var handler = new AnonymousRegistrationWhatsAppHandler(
            _mockScopeFactory.Object, CreateLogger<AnonymousRegistrationWhatsAppHandler>().Object);

        var eventId = Guid.NewGuid();
        var email = "guest@test.com";

        _mockEventRepo.Setup(r => r.GetByIdAsync(eventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateRealEvent(eventId));

        // Create a real Registration with no Contact (Contact = null → skip path)
        var registration = CreateRealRegistrationWithNoContact(eventId);
        _mockRegistrationRepo
            .Setup(r => r.GetAnonymousByEventAndEmailAsync(eventId, email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(registration);

        var domainEvent = new AnonymousRegistrationConfirmedEvent(eventId, email, 2, DateTime.UtcNow);
        await handler.Handle(new DomainEventNotification<AnonymousRegistrationConfirmedEvent>(domainEvent), CancellationToken.None);
        await Task.Delay(2000);

        _mockWhatsAppService.Verify(
            s => s.SendTemplateMessageToPhoneAsync(It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<Dictionary<string, string>>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task AnonymousRegistration_Handle_WithWhatsAppOptIn_SendsToWhatsAppPhone()
    {
        var handler = new AnonymousRegistrationWhatsAppHandler(
            _mockScopeFactory.Object, CreateLogger<AnonymousRegistrationWhatsAppHandler>().Object);

        var eventId = Guid.NewGuid();
        var email = "guest@test.com";
        var phoneNumber = "+14155552671";
        var whatsAppPhone = "+14155559999";

        _mockEventRepo.Setup(r => r.GetByIdAsync(eventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateRealEvent(eventId));

        // Phase 7A.6D: Create registration with WhatsApp opt-in and WhatsApp phone
        var registration = CreateRealRegistrationWithPhone(eventId, email, phoneNumber,
            whatsAppPhoneNumber: whatsAppPhone, whatsAppOptedIn: true);
        _mockRegistrationRepo
            .Setup(r => r.GetAnonymousByEventAndEmailAsync(eventId, email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(registration);

        var domainEvent = new AnonymousRegistrationConfirmedEvent(eventId, email, 2, DateTime.UtcNow);
        await handler.Handle(new DomainEventNotification<AnonymousRegistrationConfirmedEvent>(domainEvent), CancellationToken.None);
        await Task.Delay(2000);

        // Should send to the WhatsApp-specific phone, not the general contact phone
        _mockWhatsAppService.Verify(
            s => s.SendTemplateMessageToPhoneAsync(whatsAppPhone, It.IsAny<string>(),
                It.IsAny<Dictionary<string, string>>(), eventId, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task AnonymousRegistration_Handle_WithPhoneButNoWhatsAppOptIn_SkipsSend()
    {
        var handler = new AnonymousRegistrationWhatsAppHandler(
            _mockScopeFactory.Object, CreateLogger<AnonymousRegistrationWhatsAppHandler>().Object);

        var eventId = Guid.NewGuid();
        var email = "guest@test.com";
        var phoneNumber = "+14155552671";

        _mockEventRepo.Setup(r => r.GetByIdAsync(eventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateRealEvent(eventId));

        // Phase 7A.6D: Registration with phone but NO WhatsApp opt-in should be skipped
        var registration = CreateRealRegistrationWithPhone(eventId, email, phoneNumber);
        _mockRegistrationRepo
            .Setup(r => r.GetAnonymousByEventAndEmailAsync(eventId, email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(registration);

        var domainEvent = new AnonymousRegistrationConfirmedEvent(eventId, email, 2, DateTime.UtcNow);
        await handler.Handle(new DomainEventNotification<AnonymousRegistrationConfirmedEvent>(domainEvent), CancellationToken.None);
        await Task.Delay(2000);

        _mockWhatsAppService.Verify(
            s => s.SendTemplateMessageToPhoneAsync(It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<Dictionary<string, string>>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task AnonymousRegistration_Handle_RegistrationNotFound_SkipsSend()
    {
        var handler = new AnonymousRegistrationWhatsAppHandler(
            _mockScopeFactory.Object, CreateLogger<AnonymousRegistrationWhatsAppHandler>().Object);

        var eventId = Guid.NewGuid();
        var email = "ghost@test.com";

        _mockEventRepo.Setup(r => r.GetByIdAsync(eventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateRealEvent(eventId));
        _mockRegistrationRepo
            .Setup(r => r.GetAnonymousByEventAndEmailAsync(eventId, email, It.IsAny<CancellationToken>()))
            .ReturnsAsync((LankaConnect.Products.LankaEvents.Domain.Registration?)null);

        var domainEvent = new AnonymousRegistrationConfirmedEvent(eventId, email, 1, DateTime.UtcNow);
        await handler.Handle(new DomainEventNotification<AnonymousRegistrationConfirmedEvent>(domainEvent), CancellationToken.None);
        await Task.Delay(2000);

        _mockWhatsAppService.Verify(
            s => s.SendTemplateMessageToPhoneAsync(It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<Dictionary<string, string>>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // ─── Cross-cutting: exception resilience ──────────────────────────────────

    [Fact]
    public async Task RegistrationConfirmed_Handle_WhatsAppServiceThrows_DoesNotPropagateException()
    {
        var handler = new RegistrationConfirmedWhatsAppHandler(
            _mockScopeFactory.Object, CreateLogger<RegistrationConfirmedWhatsAppHandler>().Object);

        var attendeeId = Guid.NewGuid();
        var eventId = Guid.NewGuid();

        _mockIdentityQueries.Setup(r => r.GetUserByIdAsync(attendeeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateRealUser(attendeeId));
        _mockEventRepo.Setup(r => r.GetByIdAsync(eventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateRealEvent(eventId));
        _mockWhatsAppService
            .Setup(s => s.SendTemplateMessageAsync(
                It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<Dictionary<string, string>>(),
                It.IsAny<WhatsAppNotificationType>(), It.IsAny<Guid?>(), It.IsAny<Guid?>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("WhatsApp service unavailable"));

        var domainEvent = new RegistrationConfirmedEvent(eventId, attendeeId, 1, DateTime.UtcNow);
        var act = async () =>
        {
            await handler.Handle(new DomainEventNotification<RegistrationConfirmedEvent>(domainEvent), CancellationToken.None);
            await Task.Delay(200);
        };

        await act.Should().NotThrowAsync("exceptions inside Task.Run are caught by the handler's own try-catch");
    }

    [Fact]
    public async Task EventCancelled_Handle_BroadcastFails_DoesNotPropagateException()
    {
        var handler = new EventCancelledWhatsAppHandler(
            _mockScopeFactory.Object, CreateLogger<EventCancelledWhatsAppHandler>().Object);

        var eventId = Guid.NewGuid();
        _mockEventRepo.Setup(r => r.GetByIdAsync(eventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateRealEvent(eventId));
        _mockWhatsAppService
            .Setup(s => s.BroadcastToEventAttendeesAsync(
                It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<Dictionary<string, string>>(),
                It.IsAny<WhatsAppNotificationType>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new TimeoutException("Broadcast timed out"));

        var domainEvent = new EventCancelledEvent(eventId, "Venue closed", DateTime.UtcNow);
        var act = async () =>
        {
            await handler.Handle(new DomainEventNotification<EventCancelledEvent>(domainEvent), CancellationToken.None);
            await Task.Delay(200);
        };

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task PaymentCompleted_Handle_WhatsAppSkipped_DoesNotThrow()
    {
        var handler = new PaymentCompletedWhatsAppHandler(
            _mockScopeFactory.Object, CreateLogger<PaymentCompletedWhatsAppHandler>().Object);

        var userId = Guid.NewGuid();
        var eventId = Guid.NewGuid();

        _mockIdentityQueries.Setup(r => r.GetUserByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateRealUser(userId));
        _mockEventRepo.Setup(r => r.GetByIdAsync(eventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateRealEvent(eventId));

        // Service returns Skipped result (user opted out)
        _mockWhatsAppService
            .Setup(s => s.SendTemplateMessageAsync(
                It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<Dictionary<string, string>>(),
                It.IsAny<WhatsAppNotificationType>(), It.IsAny<Guid?>(), It.IsAny<Guid?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<WhatsAppSendResult>.Success(WhatsAppSendResult.Skipped("User opted out")));

        var domainEvent = new PaymentCompletedEvent(
            eventId, Guid.NewGuid(), userId, "user@test.com", "pi_test", 50m, 1, DateTime.UtcNow);

        var act = async () =>
        {
            await handler.Handle(new DomainEventNotification<PaymentCompletedEvent>(domainEvent), CancellationToken.None);
            await Task.Delay(200);
        };

        await act.Should().NotThrowAsync();
    }

    // ─── Helper: Create FormResponse via reflection ──────────────────────────

    private static FormResponse CreateRealFormResponse(
        Guid responseId, Guid eventFormId, Guid eventId,
        Guid? respondentUserId, string? respondentName = "Test Respondent")
    {
        var type = typeof(FormResponse);
        var ctor = type.GetConstructor(
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance,
            null, Type.EmptyTypes, null);
        var response = (FormResponse)ctor!.Invoke(null);
        SetEntityId(response, responseId);
        SetPrivateProperty(response, "EventFormId", eventFormId);
        SetPrivateProperty(response, "EventId", eventId);
        SetPrivateProperty(response, "RespondentUserId", respondentUserId);
        SetPrivateProperty(response, "RespondentName", respondentName);
        SetPrivateProperty(response, "RespondentEmail", respondentUserId.HasValue ? "respondent@test.com" : null);
        return response;
    }

    /// <summary>Creates a real Form via reflection.</summary>
    private static Form CreateRealEventForm(Guid formId, Guid eventId, string title = "Test Survey")
    {
        var type = typeof(Form);
        var ctor = type.GetConstructor(
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance,
            null, Type.EmptyTypes, null);
        var form = (Form)ctor!.Invoke(null);
        SetEntityId(form, formId);
        SetPrivateProperty(form, "EventId", eventId);
        SetPrivateProperty(form, "Title", title);
        return form;
    }

    /// <summary>Creates a real AddOnDefinition via reflection.</summary>
    private static AddOnDefinition CreateRealAddOnDefinition(Guid definitionId, Guid eventId, string name = "T-Shirt")
    {
        var type = typeof(AddOnDefinition);
        var ctor = type.GetConstructor(
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance,
            null, Type.EmptyTypes, null);
        var definition = (AddOnDefinition)ctor!.Invoke(null);
        SetEntityId(definition, definitionId);
        SetPrivateProperty(definition, "EventId", eventId);
        SetPrivateProperty(definition, "Name", name);
        return definition;
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // 12. EventApprovedWhatsAppHandler (Phase 7B.3)
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task EventApproved_Handle_ReturnsImmediately()
    {
        var handler = new EventApprovedWhatsAppHandler(
            _mockScopeFactory.Object, CreateLogger<EventApprovedWhatsAppHandler>().Object);

        var domainEvent = new EventApprovedEvent(Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow);
        var task = handler.Handle(new DomainEventNotification<EventApprovedEvent>(domainEvent), CancellationToken.None);
        task.IsCompleted.Should().BeTrue("fire-and-forget handler must return Task.CompletedTask synchronously");
        await task;
    }

    [Fact]
    public async Task EventApproved_Handle_EventNotFound_DoesNotSend()
    {
        var handler = new EventApprovedWhatsAppHandler(
            _mockScopeFactory.Object, CreateLogger<EventApprovedWhatsAppHandler>().Object);

        var eventId = Guid.NewGuid();
        _mockEventRepo.Setup(r => r.GetByIdAsync(eventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Event?)null);

        var domainEvent = new EventApprovedEvent(eventId, Guid.NewGuid(), DateTime.UtcNow);
        await handler.Handle(new DomainEventNotification<EventApprovedEvent>(domainEvent), CancellationToken.None);
        await Task.Delay(2000);

        _mockWhatsAppService.Verify(
            s => s.SendTemplateMessageAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<Dictionary<string, string>>(),
                It.IsAny<WhatsAppNotificationType>(), It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task EventApproved_Handle_ValidData_SendsWhatsApp()
    {
        var handler = new EventApprovedWhatsAppHandler(
            _mockScopeFactory.Object, CreateLogger<EventApprovedWhatsAppHandler>().Object);

        var eventId = Guid.NewGuid();
        var organizerId = Guid.NewGuid();
        var evt = CreateRealEvent(eventId);
        // Set OrganizerId via reflection (Event.Create uses the constructor param)
        SetPrivateProperty(evt, "OrganizerId", organizerId);

        _mockEventRepo.Setup(r => r.GetByIdAsync(eventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(evt);
        _mockIdentityQueries.Setup(r => r.GetUserByIdAsync(organizerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateRealUser(organizerId));

        var domainEvent = new EventApprovedEvent(eventId, Guid.NewGuid(), DateTime.UtcNow);
        await handler.Handle(new DomainEventNotification<EventApprovedEvent>(domainEvent), CancellationToken.None);
        await Task.Delay(2000);

        _mockWhatsAppService.Verify(
            s => s.SendTemplateMessageAsync(organizerId, It.IsAny<string>(), It.IsAny<Dictionary<string, string>>(),
                WhatsAppNotificationType.EventApproval, eventId, It.IsAny<Guid?>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // 13. EventRejectedWhatsAppHandler (Phase 7B.3)
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task EventRejected_Handle_ReturnsImmediately()
    {
        var handler = new EventRejectedWhatsAppHandler(
            _mockScopeFactory.Object, CreateLogger<EventRejectedWhatsAppHandler>().Object);

        var domainEvent = new EventRejectedEvent(Guid.NewGuid(), Guid.NewGuid(), "Policy violation", DateTime.UtcNow);
        var task = handler.Handle(new DomainEventNotification<EventRejectedEvent>(domainEvent), CancellationToken.None);
        task.IsCompleted.Should().BeTrue("fire-and-forget handler must return Task.CompletedTask synchronously");
        await task;
    }

    [Fact]
    public async Task EventRejected_Handle_ValidData_SendsWhatsApp()
    {
        var handler = new EventRejectedWhatsAppHandler(
            _mockScopeFactory.Object, CreateLogger<EventRejectedWhatsAppHandler>().Object);

        var eventId = Guid.NewGuid();
        var organizerId = Guid.NewGuid();
        var evt = CreateRealEvent(eventId);
        SetPrivateProperty(evt, "OrganizerId", organizerId);

        _mockEventRepo.Setup(r => r.GetByIdAsync(eventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(evt);
        _mockIdentityQueries.Setup(r => r.GetUserByIdAsync(organizerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateRealUser(organizerId));

        var domainEvent = new EventRejectedEvent(eventId, Guid.NewGuid(), "Policy violation", DateTime.UtcNow);
        await handler.Handle(new DomainEventNotification<EventRejectedEvent>(domainEvent), CancellationToken.None);
        await Task.Delay(2000);

        _mockWhatsAppService.Verify(
            s => s.SendTemplateMessageAsync(organizerId, It.IsAny<string>(), It.IsAny<Dictionary<string, string>>(),
                WhatsAppNotificationType.EventApproval, eventId, It.IsAny<Guid?>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // 14. DonationCompletedWhatsAppHandler (Phase 7B.3)
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task DonationCompleted_Handle_NoDonorUserId_ReturnsImmediately()
    {
        var handler = new DonationCompletedWhatsAppHandler(
            _mockScopeFactory.Object, CreateLogger<DonationCompletedWhatsAppHandler>().Object);

        var domainEvent = new DonationCompletedEvent(
            Guid.NewGuid(), Guid.NewGuid(), null, "Anonymous Donor", "anon@test.com",
            "pi_test", 100m, "USD", DateTime.UtcNow);

        var task = handler.Handle(new DomainEventNotification<DonationCompletedEvent>(domainEvent), CancellationToken.None);
        task.IsCompleted.Should().BeTrue();
        await task;
        await Task.Delay(50);

        _mockScopeFactory.Verify(f => f.CreateScope(), Times.Never,
            "scope must not be created when there is no DonorUserId");
    }

    [Fact]
    public async Task DonationCompleted_Handle_ValidData_SendsWhatsApp()
    {
        var handler = new DonationCompletedWhatsAppHandler(
            _mockScopeFactory.Object, CreateLogger<DonationCompletedWhatsAppHandler>().Object);

        var donorUserId = Guid.NewGuid();
        var eventId = Guid.NewGuid();

        _mockEventRepo.Setup(r => r.GetByIdAsync(eventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateRealEvent(eventId));

        var domainEvent = new DonationCompletedEvent(
            eventId, Guid.NewGuid(), donorUserId, "John Doe", "john@test.com",
            "pi_test", 50m, "USD", DateTime.UtcNow);

        await handler.Handle(new DomainEventNotification<DonationCompletedEvent>(domainEvent), CancellationToken.None);
        await Task.Delay(2000);

        _mockWhatsAppService.Verify(
            s => s.SendTemplateMessageAsync(donorUserId, It.IsAny<string>(), It.IsAny<Dictionary<string, string>>(),
                WhatsAppNotificationType.Donation, eventId, It.IsAny<Guid?>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // 15. CollectionCompletedWhatsAppHandler (Phase 7B.3)
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task CollectionCompleted_Handle_NoContributorUserId_ReturnsImmediately()
    {
        var handler = new CollectionCompletedWhatsAppHandler(
            _mockScopeFactory.Object, CreateLogger<CollectionCompletedWhatsAppHandler>().Object);

        var domainEvent = new CollectionCompletedEvent(
            Guid.NewGuid(), Guid.NewGuid(), null, "Anonymous Contributor", "anon@test.com",
            "pi_test", 75m, "USD", DateTime.UtcNow);

        var task = handler.Handle(new DomainEventNotification<CollectionCompletedEvent>(domainEvent), CancellationToken.None);
        task.IsCompleted.Should().BeTrue();
        await task;
        await Task.Delay(50);

        _mockScopeFactory.Verify(f => f.CreateScope(), Times.Never,
            "scope must not be created when there is no ContributorUserId");
    }

    [Fact]
    public async Task CollectionCompleted_Handle_ValidData_SendsWhatsApp()
    {
        var handler = new CollectionCompletedWhatsAppHandler(
            _mockScopeFactory.Object, CreateLogger<CollectionCompletedWhatsAppHandler>().Object);

        var contributorUserId = Guid.NewGuid();
        var eventId = Guid.NewGuid();

        _mockEventRepo.Setup(r => r.GetByIdAsync(eventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateRealEvent(eventId));

        var domainEvent = new CollectionCompletedEvent(
            eventId, Guid.NewGuid(), contributorUserId, "Jane Doe", "jane@test.com",
            "pi_test", 75m, "USD", DateTime.UtcNow);

        await handler.Handle(new DomainEventNotification<CollectionCompletedEvent>(domainEvent), CancellationToken.None);
        await Task.Delay(2000);

        _mockWhatsAppService.Verify(
            s => s.SendTemplateMessageAsync(contributorUserId, It.IsAny<string>(), It.IsAny<Dictionary<string, string>>(),
                WhatsAppNotificationType.Collection, eventId, It.IsAny<Guid?>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // 16. PaymentPendingWhatsAppHandler (Phase 7B.3)
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task PaymentPending_Handle_NoUserId_ReturnsImmediately()
    {
        var handler = new PaymentPendingWhatsAppHandler(
            _mockScopeFactory.Object, CreateLogger<PaymentPendingWhatsAppHandler>().Object);

        var domainEvent = new RegistrationPendingPaymentEvent(
            Guid.NewGuid(), Guid.NewGuid(), null, "anon@test.com", "Guest",
            "cs_test_session", DateTime.UtcNow.AddHours(24), 100m, "USD", 2, DateTime.UtcNow);

        var task = handler.Handle(new DomainEventNotification<RegistrationPendingPaymentEvent>(domainEvent), CancellationToken.None);
        task.IsCompleted.Should().BeTrue();
        await task;
        await Task.Delay(50);

        _mockScopeFactory.Verify(f => f.CreateScope(), Times.Never,
            "scope must not be created when there is no UserId");
    }

    [Fact]
    public async Task PaymentPending_Handle_ValidData_SendsWhatsApp()
    {
        var handler = new PaymentPendingWhatsAppHandler(
            _mockScopeFactory.Object, CreateLogger<PaymentPendingWhatsAppHandler>().Object);

        var userId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        var registrationId = Guid.NewGuid();

        _mockEventRepo.Setup(r => r.GetByIdAsync(eventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateRealEvent(eventId));

        var domainEvent = new RegistrationPendingPaymentEvent(
            eventId, registrationId, userId, "user@test.com", "Test User",
            "cs_test_session", DateTime.UtcNow.AddHours(24), 100m, "USD", 2, DateTime.UtcNow);

        await handler.Handle(new DomainEventNotification<RegistrationPendingPaymentEvent>(domainEvent), CancellationToken.None);
        await Task.Delay(2000);

        _mockWhatsAppService.Verify(
            s => s.SendTemplateMessageAsync(userId, It.IsAny<string>(), It.IsAny<Dictionary<string, string>>(),
                WhatsAppNotificationType.PaymentPending, eventId, registrationId, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // 17. AddOnPurchaseWhatsAppHandler (Phase 7B.3)
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task AddOnPurchase_Handle_NoBuyerUserId_ReturnsImmediately()
    {
        var handler = new AddOnPurchaseWhatsAppHandler(
            _mockScopeFactory.Object, CreateLogger<AddOnPurchaseWhatsAppHandler>().Object);

        var domainEvent = new AddOnPurchaseCompletedEvent(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), null, "Anonymous Buyer",
            "anon@test.com", "pi_test", 2, 10m, 20m, "USD", DateTime.UtcNow);

        var task = handler.Handle(new DomainEventNotification<AddOnPurchaseCompletedEvent>(domainEvent), CancellationToken.None);
        task.IsCompleted.Should().BeTrue();
        await task;
        await Task.Delay(50);

        _mockScopeFactory.Verify(f => f.CreateScope(), Times.Never,
            "scope must not be created when there is no BuyerUserId");
    }

    [Fact]
    public async Task AddOnPurchase_Handle_ValidData_SendsWhatsApp()
    {
        var handler = new AddOnPurchaseWhatsAppHandler(
            _mockScopeFactory.Object, CreateLogger<AddOnPurchaseWhatsAppHandler>().Object);

        var buyerUserId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        var addOnDefinitionId = Guid.NewGuid();

        _mockEventRepo.Setup(r => r.GetByIdAsync(eventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateRealEvent(eventId));
        _mockAddOnRepo.Setup(r => r.GetByIdAsync(addOnDefinitionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateRealAddOnDefinition(addOnDefinitionId, eventId, "T-Shirt"));

        var domainEvent = new AddOnPurchaseCompletedEvent(
            eventId, Guid.NewGuid(), addOnDefinitionId, buyerUserId, "Buyer Name",
            "buyer@test.com", "pi_test", 2, 15m, 30m, "USD", DateTime.UtcNow);

        await handler.Handle(new DomainEventNotification<AddOnPurchaseCompletedEvent>(domainEvent), CancellationToken.None);
        await Task.Delay(2000);

        _mockWhatsAppService.Verify(
            s => s.SendTemplateMessageAsync(buyerUserId, It.IsAny<string>(), It.IsAny<Dictionary<string, string>>(),
                WhatsAppNotificationType.AddOnPurchase, eventId, It.IsAny<Guid?>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // 18. AttendeesAddedWhatsAppHandler (Phase 7B.3)
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task AttendeesAdded_Handle_NoUserId_ReturnsImmediately()
    {
        var handler = new AttendeesAddedWhatsAppHandler(
            _mockScopeFactory.Object, CreateLogger<AttendeesAddedWhatsAppHandler>().Object);

        var domainEvent = new AttendeesAddedEvent(
            Guid.NewGuid(), Guid.NewGuid(), null, "anon@test.com",
            2, 3, 5, 45m, "USD", 95m, Guid.NewGuid(), DateTime.UtcNow);

        var task = handler.Handle(new DomainEventNotification<AttendeesAddedEvent>(domainEvent), CancellationToken.None);
        task.IsCompleted.Should().BeTrue();
        await task;
        await Task.Delay(50);

        _mockScopeFactory.Verify(f => f.CreateScope(), Times.Never,
            "scope must not be created when there is no UserId");
    }

    [Fact]
    public async Task AttendeesAdded_Handle_ValidData_SendsWhatsApp()
    {
        var handler = new AttendeesAddedWhatsAppHandler(
            _mockScopeFactory.Object, CreateLogger<AttendeesAddedWhatsAppHandler>().Object);

        var userId = Guid.NewGuid();
        var eventId = Guid.NewGuid();

        _mockEventRepo.Setup(r => r.GetByIdAsync(eventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateRealEvent(eventId));

        var domainEvent = new AttendeesAddedEvent(
            eventId, Guid.NewGuid(), userId, "user@test.com",
            2, 3, 5, 45m, "USD", 95m, Guid.NewGuid(), DateTime.UtcNow);

        await handler.Handle(new DomainEventNotification<AttendeesAddedEvent>(domainEvent), CancellationToken.None);
        await Task.Delay(2000);

        _mockWhatsAppService.Verify(
            s => s.SendTemplateMessageAsync(userId, It.IsAny<string>(), It.IsAny<Dictionary<string, string>>(),
                WhatsAppNotificationType.AttendeesAdded, eventId, It.IsAny<Guid?>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // 19. SponsorPaymentWhatsAppHandler (Phase 7B.3)
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task SponsorPayment_Handle_NoSponsorUserId_ReturnsImmediately()
    {
        var handler = new SponsorPaymentWhatsAppHandler(
            _mockScopeFactory.Object, CreateLogger<SponsorPaymentWhatsAppHandler>().Object);

        var domainEvent = new SponsorPaymentCompletedEvent(
            Guid.NewGuid(), Guid.NewGuid(), null, "Anonymous Sponsor", "anon@test.com",
            "Corp Inc", "pi_test", 500m, "USD", DateTime.UtcNow);

        var task = handler.Handle(new DomainEventNotification<SponsorPaymentCompletedEvent>(domainEvent), CancellationToken.None);
        task.IsCompleted.Should().BeTrue();
        await task;
        await Task.Delay(50);

        _mockScopeFactory.Verify(f => f.CreateScope(), Times.Never,
            "scope must not be created when there is no SponsorUserId");
    }

    [Fact]
    public async Task SponsorPayment_Handle_ValidData_SendsWhatsApp()
    {
        var handler = new SponsorPaymentWhatsAppHandler(
            _mockScopeFactory.Object, CreateLogger<SponsorPaymentWhatsAppHandler>().Object);

        var sponsorUserId = Guid.NewGuid();
        var eventId = Guid.NewGuid();

        _mockEventRepo.Setup(r => r.GetByIdAsync(eventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateRealEvent(eventId));

        var domainEvent = new SponsorPaymentCompletedEvent(
            eventId, Guid.NewGuid(), sponsorUserId, "Sponsor Name", "sponsor@test.com",
            "Corp Inc", "pi_test", 500m, "USD", DateTime.UtcNow);

        await handler.Handle(new DomainEventNotification<SponsorPaymentCompletedEvent>(domainEvent), CancellationToken.None);
        await Task.Delay(2000);

        _mockWhatsAppService.Verify(
            s => s.SendTemplateMessageAsync(sponsorUserId, It.IsAny<string>(), It.IsAny<Dictionary<string, string>>(),
                WhatsAppNotificationType.Sponsorship, eventId, It.IsAny<Guid?>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // 20. ItemSponsorWhatsAppHandler (Phase 7B.3)
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task ItemSponsor_Handle_NoSponsorUserId_ReturnsImmediately()
    {
        var handler = new ItemSponsorWhatsAppHandler(
            _mockScopeFactory.Object, CreateLogger<ItemSponsorWhatsAppHandler>().Object);

        var domainEvent = new ItemSponsorRecordedEvent(
            Guid.NewGuid(), Guid.NewGuid(), null, "Anonymous Sponsor", "anon@test.com",
            null, "Sound System", "Professional PA system", 2000m, DateTime.UtcNow);

        var task = handler.Handle(new DomainEventNotification<ItemSponsorRecordedEvent>(domainEvent), CancellationToken.None);
        task.IsCompleted.Should().BeTrue();
        await task;
        await Task.Delay(50);

        _mockScopeFactory.Verify(f => f.CreateScope(), Times.Never,
            "scope must not be created when there is no SponsorUserId");
    }

    [Fact]
    public async Task ItemSponsor_Handle_ValidData_SendsWhatsApp()
    {
        var handler = new ItemSponsorWhatsAppHandler(
            _mockScopeFactory.Object, CreateLogger<ItemSponsorWhatsAppHandler>().Object);

        var sponsorUserId = Guid.NewGuid();
        var eventId = Guid.NewGuid();

        _mockEventRepo.Setup(r => r.GetByIdAsync(eventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateRealEvent(eventId));

        var domainEvent = new ItemSponsorRecordedEvent(
            eventId, Guid.NewGuid(), sponsorUserId, "Sponsor Name", "sponsor@test.com",
            "Corp Inc", "Sound System", "Professional PA system", 2000m, DateTime.UtcNow);

        await handler.Handle(new DomainEventNotification<ItemSponsorRecordedEvent>(domainEvent), CancellationToken.None);
        await Task.Delay(2000);

        _mockWhatsAppService.Verify(
            s => s.SendTemplateMessageAsync(sponsorUserId, It.IsAny<string>(), It.IsAny<Dictionary<string, string>>(),
                WhatsAppNotificationType.Sponsorship, eventId, It.IsAny<Guid?>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // 21. FormResponseWhatsAppHandler (Phase 7B.3)
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task FormResponse_Handle_AnonymousRespondent_DoesNotSend()
    {
        var handler = new FormResponseWhatsAppHandler(
            _mockScopeFactory.Object, CreateLogger<FormResponseWhatsAppHandler>().Object);

        var formId = Guid.NewGuid();
        var responseId = Guid.NewGuid();
        var eventId = Guid.NewGuid();

        // FormResponse with null RespondentUserId (anonymous)
        var formResponse = CreateRealFormResponse(responseId, formId, eventId, respondentUserId: null, respondentName: null);
        _mockFormResponseRepo.Setup(r => r.GetByIdAsync(responseId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(formResponse);

        var domainEvent = new FormResponseSubmittedEvent(formId, responseId, null, null, DateTime.UtcNow);
        await handler.Handle(new DomainEventNotification<FormResponseSubmittedEvent>(domainEvent), CancellationToken.None);
        await Task.Delay(2000);

        _mockWhatsAppService.Verify(
            s => s.SendTemplateMessageAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<Dictionary<string, string>>(),
                It.IsAny<WhatsAppNotificationType>(), It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task FormResponse_Handle_ValidData_SendsWhatsApp()
    {
        var handler = new FormResponseWhatsAppHandler(
            _mockScopeFactory.Object, CreateLogger<FormResponseWhatsAppHandler>().Object);

        var formId = Guid.NewGuid();
        var responseId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        var respondentUserId = Guid.NewGuid();

        var formResponse = CreateRealFormResponse(responseId, formId, eventId, respondentUserId, "Respondent Name");
        _mockFormResponseRepo.Setup(r => r.GetByIdAsync(responseId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(formResponse);

        var eventForm = CreateRealEventForm(formId, eventId, "Feedback Survey");
        _mockEventFormRepo.Setup(r => r.GetByIdAsync(formId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(eventForm);

        var domainEvent = new FormResponseSubmittedEvent(formId, responseId, "respondent@test.com", "token123", DateTime.UtcNow);
        await handler.Handle(new DomainEventNotification<FormResponseSubmittedEvent>(domainEvent), CancellationToken.None);
        await Task.Delay(2000);

        _mockWhatsAppService.Verify(
            s => s.SendTemplateMessageAsync(respondentUserId, It.IsAny<string>(), It.IsAny<Dictionary<string, string>>(),
                WhatsAppNotificationType.FormResponse, eventId, It.IsAny<Guid?>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // 22. EventPostponedWhatsAppHandler (Phase 7B.3)
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task EventPostponed_Handle_ReturnsImmediately()
    {
        var handler = new EventPostponedWhatsAppHandler(
            _mockScopeFactory.Object, CreateLogger<EventPostponedWhatsAppHandler>().Object);

        var domainEvent = new EventPostponedEvent(Guid.NewGuid(), "Weather conditions", DateTime.UtcNow);
        var task = handler.Handle(new DomainEventNotification<EventPostponedEvent>(domainEvent), CancellationToken.None);
        task.IsCompleted.Should().BeTrue("fire-and-forget handler must return Task.CompletedTask synchronously");
        await task;
    }

    [Fact]
    public async Task EventPostponed_Handle_ValidData_BroadcastsWhatsApp()
    {
        var handler = new EventPostponedWhatsAppHandler(
            _mockScopeFactory.Object, CreateLogger<EventPostponedWhatsAppHandler>().Object);

        var eventId = Guid.NewGuid();
        _mockEventRepo.Setup(r => r.GetByIdAsync(eventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateRealEvent(eventId));

        var domainEvent = new EventPostponedEvent(eventId, "Weather conditions", DateTime.UtcNow);
        await handler.Handle(new DomainEventNotification<EventPostponedEvent>(domainEvent), CancellationToken.None);
        await Task.Delay(2000);

        _mockWhatsAppService.Verify(
            s => s.BroadcastToEventAttendeesAsync(eventId, It.IsAny<string>(),
                It.IsAny<Dictionary<string, string>>(), WhatsAppNotificationType.EventPostponed,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
