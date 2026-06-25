using LankaConnect.Application.Communications.Commands.SendEmailVerification;
using LankaConnect.Domain.Common;
using LankaConnect.Modules.Identity.Contracts;
using Microsoft.Extensions.Logging;
using Moq;
using FluentAssertions;
using Xunit;

namespace LankaConnect.Application.Tests.Communications.Commands;

/// <summary>
/// Wave 4.7.b (2026-06-25) -- handler now reads a UserContactDto via
/// IIdentityQueries.GetContactInfoAsync (for the already-verified +
/// throttle short-circuits) and dispatches IIdentityCommands.
/// InitiateEmailVerificationAsync for the actual mutator. Old tests
/// covered IUserRepository + IUnitOfWork concerns now owned by the
/// Identity adapter.
/// </summary>
public class SendEmailVerificationCommandHandlerTests
{
    private readonly Mock<IIdentityQueries> _identityQueries;
    private readonly Mock<IIdentityCommands> _identityCommands;
    private readonly Mock<ILogger<SendEmailVerificationCommandHandler>> _logger;
    private readonly SendEmailVerificationCommandHandler _handler;

    public SendEmailVerificationCommandHandlerTests()
    {
        _identityQueries = new Mock<IIdentityQueries>();
        _identityCommands = new Mock<IIdentityCommands>();
        _logger = new Mock<ILogger<SendEmailVerificationCommandHandler>>();

        _handler = new SendEmailVerificationCommandHandler(
            _identityQueries.Object,
            _identityCommands.Object,
            _logger.Object);
    }

    private static UserContactDto BuildContact(Guid id, bool emailVerified = false, DateTime? tokenExpiresAt = null)
    {
        return new UserContactDto(
            Id: id,
            Email: "user@example.com",
            FirstName: "Test",
            LastName: "User",
            DisplayName: "Test User",
            ProfilePhotoUrl: null,
            IsActive: true,
            IsEmailVerified: emailVerified,
            IsAccountLocked: false,
            Role: UserRoleDto.GeneralUser,
            EmailVerificationTokenExpiresAt: tokenExpiresAt,
            PhoneNumber: null);
    }

    [Fact]
    public async Task Handle_WhenUserNotFound_ShouldReturnFailure()
    {
        var userId = Guid.NewGuid();
        _identityQueries.Setup(x => x.GetContactInfoAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserContactDto?)null);

        var result = await _handler.Handle(new SendEmailVerificationCommand(userId), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("User not found");
    }

    [Fact]
    public async Task Handle_WhenEmailAlreadyVerifiedAndNotForceResend_ShouldShortCircuit()
    {
        var userId = Guid.NewGuid();
        _identityQueries.Setup(x => x.GetContactInfoAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildContact(userId, emailVerified: true));

        var result = await _handler.Handle(new SendEmailVerificationCommand(userId, ForceResend: false), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.WasRecentlySent.Should().BeFalse();
        _identityCommands.Verify(x => x.InitiateEmailVerificationAsync(
            It.IsAny<Guid>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenIdentityInitiates_ShouldReturnSuccessResponse()
    {
        var userId = Guid.NewGuid();
        var expiresAt = DateTime.UtcNow.AddHours(24);
        _identityQueries.Setup(x => x.GetContactInfoAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildContact(userId, emailVerified: false));
        _identityCommands.Setup(x => x.InitiateEmailVerificationAsync(userId, It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EmailVerificationInitiatedDto(userId, "user@example.com", "Test User", "token123", expiresAt));

        var result = await _handler.Handle(new SendEmailVerificationCommand(userId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.UserId.Should().Be(userId);
        result.Value.TokenExpiresAt.Should().Be(expiresAt);
    }
}
