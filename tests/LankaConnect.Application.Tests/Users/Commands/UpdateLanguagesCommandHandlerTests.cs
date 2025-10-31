using FluentAssertions;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Users.Commands.UpdateLanguages;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Shared.ValueObjects;
using LankaConnect.Domain.Users;
using LankaConnect.Domain.Users.Enums;
using LankaConnect.Domain.Users.ValueObjects;
using Moq;
using Xunit;

namespace LankaConnect.Application.Tests.Users.Commands;

/// <summary>
/// TDD RED phase tests for UpdateLanguagesCommand
/// Business rules: 1-5 languages required (cannot be empty)
/// </summary>
public class UpdateLanguagesCommandHandlerTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly UpdateLanguagesCommandHandler _handler;

    public UpdateLanguagesCommandHandlerTests()
    {
        _userRepositoryMock = new Mock<IUserRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _handler = new UpdateLanguagesCommandHandler(
            _userRepositoryMock.Object,
            _unitOfWorkMock.Object);
    }

    private User CreateTestUser()
    {
        var email = Email.Create("test@example.com");
        return User.Create(email.Value, "John", "Doe").Value;
    }

    [Fact]
    public async Task Handle_Should_Update_Languages_Successfully()
    {
        // Arrange
        var user = CreateTestUser();
        var command = new UpdateLanguagesCommand
        {
            UserId = user.Id,
            Languages = new List<LanguageDto>
            {
                new() { LanguageCode = "si", ProficiencyLevel = ProficiencyLevel.Native },
                new() { LanguageCode = "en", ProficiencyLevel = ProficiencyLevel.Advanced }
            }
        };

        _userRepositoryMock
            .Setup(r => r.GetByIdAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        user.Languages.Should().HaveCount(2);
        _userRepositoryMock.Verify(r => r.Update(user), Times.Once);
        _unitOfWorkMock.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_Should_Fail_When_User_Not_Found()
    {
        // Arrange
        var command = new UpdateLanguagesCommand
        {
            UserId = Guid.NewGuid(),
            Languages = new List<LanguageDto>
            {
                new() { LanguageCode = "si", ProficiencyLevel = ProficiencyLevel.Native }
            }
        };

        _userRepositoryMock
            .Setup(r => r.GetByIdAsync(command.UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("not found");
    }

    [Fact]
    public async Task Handle_Should_Fail_When_Empty_Languages()
    {
        // Arrange
        var user = CreateTestUser();
        var command = new UpdateLanguagesCommand
        {
            UserId = user.Id,
            Languages = new List<LanguageDto>()
        };

        _userRepositoryMock
            .Setup(r => r.GetByIdAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("At least 1");
    }

    [Fact]
    public async Task Handle_Should_Fail_When_Invalid_Language_Code()
    {
        // Arrange
        var user = CreateTestUser();
        var command = new UpdateLanguagesCommand
        {
            UserId = user.Id,
            Languages = new List<LanguageDto>
            {
                new() { LanguageCode = "invalid", ProficiencyLevel = ProficiencyLevel.Native }
            }
        };

        _userRepositoryMock
            .Setup(r => r.GetByIdAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Invalid");
    }

    [Fact]
    public async Task Handle_Should_Fail_When_More_Than_5_Languages()
    {
        // Arrange
        var user = CreateTestUser();
        var command = new UpdateLanguagesCommand
        {
            UserId = user.Id,
            Languages = Enumerable.Range(0, 6).Select(_ => new LanguageDto
            {
                LanguageCode = "si",
                ProficiencyLevel = ProficiencyLevel.Native
            }).ToList()
        };

        _userRepositoryMock
            .Setup(r => r.GetByIdAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("5");
    }
}
