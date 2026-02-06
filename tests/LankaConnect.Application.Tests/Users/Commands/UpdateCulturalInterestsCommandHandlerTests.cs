using FluentAssertions;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Users.Commands.UpdateCulturalInterests;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Shared.ValueObjects;
using LankaConnect.Domain.Users;
using LankaConnect.Domain.Users.ValueObjects;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace LankaConnect.Application.Tests.Users.Commands;

/// <summary>
/// TDD RED phase tests for UpdateCulturalInterestsCommand
/// Business rules: 0-10 cultural interests allowed (privacy choice)
/// </summary>
public class UpdateCulturalInterestsCommandHandlerTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly UpdateCulturalInterestsCommandHandler _handler;

    public UpdateCulturalInterestsCommandHandlerTests()
    {
        _userRepositoryMock = new Mock<IUserRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _handler = new UpdateCulturalInterestsCommandHandler(
            _userRepositoryMock.Object,
            _unitOfWorkMock.Object,
            NullLogger<UpdateCulturalInterestsCommandHandler>.Instance);
    }

    private User CreateTestUser()
    {
        var email = Email.Create("test@example.com");
        return User.Create(email.Value, "John", "Doe").Value;
    }

    [Fact]
    public async Task Handle_Should_Update_Cultural_Interests_Successfully()
    {
        // Arrange
        var user = CreateTestUser();
        var command = new UpdateCulturalInterestsCommand
        {
            UserId = user.Id,
            InterestCodes = new List<string> { "SL_CUISINE", "CRICKET" }
        };

        _userRepositoryMock
            .Setup(r => r.GetByIdAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        user.CulturalInterests.Should().HaveCount(2);
        _userRepositoryMock.Verify(r => r.Update(user), Times.Once);
        _unitOfWorkMock.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_Should_Clear_Interests_When_Empty_List()
    {
        // Arrange
        var user = CreateTestUser();
        user.UpdateCulturalInterests(new List<CulturalInterest> { CulturalInterest.SriLankanCuisine });

        var command = new UpdateCulturalInterestsCommand
        {
            UserId = user.Id,
            InterestCodes = new List<string>()
        };

        _userRepositoryMock
            .Setup(r => r.GetByIdAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        user.CulturalInterests.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_Should_Fail_When_User_Not_Found()
    {
        // Arrange
        var command = new UpdateCulturalInterestsCommand
        {
            UserId = Guid.NewGuid(),
            InterestCodes = new List<string> { "SL_CUISINE" }
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
    public async Task Handle_Should_Accept_Dynamic_EventCategory_Codes()
    {
        // Arrange - Phase 6A.47: Now accepts any EventCategory code
        var user = CreateTestUser();
        var command = new UpdateCulturalInterestsCommand
        {
            UserId = user.Id,
            InterestCodes = new List<string> { "Business", "Cultural", "Community" }
        };

        _userRepositoryMock
            .Setup(r => r.GetByIdAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert - Should succeed with dynamic codes
        result.IsSuccess.Should().BeTrue();
        user.CulturalInterests.Should().HaveCount(3);
    }

    [Fact]
    public async Task Handle_Should_Accept_More_Than_10_Interests()
    {
        // Arrange - Phase 6A.47: Removed 10-interest limit, now unlimited
        var user = CreateTestUser();
        var command = new UpdateCulturalInterestsCommand
        {
            UserId = user.Id,
            InterestCodes = Enumerable.Range(0, 15).Select(i => $"Interest_{i}").ToList()
        };

        _userRepositoryMock
            .Setup(r => r.GetByIdAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert - Should succeed with 15 interests
        result.IsSuccess.Should().BeTrue();
        user.CulturalInterests.Should().HaveCount(15);
    }
}
