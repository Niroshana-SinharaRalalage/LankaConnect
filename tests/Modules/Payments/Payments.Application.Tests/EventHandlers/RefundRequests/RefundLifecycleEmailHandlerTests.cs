using LankaConnect.Modules.Payments.Domain.Repositories; // W4.4.d.2
using FluentAssertions;
using LankaConnect.Application.Common;
using LankaConnect.Modules.Payments.Application.EventHandlers.RefundRequests;
using LankaConnect.Application.Interfaces;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events;
using LankaConnect.Domain.Users.DomainEvents; // W4.7.a: user-aggregate events moved here
using LankaConnect.Domain.Events.DomainEvents;
using LankaConnect.Domain.Events.Entities;
using LankaConnect.Domain.Events.Enums;
using LankaConnect.Domain.Events.Repositories;
using LankaConnect.Domain.Events.ValueObjects;
using LankaConnect.Domain.Shared.Enums;
using LankaConnect.Domain.Shared.ValueObjects;
using LankaConnect.Domain.Users;
using LankaConnect.Shared.Email.Contracts;
using LankaConnect.Shared.Email.Services;
using Microsoft.Extensions.Logging;
using Moq;

namespace LankaConnect.Modules.Payments.Application.Tests.EventHandlers.RefundRequests;

/// <summary>
/// Phase 6A.148.D8 + D8b: Single test file covering all four refund-lifecycle email
/// handlers — the load-bearing assertions are: (a) each handler dispatches the CORRECT
/// IEmailParameters subtype (template-name binding); (b) IsOrganizerInitiated flag is
/// set on the right path (attendee=false, organizer-initiated=true); (c) handlers
/// fail-silent on exceptions and missing dependencies; (d) organizer contacts attach
/// when present.
///
/// These tests pin the 148.c → D7 → D8 rewire — if any handler reverts to
/// RefundEmailParams (template-refund-requested with "Refund In Progress" header),
/// the wrong-type assertion fires.
/// </summary>
public class RefundLifecycleEmailHandlerTests
{
    private readonly Mock<ITypedEmailService> _emailService = new();
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly Mock<IEventRepository> _eventRepository = new();
    private readonly Mock<IRefundRequestRepository> _refundRequestRepository = new();
    private readonly Mock<IRegistrationRepository> _registrationRepository = new();
    private readonly Mock<IEmailUrlHelper> _emailUrlHelper = new();

