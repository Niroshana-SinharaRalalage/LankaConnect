using Xunit;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using LankaConnect.Modules.Communications.Application.Commands.VerifyEmail;
using LankaConnect.BuildingBlocks.Domain;
using LankaConnect.Modules.Identity.Contracts;
using LankaConnect.Modules.Communications.Contracts.Email.Contracts;
using LankaConnect.Modules.Communications.Contracts.Email.Services;

namespace LankaConnect.Application.Tests.Communications.Commands;

/// <summary>
/// Wave 4.7.b (2026-06-25) -- handler now thin shell over
/// <see cref="IIdentityCommands.CompleteEmailVerificationAsync"/>. Old
/// tests covered IUserRepository + IUnitOfWork concerns which now live
/// inside the Identity adapter.
/// </summary>
public class VerifyEmailCommandHandlerTests
{
    private readonly Mock<IIdentityCommands> _identityCommands;
    private readonly Mock<ITypedEmailService> _typedEmailService;
    private readonly Mock<ILogger<VerifyEmailCommandHandler>> _logger;
    private readonly VerifyEmailCommandHandler _handler;

    public VerifyEmailCommandHandlerTests()
    {
        _identityCommands = new Mock<IIdentityCommands>();
        _typedEmailService = new Mock<ITypedEmailService>();
        _logger = new Mock<ILogger<VerifyEmailCommandHandler>>();

        _typedEmailService.Setup(x => x.SendEmailAsync(
            It.IsAny<IEmailParameters>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(TypedEmailSendResult.Ok(Guid.NewGuid().ToString(), 100));

        _handler = new VerifyEmailCommandHandler(
            _identityCommands.Object,
            _typedEmailService.Object,
            _logger.Object);
    }

    [Fact]
    public async Task Handle_WithEmptyToken_ShouldReturnFailure()
    {
        var result = await _handler.Handle(new VerifyEmailCommand(""), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Invalid or expired verification token");
    }

    [Fact]
    public async Task Handle_WhenIdentityCompletes_ShouldReturnSuccessResponse()
    {
        var userId = Guid.NewGuid();
        _identityCommands.Setup(x => x.CompleteEmailVerificationAsync("validtoken", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EmailVerificationCompletedDto(userId, "user@example.com", "Test User"));

        var result = await _handler.Handle(new VerifyEmailCommand("validtoken"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.UserId.Should().Be(userId);
        result.Value.Email.Should().Be("user@example.com");
    }

    [Fact]
    public async Task Handle_WhenIdentityThrowsInvalidOperation_ShouldReturnGenericFailure()
    {
        _identityCommands.Setup(x => x.CompleteEmailVerificationAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("token mismatch"));

        var result = await _handler.Handle(new VerifyEmailCommand("badtoken"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Invalid or expired verification token");
    }
}
