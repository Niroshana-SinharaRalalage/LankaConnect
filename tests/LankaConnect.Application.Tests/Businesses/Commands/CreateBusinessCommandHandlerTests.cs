using LankaConnect.Application.Businesses.Commands.CreateBusiness;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Tests.TestHelpers;
using LankaConnect.Domain.Business;
using LankaConnect.Domain.Business.Enums;
using LankaConnect.Domain.Common;
using Microsoft.Extensions.Logging;

namespace LankaConnect.Application.Tests.Businesses.Commands;

public class CreateBusinessCommandHandlerTests
{
    private readonly Mock<IBusinessRepository> _mockBusinessRepository;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<ILogger<CreateBusinessCommandHandler>> _mockLogger;
    private readonly CreateBusinessCommandHandler _handler;
    private readonly Fixture _fixture;

    public CreateBusinessCommandHandlerTests()
    {
        _mockBusinessRepository = new Mock<IBusinessRepository>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockLogger = new Mock<ILogger<CreateBusinessCommandHandler>>();
        _handler = new CreateBusinessCommandHandler(_mockBusinessRepository.Object, _mockUnitOfWork.Object);
        _fixture = new Fixture();
    }

    [Fact]
    public async Task Handle_WithValidUsBusinessCommand_ShouldCreateBusinessSuccessfully()
    {
        // Arrange
        var command = TestDataBuilder.CreateValidUsBusinessCommand();
        var cancellationToken = CancellationToken.None;

        _mockBusinessRepository
            .Setup(x => x.AddAsync(It.IsAny<Business>(), cancellationToken))
            .Returns(Task.CompletedTask);

        _mockUnitOfWork
            .Setup(x => x.CommitAsync(cancellationToken))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, cancellationToken);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBe(Guid.Empty);