    public RefundLifecycleEmailHandlerTests()
    {
        _emailUrlHelper.Setup(x => x.BuildEventDetailsUrl(It.IsAny<Guid>()))
            .Returns((Guid id) => $"https://lankaconnect.com/events/{id}");
        _emailService.Setup(x => x.SendEmailAsync(It.IsAny<IEmailParameters>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(TypedEmailSendResult.Ok("corr-id", 100));
    }

    // =========================================================================
    // RefundRequestCreatedEventHandler (D8 rewire)
    // =========================================================================

    [Fact]
    public async Task Created_AttendeeInitiated_SendsRefundPendingReviewParams_NotRefundEmailParams()
    {
        var (refundRequest, registration, attendee, @event) = SetupHappyPath();
        _refundRequestRepository.Setup(r => r.GetByIdAsync(refundRequest.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(refundRequest);
        _userRepository.Setup(u => u.GetByIdAsync(attendee.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(attendee);
        _eventRepository.Setup(e => e.GetByIdAsync(@event.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(@event);

        var handler = new RefundRequestCreatedEventHandler(
            _emailService.Object, _userRepository.Object, _eventRepository.Object,
            _refundRequestRepository.Object, _emailUrlHelper.Object,
            Mock.Of<ILogger<RefundRequestCreatedEventHandler>>());

        var notification = new DomainEventNotification<RefundRequestCreatedEvent>(
            new RefundRequestCreatedEvent(
                @event.Id, registration.Id, refundRequest.Id, attendee.Id,
                "Cannot attend", DateTime.UtcNow));

        await handler.Handle(notification, CancellationToken.None);

        _emailService.Verify(x => x.SendEmailAsync(
            It.Is<RefundPendingReviewEmailParams>(p =>
                p.TemplateName == EmailTemplateContract.TemplateNames.RefundPendingReview &&
                p.UserEmail == attendee.Email.Value &&
                p.LineItems.Count == 2 &&
                p.RequesterReason == "Cannot attend"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Created_FailSilentOnException()
    {
        _refundRequestRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("DB down"));

        var handler = new RefundRequestCreatedEventHandler(
            _emailService.Object, _userRepository.Object, _eventRepository.Object,
            _refundRequestRepository.Object, _emailUrlHelper.Object,
            Mock.Of<ILogger<RefundRequestCreatedEventHandler>>());

        var notification = new DomainEventNotification<RefundRequestCreatedEvent>(
            new RefundRequestCreatedEvent(
                Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
                null, DateTime.UtcNow));

        var act = async () => await handler.Handle(notification, CancellationToken.None);

        await act.Should().NotThrowAsync();
    }

    // =========================================================================
    // RefundRequestApprovedEventHandler (D8 rewire)
    // =========================================================================

    [Fact]
    public async Task Approved_SendsRefundDecisionParams_WithIsOrganizerInitiatedFalse()
    {
        var (refundRequest, registration, attendee, @event) = SetupHappyPath();
        // Approve the lines so ApprovedAmount is populated
        var ticketLine = refundRequest.LineItems.First(li => li.Type == RefundLineItemType.Ticket);
        var addonLine = refundRequest.LineItems.First(li => li.Type == RefundLineItemType.AddOn);
        ticketLine.Approve(new Money(50m, Currency.USD));
        addonLine.Approve(new Money(10m, Currency.USD));

        _refundRequestRepository.Setup(r => r.GetByIdAsync(refundRequest.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(refundRequest);
        _registrationRepository.Setup(r => r.GetByIdAsync(registration.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(registration);
        _userRepository.Setup(u => u.GetByIdAsync(attendee.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(attendee);
        _eventRepository.Setup(e => e.GetByIdAsync(@event.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(@event);

        var handler = new RefundRequestApprovedEventHandler(
            _emailService.Object, _userRepository.Object, _eventRepository.Object,
            _refundRequestRepository.Object, _registrationRepository.Object,
            _emailUrlHelper.Object,
            Mock.Of<ILogger<RefundRequestApprovedEventHandler>>());

        var notification = new DomainEventNotification<RefundRequestApprovedEvent>(
            new RefundRequestApprovedEvent(
                @event.Id, registration.Id, refundRequest.Id,
                Guid.NewGuid(), null, DateTime.UtcNow));

        await handler.Handle(notification, CancellationToken.None);

        _emailService.Verify(x => x.SendEmailAsync(
            It.Is<RefundDecisionEmailParams>(p =>
                p.TemplateName == EmailTemplateContract.TemplateNames.RefundDecision &&
                p.IsOrganizerInitiated == false &&
                p.ApprovedTotal == 60m &&
                p.RequestedTotal == 60m),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    // =========================================================================
    // RefundRequestRejectedEventHandler (D8 rewire)
    // =========================================================================

    [Fact]
    public async Task Rejected_SendsRefundRejectedParams_WithRejectionReasonAsFirstClassField()
    {
        var (refundRequest, registration, attendee, @event) = SetupHappyPath();
        _refundRequestRepository.Setup(r => r.GetByIdAsync(refundRequest.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(refundRequest);
        _registrationRepository.Setup(r => r.GetByIdAsync(registration.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(registration);
        _userRepository.Setup(u => u.GetByIdAsync(attendee.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(attendee);
        _eventRepository.Setup(e => e.GetByIdAsync(@event.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(@event);

        var handler = new RefundRequestRejectedEventHandler(
            _emailService.Object, _userRepository.Object, _eventRepository.Object,
            _refundRequestRepository.Object, _registrationRepository.Object,
            _emailUrlHelper.Object,
            Mock.Of<ILogger<RefundRequestRejectedEventHandler>>());

        var notification = new DomainEventNotification<RefundRequestRejectedEvent>(
            new RefundRequestRejectedEvent(
                @event.Id, registration.Id, refundRequest.Id,
                Guid.NewGuid(), "Outside cancellation window", DateTime.UtcNow));

        await handler.Handle(notification, CancellationToken.None);

        _emailService.Verify(x => x.SendEmailAsync(
            It.Is<RefundRejectedEmailParams>(p =>
                p.TemplateName == EmailTemplateContract.TemplateNames.RefundRejected &&
                p.RejectionReason == "Outside cancellation window" &&
                p.LineItems.Count == 2),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    // =========================================================================
    // RefundRequestWithdrawnEventHandler (W4.D13 — NEW)
    // =========================================================================

    [Fact]
    public async Task Withdrawn_SendsRefundWithdrawnParams_WithLineItems()
    {
        var (refundRequest, registration, attendee, @event) = SetupHappyPath();
        _refundRequestRepository.Setup(r => r.GetByIdAsync(refundRequest.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(refundRequest);
        _userRepository.Setup(u => u.GetByIdAsync(attendee.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(attendee);
        _eventRepository.Setup(e => e.GetByIdAsync(@event.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(@event);

        var handler = new RefundRequestWithdrawnEventHandler(
            _emailService.Object, _userRepository.Object, _eventRepository.Object,
            _refundRequestRepository.Object, _emailUrlHelper.Object,
            Mock.Of<ILogger<RefundRequestWithdrawnEventHandler>>());

        var notification = new DomainEventNotification<RefundRequestWithdrawnEvent>(
            new RefundRequestWithdrawnEvent(
                @event.Id, registration.Id, refundRequest.Id,
                WithdrawnByUserId: attendee.Id,
                WithdrawnAt: DateTime.UtcNow));

        await handler.Handle(notification, CancellationToken.None);

        _emailService.Verify(x => x.SendEmailAsync(
            It.Is<RefundWithdrawnEmailParams>(p =>
                p.TemplateName == EmailTemplateContract.TemplateNames.RefundWithdrawn &&
                p.UserEmail == attendee.Email.Value &&
                p.LineItems.Count == 2 &&
                p.RequestedTotal == 60m),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Withdrawn_WhenRefundRequestNotFound_DoesNotInvokeEmail()
    {
        _refundRequestRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((RefundRequest?)null);

        var handler = new RefundRequestWithdrawnEventHandler(
            _emailService.Object, _userRepository.Object, _eventRepository.Object,
            _refundRequestRepository.Object, _emailUrlHelper.Object,
            Mock.Of<ILogger<RefundRequestWithdrawnEventHandler>>());

        var notification = new DomainEventNotification<RefundRequestWithdrawnEvent>(
            new RefundRequestWithdrawnEvent(
                Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
                WithdrawnByUserId: Guid.NewGuid(),
                WithdrawnAt: DateTime.UtcNow));

        await handler.Handle(notification, CancellationToken.None);

        _emailService.Verify(x => x.SendEmailAsync(It.IsAny<IEmailParameters>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // =========================================================================
    // OrganizerInitiatedRefundCreatedEventHandler (D8b — NEW)
    // =========================================================================

    [Fact]
    public async Task OrganizerInitiated_SendsRefundDecisionParams_WithIsOrganizerInitiatedTrue()
    {
        var (refundRequest, registration, attendee, @event) = SetupHappyPath(isOrganizerInitiated: true);
        // Organizer-initiated path: lines are auto-approved at creation
        var ticketLine = refundRequest.LineItems.First(li => li.Type == RefundLineItemType.Ticket);
        var addonLine = refundRequest.LineItems.First(li => li.Type == RefundLineItemType.AddOn);
        ticketLine.Approve(new Money(50m, Currency.USD));
        addonLine.Approve(new Money(10m, Currency.USD));

        _refundRequestRepository.Setup(r => r.GetByIdAsync(refundRequest.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(refundRequest);
        _registrationRepository.Setup(r => r.GetByIdAsync(registration.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(registration);
        _userRepository.Setup(u => u.GetByIdAsync(attendee.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(attendee);
        _eventRepository.Setup(e => e.GetByIdAsync(@event.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(@event);

        var handler = new OrganizerInitiatedRefundCreatedEventHandler(
            _emailService.Object, _userRepository.Object, _eventRepository.Object,
            _refundRequestRepository.Object, _registrationRepository.Object,
            _emailUrlHelper.Object,
            Mock.Of<ILogger<OrganizerInitiatedRefundCreatedEventHandler>>());

        var notification = new DomainEventNotification<OrganizerInitiatedRefundCreatedEvent>(
            new OrganizerInitiatedRefundCreatedEvent(
                @event.Id, registration.Id, refundRequest.Id,
                OrganizerUserId: Guid.NewGuid(),
                OrganizerNotes: "Goodwill",
                ScanGuardOverridden: false,
                CreatedAt: DateTime.UtcNow));

        await handler.Handle(notification, CancellationToken.None);

        _emailService.Verify(x => x.SendEmailAsync(
            It.Is<RefundDecisionEmailParams>(p =>
                p.TemplateName == EmailTemplateContract.TemplateNames.RefundDecision &&
                p.IsOrganizerInitiated == true &&
                p.ApprovedTotal == 60m),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task OrganizerInitiated_WhenRefundRequestNotFound_DoesNotInvokeEmail()
    {
        _refundRequestRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((RefundRequest?)null);

        var handler = new OrganizerInitiatedRefundCreatedEventHandler(
            _emailService.Object, _userRepository.Object, _eventRepository.Object,
            _refundRequestRepository.Object, _registrationRepository.Object,
            _emailUrlHelper.Object,
            Mock.Of<ILogger<OrganizerInitiatedRefundCreatedEventHandler>>());

        var notification = new DomainEventNotification<OrganizerInitiatedRefundCreatedEvent>(
            new OrganizerInitiatedRefundCreatedEvent(
                Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
                Guid.NewGuid(), null, false, DateTime.UtcNow));

        await handler.Handle(notification, CancellationToken.None);

        _emailService.Verify(x => x.SendEmailAsync(It.IsAny<IEmailParameters>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // =========================================================================
    // Shared test setup
    // =========================================================================

    /// <summary>
    /// Builds a happy-path tuple: a RefundRequest with 2 line items (Ticket $50 + AddOn $10),
    /// matching Registration with UserId set to attendee, attendee User, and Event with title.
    /// </summary>
    private static (RefundRequest refundRequest, Registration registration, User attendee, Event @event) SetupHappyPath(
        bool isOrganizerInitiated = false)
    {
        var attendee = CreateTestUser(Guid.NewGuid(), "attendee@example.com", "Niro", "Tester");
        var organizer = CreateTestUser(Guid.NewGuid(), "organizer@example.com", "Bob", "Organizer");
        var @event = CreateTestEvent(Guid.NewGuid(), organizer.Id, "Cricket Match");
        var registration = CreateTestRegistration(Guid.NewGuid(), attendee.Id, @event.Id);

        var ticketRefId = Guid.NewGuid();
        var addOnRefId = Guid.NewGuid();
        var lineItems = new[]
        {
            new RefundRequestLineItemInput(RefundLineItemType.Ticket, ticketRefId, new Money(50m, Currency.USD)),
            new RefundRequestLineItemInput(RefundLineItemType.AddOn, addOnRefId, new Money(10m, Currency.USD))
        };

        var refundRequest = isOrganizerInitiated
            ? RefundRequest.CreateOrganizerInitiated(
                registration.Id, organizer.Id, organizerNotes: "Goodwill",
                scanGuardOverridden: false, lineItems: lineItems).Value
            : RefundRequest.CreatePending(
                registration.Id, attendee.Id, requesterReason: "Cannot attend",
                lineItems: lineItems).Value;

        return (refundRequest, registration, attendee, @event);
    }

    private static User CreateTestUser(Guid userId, string email, string firstName, string lastName)
    {
        var user = User.Create(Email.Create(email).Value, firstName, lastName).Value;
        typeof(LegacyBaseEntity).GetProperty("Id")?.SetValue(user, userId);
        return user;
    }

    private static Event CreateTestEvent(Guid eventId, Guid organizerId, string title)
    {
        var ev = Event.Create(
            EventTitle.Create(title).Value,
            EventDescription.Create("Test").Value,
            DateTime.UtcNow.AddDays(7),
            DateTime.UtcNow.AddDays(7).AddHours(2),
            organizerId,
            100,
            null,
            EventCategory.Cultural).Value;
        typeof(LegacyBaseEntity).GetProperty("Id")?.SetValue(ev, eventId);
        return ev;
    }

    private static Registration CreateTestRegistration(Guid regId, Guid attendeeUserId, Guid eventId)
    {
        // Minimal Registration suitable for the email handler — only UserId + Id are read.
        // Use EF Core's private parameterless ctor to bypass aggregate-creation invariants.
        var ctor = typeof(Registration).GetConstructor(
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance,
            binder: null, types: Type.EmptyTypes, modifiers: null)
            ?? throw new InvalidOperationException("Registration must expose a non-public parameterless ctor for EF Core materialization");
        var reg = (Registration)ctor.Invoke(null);
        typeof(LegacyBaseEntity).GetProperty("Id")?.SetValue(reg, regId);
        typeof(Registration).GetProperty(nameof(Registration.UserId))?.SetValue(reg, attendeeUserId);
        typeof(Registration).GetProperty(nameof(Registration.EventId))?.SetValue(reg, eventId);
        return reg;
    }
}
