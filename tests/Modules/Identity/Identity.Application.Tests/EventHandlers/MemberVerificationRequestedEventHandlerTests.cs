using LankaConnect.BuildingBlocks.Application.Common;
using LankaConnect.BuildingBlocks.Application.Common.Constants;
using LankaConnect.BuildingBlocks.Application.Common.Interfaces;
using LankaConnect.Modules.Identity.Application.EventHandlers;
using LankaConnect.BuildingBlocks.Domain;
using LankaConnect.Modules.Identity.Domain.DomainEvents;
using LankaConnect.Modules.Communications.Contracts.Email.Contracts;
using LankaConnect.Modules.Communications.Contracts.Email.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace LankaConnect.Modules.Identity.Application.Tests.EventHandlers;

/// <summary>
/// Tests for MemberVerificationRequestedEventHandler
/// Phase 6A.53: Member Email Verification System
/// Phase 6A.100: Migrated to ITypedEmailService only - removed IEmailService dependency
/// TDD approach - tests written before implementation changes
/// </summary>
public class MemberVerificationRequestedEventHandlerTests
{
    private readonly Mock<ITypedEmailService> _typedEmailService;
    private readonly Mock<ILogger<MemberVerificationRequestedEventHandler>> _logger;
    private readonly Mock<IApplicationUrlsService> _urlsService;
    private readonly MemberVerificationRequestedEventHandler _handler;

    public MemberVerificationRequestedEventHandlerTests()
    {
        _typedEmailService = new Mock<ITypedEmailService>();
        _logger = new Mock<ILogger<MemberVerificationRequestedEventHandler>>();
        _urlsService = new Mock<IApplicationUrlsService>();

        // Phase 6A.100: Constructor now only takes 3 arguments
        _handler = new MemberVerificationRequestedEventHandler(
            _typedEmailService.Object,
            _logger.Object,
            _urlsService.Object);
    }

    [Fact]
    public async Task Handle_WithValidEvent_ShouldSendEmailWithUserName()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var email = "test@example.com";
        var firstName = "John";
        var lastName = "Doe";
        var verificationToken = Guid.NewGuid().ToString("N");
        var verificationUrl = $"https://lankaconnect.com/verify-email?token={verificationToken}";

        var domainEvent = new MemberVerificationRequestedEvent(
            userId,
            email,
            verificationToken,
            DateTimeOffset.UtcNow,
            firstName,
            lastName);
        var notification = new DomainEventNotification<MemberVerificationRequestedEvent>(domainEvent);

        _urlsService.Setup(x => x.GetEmailVerificationUrl(verificationToken))
            .Returns(verificationUrl);
        // Phase 6A.87: Setup typed email service mock
        _typedEmailService.Setup(x => x.SendEmailAsync(
            It.IsAny<IEmailParameters>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(TypedEmailSendResult.Ok(Guid.NewGuid().ToString(), 100));

        // Act
        await _handler.Handle(notification, CancellationToken.None);

        // Assert - Phase 6A.87: Verify typed email service was called with correct parameters
        _typedEmailService.Verify(x => x.SendEmailAsync(
            It.Is<EmailVerificationEmailParams>(p =>
                p.Email == email &&
                p.VerificationUrl == verificationUrl &&
                p.UserName == "John Doe" &&
                p.ExpirationHours == "24"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithFirstNameOnly_ShouldUseFirstNameAsUserName()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var email = "test@example.com";
        var firstName = "John";
        var verificationToken = Guid.NewGuid().ToString("N");

        var domainEvent = new MemberVerificationRequestedEvent(
            userId,
            email,
            verificationToken,
            DateTimeOffset.UtcNow,
            firstName,
            string.Empty);
        var notification = new DomainEventNotification<MemberVerificationRequestedEvent>(domainEvent);

        _urlsService.Setup(x => x.GetEmailVerificationUrl(It.IsAny<string>()))
            .Returns("https://lankaconnect.com/verify-email?token=test");
        // Phase 6A.87: Setup typed email service mock
        _typedEmailService.Setup(x => x.SendEmailAsync(
            It.IsAny<IEmailParameters>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(TypedEmailSendResult.Ok(Guid.NewGuid().ToString(), 100));

        // Act
        await _handler.Handle(notification, CancellationToken.None);

        // Assert - Phase 6A.87: Verify typed email service was called with first name only
        _typedEmailService.Verify(x => x.SendEmailAsync(
            It.Is<EmailVerificationEmailParams>(p =>
                p.Email == email &&
                p.UserName == "John"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithNoNames_ShouldUseFriendAsUserName()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var email = "test@example.com";
        var verificationToken = Guid.NewGuid().ToString("N");

        var domainEvent = new MemberVerificationRequestedEvent(
            userId,
            email,
            verificationToken,
            DateTimeOffset.UtcNow,
            string.Empty,
            string.Empty);
        var notification = new DomainEventNotification<MemberVerificationRequestedEvent>(domainEvent);

        _urlsService.Setup(x => x.GetEmailVerificationUrl(It.IsAny<string>()))
            .Returns("https://lankaconnect.com/verify-email?token=test");
        // Phase 6A.87: Setup typed email service mock
        _typedEmailService.Setup(x => x.SendEmailAsync(
            It.IsAny<IEmailParameters>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(TypedEmailSendResult.Ok(Guid.NewGuid().ToString(), 100));

        // Act
        await _handler.Handle(notification, CancellationToken.None);

        // Assert - Phase 6A.87: Verify typed email service was called with "Friend"
        _typedEmailService.Verify(x => x.SendEmailAsync(
            It.Is<EmailVerificationEmailParams>(p =>
                p.Email == email &&
                p.UserName == "Friend"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_EmailServiceFailure_ShouldNotThrow()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var domainEvent = new MemberVerificationRequestedEvent(
            userId,
            "test@example.com",
            Guid.NewGuid().ToString("N"),
            DateTimeOffset.UtcNow,
            "John",
            "Doe");
        var notification = new DomainEventNotification<MemberVerificationRequestedEvent>(domainEvent);

        _urlsService.Setup(x => x.GetEmailVerificationUrl(It.IsAny<string>()))
            .Returns("https://lankaconnect.com/verify-email?token=test");
        // Phase 6A.87: Setup typed email service to return failure
        _typedEmailService.Setup(x => x.SendEmailAsync(
            It.IsAny<IEmailParameters>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(TypedEmailSendResult.Fail(Guid.NewGuid().ToString(), new List<string> { "Email service error" }));

        // Act - Should not throw (fail-silent pattern)
        var act = async () => await _handler.Handle(notification, CancellationToken.None);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task Handle_ExceptionDuringProcessing_ShouldNotThrow()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var domainEvent = new MemberVerificationRequestedEvent(
            userId,
            "test@example.com",
            Guid.NewGuid().ToString("N"),
            DateTimeOffset.UtcNow,
            "John",
            "Doe");
        var notification = new DomainEventNotification<MemberVerificationRequestedEvent>(domainEvent);

        _urlsService.Setup(x => x.GetEmailVerificationUrl(It.IsAny<string>()))
            .Throws(new Exception("URL service error"));

        // Act - Should not throw (fail-silent pattern)
        var act = async () => await _handler.Handle(notification, CancellationToken.None);

        // Assert
        await act.Should().NotThrowAsync();
    }
}