        _mockBusinessRepository.Verify(x => x.AddAsync(It.IsAny<Business>(), cancellationToken), Times.Once);
        _mockUnitOfWork.Verify(x => x.CommitAsync(cancellationToken), Times.Once);
    }

    [Fact]
    public async Task Handle_WithUsPhoneNumber_ShouldCreateBusinessWithCorrectContactInfo()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var command = TestDataBuilder.CreateValidUsBusinessCommand(ownerId) with
        {
            ContactPhone = "+1-555-123-4567",
            ContactEmail = "test@usrestaurant.com"
        };

        Business capturedBusiness = null!;
        _mockBusinessRepository
            .Setup(x => x.AddAsync(It.IsAny<Business>(), It.IsAny<CancellationToken>()))
            .Callback<Business, CancellationToken>((business, _) => capturedBusiness = business)
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        capturedBusiness.Should().NotBeNull();
        capturedBusiness.ContactInfo.PhoneNumber?.Value.Should().Be("+1-555-123-4567");
        capturedBusiness.ContactInfo.Email?.Value.Should().Be("test@usrestaurant.com");
        capturedBusiness.OwnerId.Should().Be(ownerId);
    }

    [Fact]
    public async Task Handle_WithUsAddress_ShouldCreateBusinessWithCorrectLocation()
    {
        // Arrange
        var command = TestDataBuilder.CreateValidUsBusinessCommand() with
        {
            Address = "123 Main Street",
            City = "New York",
            Province = "NY", // US State
            PostalCode = "10001"
        };

        Business capturedBusiness = null!;
        _mockBusinessRepository
            .Setup(x => x.AddAsync(It.IsAny<Business>(), It.IsAny<CancellationToken>()))
            .Callback<Business, CancellationToken>((business, _) => capturedBusiness = business)
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        capturedBusiness.Should().NotBeNull();
        capturedBusiness.Location.Address.Street.Should().Be("123 Main Street");
        capturedBusiness.Location.Address.City.Should().Be("New York");
        capturedBusiness.Location.Address.State.Should().Be("NY");
        capturedBusiness.Location.Address.ZipCode.Should().Be("10001");
    }

    [Fact]
    public async Task Handle_WithUsRestaurantCategory_ShouldSetCorrectCategory()
    {
        // Arrange
        var command = TestDataBuilder.CreateValidUsBusinessCommand() with
        {
            Category = BusinessCategory.Restaurant,
            Categories = new List<string> { "Sri Lankan", "Asian", "Fine Dining" },
            Tags = new List<string> { "authentic", "spicy", "family-owned" }
        };

        Business capturedBusiness = null!;
        _mockBusinessRepository
            .Setup(x => x.AddAsync(It.IsAny<Business>(), It.IsAny<CancellationToken>()))
            .Callback<Business, CancellationToken>((business, _) => capturedBusiness = business)
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        capturedBusiness.Should().NotBeNull();
        capturedBusiness.Category.Should().Be(BusinessCategory.Restaurant);
    }

    [Fact]
    public async Task Handle_WithUsCoordinates_ShouldCreateBusinessWithCorrectLocation()
    {
        // Arrange - Manhattan coordinates
        var command = TestDataBuilder.CreateValidUsBusinessCommand() with
        {
            Latitude = 40.7589, // Manhattan latitude
            Longitude = -73.9851 // Manhattan longitude
        };

        Business capturedBusiness = null!;
        _mockBusinessRepository
            .Setup(x => x.AddAsync(It.IsAny<Business>(), It.IsAny<CancellationToken>()))
            .Callback<Business, CancellationToken>((business, _) => capturedBusiness = business)
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        capturedBusiness.Should().NotBeNull();
        capturedBusiness.Location.Coordinates?.Latitude.Should().Be(40.7589m);
        capturedBusiness.Location.Coordinates?.Longitude.Should().Be(-73.9851m);
    }

    [Theory]
    [InlineData(BusinessCategory.Restaurant)]
    [InlineData(BusinessCategory.Healthcare)]
    [InlineData(BusinessCategory.Services)]
    [InlineData(BusinessCategory.Retail)]
    [InlineData(BusinessCategory.Transportation)]
    public async Task Handle_WithDifferentUsBusinessCategories_ShouldCreateSuccessfully(BusinessCategory category)
    {
        // Arrange
        var command = TestDataBuilder.CreateValidUsBusinessCommand() with { Category = category };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public async Task Handle_WhenRepositoryThrowsException_ShouldPropagateException()
    {
        // Arrange
        var command = TestDataBuilder.CreateValidUsBusinessCommand();
        var expectedException = new InvalidOperationException("Database error");

        _mockBusinessRepository
            .Setup(x => x.AddAsync(It.IsAny<Business>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(expectedException);

        // Act & Assert
        await _handler.Invoking(x => x.Handle(command, CancellationToken.None))
                     .Should().ThrowAsync<InvalidOperationException>()
                     .WithMessage("Database error");

        _mockUnitOfWork.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenUnitOfWorkFails_ShouldPropagateException()
    {
        // Arrange
        var command = TestDataBuilder.CreateValidUsBusinessCommand();
        var expectedException = new InvalidOperationException("Transaction failed");

        _mockBusinessRepository
            .Setup(x => x.AddAsync(It.IsAny<Business>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockUnitOfWork
            .Setup(x => x.CommitAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(expectedException);

        // Act & Assert
        await _handler.Invoking(x => x.Handle(command, CancellationToken.None))
                     .Should().ThrowAsync<InvalidOperationException>()
                     .WithMessage("Transaction failed");
    }

    [Fact]
    public async Task Handle_WithCancellationToken_ShouldPassTokenToRepository()
    {
        // Arrange
        var command = TestDataBuilder.CreateValidUsBusinessCommand();
        var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;

        // Act
        await _handler.Handle(command, cancellationToken);

        // Assert
        _mockBusinessRepository.Verify(x => x.AddAsync(It.IsAny<Business>(), cancellationToken), Times.Once);
        _mockUnitOfWork.Verify(x => x.CommitAsync(cancellationToken), Times.Once);
    }

    [Fact]
    public async Task Handle_WithMinimumValidData_ShouldCreateBusinessSuccessfully()
    {
        // Arrange - Test with minimal required US business data
        var ownerId = Guid.NewGuid();
        var command = new CreateBusinessCommand(
            Name: "Lanka Cafe",
            Description: "Sri Lankan food",
            ContactPhone: "+1-555-123-4567",
            ContactEmail: "info@lankacafe.com",
            Website: "https://lankacafe.com",
            Address: "100 Main St",
            City: "Boston",
            Province: "MA",
            PostalCode: "02101",
            Latitude: 42.3601,
            Longitude: -71.0589,
            Category: BusinessCategory.Restaurant,
            OwnerId: ownerId,
            Categories: new List<string> { "Restaurant" },
            Tags: new List<string> { "sri-lankan" }
        );

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public async Task Handle_WithComplexUsBusinessData_ShouldHandleAllProperties()
    {
        // Arrange
        var command = TestDataBuilder.CreateValidUsBusinessCommand() with
        {
            Name = "Royal Ceylon Palace Restaurant",
            Description = "Authentic Sri Lankan cuisine with modern American twist, serving the greater Los Angeles area since 1995",
            ContactPhone = "+1-323-555-0199",
            ContactEmail = "contact@royalceylonpalace.com",
            Website = "https://www.royalceylonpalace.com",
            Address = "8847 Sunset Boulevard",
            City = "West Hollywood",
            Province = "CA",
            PostalCode = "90069",
            Latitude = 34.0972,
            Longitude = -118.3865,
            Category = BusinessCategory.Restaurant,
            Categories = new List<string> { "Sri Lankan Cuisine", "Asian Fusion", "Fine Dining", "Vegetarian Options", "Halal Certified" },
            Tags = new List<string> { "authentic", "family-owned", "dine-in", "takeout", "catering", "spicy", "traditional", "modern" }
        };

        Business capturedBusiness = null!;
        _mockBusinessRepository
            .Setup(x => x.AddAsync(It.IsAny<Business>(), It.IsAny<CancellationToken>()))
            .Callback<Business, CancellationToken>((business, _) => capturedBusiness = business);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        capturedBusiness.Should().NotBeNull();
        capturedBusiness.Profile.Name.Should().Be("Royal Ceylon Palace Restaurant");
        capturedBusiness.Profile.Description.Should().Contain("Los Angeles");
        capturedBusiness.ContactInfo.PhoneNumber?.Value.Should().Be("+1-323-555-0199");
        capturedBusiness.ContactInfo.Email?.Value.Should().Be("contact@royalceylonpalace.com");
        capturedBusiness.Location.Address.City.Should().Be("West Hollywood");
        capturedBusiness.Location.Address.State.Should().Be("CA");
    }
}