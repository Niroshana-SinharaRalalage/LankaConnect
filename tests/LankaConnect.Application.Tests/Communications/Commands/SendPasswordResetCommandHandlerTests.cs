using Xunit;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using LankaConnect.Application.Communications.Commands.SendPasswordReset;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Interfaces;
using LankaConnect.Modules.Identity.Contracts;
using LankaConnect.Shared.Email.Contracts;
using LankaConnect.Shared.Email.Services;

namespace LankaConnect.Application.Tests.Communications.Commands;

/// <summary>
/// Wave 4.7.b (2026-06-25) -- handler now thin shell over
/// <see cref="IIdentityCommands.InitiatePasswordResetAsync"/>. Old tests
/// covered token-generation + throttle + unit-of-work concerns which now
/// live inside the Identity adapter; this rewrite verifies only the
/// handler's responsibilities: validate email format, dispatch mutator,
/// handle null (security-success), dispatch email, shape response.
/// </summary>
public class SendPasswordResetCommandHandlerTests
{
    private readonly Mock<IIdentityCommands> _identityCommands;
    private readonly Mock<ITypedEmailService> _typedEmailService;
    private readonly Mock<IEmailUrlHelper> _emailUrlHelper;
    private readonly Mock<ILogger<SendPasswordResetCommandHandler>> _logger;
    private readonly SendPasswordResetCommandHandler _handler;

    public SendPasswordResetCommandHandlerTests()
    {
        _identityCommands = new Mock<IIdentityCommands>();
        _typedEmailService = new Mock<ITypedEmailService>();
        _emailUrlHelper = new Mock<IEmailUrlHelper>();
        _logger = new Mock<ILogger<SendPasswordResetCommandHandler>>();

        _emailUrlHelper.Setup(x => x.BuildPasswordResetUrl(It.IsAny<string>()))
            .Returns<string>(token => $"https://staging/reset?token={token}");
        _typedEmailService.Setup(x => x.SendEmailAsync(
            It.IsAny<IEmailParameters>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(TypedEmailSendResult.Ok(Guid.NewGuid().ToString(), 100));

        _handler = new SendPasswordResetCommandHandler(
            _identityCommands.Object,
            _typedEmailService.Object,
            _emailUrlHelper.Object,
            _logger.Object);
    }

    [Fact]
    public async Task Handle_WhenIdentityInitiates_ShouldDispatchEmailAndReturnSuccess()
    {
        var userId = Guid.NewGuid();
        var expiresAt = DateTime.UtcNow.AddHours(1);
        _identityCommands.Setup(x => x.InitiatePasswordResetAsync(
                "user@example.com", It.IsAny<TimeSpan>(), false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PasswordResetInitiatedDto(
                userId, "user@example.com", "Test User", "token123", expiresAt, false));

        var result = await _handler.Handle(new SendPasswordResetCommand("user@example.com"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.UserId.Should().Be(userId);
        result.Value.TokenExpiresAt.Should().Be(expiresAt);
        result.Value.UserNotFound.Should().BeFalse();
        _typedEmailService.Verify(x => x.SendEmailAsync(
            It.IsAny<IEmailParameters>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenIdentityReturnsNull_ShouldReturnSecuritySuccess()
    {
        _identityCommands.Setup(x => x.InitiatePasswordResetAsync(
                It.IsAny<string>(), It.IsAny<TimeSpan>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PasswordResetInitiatedDto?)null);

        var result = await _handler.Handle(new SendPasswordResetCommand("missing@example.com"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.UserNotFound.Should().BeTrue();
        _typedEmailService.Verify(x => x.SendEmailAsync(
            It.IsAny<IEmailParameters>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithInvalidEmailFormat_ShouldReturnFailure()
    {
        var result = await _handler.Handle(new SendPasswordResetCommand("not-an-email"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Invalid email format");
        _identityCommands.Verify(x => x.InitiatePasswordResetAsync(
            It.IsAny<string>(), It.IsAny<TimeSpan>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
