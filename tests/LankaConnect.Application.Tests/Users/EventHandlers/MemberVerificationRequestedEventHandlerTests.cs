using LankaConnect.Application.Common;
using LankaConnect.Application.Common.Constants;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Users.EventHandlers;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Users.DomainEvents;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace LankaConnect.Application.Tests.Users.EventHandlers;

/// <summary>
/// Tests for MemberVerificationRequestedEventHandler
/// Phase 6A.53: Member Email Verification System
/// TDD approach - tests written before implementation changes
/// </summary>
public class MemberVerificationRequestedEventHandlerTests
{
    private readonly Mock<IEmailService> _emailService;
    private readonly Mock<ILogger<MemberVerificationRequestedEventHandler>> _logger;
    private readonly Mock<IApplicationUrlsService> _urlsService;
    private readonly MemberVerificationRequestedEventHandler _handler;

    public MemberVerificationRequestedEventHandlerTests()
    {
        _emailService = new Mock<IEmailService>();
        _logger = new Mock<ILogger<MemberVerificationRequestedEventHandler>>();
        _urlsService = new Mock<IApplicationUrlsService>();

        _handler = new MemberVerificationRequestedEventHandler(
            _emailService.Object,
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
        _emailService.Setup(x => x.SendTemplatedEmailAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<Dictionary<string, object>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        // Act
        await _handler.Handle(notification, CancellationToken.None);

        // Assert - Phase 6A.76: Updated to use new template name
        // Phase 6A.83: Parameter name changed from ExpirationHours to TokenExpiry to match template
        _emailService.Verify(x => x.SendTemplatedEmailAsync(
            EmailTemplateNames.MemberEmailVerification,
            email,
            It.Is<Dictionary<string, object>>(dict =>
                dict.ContainsKey("Email") &&
                dict.ContainsKey("VerificationUrl") &&
                dict.ContainsKey("TokenExpiry") &&
                dict.ContainsKey("UserName") &&
                dict["Email"].ToString() == email &&
                dict["VerificationUrl"].ToString() == verificationUrl &&
                dict["UserName"].ToString() == "John Doe"),
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
        _emailService.Setup(x => x.SendTemplatedEmailAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<Dictionary<string, object>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        // Act
        await _handler.Handle(notification, CancellationToken.None);

        // Assert - Phase 6A.76: Updated to use new template name
        _emailService.Verify(x => x.SendTemplatedEmailAsync(
            EmailTemplateNames.MemberEmailVerification,
            email,
            It.Is<Dictionary<string, object>>(dict =>
                dict.ContainsKey("UserName") &&
                dict["UserName"].ToString() == "John"),
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
        _emailService.Setup(x => x.SendTemplatedEmailAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<Dictionary<string, object>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        // Act
        await _handler.Handle(notification, CancellationToken.None);

        // Assert - Phase 6A.76: Updated to use new template name
        _emailService.Verify(x => x.SendTemplatedEmailAsync(
            EmailTemplateNames.MemberEmailVerification,
            email,
            It.Is<Dictionary<string, object>>(dict =>
                dict.ContainsKey("UserName") &&
                dict["UserName"].ToString() == "Friend"),
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
        _emailService.Setup(x => x.SendTemplatedEmailAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<Dictionary<string, object>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure("Email service error"));

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
