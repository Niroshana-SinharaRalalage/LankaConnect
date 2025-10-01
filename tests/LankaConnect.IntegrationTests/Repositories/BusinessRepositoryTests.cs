using Microsoft.EntityFrameworkCore;
using LankaConnect.Domain.Business;
using LankaConnect.Domain.Business.Enums;
using LankaConnect.Domain.Business.ValueObjects;
using LankaConnect.Domain.Shared.ValueObjects;
using LankaConnect.Domain.Shared.Enums;
using LankaConnect.Infrastructure.Data;
using LankaConnect.Infrastructure.Data.Repositories;
using LankaConnect.IntegrationTests.Common;
using LankaConnect.TestUtilities.Builders;

namespace LankaConnect.IntegrationTests.Repositories;

public class BusinessRepositoryTests : BaseIntegrationTest
{
    private BusinessRepository _repository = null!;
    private AppDbContext _context = null!;

    [Fact]
    public async Task AddAsync_Should_Add_Business_Successfully()
    {
        // Arrange
        var business = await CreateTestBusinessAsync();

        // Arrange - Initialize repository
        _context = DbContext;
        _repository = new BusinessRepository(_context);

        // Act
        await _repository.AddAsync(business);
        await _context.CommitAsync();

        // Assert
        var savedBusiness = await _repository.GetByIdAsync(business.Id);
        Assert.NotNull(savedBusiness);
        Assert.Equal(business.Profile.Name, savedBusiness.Profile.Name);
        Assert.Equal(business.Category, savedBusiness.Category);
        Assert.Equal(business.Status, savedBusiness.Status);
    }

    [Fact]
    public async Task GetByOwnerIdAsync_Should_Return_Businesses_For_Owner()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var business1 = await CreateTestBusinessAsync(ownerId: ownerId);
        var business2 = await CreateTestBusinessAsync(ownerId: ownerId);
        var business3 = await CreateTestBusinessAsync(); // Different owner

        await _repository.AddAsync(business1);
        await _repository.AddAsync(business2);
        await _repository.AddAsync(business3);
        await _context.CommitAsync();

        // Act
        var businesses = await _repository.GetByOwnerIdAsync(ownerId);

