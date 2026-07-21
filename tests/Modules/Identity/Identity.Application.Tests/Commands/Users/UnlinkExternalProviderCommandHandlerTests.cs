using FluentAssertions;
using LankaConnect.BuildingBlocks.Application.Common.Interfaces;
using LankaConnect.Application.Tests.TestHelpers;
using LankaConnect.Modules.Identity.Application.Commands.Users.UnlinkExternalProvider;
using LankaConnect.BuildingBlocks.Domain;
using LankaConnect.SharedKernel.Money;
using LankaConnect.Modules.Identity.Domain.Entities;
using LankaConnect.Modules.Identity.Domain.Repositories;
using LankaConnect.Modules.Identity.Domain.DomainEvents;
using LankaConnect.Modules.Identity.Domain.Events;
using LankaConnect.Modules.Identity.Domain.Enums;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace LankaConnect.Modules.Identity.Application.Tests.Commands.Users;

/// <summary>
/// TDD RED Phase - Tests for UnlinkExternalProviderCommandHandler
/// Epic 1 Phase 2: Multi-Provider Social Login
/// These tests will FAIL until we implement the handler (expected behavior for TDD RED)
/// </summary>
public class UnlinkExternalProviderCommandHandlerTests
{
    private readonly Mock<IUserRepository> _userRepository;
    private readonly Mock<IUnitOfWork> _unitOfWork;
    private readonly Mock<ILogger<UnlinkExternalProviderHandler>> _logger;
    private readonly UnlinkExternalProviderHandler _handler;

    public UnlinkExternalProviderCommandHandlerTests()
    {
        _userRepository = LankaConnect.Application.Tests.TestHelpers.MockRepository.CreateUserRepository();
        _unitOfWork = LankaConnect.Application.Tests.TestHelpers.MockRepository.CreateUnitOfWork();
        _logger = new Mock<ILogger<UnlinkExternalProviderHandler>>();
        _handler = new UnlinkExternalProviderHandler(
            _userRepository.Object,
            _unitOfWork.Object,
            _logger.Object);
    }

    [Fact]
    public async Task Handle_WithValidData_ShouldUnlinkProviderAndReturnSuccess()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var email = Email.Create("test@example.com").Value;
        var user = User.Create(email, "John", "Doe").Value;
        user.SetPassword("hashed-password"); // User has password, can unlink

        // Link Facebook
        user.LinkExternalProvider(FederatedProvider.Facebook, "facebook-123", "john@facebook.com");

        var command = new UnlinkExternalProviderCommand(userId, FederatedProvider.Facebook);

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
        result.Value.UnlinkedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));

        user.ExternalLogins.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_WhenUserNotFound_ShouldReturnFailure()
    {
        // Arrange
        var command = new UnlinkExternalProviderCommand(Guid.NewGuid(), FederatedProvider.Google);

        _userRepository.Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Contains("User not found") || e.Contains("not found"));
    }

    [Fact]
    public async Task Handle_WhenProviderNotLinked_ShouldReturnFailure()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var email = Email.Create("test@example.com").Value;
        var user = User.Create(email, "John", "Doe").Value;

        var command = new UnlinkExternalProviderCommand(userId, FederatedProvider.Apple);

        _userRepository.Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Contains("not linked"));
    }

    [Fact]
    public async Task Handle_WhenLastAuthMethod_ShouldReturnFailure()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var email = Email.Create("external@example.com").Value;
        var user = User.CreateFromExternalProvider(
            IdentityProvider.EntraExternal,
            "entra-123",
            email,
            "John",
            "Doe",
            FederatedProvider.Microsoft,
            email.Value).Value;

        // User has only Microsoft provider, no password
        var command = new UnlinkExternalProviderCommand(userId, FederatedProvider.Microsoft);

        _userRepository.Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Contains("last authentication method"));
    }

    [Fact]
    public async Task Handle_WhenUserHasOtherProviders_ShouldUnlinkSuccessfully()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var email = Email.Create("test@example.com").Value;
        var user = User.CreateFromExternalProvider(
            IdentityProvider.EntraExternal,
            "entra-123",
            email,
            "John",
            "Doe",
            FederatedProvider.Microsoft,
            email.Value).Value;

        // Link additional providers
        user.LinkExternalProvider(FederatedProvider.Facebook, "facebook-123", "john@facebook.com");
        user.LinkExternalProvider(FederatedProvider.Google, "google-456", "john@google.com");

        var command = new UnlinkExternalProviderCommand(userId, FederatedProvider.Facebook);

        _userRepository.Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _unitOfWork.Setup(x => x.CommitAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        user.ExternalLogins.Should().HaveCount(2); // Microsoft and Google remain
        user.ExternalLogins.Should().NotContain(login => login.Provider == FederatedProvider.Facebook);
    }

    [Fact]
    public async Task Handle_ShouldCallUnitOfWorkCommit()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var email = Email.Create("test@example.com").Value;
        var user = User.Create(email, "John", "Doe").Value;
        user.SetPassword("password");
        user.LinkExternalProvider(FederatedProvider.Google, "google-123", "john@google.com");

        var command = new UnlinkExternalProviderCommand(userId, FederatedProvider.Google);

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
        user.SetPassword("password");
        user.LinkExternalProvider(FederatedProvider.Apple, "apple-123", "john@apple.com");

        var command = new UnlinkExternalProviderCommand(userId, FederatedProvider.Apple);

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
    public async Task Handle_ShouldRaiseDomainEvent()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var email = Email.Create("test@example.com").Value;
        var user = User.Create(email, "John", "Doe").Value;
        user.SetPassword("password");
        user.LinkExternalProvider(FederatedProvider.Microsoft, "microsoft-123", "john@outlook.com");
        user.ClearDomainEvents(); // Clear UserCreatedEvent and ExternalProviderLinkedEvent

        var command = new UnlinkExternalProviderCommand(userId, FederatedProvider.Microsoft);

        _userRepository.Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _unitOfWork.Setup(x => x.CommitAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        user.DomainEvents.Should().ContainSingle();
        user.DomainEvents.First().GetType().Name.Should().Be("ExternalProviderUnlinkedEvent");
    }
}
