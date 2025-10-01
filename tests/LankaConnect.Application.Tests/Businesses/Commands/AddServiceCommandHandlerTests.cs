using FluentAssertions;
using LankaConnect.Application.Businesses.Commands.AddService;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Tests.TestHelpers;
using LankaConnect.Domain.Business;
using LankaConnect.Domain.Business.ValueObjects;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Common.Exceptions;
using Moq;

namespace LankaConnect.Application.Tests.Businesses.Commands;

public class AddServiceCommandHandlerTests
{
    private readonly Mock<IBusinessRepository> _mockBusinessRepository;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly AddServiceCommandHandler _handler;

    public AddServiceCommandHandlerTests()
    {
        _mockBusinessRepository = new Mock<IBusinessRepository>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _handler = new AddServiceCommandHandler(_mockBusinessRepository.Object, _mockUnitOfWork.Object);
    }

    [Fact]
    public async Task Handle_WithValidServiceCommand_ShouldAddServiceSuccessfully()
    {
        // Arrange
        var businessId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var existingBusiness = TestDataBuilder.CreateValidBusiness(ownerId);
        var command = new AddServiceCommand(
            businessId,
            "Lunch Buffet",
            "All-you-can-eat lunch buffet",
            25.99m,
            "90 minutes",
            true
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
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBe(Guid.Empty);

        // Service is added to business directly, no separate repository call
        _mockUnitOfWork.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithUsRestaurantService_ShouldAddServiceWithUsdPricing()
    {
        // Arrange - Add a typical US restaurant service with USD pricing
        var businessId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var existingBusiness = TestDataBuilder.CreateValidBusiness(ownerId);
        var command = new AddServiceCommand(
            businessId,
            "Sri Lankan Lunch Buffet",
            "All-you-can-eat authentic Sri Lankan lunch buffet featuring rice, curry, and traditional sides",
            25.99m, // USD pricing typical for US restaurant buffets
            "90 minutes", // 1.5 hour service duration
            true // Available
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
        result.Value.Should().NotBe(Guid.Empty);
        
        // Verify business repository was called
        _mockBusinessRepository.Verify(x => x.GetByIdAsync(businessId, It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData("Takeout Service", "Quick takeout for busy professionals", 15.50)]
    [InlineData("Catering Package", "Full-service catering for events up to 50 people", 450.00)]
    [InlineData("Private Dining", "Exclusive private dining experience for special occasions", 75.00)]
    [InlineData("Lunch Special", "Daily lunch special with rice, curry, and beverage", 12.99)]
    public async Task Handle_WithVariousUsRestaurantServices_ShouldAddSuccessfully(string serviceName, string description, decimal price)
    {
        // Arrange
        var businessId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var existingBusiness = TestDataBuilder.CreateValidBusiness(ownerId);
        var command = new AddServiceCommand(businessId, serviceName, description, price, "60 minutes", true);

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
        result.Value.Should().NotBe(Guid.Empty);
        
        _mockBusinessRepository.Verify(x => x.GetByIdAsync(businessId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithNonExistentBusiness_ShouldThrowBusinessNotFoundException()
    {
        // Arrange
        var businessId = Guid.NewGuid();
        var command = new AddServiceCommand(
            businessId,
            "Test Service",
            "Test Description",
            10.00m,
            "30 minutes",
            true
        );

        _mockBusinessRepository
            .Setup(x => x.GetByIdAsync(businessId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Business?)null);

        // Act & Assert
        await _handler.Invoking(x => x.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<BusinessNotFoundException>();

        _mockUnitOfWork.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithInactiveService_ShouldAddInactiveService()
    {
        // Arrange
        var businessId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var existingBusiness = TestDataBuilder.CreateValidBusiness(ownerId);
        var command = new AddServiceCommand(
            businessId,
            "Seasonal Special",
            "Limited time seasonal menu item",
            18.99m,
            "45 minutes",
            false // Not available
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
        result.Value.Should().NotBe(Guid.Empty);

        _mockUnitOfWork.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task Handle_WithInvalidServiceName_ShouldReturnFailureResult(string? invalidName)
    {
        // Arrange
        var businessId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var existingBusiness = TestDataBuilder.CreateValidBusiness(ownerId);
        var command = new AddServiceCommand(
            businessId,
            invalidName!,
            "Valid description",
            10.00m,
            "30 minutes",
            true
        );

        _mockBusinessRepository
            .Setup(x => x.GetByIdAsync(businessId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingBusiness);

        // Act & Assert
        await _handler.Invoking(x => x.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<InvalidOperationException>();

        _mockUnitOfWork.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData(-1.00)]
    [InlineData(-0.01)]
    [InlineData(-100.00)]
    public async Task Handle_WithInvalidPrice_ShouldThrowInvalidOperationException(decimal invalidPrice)
    {
        // Arrange
        var businessId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var existingBusiness = TestDataBuilder.CreateValidBusiness(ownerId);
        var command = new AddServiceCommand(
            businessId,
            "Valid Service",
            "Valid description",
            invalidPrice,
            "30 minutes",
            true
        );

        _mockBusinessRepository
            .Setup(x => x.GetByIdAsync(businessId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingBusiness);

        // Act & Assert
        await _handler.Invoking(x => x.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<InvalidOperationException>();

        _mockUnitOfWork.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithFreeService_ShouldAddSuccessfully()
    {
        // Arrange - Test that free services (price = 0) are allowed
        var businessId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var existingBusiness = TestDataBuilder.CreateValidBusiness(ownerId);
        var command = new AddServiceCommand(
            businessId,
            "Free Consultation",
            "Initial consultation is free for new customers",
            0.00m, // Free service
            "15 minutes",
            true
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
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBe(Guid.Empty);

        _mockUnitOfWork.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}