        // Assert
        Assert.Equal(2, businesses.Count);
        Assert.All(businesses, b => Assert.Equal(ownerId, b.OwnerId));
    }

    [Fact]
    public async Task GetByCategoryAsync_Should_Return_Active_Businesses_In_Category()
    {
        // Arrange
        var category = BusinessCategory.Restaurant;
        var activeBusiness = await CreateTestBusinessAsync(category: category, status: BusinessStatus.Active);
        var inactiveBusiness = await CreateTestBusinessAsync(category: category, status: BusinessStatus.Inactive);
        var differentCategoryBusiness = await CreateTestBusinessAsync(category: BusinessCategory.Technology, status: BusinessStatus.Active);

        await _repository.AddAsync(activeBusiness);
        await _repository.AddAsync(inactiveBusiness);
        await _repository.AddAsync(differentCategoryBusiness);
        await _context.CommitAsync();

        // Act
        var businesses = await _repository.GetByCategoryAsync(category);

        // Assert
        Assert.Single(businesses);
        Assert.Equal(activeBusiness.Id, businesses.First().Id);
        Assert.Equal(category, businesses.First().Category);
        Assert.Equal(BusinessStatus.Active, businesses.First().Status);
    }

    [Fact]
    public async Task SearchByNameAsync_Should_Return_Matching_Businesses()
    {
        // Arrange
        var business1 = await CreateTestBusinessAsync(name: "Amazing Restaurant");
        var business2 = await CreateTestBusinessAsync(name: "Great Restaurant");
        var business3 = await CreateTestBusinessAsync(name: "Tech Company");

        await _repository.AddAsync(business1);
        await _repository.AddAsync(business2);
        await _repository.AddAsync(business3);
        await _context.CommitAsync();

        // Act
        var businesses = await _repository.SearchByNameAsync("restaurant");

        // Assert
        Assert.Equal(2, businesses.Count);
        Assert.All(businesses, b => Assert.Contains("restaurant", b.Profile.Name.ToLower()));
    }

    [Fact]
    public async Task SearchByLocationAsync_Should_Return_Businesses_In_City()
    {
        // Arrange
        var business1 = await CreateTestBusinessAsync(city: "Colombo");
        var business2 = await CreateTestBusinessAsync(city: "Colombo");
        var business3 = await CreateTestBusinessAsync(city: "Kandy");

        await _repository.AddAsync(business1);
        await _repository.AddAsync(business2);
        await _repository.AddAsync(business3);
        await _context.CommitAsync();

        // Act
        var businesses = await _repository.SearchByLocationAsync("Colombo");

        // Assert
        Assert.Equal(2, businesses.Count);
        Assert.All(businesses, b => Assert.Equal("Colombo", b.Location.Address.City));
    }

    [Fact]
    public async Task GetBusinessesWithFiltersAsync_Should_Apply_Multiple_Filters()
    {
        // Arrange
        var business1 = await CreateTestBusinessAsync(
            category: BusinessCategory.Restaurant, 
            status: BusinessStatus.Active, 
            isVerified: true,
            city: "Colombo");
        
        var business2 = await CreateTestBusinessAsync(
            category: BusinessCategory.Restaurant, 
            status: BusinessStatus.Active, 
            isVerified: false,
            city: "Colombo");
        
        var business3 = await CreateTestBusinessAsync(
            category: BusinessCategory.Technology, 
            status: BusinessStatus.Active, 
            isVerified: true,
            city: "Colombo");

        await _repository.AddAsync(business1);
        await _repository.AddAsync(business2);
        await _repository.AddAsync(business3);
        await _context.CommitAsync();

        // Act
        var businesses = await _repository.GetBusinessesWithFiltersAsync(
            category: BusinessCategory.Restaurant,
            status: BusinessStatus.Active,
            isVerified: true,
            city: "Colombo");

        // Assert
        Assert.Single(businesses);
        Assert.Equal(business1.Id, businesses.First().Id);
    }

    [Fact]
    public async Task GetWithServicesAsync_Should_Include_Active_Services()
    {
        // Arrange
        var business = await CreateTestBusinessAsync();
        var activeService = Service.Create("Active Service", "Description", null, null, business.Id).Value;
        var inactiveService = Service.Create("Inactive Service", "Description", null, null, business.Id).Value;
        inactiveService.Deactivate();

        business.AddService(activeService);
        business.AddService(inactiveService);

        await _repository.AddAsync(business);
        await _context.CommitAsync();

        // Act
        var result = await _repository.GetWithServicesAsync(business.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.Services);
        Assert.Equal("Active Service", result.Services.First().Name);
    }

    [Fact]
    public async Task IsOwnerOfBusinessAsync_Should_Return_True_For_Owner()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var business = await CreateTestBusinessAsync(ownerId: ownerId);

        await _repository.AddAsync(business);
        await _context.CommitAsync();

        // Act
        var result = await _repository.IsOwnerOfBusinessAsync(business.Id, ownerId);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task IsOwnerOfBusinessAsync_Should_Return_False_For_Non_Owner()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var nonOwnerId = Guid.NewGuid();
        var business = await CreateTestBusinessAsync(ownerId: ownerId);

        await _repository.AddAsync(business);
        await _context.CommitAsync();

        // Act
        var result = await _repository.IsOwnerOfBusinessAsync(business.Id, nonOwnerId);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task GetBusinessCountByCategoryAsync_Should_Return_Correct_Count()
    {
        // Arrange
        var category = BusinessCategory.Restaurant;
        var business1 = await CreateTestBusinessAsync(category: category, status: BusinessStatus.Active);
        var business2 = await CreateTestBusinessAsync(category: category, status: BusinessStatus.Active);
        var business3 = await CreateTestBusinessAsync(category: category, status: BusinessStatus.Inactive); // Should not count

        await _repository.AddAsync(business1);
        await _repository.AddAsync(business2);
        await _repository.AddAsync(business3);
        await _context.CommitAsync();

        // Act
        var count = await _repository.GetBusinessCountByCategoryAsync(category);

        // Assert
        Assert.Equal(2, count);
    }

    private Task<Business> CreateTestBusinessAsync(
        string name = "Test Business",
        BusinessCategory category = BusinessCategory.Restaurant,
        BusinessStatus status = BusinessStatus.PendingApproval,
        Guid? ownerId = null,
        bool isVerified = false,
        string city = "Test City")
    {
        var profile = BusinessProfile.Create(
            name,
            "Test business description",
            "https://www.testbusiness.com",
            null,
            new List<string> { "Service 1", "Service 2" },
            new List<string> { "Specialization 1" }).Value;

        var address = Address.Create(
            "123 Test Street",
            city,
            "Test State",
            "12345",
            "Test Country").Value;

        var coordinates = GeoCoordinate.Create(6.9271m, 79.8612m).Value; // Colombo coordinates
        var location = BusinessLocation.Create(address, coordinates).Value;

        var email = EmailTestDataBuilder.CreateValidEmail("test@business.com");
        var phone = PhoneNumber.Create("+94771234567").Value;
        var contactInfo = ContactInformation.Create(
            email.Value,
            phone.Value,
            "https://www.testbusiness.com").Value;

        var hours = BusinessHours.Create(new Dictionary<DayOfWeek, (TimeOnly? open, TimeOnly? close)>
        {
            { DayOfWeek.Monday, (new TimeOnly(9, 0), new TimeOnly(17, 0)) },
            { DayOfWeek.Tuesday, (new TimeOnly(9, 0), new TimeOnly(17, 0)) },
            { DayOfWeek.Wednesday, (new TimeOnly(9, 0), new TimeOnly(17, 0)) },
            { DayOfWeek.Thursday, (new TimeOnly(9, 0), new TimeOnly(17, 0)) },
            { DayOfWeek.Friday, (new TimeOnly(9, 0), new TimeOnly(17, 0)) },
            { DayOfWeek.Saturday, (null, null) },
            { DayOfWeek.Sunday, (null, null) }
        }).Value;

        var business = Business.Create(
            profile,
            location,
            contactInfo,
            hours,
            category,
            ownerId ?? Guid.NewGuid()).Value;

        if (status != BusinessStatus.PendingApproval)
        {
            switch (status)
            {
                case BusinessStatus.Active:
                    business.Activate();
                    break;
                case BusinessStatus.Suspended:
                    business.Suspend();
                    break;
                case BusinessStatus.Inactive:
                    business.Deactivate();
                    break;
            }
        }

        if (isVerified)
        {
            business.Verify();
        }

        return Task.FromResult(business);
    }
}