using FluentAssertions;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Tests.TestHelpers;
using LankaConnect.Application.Users.Commands.LinkExternalProvider;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Shared.ValueObjects;
using LankaConnect.Domain.Users;
using LankaConnect.Domain.Users.Enums;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace LankaConnect.Application.Tests.Users.Commands;

/// <summary>
/// TDD RED Phase - Tests for LinkExternalProviderCommandHandler
/// Epic 1 Phase 2: Multi-Provider Social Login
/// These tests will FAIL until we implement the handler (expected behavior for TDD RED)
/// </summary>
public class LinkExternalProviderCommandHandlerTests
{
    private readonly Mock<IUserRepository> _userRepository;
    private readonly Mock<IUnitOfWork> _unitOfWork;
    private readonly Mock<ILogger<LinkExternalProviderHandler>> _logger;
    private readonly LinkExternalProviderHandler _handler;

    public LinkExternalProviderCommandHandlerTests()
    {
        _userRepository = TestHelpers.MockRepository.CreateUserRepository();
        _unitOfWork = TestHelpers.MockRepository.CreateUnitOfWork();
        _logger = new Mock<ILogger<LinkExternalProviderHandler>>();
        _handler = new LinkExternalProviderHandler(
            _userRepository.Object,
            _unitOfWork.Object,
            _logger.Object);
    }

    [Fact]
    public async Task Handle_WithValidData_ShouldLinkProviderAndReturnSuccess()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var email = Email.Create("test@example.com").Value;
        var user = User.Create(email, "John", "Doe").Value;

        var command = new LinkExternalProviderCommand(
            userId,
            FederatedProvider.Facebook,
            "facebook-123456",
            "john@facebook.com");

        _userRepository.Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _unitOfWork.Setup(x => x.CommitAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.UserId.Should().Be(userId);
        result.Value.Provider.Should().Be(FederatedProvider.Facebook);
        result.Value.ProviderEmail.Should().Be("john@facebook.com");
        result.Value.LinkedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));

        user.ExternalLogins.Should().ContainSingle();
        user.ExternalLogins.Should().Contain(login =>
            login.Provider == FederatedProvider.Facebook &&
            login.ExternalProviderId == "facebook-123456" &&
            login.ProviderEmail == "john@facebook.com");
    }

    [Fact]
    public async Task Handle_WhenUserNotFound_ShouldReturnFailure()
    {
        // Arrange
        var command = new LinkExternalProviderCommand(
            Guid.NewGuid(),
            FederatedProvider.Google,
            "google-789",
            "test@google.com");

        _userRepository.Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Contains("User not found") || e.Contains("not found"));
    }

    [Fact]
    public async Task Handle_WhenProviderAlreadyLinked_ShouldReturnFailure()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var email = Email.Create("test@example.com").Value;
        var user = User.Create(email, "John", "Doe").Value;

        // User already has Facebook linked
        user.LinkExternalProvider(FederatedProvider.Facebook, "facebook-existing", "john@facebook.com");

        var command = new LinkExternalProviderCommand(
            userId,
            FederatedProvider.Facebook,
            "facebook-new-id",
            "john@facebook.com");

        _userRepository.Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Contains("already linked"));
    }

    [Fact]
    public async Task Handle_ShouldCallUnitOfWorkCommit()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var email = Email.Create("test@example.com").Value;
        var user = User.Create(email, "John", "Doe").Value;

        var command = new LinkExternalProviderCommand(
            userId,
            FederatedProvider.Apple,
            "apple-id-123",
            "john@appleid.apple.com");

        _userRepository.Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _unitOfWork.Setup(x => x.CommitAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _unitOfWork.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenCommitFails_ShouldReturnFailure()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var email = Email.Create("test@example.com").Value;
        var user = User.Create(email, "John", "Doe").Value;

        var command = new LinkExternalProviderCommand(
            userId,
            FederatedProvider.Google,
            "google-123",
            "john@google.com");

        _userRepository.Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _unitOfWork.Setup(x => x.CommitAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(0); // Commit failed

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Contains("Failed to save") || e.Contains("save"));
    }

    [Fact]
    public async Task Handle_WithMultipleProviders_ShouldLinkSuccessfully()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var email = Email.Create("test@example.com").Value;
        var user = User.Create(email, "John", "Doe").Value;

        // User already has Facebook linked
        user.LinkExternalProvider(FederatedProvider.Facebook, "facebook-123", "john@facebook.com");

        // Now linking Google
        var command = new LinkExternalProviderCommand(
            userId,
            FederatedProvider.Google,
            "google-456",
            "john@google.com");

        _userRepository.Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _unitOfWork.Setup(x => x.CommitAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        user.ExternalLogins.Should().HaveCount(2);
        user.ExternalLogins.Should().Contain(login => login.Provider == FederatedProvider.Facebook);
        user.ExternalLogins.Should().Contain(login => login.Provider == FederatedProvider.Google);
    }

    [Fact]
    public async Task Handle_ShouldLogInformationOnSuccess()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var email = Email.Create("test@example.com").Value;
        var user = User.Create(email, "John", "Doe").Value;

        var command = new LinkExternalProviderCommand(
            userId,
            FederatedProvider.Microsoft,
            "microsoft-789",
            "john@outlook.com");

        _userRepository.Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _unitOfWork.Setup(x => x.CommitAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _logger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("linked") || v.ToString()!.Contains("Microsoft")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task Handle_ShouldRaiseDomainEvent()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var email = Email.Create("test@example.com").Value;
        var user = User.Create(email, "John", "Doe").Value;
        user.ClearDomainEvents(); // Clear UserCreatedEvent

        var command = new LinkExternalProviderCommand(
            userId,
            FederatedProvider.Apple,
            "apple-xyz",
            "john@apple.com");

        _userRepository.Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _unitOfWork.Setup(x => x.CommitAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        user.DomainEvents.Should().ContainSingle();
        user.DomainEvents.First().GetType().Name.Should().Be("ExternalProviderLinkedEvent");
    }
}
