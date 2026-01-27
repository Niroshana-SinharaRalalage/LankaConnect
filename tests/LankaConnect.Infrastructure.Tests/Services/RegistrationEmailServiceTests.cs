using Xunit;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Common.Constants;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events;
using LankaConnect.Domain.Events.Entities;
using LankaConnect.Domain.Events.ValueObjects;
using LankaConnect.Domain.Users;
using LankaConnect.Infrastructure.Services;
using LankaConnect.Application.Common.DTOs;
using LankaConnect.Domain.Business.ValueObjects;
using LankaConnect.Domain.Shared.ValueObjects;
using LankaConnect.Domain.Events.Enums;
using LankaConnect.Domain.Shared.Enums;

namespace LankaConnect.Infrastructure.Tests.Services;

/// <summary>
/// Phase 6A.X: Tests for RegistrationEmailService
/// TDD RED phase - tests written first, implementation follows
/// </summary>
public class RegistrationEmailServiceTests
{
    private readonly Mock<IEmailService> _emailService;
    private readonly Mock<IEmailTemplateService> _emailTemplateService;
    private readonly Mock<ILogger<RegistrationEmailService>> _logger;
    private readonly RegistrationEmailService _sut;

    public RegistrationEmailServiceTests()
    {
        _emailService = new Mock<IEmailService>();
        _emailTemplateService = new Mock<IEmailTemplateService>();
        _logger = new Mock<ILogger<RegistrationEmailService>>();

        _sut = new RegistrationEmailService(
            _emailService.Object,
            _emailTemplateService.Object,
            _logger.Object);
    }

    #region Free Event Tests - Member Registration

