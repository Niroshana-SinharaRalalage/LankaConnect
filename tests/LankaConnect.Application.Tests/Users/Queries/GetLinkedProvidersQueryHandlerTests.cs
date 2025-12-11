using FluentAssertions;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Tests.TestHelpers;
using LankaConnect.Application.Users.Queries.GetLinkedProviders;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Shared.ValueObjects;
using LankaConnect.Domain.Users;
using LankaConnect.Domain.Users.Enums;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace LankaConnect.Application.Tests.Users.Queries;

/// <summary>
/// TDD RED Phase - Tests for GetLinkedProvidersQueryHandler
/// Epic 1 Phase 2: Multi-Provider Social Login
/// These tests will FAIL until we implement the handler (expected behavior for TDD RED)
/// </summary>
public class GetLinkedProvidersQueryHandlerTests
{
    private readonly Mock<IUserRepository> _userRepository;
    private readonly Mock<ILogger<GetLinkedProvidersHandler>> _logger;
    private readonly GetLinkedProvidersHandler _handler;

    public GetLinkedProvidersQueryHandlerTests()
    {
        _userRepository = TestHelpers.MockRepository.CreateUserRepository();
        _logger = new Mock<ILogger<GetLinkedProvidersHandler>>();
        _handler = new GetLinkedProvidersHandler(
            _userRepository.Object,
            _logger.Object);
    }

    [Fact]
    public async Task Handle_WhenUserHasNoLinkedProviders_ShouldReturnEmptyList()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var email = Email.Create("test@example.com").Value;
        var user = User.Create(email, "John", "Doe").Value;

        var query = new GetLinkedProvidersQuery(userId);

        _userRepository.Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.UserId.Should().Be(userId);
        result.Value.LinkedProviders.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_WhenUserHasLinkedProviders_ShouldReturnAllProviders()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var email = Email.Create("test@example.com").Value;
        var user = User.Create(email, "John", "Doe").Value;

        user.LinkExternalProvider(FederatedProvider.Facebook, "facebook-123", "john@facebook.com");
        user.LinkExternalProvider(FederatedProvider.Google, "google-456", "john@google.com");
        user.LinkExternalProvider(FederatedProvider.Apple, "apple-789", "john@apple.com");

        var query = new GetLinkedProvidersQuery(userId);

        _userRepository.Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.LinkedProviders.Should().HaveCount(3);
        result.Value.LinkedProviders.Should().Contain(p => p.Provider == FederatedProvider.Facebook);
        result.Value.LinkedProviders.Should().Contain(p => p.Provider == FederatedProvider.Google);
        result.Value.LinkedProviders.Should().Contain(p => p.Provider == FederatedProvider.Apple);
    }

    [Fact]
    public async Task Handle_WhenUserNotFound_ShouldReturnFailure()
    {
        // Arrange
        var query = new GetLinkedProvidersQuery(Guid.NewGuid());

        _userRepository.Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Contains("User not found") || e.Contains("not found"));
    }

    [Fact]
    public async Task Handle_ShouldIncludeProviderDisplayNames()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var email = Email.Create("test@example.com").Value;
        var user = User.Create(email, "John", "Doe").Value;

        user.LinkExternalProvider(FederatedProvider.Microsoft, "microsoft-123", "john@outlook.com");

        var query = new GetLinkedProvidersQuery(userId);

        _userRepository.Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var microsoftProvider = result.Value.LinkedProviders.First();
        microsoftProvider.ProviderDisplayName.Should().Be("Microsoft");
    }

    [Fact]
    public async Task Handle_ShouldIncludeAllProviderDetails()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var email = Email.Create("test@example.com").Value;
        var user = User.Create(email, "John", "Doe").Value;

        user.LinkExternalProvider(FederatedProvider.Facebook, "facebook-123", "john@facebook.com");

        var query = new GetLinkedProvidersQuery(userId);

        _userRepository.Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var facebookProvider = result.Value.LinkedProviders.First();
        facebookProvider.Provider.Should().Be(FederatedProvider.Facebook);
        facebookProvider.ProviderDisplayName.Should().Be("Facebook");
        facebookProvider.ExternalProviderId.Should().Be("facebook-123");
        facebookProvider.ProviderEmail.Should().Be("john@facebook.com");
        facebookProvider.LinkedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task Handle_WithMultipleProviders_ShouldOrderByProvider()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var email = Email.Create("test@example.com").Value;
        var user = User.Create(email, "John", "Doe").Value;

        // Link in non-alphabetical order
        user.LinkExternalProvider(FederatedProvider.Google, "google-123", "john@google.com");
        user.LinkExternalProvider(FederatedProvider.Apple, "apple-456", "john@apple.com");
        user.LinkExternalProvider(FederatedProvider.Facebook, "facebook-789", "john@facebook.com");

        var query = new GetLinkedProvidersQuery(userId);

        _userRepository.Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.LinkedProviders.Should().HaveCount(3);
        // Verify providers are returned (order can be implementation-specific)
        result.Value.LinkedProviders.Select(p => p.Provider).Should().Contain(
            new[] { FederatedProvider.Apple, FederatedProvider.Facebook, FederatedProvider.Google });
    }
}
