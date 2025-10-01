using LankaConnect.Application.Common.Exceptions;
using LankaConnect.Application.Tests.TestHelpers;
using LankaConnect.Application.Users.DTOs;
using LankaConnect.Application.Users.Queries.GetUserById;
using LankaConnect.Domain.Users;

namespace LankaConnect.Application.Tests.Users.Queries;

public class GetUserByIdQueryHandlerTests
{
    private readonly Mock<IUserRepository> _userRepository;
    private readonly GetUserByIdQueryHandler _handler;

    public GetUserByIdQueryHandlerTests()
    {
        _userRepository = TestHelpers.MockRepository.CreateUserRepository();
        _handler = new GetUserByIdQueryHandler(_userRepository.Object);
    }

    [Fact]
    public async Task Handle_WithExistingUser_ShouldReturnUserDto()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var query = new GetUserByIdQuery(userId);
        var user = TestDataBuilder.CreateValidUser();
        var userDto = TestDataBuilder.CreateValidUserDto();

        _userRepository.Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Id.Should().Be(user.Id);
        result.Value.Email.Should().Be(user.Email.Value);
        result.Value.FirstName.Should().Be(user.FirstName);
        result.Value.LastName.Should().Be(user.LastName);
        
        _userRepository.Verify(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithNonExistentUser_ShouldReturnFailure()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var query = new GetUserByIdQuery(userId);

        _userRepository.Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("User not found");
    }

    [Fact]
    public async Task Handle_WithEmptyGuid_ShouldReturnFailure()
    {
        // Arrange
        var query = new GetUserByIdQuery(Guid.Empty);
        
        _userRepository.Setup(x => x.GetByIdAsync(Guid.Empty, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("User not found");
        
        _userRepository.Verify(x => x.GetByIdAsync(Guid.Empty, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenRepositoryThrows_ShouldPropagateException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var query = new GetUserByIdQuery(userId);
        
        _userRepository.Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Database error"));

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => 
            _handler.Handle(query, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_WithUserWithoutPhoneNumber_ShouldReturnUserDtoWithNullPhone()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var query = new GetUserByIdQuery(userId);
        var user = TestDataBuilder.CreateValidUser();

        _userRepository.Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.PhoneNumber.Should().BeNull(); // User created without phone number
    }

    [Fact]
    public async Task Handle_WithCancellationRequested_ShouldThrowOperationCancelledException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var query = new GetUserByIdQuery(userId);
        var cancellationToken = new CancellationToken(true);
        
        _userRepository.Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException());

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(() => 
            _handler.Handle(query, cancellationToken));
    }

    [Fact]
    public async Task Handle_WithValidRequest_ShouldCallRepositoryOnce()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var query = new GetUserByIdQuery(userId);
        var user = TestDataBuilder.CreateValidUser();
        var userDto = TestDataBuilder.CreateValidUserDto();

        _userRepository.Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        await _handler.Handle(query, CancellationToken.None);

        // Assert - Verify single repository call
        _userRepository.Verify(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()), Times.Once);
        _userRepository.VerifyNoOtherCalls();
    }
}