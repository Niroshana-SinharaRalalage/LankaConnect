using LankaConnect.Application.Tests.TestHelpers;
using LankaConnect.Modules.Identity.Application.Commands.Users.CreateUser;
using LankaConnect.Domain.Common;
using LankaConnect.Modules.Identity.Domain.Entities;
using LankaConnect.Modules.Identity.Domain.Repositories;
using LankaConnect.Modules.Identity.Domain.DomainEvents;
using LankaConnect.Modules.Identity.Domain.Events;
using LankaConnect.Modules.Identity.Domain.ValueObjects;
using Microsoft.Extensions.Logging.Abstractions;
using Email = LankaConnect.Domain.Shared.ValueObjects.Email;

namespace LankaConnect.Modules.Identity.Application.Tests.Commands.Users;

public class CreateUserCommandHandlerTests
{
    private readonly Mock<IUserRepository> _userRepository;
    private readonly Mock<IUnitOfWork> _unitOfWork;
    private readonly CreateUserCommandHandler _handler;

    public CreateUserCommandHandlerTests()
    {
        _userRepository = LankaConnect.Application.Tests.TestHelpers.MockRepository.CreateUserRepository();
        _unitOfWork = LankaConnect.Application.Tests.TestHelpers.MockRepository.CreateUnitOfWork();
        _handler = new CreateUserCommandHandler(_userRepository.Object, _unitOfWork.Object, NullLogger<CreateUserCommandHandler>.Instance);
    }

    [Fact]
    public async Task Handle_WithValidCommand_ShouldCreateUserAndReturnId()
    {
        // Arrange
        var command = LankaConnect.Application.Tests.TestHelpers.TestDataBuilder.CreateValidUserCommand();

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeEmpty();
        
        _userRepository.Verify(x => x.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithInvalidEmail_ShouldReturnFailure()
    {
        // Arrange
        var command = LankaConnect.Application.Tests.TestHelpers.TestDataBuilder.CreateUserCommandWithInvalidEmail();

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().NotBeEmpty();
        
        _userRepository.Verify(x => x.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWork.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenEmailAlreadyExists_ShouldReturnFailure()
    {
        // Arrange
        var command = LankaConnect.Application.Tests.TestHelpers.TestDataBuilder.CreateValidUserCommand();
        var email = Email.Create(command.Email).Value;
        
        _userRepository.Setup(x => x.ExistsWithEmailAsync(email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain("A user with this email already exists");
        
        _userRepository.Verify(x => x.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWork.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithInvalidPhoneNumber_ShouldReturnFailure()
    {
        // Arrange
        var command = LankaConnect.Application.Tests.TestHelpers.TestDataBuilder.CreateValidUserCommand() with { PhoneNumber = "invalid-phone" };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().NotBeEmpty();
        
        _userRepository.Verify(x => x.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWork.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithEmptyFirstName_ShouldReturnFailure()
    {
        // Arrange
        var command = LankaConnect.Application.Tests.TestHelpers.TestDataBuilder.CreateValidUserCommand() with { FirstName = "" };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Handle_WithEmptyLastName_ShouldReturnFailure()
    {
        // Arrange
        var command = LankaConnect.Application.Tests.TestHelpers.TestDataBuilder.CreateValidUserCommand() with { LastName = "" };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Handle_WithoutOptionalFields_ShouldCreateUser()
    {
        // Arrange
        var command = LankaConnect.Application.Tests.TestHelpers.TestDataBuilder.CreateValidUserCommand() with 
        { 
            PhoneNumber = null, 
            Bio = null 
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeEmpty();
        
        _userRepository.Verify(x => x.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenRepositoryThrows_ShouldPropagateException()
    {
        // Arrange
        var command = LankaConnect.Application.Tests.TestHelpers.TestDataBuilder.CreateValidUserCommand();
        _userRepository.Setup(x => x.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Database error"));

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => 
            _handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_WhenUnitOfWorkFails_ShouldPropagateException()
    {
        // Arrange
        var command = LankaConnect.Application.Tests.TestHelpers.TestDataBuilder.CreateValidUserCommand();
        _unitOfWork.Setup(x => x.CommitAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Transaction failed"));

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => 
            _handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_WithCancellationRequested_ShouldThrowOperationCancelledException()
    {
        // Arrange
        var command = LankaConnect.Application.Tests.TestHelpers.TestDataBuilder.CreateValidUserCommand();
        var cancellationToken = new CancellationToken(true);

        _userRepository.Setup(x => x.ExistsWithEmailAsync(It.IsAny<Email>(), It.IsAny<CancellationToken>()))
            .Returns((Email email, CancellationToken ct) => {
                ct.ThrowIfCancellationRequested();
                return Task.FromResult(false);
            });

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(() => 
            _handler.Handle(command, cancellationToken));
    }
}
