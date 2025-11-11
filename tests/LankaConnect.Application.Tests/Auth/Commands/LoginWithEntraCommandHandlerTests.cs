using Xunit;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using LankaConnect.Application.Auth.Commands.LoginWithEntra;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Users;
using LankaConnect.Domain.Users.Enums;
using LankaConnect.Domain.Common;
using Email = LankaConnect.Domain.Shared.ValueObjects.Email;

namespace LankaConnect.Application.Tests.Auth.Commands;

public class LoginWithEntraCommandHandlerTests
{
    private readonly Mock<IUserRepository> _mockUserRepository;
    private readonly Mock<IEntraExternalIdService> _mockEntraService;
    private readonly Mock<IJwtTokenService> _mockJwtTokenService;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<ITokenConfiguration> _mockTokenConfiguration;
    private readonly Mock<ILogger<LoginWithEntraCommandHandler>> _mockLogger;
    private readonly LoginWithEntraCommandHandler _handler;

    public LoginWithEntraCommandHandlerTests()
    {
        _mockUserRepository = new Mock<IUserRepository>();
        _mockEntraService = new Mock<IEntraExternalIdService>();
        _mockJwtTokenService = new Mock<IJwtTokenService>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockTokenConfiguration = new Mock<ITokenConfiguration>();
        _mockLogger = new Mock<ILogger<LoginWithEntraCommandHandler>>();

        _mockTokenConfiguration.Setup(c => c.AccessTokenExpirationMinutes).Returns(15);
        _mockTokenConfiguration.Setup(c => c.RefreshTokenExpirationDays).Returns(7);
        _mockEntraService.Setup(e => e.IsEnabled).Returns(true);

        _handler = new LoginWithEntraCommandHandler(
            _mockUserRepository.Object,
            _mockEntraService.Object,
            _mockJwtTokenService.Object,
            _mockUnitOfWork.Object,
            _mockTokenConfiguration.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task Handle_WithValidToken_ExistingUser_ShouldReturnSuccessWithTokens()
    {
        // Arrange
        var request = new LoginWithEntraCommand("valid-entra-token", "127.0.0.1");

        var entraUserInfo = new EntraUserInfo
        {
            ObjectId = "entra-oid-123",
            Email = "test@example.com",
            FirstName = "John",
            LastName = "Doe",
            DisplayName = "John Doe",
            EmailVerified = true
        };

        var email = Email.Create(entraUserInfo.Email).Value;
        var existingUser = User.CreateFromExternalProvider(
            IdentityProvider.EntraExternal,
            entraUserInfo.ObjectId,
            email,
            entraUserInfo.FirstName,
            entraUserInfo.LastName,
            FederatedProvider.Microsoft,
            entraUserInfo.Email,
            UserRole.GeneralUser).Value;

        _mockEntraService.Setup(e => e.GetUserInfoAsync(request.AccessToken))
            .ReturnsAsync(Result<EntraUserInfo>.Success(entraUserInfo));

        _mockUserRepository.Setup(r => r.GetByExternalProviderIdAsync(entraUserInfo.ObjectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingUser);

        _mockJwtTokenService.Setup(j => j.GenerateAccessTokenAsync(existingUser))
            .ReturnsAsync(Result<string>.Success("access-token"));

        _mockJwtTokenService.Setup(j => j.GenerateRefreshTokenAsync())
            .ReturnsAsync(Result<string>.Success("refresh-token"));

        _mockUnitOfWork.Setup(u => u.CommitAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.UserId.Should().Be(existingUser.Id);
        result.Value.Email.Should().Be(entraUserInfo.Email);
        result.Value.FullName.Should().Be($"{entraUserInfo.FirstName} {entraUserInfo.LastName}");
        result.Value.Role.Should().Be(UserRole.GeneralUser);
        result.Value.AccessToken.Should().Be("access-token");
        result.Value.RefreshToken.Should().Be("refresh-token");
        result.Value.IsNewUser.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_WithValidToken_NewUser_ShouldAutoProvisionAndReturnSuccess()
    {
        // Arrange
        var request = new LoginWithEntraCommand("valid-entra-token", "127.0.0.1");

        var entraUserInfo = new EntraUserInfo
        {
            ObjectId = "entra-oid-new-user",
            Email = "newuser@example.com",
            FirstName = "Jane",
            LastName = "Smith",
            DisplayName = "Jane Smith",
            EmailVerified = true
        };

        _mockEntraService.Setup(e => e.GetUserInfoAsync(request.AccessToken))
            .ReturnsAsync(Result<EntraUserInfo>.Success(entraUserInfo));

        _mockUserRepository.Setup(r => r.GetByExternalProviderIdAsync(entraUserInfo.ObjectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        _mockUserRepository.Setup(r => r.ExistsWithEmailAsync(It.IsAny<Email>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _mockJwtTokenService.Setup(j => j.GenerateAccessTokenAsync(It.IsAny<User>()))
            .ReturnsAsync(Result<string>.Success("access-token"));

        _mockJwtTokenService.Setup(j => j.GenerateRefreshTokenAsync())
            .ReturnsAsync(Result<string>.Success("refresh-token"));

        _mockUnitOfWork.Setup(u => u.CommitAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Email.Should().Be(entraUserInfo.Email);
        result.Value.FullName.Should().Be($"{entraUserInfo.FirstName} {entraUserInfo.LastName}");
        result.Value.Role.Should().Be(UserRole.GeneralUser);
        result.Value.IsNewUser.Should().BeTrue();

        _mockUserRepository.Verify(r => r.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithInvalidToken_ShouldReturnFailure()
    {
        // Arrange
        var request = new LoginWithEntraCommand("invalid-token", "127.0.0.1");

        _mockEntraService.Setup(e => e.GetUserInfoAsync(request.AccessToken))
            .ReturnsAsync(Result<EntraUserInfo>.Failure("Invalid access token"));

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain("Invalid access token");
    }

    [Fact]
    public async Task Handle_WhenEntraDisabled_ShouldReturnFailure()
    {
        // Arrange
        _mockEntraService.Setup(e => e.IsEnabled).Returns(false);
        var request = new LoginWithEntraCommand("token", "127.0.0.1");

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain("Entra External ID authentication is not enabled");
    }

    [Fact]
    public async Task Handle_WhenEmailAlreadyExistsWithDifferentProvider_ShouldReturnFailure()
    {
        // Arrange
        var request = new LoginWithEntraCommand("valid-entra-token", "127.0.0.1");

        var entraUserInfo = new EntraUserInfo
        {
            ObjectId = "entra-oid-conflict",
            Email = "existing@example.com",
            FirstName = "John",
            LastName = "Conflict",
            DisplayName = "John Conflict",
            EmailVerified = true
        };

        _mockEntraService.Setup(e => e.GetUserInfoAsync(request.AccessToken))
            .ReturnsAsync(Result<EntraUserInfo>.Success(entraUserInfo));

        _mockUserRepository.Setup(r => r.GetByExternalProviderIdAsync(entraUserInfo.ObjectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        _mockUserRepository.Setup(r => r.ExistsWithEmailAsync(It.IsAny<Email>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Contains("already registered"));
    }

    [Fact]
    public async Task Handle_WithValidToken_ShouldAddRefreshTokenWithCorrectExpiry()
    {
        // Arrange
        var request = new LoginWithEntraCommand("valid-entra-token", "127.0.0.1");

        var entraUserInfo = new EntraUserInfo
        {
            ObjectId = "entra-oid-token-test",
            Email = "tokentest@example.com",
            FirstName = "Token",
            LastName = "Test",
            DisplayName = "Token Test",
            EmailVerified = true
        };

        var email = Email.Create(entraUserInfo.Email).Value;
        var existingUser = User.CreateFromExternalProvider(
            IdentityProvider.EntraExternal,
            entraUserInfo.ObjectId,
            email,
            entraUserInfo.FirstName,
            entraUserInfo.LastName,
            FederatedProvider.Microsoft,
            entraUserInfo.Email,
            UserRole.GeneralUser).Value;

        _mockEntraService.Setup(e => e.GetUserInfoAsync(request.AccessToken))
            .ReturnsAsync(Result<EntraUserInfo>.Success(entraUserInfo));

        _mockUserRepository.Setup(r => r.GetByExternalProviderIdAsync(entraUserInfo.ObjectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingUser);

        _mockJwtTokenService.Setup(j => j.GenerateAccessTokenAsync(existingUser))
            .ReturnsAsync(Result<string>.Success("access-token"));

        _mockJwtTokenService.Setup(j => j.GenerateRefreshTokenAsync())
            .ReturnsAsync(Result<string>.Success("refresh-token"));

        _mockUnitOfWork.Setup(u => u.CommitAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.TokenExpiresAt.Should().BeCloseTo(DateTime.UtcNow.AddMinutes(15), TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task Handle_WithIpAddress_ShouldStoreIpInRefreshToken()
    {
        // Arrange
        var ipAddress = "192.168.1.100";
        var request = new LoginWithEntraCommand("valid-entra-token", ipAddress);

        var entraUserInfo = new EntraUserInfo
        {
            ObjectId = "entra-oid-ip-test",
            Email = "iptest@example.com",
            FirstName = "IP",
            LastName = "Test",
            DisplayName = "IP Test",
            EmailVerified = true
        };

        var email = Email.Create(entraUserInfo.Email).Value;
        var existingUser = User.CreateFromExternalProvider(
            IdentityProvider.EntraExternal,
            entraUserInfo.ObjectId,
            email,
            entraUserInfo.FirstName,
            entraUserInfo.LastName,
            FederatedProvider.Microsoft,
            entraUserInfo.Email,
            UserRole.GeneralUser).Value;

        _mockEntraService.Setup(e => e.GetUserInfoAsync(request.AccessToken))
            .ReturnsAsync(Result<EntraUserInfo>.Success(entraUserInfo));

        _mockUserRepository.Setup(r => r.GetByExternalProviderIdAsync(entraUserInfo.ObjectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingUser);

        _mockJwtTokenService.Setup(j => j.GenerateAccessTokenAsync(existingUser))
            .ReturnsAsync(Result<string>.Success("access-token"));

        _mockJwtTokenService.Setup(j => j.GenerateRefreshTokenAsync())
            .ReturnsAsync(Result<string>.Success("refresh-token"));

        _mockUnitOfWork.Setup(u => u.CommitAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        // Verify refresh token was added with IP address
        existingUser.RefreshTokens.Should().Contain(rt => rt.CreatedByIp == ipAddress);
    }
}
