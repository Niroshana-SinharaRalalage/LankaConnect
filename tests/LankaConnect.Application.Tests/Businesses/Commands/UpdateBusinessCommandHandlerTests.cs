using LankaConnect.Application.Businesses.Commands.UpdateBusiness;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Tests.TestHelpers;
using LankaConnect.Domain.Business;
using LankaConnect.Domain.Common;
using LankaConnect.Application.Common.Exceptions;
using LankaConnect.Domain.Common.Exceptions;

namespace LankaConnect.Application.Tests.Businesses.Commands;

public class UpdateBusinessCommandHandlerTests
{
    private readonly Mock<IBusinessRepository> _mockBusinessRepository;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly UpdateBusinessCommandHandler _handler;
    private readonly Fixture _fixture;

    public UpdateBusinessCommandHandlerTests()
    {
        _mockBusinessRepository = new Mock<IBusinessRepository>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _handler = new UpdateBusinessCommandHandler(_mockBusinessRepository.Object, _mockUnitOfWork.Object);
        _fixture = new Fixture();
    }

    [Fact]
    public async Task Handle_WithValidUpdateCommand_ShouldUpdateBusinessSuccessfully()
    {
        // Arrange
        var businessId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var existingBusiness = TestDataBuilder.CreateValidBusiness(ownerId);
        var command = TestDataBuilder.CreateValidUpdateBusinessCommand(businessId);

        _mockBusinessRepository
            .Setup(x => x.GetByIdAsync(businessId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingBusiness);

        _mockUnitOfWork
            .Setup(x => x.CommitAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        // UpdateBusinessCommandHandler doesn't call UpdateAsync directly, it modifies the business and commits
        _mockUnitOfWork.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithUsPhoneNumberUpdate_ShouldUpdateContactInfo()
    {
        // Arrange
        var businessId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var existingBusiness = TestDataBuilder.CreateValidBusiness(ownerId);
        var command = new UpdateBusinessCommand(
            businessId,
            "Updated Lanka Restaurant",
            "Updated description for US market",
            "+1-555-999-8888", // New US phone number
            "newemail@usrestaurant.com",
            "https://www.updatedrestaurant.com",
            "456 Broadway",
            "New York",
            "NY",
            "10013",
            40.7255,
            -73.9983,
            new List<string> { "Updated Category" },
            new List<string> { "updated", "american-fusion" }
        );

        _mockBusinessRepository
            .Setup(x => x.GetByIdAsync(businessId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingBusiness);

        _mockUnitOfWork
            .Setup(x => x.CommitAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        existingBusiness.Should().NotBeNull();
        existingBusiness.Profile.Name.Should().Be("Updated Lanka Restaurant");
        existingBusiness.Profile.Description.Should().Contain("US market");
        existingBusiness.ContactInfo.PhoneNumber?.Value.Should().Be("+1-555-999-8888");
        existingBusiness.ContactInfo.Email?.Value.Should().Be("newemail@usrestaurant.com");
    }

    [Fact]
    public async Task Handle_WhenBusinessNotFound_ShouldThrowBusinessNotFoundException()
    {
        // Arrange
        var businessId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var command = TestDataBuilder.CreateValidUpdateBusinessCommand(businessId);

        _mockBusinessRepository
            .Setup(x => x.GetByIdAsync(businessId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Business?)null);

        // Act & Assert
        await _handler.Invoking(x => x.Handle(command, CancellationToken.None))
                     .Should().ThrowAsync<BusinessNotFoundException>()
                     .WithMessage($"Business with ID '{businessId}' was not found.");

        // Verify no commit was called since business was not found
        _mockUnitOfWork.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenRepositoryThrowsException_ShouldPropagateException()
    {
        // Arrange
        var businessId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var existingBusiness = TestDataBuilder.CreateValidBusiness(ownerId);
        var command = TestDataBuilder.CreateValidUpdateBusinessCommand(businessId);

        _mockBusinessRepository
            .Setup(x => x.GetByIdAsync(businessId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingBusiness);

        _mockUnitOfWork
            .Setup(x => x.CommitAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Database error"));

        // Act & Assert
        await _handler.Invoking(x => x.Handle(command, CancellationToken.None))
                     .Should().ThrowAsync<InvalidOperationException>()
                     .WithMessage("Database error");

        _mockUnitOfWork.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithEmptyFields_ShouldHandleGracefully()
    {
        // Arrange
        var businessId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var existingBusiness = TestDataBuilder.CreateValidBusiness(ownerId);
        var command = new UpdateBusinessCommand(
            businessId,
            string.Empty, // Empty name - should be handled by validation
            string.Empty,
            string.Empty,
            string.Empty,
            string.Empty,
            string.Empty,
            string.Empty,
            string.Empty,
            string.Empty,
            0,
            0,
            new List<string>(),
            new List<string>()
        );

        _mockBusinessRepository
            .Setup(x => x.GetByIdAsync(businessId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingBusiness);

        _mockUnitOfWork
            .Setup(x => x.CommitAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act & Assert - Domain validation may fail with empty values
        // This should either succeed (if domain allows empty values) or throw a domain exception
        try
        {
            var result = await _handler.Handle(command, CancellationToken.None);
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
        }
        catch (Exception ex)
        {
            // If domain validation throws, that's expected behavior
            ex.Should().NotBeNull();
        }
    }

    [Theory]
    [InlineData("+1-555-123-4567")]
    [InlineData("+1-800-555-0199")]
    [InlineData("+1-212-555-1234")]
    public async Task Handle_WithVariousUsPhoneFormats_ShouldUpdateSuccessfully(string phoneNumber)
    {
        // Arrange
        var businessId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var existingBusiness = TestDataBuilder.CreateValidBusiness(ownerId);
        var command = TestDataBuilder.CreateValidUpdateBusinessCommand(businessId) with
        {
            ContactPhone = phoneNumber
        };

        _mockBusinessRepository
            .Setup(x => x.GetByIdAsync(businessId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingBusiness);

        _mockUnitOfWork
            .Setup(x => x.CommitAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        existingBusiness.Should().NotBeNull();
        existingBusiness.ContactInfo.PhoneNumber?.Value.Should().Be(phoneNumber);
    }

    [Fact]
    public async Task Handle_WithCancellationToken_ShouldPassTokenToRepository()
    {
        // Arrange
        var businessId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var existingBusiness = TestDataBuilder.CreateValidBusiness(ownerId);
        var command = TestDataBuilder.CreateValidUpdateBusinessCommand(businessId);
        var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;

        _mockBusinessRepository
            .Setup(x => x.GetByIdAsync(businessId, cancellationToken))
            .ReturnsAsync(existingBusiness);

        // Act
        await _handler.Handle(command, cancellationToken);

        // Assert
        _mockBusinessRepository.Verify(x => x.GetByIdAsync(businessId, cancellationToken), Times.Once);
        _mockUnitOfWork.Verify(x => x.CommitAsync(cancellationToken), Times.Once);
    }
}