    [Fact]
    public async Task SendFreeEventConfirmationEmailAsync_ForMember_ShouldSendEmailSuccessfully()
    {
        // Arrange
        var registration = CreateTestRegistration(isFree: true);
        var @event = CreateTestEvent(isFree: true);
        var user = CreateTestUser();

        _emailTemplateService.Setup(x => x.RenderTemplateAsync(
                EmailTemplateNames.FreeEventRegistration,
                It.IsAny<Dictionary<string, object>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<RenderedEmailTemplate>.Success(new RenderedEmailTemplate
            {
                Subject = "Test Subject",
                HtmlBody = "<p>Test HTML</p>",
                PlainTextBody = "Test Plain Text"
            }));

        _emailService.Setup(x => x.SendEmailAsync(
                It.IsAny<EmailMessageDto>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        // Act
        var result = await _sut.SendFreeEventConfirmationEmailAsync(
            registration, @event, user, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _emailTemplateService.Verify(x => x.RenderTemplateAsync(
            EmailTemplateNames.FreeEventRegistration,
            It.Is<Dictionary<string, object>>(p =>
                p.ContainsKey("UserName") &&
                p["UserName"].ToString() == $"{user.FirstName} {user.LastName}"),
            It.IsAny<CancellationToken>()), Times.Once);
        _emailService.Verify(x => x.SendEmailAsync(
            It.Is<EmailMessageDto>(m => m.ToEmail == user.Email.Value),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SendFreeEventConfirmationEmailAsync_ForMember_ShouldIncludeEventDetails()
    {
        // Arrange
        var registration = CreateTestRegistration(isFree: true);
        var @event = CreateTestEvent(isFree: true);
        var user = CreateTestUser();

        Dictionary<string, object>? capturedParameters = null;
        _emailTemplateService.Setup(x => x.RenderTemplateAsync(
                EmailTemplateNames.FreeEventRegistration,
                It.IsAny<Dictionary<string, object>>(),
                It.IsAny<CancellationToken>()))
            .Callback<string, Dictionary<string, object>, CancellationToken>((_, p, _) => capturedParameters = p)
            .ReturnsAsync(Result<RenderedEmailTemplate>.Success(new RenderedEmailTemplate
            {
                Subject = "Test Subject",
                HtmlBody = "<p>Test</p>",
                PlainTextBody = "Test"
            }));

        _emailService.Setup(x => x.SendEmailAsync(
                It.IsAny<EmailMessageDto>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        // Act
        await _sut.SendFreeEventConfirmationEmailAsync(
            registration, @event, user, CancellationToken.None);

        // Assert
        capturedParameters.Should().NotBeNull();
        capturedParameters!["EventTitle"].Should().Be(@event.Title.Value);
        capturedParameters.Should().ContainKey("EventDateTime");
        capturedParameters.Should().ContainKey("EventLocation");
        capturedParameters.Should().ContainKey("RegistrationDate");
    }

    [Fact]
    public async Task SendFreeEventConfirmationEmailAsync_ForMember_ShouldIncludeAttendeeDetails()
    {
        // Arrange
        var registration = CreateTestRegistrationWithAttendees();
        var @event = CreateTestEvent(isFree: true);
        var user = CreateTestUser();

        Dictionary<string, object>? capturedParameters = null;
        _emailTemplateService.Setup(x => x.RenderTemplateAsync(
                EmailTemplateNames.FreeEventRegistration,
                It.IsAny<Dictionary<string, object>>(),
                It.IsAny<CancellationToken>()))
            .Callback<string, Dictionary<string, object>, CancellationToken>((_, p, _) => capturedParameters = p)
            .ReturnsAsync(Result<RenderedEmailTemplate>.Success(new RenderedEmailTemplate
            {
                Subject = "Test",
                HtmlBody = "<p>Test</p>",
                PlainTextBody = "Test"
            }));

        _emailService.Setup(x => x.SendEmailAsync(
                It.IsAny<EmailMessageDto>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        // Act
        await _sut.SendFreeEventConfirmationEmailAsync(
            registration, @event, user, CancellationToken.None);

        // Assert
        capturedParameters.Should().NotBeNull();
        capturedParameters!["HasAttendeeDetails"].Should().Be(true);
        capturedParameters["Attendees"].ToString().Should().Contain("John Doe");
        capturedParameters["Attendees"].ToString().Should().Contain("Jane Smith");
    }

    #endregion

    #region Free Event Tests - Anonymous Registration

    [Fact]
    public async Task SendFreeEventConfirmationEmailAsync_ForAnonymous_ShouldSendToContactEmail()
    {
        // Arrange
        var registration = CreateTestRegistration(isFree: true);
        var @event = CreateTestEvent(isFree: true);
        User? user = null; // Anonymous registration

        _emailTemplateService.Setup(x => x.RenderTemplateAsync(
                EmailTemplateNames.FreeEventRegistration,
                It.IsAny<Dictionary<string, object>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<RenderedEmailTemplate>.Success(new RenderedEmailTemplate
            {
                Subject = "Test",
                HtmlBody = "<p>Test</p>",
                PlainTextBody = "Test"
            }));

        _emailService.Setup(x => x.SendEmailAsync(
                It.IsAny<EmailMessageDto>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        // Act
        var result = await _sut.SendFreeEventConfirmationEmailAsync(
            registration, @event, user, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _emailService.Verify(x => x.SendEmailAsync(
            It.Is<EmailMessageDto>(m => m.ToEmail == registration.Contact!.Email),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SendFreeEventConfirmationEmailAsync_ForAnonymous_ShouldUseContactNameAsUserName()
    {
        // Arrange
        var registration = CreateTestRegistrationWithAttendees();
        var @event = CreateTestEvent(isFree: true);
        User? user = null;

        Dictionary<string, object>? capturedParameters = null;
        _emailTemplateService.Setup(x => x.RenderTemplateAsync(
                EmailTemplateNames.FreeEventRegistration,
                It.IsAny<Dictionary<string, object>>(),
                It.IsAny<CancellationToken>()))
            .Callback<string, Dictionary<string, object>, CancellationToken>((_, p, _) => capturedParameters = p)
            .ReturnsAsync(Result<RenderedEmailTemplate>.Success(new RenderedEmailTemplate
            {
                Subject = "Test",
                HtmlBody = "<p>Test</p>",
                PlainTextBody = "Test"
            }));

        _emailService.Setup(x => x.SendEmailAsync(
                It.IsAny<EmailMessageDto>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        // Act
        await _sut.SendFreeEventConfirmationEmailAsync(
            registration, @event, user, CancellationToken.None);

        // Assert
        capturedParameters.Should().NotBeNull();
        capturedParameters!["UserName"].Should().Be("John Doe"); // First attendee's name
    }

    #endregion

    #region Paid Event Tests - Member Registration

    [Fact]
    public async Task SendPaidEventConfirmationEmailAsync_ForMember_ShouldSendEmailWithPdfAttachment()
    {
        // Arrange
        var registration = CreateTestRegistration(isFree: false);
        var @event = CreateTestEvent(isFree: false);
        var ticket = CreateTestTicket();
        var ticketPdf = new byte[] { 1, 2, 3, 4, 5 };
        var user = CreateTestUser();

        _emailTemplateService.Setup(x => x.RenderTemplateAsync(
                EmailTemplateNames.PaidEventRegistration,
                It.IsAny<Dictionary<string, object>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<RenderedEmailTemplate>.Success(new RenderedEmailTemplate
            {
                Subject = "Paid Event Confirmation",
                HtmlBody = "<p>Ticket attached</p>",
                PlainTextBody = "Ticket attached"
            }));

        _emailService.Setup(x => x.SendEmailAsync(
                It.IsAny<EmailMessageDto>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        // Act
        var result = await _sut.SendPaidEventConfirmationEmailAsync(
            registration, @event, ticket, ticketPdf, user, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _emailService.Verify(x => x.SendEmailAsync(
            It.Is<EmailMessageDto>(m =>
                m.ToEmail == user.Email.Value &&
                m.Attachments != null &&
                m.Attachments.Count == 1 &&
                m.Attachments[0].FileName.Contains(ticket.TicketCode) &&
                m.Attachments[0].ContentType == "application/pdf"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SendPaidEventConfirmationEmailAsync_ForMember_ShouldIncludeTicketDetails()
    {
        // Arrange
        var registration = CreateTestRegistration(isFree: false);
        var @event = CreateTestEvent(isFree: false);
        var ticket = CreateTestTicket();
        var ticketPdf = new byte[] { 1, 2, 3 };
        var user = CreateTestUser();

        Dictionary<string, object>? capturedParameters = null;
        _emailTemplateService.Setup(x => x.RenderTemplateAsync(
                EmailTemplateNames.PaidEventRegistration,
                It.IsAny<Dictionary<string, object>>(),
                It.IsAny<CancellationToken>()))
            .Callback<string, Dictionary<string, object>, CancellationToken>((_, p, _) => capturedParameters = p)
            .ReturnsAsync(Result<RenderedEmailTemplate>.Success(new RenderedEmailTemplate
            {
                Subject = "Test",
                HtmlBody = "<p>Test</p>",
                PlainTextBody = "Test"
            }));

        _emailService.Setup(x => x.SendEmailAsync(
                It.IsAny<EmailMessageDto>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        // Act
        await _sut.SendPaidEventConfirmationEmailAsync(
            registration, @event, ticket, ticketPdf, user, CancellationToken.None);

        // Assert
        capturedParameters.Should().NotBeNull();
        capturedParameters!["HasTicket"].Should().Be(true);
        capturedParameters["TicketCode"].Should().Be(ticket.TicketCode);
        capturedParameters.Should().ContainKey("TicketExpiryDate");
        capturedParameters.Should().ContainKey("AmountPaid");
        capturedParameters.Should().ContainKey("PaymentIntentId");
    }

    #endregion

    #region Paid Event Tests - Anonymous Registration

    [Fact]
    public async Task SendPaidEventConfirmationEmailAsync_ForAnonymous_ShouldSendToContactEmail()
    {
        // Arrange
        var registration = CreateTestRegistration(isFree: false);
        var @event = CreateTestEvent(isFree: false);
        var ticket = CreateTestTicket();
        var ticketPdf = new byte[] { 1, 2, 3 };
        User? user = null;

        _emailTemplateService.Setup(x => x.RenderTemplateAsync(
                EmailTemplateNames.PaidEventRegistration,
                It.IsAny<Dictionary<string, object>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<RenderedEmailTemplate>.Success(new RenderedEmailTemplate
            {
                Subject = "Test",
                HtmlBody = "<p>Test</p>",
                PlainTextBody = "Test"
            }));

        _emailService.Setup(x => x.SendEmailAsync(
                It.IsAny<EmailMessageDto>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        // Act
        var result = await _sut.SendPaidEventConfirmationEmailAsync(
            registration, @event, ticket, ticketPdf, user, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _emailService.Verify(x => x.SendEmailAsync(
            It.Is<EmailMessageDto>(m => m.ToEmail == registration.Contact!.Email),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    public async Task SendFreeEventConfirmationEmailAsync_WhenTemplateRenderingFails_ShouldReturnFailure()
    {
        // Arrange
        var registration = CreateTestRegistration(isFree: true);
        var @event = CreateTestEvent(isFree: true);
        var user = CreateTestUser();

        _emailTemplateService.Setup(x => x.RenderTemplateAsync(
                EmailTemplateNames.FreeEventRegistration,
                It.IsAny<Dictionary<string, object>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<RenderedEmailTemplate>.Failure("Template rendering failed"));

        // Act
        var result = await _sut.SendFreeEventConfirmationEmailAsync(
            registration, @event, user, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Template rendering failed");
        _emailService.Verify(x => x.SendEmailAsync(
            It.IsAny<EmailMessageDto>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SendFreeEventConfirmationEmailAsync_WhenEmailSendingFails_ShouldReturnFailure()
    {
        // Arrange
        var registration = CreateTestRegistration(isFree: true);
        var @event = CreateTestEvent(isFree: true);
        var user = CreateTestUser();

        _emailTemplateService.Setup(x => x.RenderTemplateAsync(
                EmailTemplateNames.FreeEventRegistration,
                It.IsAny<Dictionary<string, object>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<RenderedEmailTemplate>.Success(new RenderedEmailTemplate
            {
                Subject = "Test",
                HtmlBody = "<p>Test</p>",
                PlainTextBody = "Test"
            }));

        _emailService.Setup(x => x.SendEmailAsync(
                It.IsAny<EmailMessageDto>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure("SMTP connection failed"));

        // Act
        var result = await _sut.SendFreeEventConfirmationEmailAsync(
            registration, @event, user, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("SMTP connection failed");
    }

    [Fact]
    public async Task SendPaidEventConfirmationEmailAsync_WhenEmailSendingFails_ShouldReturnFailure()
    {
        // Arrange
        var registration = CreateTestRegistration(isFree: false);
        var @event = CreateTestEvent(isFree: false);
        var ticket = CreateTestTicket();
        var ticketPdf = new byte[] { 1, 2, 3 };
        var user = CreateTestUser();

        _emailTemplateService.Setup(x => x.RenderTemplateAsync(
                EmailTemplateNames.PaidEventRegistration,
                It.IsAny<Dictionary<string, object>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<RenderedEmailTemplate>.Success(new RenderedEmailTemplate
            {
                Subject = "Test",
                HtmlBody = "<p>Test</p>",
                PlainTextBody = "Test"
            }));

        _emailService.Setup(x => x.SendEmailAsync(
                It.IsAny<EmailMessageDto>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure("Email service unavailable"));

        // Act
        var result = await _sut.SendPaidEventConfirmationEmailAsync(
            registration, @event, ticket, ticketPdf, user, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Email service unavailable");
    }

    #endregion

    #region Helper Methods

    private Registration CreateTestRegistration(bool isFree)
    {
        var eventId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        // Registration.Create takes (eventId, userId, quantity) - 3 parameters
        var registration = Registration.Create(eventId, userId, 1);

        return registration.Value;
    }

    private Registration CreateTestRegistrationWithAttendees()
    {
        var eventId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        // Registration.Create takes (eventId, userId, quantity) - 3 parameters
        // Note: Adding attendees is done through a different mechanism in the current API
        var registration = Registration.Create(eventId, userId, 2);

        return registration.Value;
    }

    private Event CreateTestEvent(bool isFree)
    {
        var organizerId = Guid.NewGuid();

        // Event.Create uses EventTitle, EventDescription, and optional location/ticketPrice
        var eventResult = Event.Create(
            EventTitle.Create("Test Event").Value,
            EventDescription.Create("Test Description").Value,
            DateTime.UtcNow.AddDays(7),
            DateTime.UtcNow.AddDays(7).AddHours(2),
            organizerId,
            100, // capacity
            EventLocation.Create(
                Address.Create("123 Test St", "Colombo", "Western", "10100", "LK").Value).Value,
            EventCategory.Community,
            isFree ? null : Money.Create(50.00m, Currency.USD).Value);

        return eventResult.Value;
    }

    private Ticket CreateTestTicket()
    {
        var ticketResult = Ticket.Create(
            Guid.NewGuid(), // registrationId
            Guid.NewGuid(), // eventId
            Guid.NewGuid(), // userId
            DateTime.UtcNow.AddDays(8));

        return ticketResult.Value;
    }

    private User CreateTestUser()
    {
        var email = LankaConnect.Domain.Shared.ValueObjects.Email.Create("test@example.com").Value;
        var userResult = User.Create(email, "John", "Doe");

        return userResult.Value;
    }

    #endregion
}
