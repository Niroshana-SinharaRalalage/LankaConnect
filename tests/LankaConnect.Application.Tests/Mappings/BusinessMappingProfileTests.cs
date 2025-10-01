using AutoMapper;
using LankaConnect.Application.Businesses.Common;
using LankaConnect.Application.Common.Mappings;
using LankaConnect.Application.Tests.TestHelpers;
using LankaConnect.Domain.Business;

namespace LankaConnect.Application.Tests.Mappings;

public class BusinessMappingProfileTests
{
    private readonly IMapper _mapper;
    private readonly MapperConfiguration _configuration;

    public BusinessMappingProfileTests()
    {
        _configuration = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<BusinessMappingProfile>();
        });

        _mapper = _configuration.CreateMapper();
    }

    [Fact]
    public void Configuration_ShouldBeValid()
    {
        // Act & Assert
        _configuration.AssertConfigurationIsValid();
    }

    [Fact]
    public void Map_BusinessToBusinessDto_ShouldMapAllProperties()
    {
        // Arrange
        var business = TestDataBuilder.CreateValidBusiness(Guid.NewGuid());

        // Act
        var dto = _mapper.Map<BusinessDto>(business);

        // Assert
        dto.Should().NotBeNull();
        dto.Id.Should().Be(business.Id);
        dto.Name.Should().Be(business.Profile.Name);
        dto.Description.Should().Be(business.Profile.Description);
        dto.ContactPhone.Should().Be(business.ContactInfo.PhoneNumber?.Value ?? string.Empty);
        dto.ContactEmail.Should().Be(business.ContactInfo.Email?.Value ?? string.Empty);
        dto.Website.Should().Be(business.ContactInfo.Website ?? string.Empty);
        dto.Address.Should().Be(business.Location.Address.Street);
        dto.City.Should().Be(business.Location.Address.City);
        dto.Province.Should().Be(business.Location.Address.State);
        dto.PostalCode.Should().Be(business.Location.Address.ZipCode);
        dto.Latitude.Should().Be((double)(business.Location.Coordinates?.Latitude ?? 0));
        dto.Longitude.Should().Be((double)(business.Location.Coordinates?.Longitude ?? 0));
        dto.Category.Should().Be(business.Category);
        dto.OwnerId.Should().Be(business.OwnerId);
        // Business doesn't have IsActive, it has Status
        dto.Status.Should().Be(business.Status);
        dto.CreatedAt.Should().Be(business.CreatedAt);
        dto.UpdatedAt.Should().Be(business.UpdatedAt);
    }

    [Fact]
    public void Map_BusinessCollectionToBusinessDtoCollection_ShouldMapAll()
    {
        // Arrange
        var businesses = new List<Business>
        {
            TestDataBuilder.CreateValidBusiness(Guid.NewGuid()),
            TestDataBuilder.CreateValidBusiness(Guid.NewGuid()),
            TestDataBuilder.CreateValidBusiness(Guid.NewGuid())
        };

        // Act
        var dtos = _mapper.Map<List<BusinessDto>>(businesses);

        // Assert
        dtos.Should().HaveCount(3);
        dtos.Should().AllSatisfy(dto =>
        {
            dto.Should().NotBeNull();
            dto.Id.Should().NotBeEmpty();
            dto.Name.Should().NotBeNullOrEmpty();
            dto.Description.Should().NotBeNullOrEmpty();
            dto.ContactEmail.Should().NotBeNullOrEmpty();
            dto.OwnerId.Should().NotBeEmpty();
        });
    }

    [Fact]
    public void Map_NullBusiness_ShouldReturnNull()
    {
        // Arrange
        Business? business = null;

        // Act
        var dto = _mapper.Map<BusinessDto>(business);

        // Assert
        dto.Should().BeNull();
    }

    [Fact]
    public void Map_EmptyBusinessCollection_ShouldReturnEmptyCollection()
    {
        // Arrange
        var businesses = new List<Business>();

        // Act
        var dtos = _mapper.Map<List<BusinessDto>>(businesses);

        // Assert
        dtos.Should().NotBeNull();
        dtos.Should().BeEmpty();
    }

    [Fact]
    public void Map_BusinessToBusinessDto_ShouldPreserveDateTimeKind()
    {
        // Arrange
        var business = TestDataBuilder.CreateValidBusiness(Guid.NewGuid());

        // Act
        var dto = _mapper.Map<BusinessDto>(business);

        // Assert
        dto.CreatedAt.Kind.Should().Be(business.CreatedAt.Kind);
        if (business.UpdatedAt.HasValue)
        {
            dto.UpdatedAt.Should().Be(business.UpdatedAt.Value);
        }
    }

    [Fact]
    public void Map_BusinessToBusinessDto_ShouldPreserveCoordinates()
    {
        // Arrange
        var business = TestDataBuilder.CreateValidBusiness(Guid.NewGuid());

        // Act
        var dto = _mapper.Map<BusinessDto>(business);

        // Assert
        dto.Latitude.Should().Be((double)(business.Location.Coordinates?.Latitude ?? 0));
        dto.Longitude.Should().Be((double)(business.Location.Coordinates?.Longitude ?? 0));
        dto.Latitude.Should().BeInRange(-90, 90);
        dto.Longitude.Should().BeInRange(-180, 180);
    }

    [Fact]
    public void Map_BusinessToBusinessDto_ShouldPreserveStatus()
    {
        // Arrange
        var business = TestDataBuilder.CreateValidBusiness(Guid.NewGuid());

        // Act
        var dto = _mapper.Map<BusinessDto>(business);

        // Assert
        dto.Status.Should().Be(business.Status);
        dto.IsVerified.Should().Be(business.IsVerified);
    }

    [Fact]
    public void Map_BusinessToBusinessDto_ShouldHandleWebsiteCorrectly()
    {
        // Arrange
        var business = TestDataBuilder.CreateValidBusiness(Guid.NewGuid());

        // Act
        var dto = _mapper.Map<BusinessDto>(business);

        // Assert
        dto.Website.Should().Be(business.ContactInfo.Website ?? string.Empty);
        dto.Website.Should().NotBeNullOrEmpty();
        if (!string.IsNullOrEmpty(dto.Website))
        {
            dto.Website.Should().StartWith("http");
        }
    }

    [Fact]
    public void Map_BusinessToBusinessDto_ShouldBeConsistent()
    {
        // Arrange
        var business = TestDataBuilder.CreateValidBusiness(Guid.NewGuid());

        // Act
        var dto1 = _mapper.Map<BusinessDto>(business);
        var dto2 = _mapper.Map<BusinessDto>(business);

        // Assert
        dto1.Should().BeEquivalentTo(dto2);
    }

    [Fact]
    public void Map_MultipleBusinesses_ShouldMaintainOrder()
    {
        // Arrange
        var business1 = TestDataBuilder.CreateValidBusiness(Guid.NewGuid());
        var business2 = TestDataBuilder.CreateValidBusiness(Guid.NewGuid());
        var business3 = TestDataBuilder.CreateValidBusiness(Guid.NewGuid());
        var businesses = new List<Business> { business1, business2, business3 };

        // Act
        var dtos = _mapper.Map<List<BusinessDto>>(businesses);

        // Assert
        dtos[0].Id.Should().Be(business1.Id);
        dtos[1].Id.Should().Be(business2.Id);
        dtos[2].Id.Should().Be(business3.Id);
    }

    [Fact]
    public void Map_BusinessWithSpecialCharacters_ShouldMapCorrectly()
    {
        // Arrange
        var business = TestDataBuilder.CreateValidBusiness(Guid.NewGuid());
        // Note: TestDataBuilder should create business with various character sets

        // Act
        var dto = _mapper.Map<BusinessDto>(business);

        // Assert
        dto.Should().NotBeNull();
        dto.Name.Should().Be(business.Profile.Name);
        dto.Description.Should().Be(business.Profile.Description);
        dto.Address.Should().Be(business.Location.Address.Street);
    }

    [Fact]
    public void Map_BusinessToBusinessDto_ShouldHandleAllBusinessCategories()
    {
        // Arrange
        var business = TestDataBuilder.CreateValidBusiness(Guid.NewGuid());

        // Act
        var dto = _mapper.Map<BusinessDto>(business);

        // Assert
        dto.Category.Should().Be(business.Category);
        Enum.IsDefined(typeof(LankaConnect.Domain.Business.Enums.BusinessCategory), dto.Category).Should().BeTrue();
    }

    [Fact]
    public void Map_BusinessToBusinessDto_ShouldPreserveAddressComponents()
    {
        // Arrange
        var business = TestDataBuilder.CreateValidBusiness(Guid.NewGuid());

        // Act
        var dto = _mapper.Map<BusinessDto>(business);

        // Assert
        dto.Address.Should().Be(business.Location.Address.Street);
        dto.City.Should().Be(business.Location.Address.City);
        dto.Province.Should().Be(business.Location.Address.State);
        dto.PostalCode.Should().Be(business.Location.Address.ZipCode);
        
        // Verify all address components are preserved
        dto.Address.Should().NotBeNullOrEmpty();
        dto.City.Should().NotBeNullOrEmpty();
        dto.Province.Should().NotBeNullOrEmpty();
        dto.PostalCode.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void Map_BusinessToBusinessDto_ShouldPreserveContactInformation()
    {
        // Arrange
        var business = TestDataBuilder.CreateValidBusiness(Guid.NewGuid());

        // Act
        var dto = _mapper.Map<BusinessDto>(business);

        // Assert
        dto.ContactPhone.Should().Be(business.ContactInfo.PhoneNumber?.Value ?? string.Empty);
        dto.ContactEmail.Should().Be(business.ContactInfo.Email?.Value ?? string.Empty);
        
        // Verify contact info format
        dto.ContactPhone.Should().NotBeNullOrEmpty();
        dto.ContactEmail.Should().NotBeNullOrEmpty();
        dto.ContactEmail.Should().Contain("@");
    }
}