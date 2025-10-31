using FluentAssertions;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Tests.TestHelpers;
using LankaConnect.Application.Users.Commands.UpdateUserLocation;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Shared.ValueObjects;
using LankaConnect.Domain.Users;
using LankaConnect.Domain.Users.ValueObjects;
using Moq;
using Xunit;

namespace LankaConnect.Application.Tests.Users.Commands;

/// <summary>
/// TDD RED Phase - Tests for UpdateUserLocationCommandHandler
/// </summary>
public class UpdateUserLocationCommandHandlerTests
{
    private readonly Mock<IUserRepository> _userRepository;
    private readonly Mock<IUnitOfWork> _unitOfWork;
    private readonly UpdateUserLocationCommandHandler _handler;

    public UpdateUserLocationCommandHandlerTests()
    {
        _userRepository = TestHelpers.MockRepository.CreateUserRepository();
        _unitOfWork = TestHelpers.MockRepository.CreateUnitOfWork();
        _handler = new UpdateUserLocationCommandHandler(
            _userRepository.Object,
            _unitOfWork.Object);
    }

    [Fact]
    public async Task Handle_WithValidLocation_ShouldUpdateUserAndReturnSuccess()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var email = Email.Create("test@example.com").Value;
        var user = User.Create(email, "John", "Doe").Value;

        var command = new UpdateUserLocationCommand
        {
            UserId = userId,
            City = "Los Angeles",
            State = "California",
            ZipCode = "90001",
            Country = "United States"
        };

        _userRepository.Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        user.Location.Should().NotBeNull();
        user.Location!.City.Should().Be("Los Angeles");
        user.Location.State.Should().Be("California");
        user.Location.ZipCode.Should().Be("90001");
        user.Location.Country.Should().Be("United States");

        _userRepository.Verify(x => x.Update(user), Times.Once);
        _unitOfWork.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenUserNotFound_ShouldReturnFailure()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var command = new UpdateUserLocationCommand
        {
            UserId = userId,
            City = "Los Angeles",
            State = "California",
            ZipCode = "90001",
            Country = "United States"
        };

        _userRepository.Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain("User not found");

        _userRepository.Verify(x => x.Update(It.IsAny<User>()), Times.Never);
        _unitOfWork.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithInvalidCity_ShouldReturnFailure()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var email = Email.Create("test@example.com").Value;
        var user = User.Create(email, "John", "Doe").Value;

        var command = new UpdateUserLocationCommand
        {
            UserId = userId,
            City = "",  // Invalid
            State = "California",
            ZipCode = "90001",
            Country = "United States"
        };

        _userRepository.Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain("City is required");

        _userRepository.Verify(x => x.Update(It.IsAny<User>()), Times.Never);
        _unitOfWork.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithNullLocation_ShouldClearLocationAndReturnSuccess()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var email = Email.Create("test@example.com").Value;
        var user = User.Create(email, "John", "Doe").Value;

        // Set initial location
        var initialLocation = UserLocation.Create("Toronto", "Ontario", "M5H 2N2", "Canada").Value;
        user.UpdateLocation(initialLocation);

        var command = new UpdateUserLocationCommand
        {
            UserId = userId,
            City = null,  // Clearing location
            State = null,
            ZipCode = null,
            Country = null
        };

        _userRepository.Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        user.Location.Should().BeNull();

        _userRepository.Verify(x => x.Update(user), Times.Once);
        _unitOfWork.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithInternationalLocation_ShouldSucceed()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var email = Email.Create("test@example.com").Value;
        var user = User.Create(email, "John", "Doe").Value;

        var command = new UpdateUserLocationCommand
        {
            UserId = userId,
            City = "London",
            State = "England",
            ZipCode = "SW1A 1AA",
            Country = "United Kingdom"
        };

        _userRepository.Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        user.Location.Should().NotBeNull();
        user.Location!.City.Should().Be("London");
        user.Location.Country.Should().Be("United Kingdom");
    }

    [Fact]
    public async Task Handle_ShouldUpdateExistingLocation()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var email = Email.Create("test@example.com").Value;
        var user = User.Create(email, "John", "Doe").Value;

        // Set initial location
        var initialLocation = UserLocation.Create("Los Angeles", "California", "90001", "United States").Value;
        user.UpdateLocation(initialLocation);

        var command = new UpdateUserLocationCommand
        {
            UserId = userId,
            City = "San Francisco",
            State = "California",
            ZipCode = "94102",
            Country = "United States"
        };

        _userRepository.Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        user.Location.Should().NotBeNull();
        user.Location!.City.Should().Be("San Francisco");
        user.Location.ZipCode.Should().Be("94102");
    }
}
