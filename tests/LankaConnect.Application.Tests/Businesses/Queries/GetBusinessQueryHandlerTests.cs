using AutoMapper;
using LankaConnect.Application.Businesses.Common;
using LankaConnect.Application.Businesses.Queries.GetBusiness;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Tests.TestHelpers;
using LankaConnect.Domain.Business;

namespace LankaConnect.Application.Tests.Businesses.Queries;

public class GetBusinessQueryHandlerTests
{
    private readonly Mock<IBusinessRepository> _businessRepository;
    private readonly Mock<IMapper> _mapper;
    private readonly GetBusinessQueryHandler _handler;
    private readonly Fixture _fixture;

    public GetBusinessQueryHandlerTests()
    {
        _businessRepository = new Mock<IBusinessRepository>();
        _mapper = new Mock<IMapper>();
        _handler = new GetBusinessQueryHandler(_businessRepository.Object, _mapper.Object);
        _fixture = new Fixture();
    }

    [Fact]
    public async Task Handle_WithExistingBusiness_ShouldReturnBusinessDto()
    {
        // Arrange
        var businessId = Guid.NewGuid();
        var query = new GetBusinessQuery(businessId);
        var business = TestDataBuilder.CreateValidBusiness(Guid.NewGuid());
        var businessDto = TestDataBuilder.CreateValidBusinessDto();

        _businessRepository.Setup(x => x.GetByIdAsync(businessId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(business);
        _mapper.Setup(x => x.Map<BusinessDto>(business))
            .Returns(businessDto);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEquivalentTo(businessDto);
        
        _businessRepository.Verify(x => x.GetByIdAsync(businessId, It.IsAny<CancellationToken>()), Times.Once);
        _mapper.Verify(x => x.Map<BusinessDto>(business), Times.Once);
    }

    [Fact]
    public async Task Handle_WithNonExistentBusiness_ShouldReturnSuccessWithNull()
    {
        // Arrange
        var businessId = Guid.NewGuid();
        var query = new GetBusinessQuery(businessId);

        _businessRepository.Setup(x => x.GetByIdAsync(businessId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Business?)null);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeNull();
        
        _mapper.Verify(x => x.Map<BusinessDto>(It.IsAny<Business>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithEmptyGuid_ShouldReturnSuccessWithNull()
    {
        // Arrange
        var query = new GetBusinessQuery(Guid.Empty);

        _businessRepository.Setup(x => x.GetByIdAsync(Guid.Empty, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Business?)null);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeNull();
        
        _businessRepository.Verify(x => x.GetByIdAsync(Guid.Empty, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenRepositoryThrows_ShouldPropagateException()
    {
        // Arrange
        var businessId = Guid.NewGuid();
        var query = new GetBusinessQuery(businessId);
        
        _businessRepository.Setup(x => x.GetByIdAsync(businessId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Database error"));

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => 
            _handler.Handle(query, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_WhenMapperThrows_ShouldPropagateException()
    {
        // Arrange
        var businessId = Guid.NewGuid();
        var query = new GetBusinessQuery(businessId);
        var business = TestDataBuilder.CreateValidBusiness(Guid.NewGuid());

        _businessRepository.Setup(x => x.GetByIdAsync(businessId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(business);
        _mapper.Setup(x => x.Map<BusinessDto>(business))
            .Throws(new InvalidOperationException("Mapping error"));

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => 
            _handler.Handle(query, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_WithCancellationRequested_ShouldThrowOperationCancelledException()
    {
        // Arrange
        var businessId = Guid.NewGuid();
        var query = new GetBusinessQuery(businessId);
        var cancellationToken = new CancellationToken(true);

        _businessRepository.Setup(x => x.GetByIdAsync(businessId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException());

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(() => 
            _handler.Handle(query, cancellationToken));
    }

    [Fact]
    public async Task Handle_WithValidRequest_ShouldCallRepositoryOnce()
    {
        // Arrange
        var businessId = Guid.NewGuid();
        var query = new GetBusinessQuery(businessId);
        var business = TestDataBuilder.CreateValidBusiness(Guid.NewGuid());
        var businessDto = TestDataBuilder.CreateValidBusinessDto();

        _businessRepository.Setup(x => x.GetByIdAsync(businessId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(business);
        _mapper.Setup(x => x.Map<BusinessDto>(business))
            .Returns(businessDto);

        // Act
        await _handler.Handle(query, CancellationToken.None);

        // Assert - Verify single repository call
        _businessRepository.Verify(x => x.GetByIdAsync(businessId, It.IsAny<CancellationToken>()), Times.Once);
        _businessRepository.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Handle_WithUsBusinessData_ShouldReturnUsFormattedData()
    {
        // Arrange - Test retrieval of US-based Sri Lankan business
        var businessId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var usBusiness = TestDataBuilder.CreateValidBusiness(ownerId);
        var usBusinessDto = TestDataBuilder.CreateValidBusinessDto() with
        {
            Id = businessId,
            ContactPhone = "+1-555-123-4567", // US phone format
            ContactEmail = "info@ceyloncafe.com",
            Address = "123 Broadway",
            City = "New York",
            Province = "NY", // US State format
            PostalCode = "10001" // US ZIP code
        };
        var query = new GetBusinessQuery(businessId);

        _businessRepository.Setup(x => x.GetByIdAsync(businessId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(usBusiness);
        _mapper.Setup(x => x.Map<BusinessDto>(usBusiness))
            .Returns(usBusinessDto);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.ContactPhone.Should().StartWith("+1-"); // US country code
        result.Value.Province.Should().HaveLength(2); // US state abbreviation
        result.Value.PostalCode.Should().MatchRegex(@"^\d{5}$"); // US ZIP format
        result.Value.City.Should().Be("New York");
    }

    [Fact]
    public async Task Handle_WithVerifiedUsBusiness_ShouldReturnVerificationStatus()
    {
        // Arrange
        var businessId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var business = TestDataBuilder.CreateValidBusiness(ownerId);
        var verifiedBusinessDto = TestDataBuilder.CreateValidBusinessDto() with
        {
            Id = businessId,
            IsVerified = true,
            VerifiedAt = DateTime.UtcNow.AddDays(-30)
        };
        var query = new GetBusinessQuery(businessId);

        _businessRepository.Setup(x => x.GetByIdAsync(businessId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(business);
        _mapper.Setup(x => x.Map<BusinessDto>(business))
            .Returns(verifiedBusinessDto);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.IsVerified.Should().BeTrue();
        result.Value.VerifiedAt.Should().NotBeNull();
        result.Value.VerifiedAt.Should().BeCloseTo(DateTime.UtcNow.AddDays(-30), TimeSpan.FromHours(1));
    }

    [Fact]
    public async Task Handle_WithHighRatedUsBusiness_ShouldReturnRatingData()
    {
        // Arrange
        var businessId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var business = TestDataBuilder.CreateValidBusiness(ownerId);
        var highRatedBusinessDto = TestDataBuilder.CreateValidBusinessDto() with
        {
            Id = businessId,
            Rating = 4.8m,
            ReviewCount = 127
        };
        var query = new GetBusinessQuery(businessId);

        _businessRepository.Setup(x => x.GetByIdAsync(businessId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(business);
        _mapper.Setup(x => x.Map<BusinessDto>(business))
            .Returns(highRatedBusinessDto);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Rating.Should().Be(4.8m);
        result.Value.ReviewCount.Should().Be(127);
        result.Value.Rating.Should().BeInRange(1m, 5m);
    }

    [Fact]
    public async Task Handle_WithUsBusinessCategories_ShouldReturnCategoryData()
    {
        // Arrange
        var businessId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var business = TestDataBuilder.CreateValidBusiness(ownerId);
        var categorizedBusinessDto = TestDataBuilder.CreateValidBusinessDto() with
        {
            Id = businessId,
            Categories = new List<string> { "Sri Lankan Cuisine", "Asian Restaurant", "Halal Certified", "Vegetarian Options" },
            Tags = new List<string> { "authentic", "family-owned", "takeout", "dine-in", "spicy" }
        };
        var query = new GetBusinessQuery(businessId);

        _businessRepository.Setup(x => x.GetByIdAsync(businessId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(business);
        _mapper.Setup(x => x.Map<BusinessDto>(business))
            .Returns(categorizedBusinessDto);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Categories.Should().NotBeEmpty();
        result.Value.Categories.Should().Contain("Sri Lankan Cuisine");
        result.Value.Tags.Should().Contain("authentic");
        result.Value.Tags.Should().Contain("family-owned");
    }
}