using Xunit;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using LankaConnect.Application.Communications.Commands.ResetPassword;
using LankaConnect.Domain.Common;
using LankaConnect.Modules.Identity.Contracts;
using LankaConnect.Shared.Email.Contracts;
using LankaConnect.Shared.Email.Services;

namespace LankaConnect.Application.Tests.Communications.Commands;

/// <summary>
/// Wave 4.7.b (2026-06-25) -- handler now thin shell over
/// <see cref="IIdentityCommands.CompletePasswordResetAsync"/>. Old tests
/// covered password-hashing + token-validation + unit-of-work concerns
/// which now live inside the Identity adapter; this rewrite verifies only
/// the handler's responsibilities: dispatch the mutator, shape the
/// response, and fire-and-forget the confirmation email.
/// </summary>
public class ResetPasswordCommandHandlerTests
{
    private readonly Mock<IIdentityCommands> _identityCommands;
    private readonly Mock<ITypedEmailService> _typedEmailService;
    private readonly Mock<ILogger<ResetPasswordCommandHandler>> _logger;
    private readonly ResetPasswordCommandHandler _handler;

    public ResetPasswordCommandHandlerTests()
    {
        _identityCommands = new Mock<IIdentityCommands>();
        _typedEmailService = new Mock<ITypedEmailService>();
        _logger = new Mock<ILogger<ResetPasswordCommandHandler>>();

        _typedEmailService.Setup(x => x.SendEmailAsync(
            It.IsAny<IEmailParameters>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(TypedEmailSendResult.Ok(Guid.NewGuid().ToString(), 100));

        _handler = new ResetPasswordCommandHandler(
            _identityCommands.Object,
            _typedEmailService.Object,
            _logger.Object);
    }

    [Fact]
    public async Task Handle_WhenIdentityCompletes_ShouldReturnSuccessResponse()
    {
        var userId = Guid.NewGuid();
        var command = new ResetPasswordCommand("validtoken", "NewP@ss123!", "user@example.com");
        _identityCommands.Setup(x => x.CompletePasswordResetAsync(
                command.Token, command.Email, command.NewPassword, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PasswordResetCompletedDto(userId, "user@example.com", "Test User"));

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.UserId.Should().Be(userId);
        result.Value.Email.Should().Be("user@example.com");
    }

    [Fact]
    public async Task Handle_WhenIdentityThrowsInvalidOperation_ShouldReturnGenericFailure()
    {
        var command = new ResetPasswordCommand("badtoken", "NewP@ss123!", null);
        _identityCommands.Setup(x => x.CompletePasswordResetAsync(
                It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("token expired"));

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Invalid or expired reset token");
    }
}
