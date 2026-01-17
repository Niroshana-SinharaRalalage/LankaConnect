using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using LankaConnect.Domain.Business;
using LankaConnect.Domain.Business.Enums;
using LankaConnect.Domain.Shared.ValueObjects;
using LankaConnect.Domain.Shared.Enums;
using LankaConnect.Infrastructure.Data;
using LankaConnect.Infrastructure.Data.Repositories;
using LankaConnect.IntegrationTests.Common;
using LankaConnect.TestUtilities.Builders;

namespace LankaConnect.IntegrationTests.Repositories;

public class ServiceRepositoryTests : DockerComposeWebApiTestBase
{
    private ServiceRepository _repository = null!;
    private BusinessRepository _businessRepository = null!;
    private AppDbContext _context = null!;

    private void InitializeRepositories()
    {
        _context = DbContext;
        _repository = new ServiceRepository(_context);
        _businessRepository = new BusinessRepository(_context, NullLogger<BusinessRepository>.Instance);
    }

    [Fact]
    public async Task AddAsync_Should_Add_Service_Successfully()
    {
        // Arrange
        InitializeRepositories();
        var business = await CreateTestBusinessAsync();
        await _businessRepository.AddAsync(business);
        await _context.CommitAsync();

        var service = Service.Create(
            "Test Service",
            "Test service description",
            Money.Create(100, Currency.LKR).Value,
            "1 hour",
            business.Id).Value;

        // Act
        await _repository.AddAsync(service);
        await _context.CommitAsync();

        // Assert
        var savedService = await _repository.GetByIdAsync(service.Id);
        Assert.NotNull(savedService);
        Assert.Equal("Test Service", savedService.Name);
        Assert.Equal("Test service description", savedService.Description);
        Assert.Equal(100, savedService.Price?.Amount);
        Assert.Equal(Currency.LKR, savedService.Price?.Currency);
        Assert.Equal("1 hour", savedService.Duration);
        Assert.Equal(business.Id, savedService.BusinessId);
        Assert.True(savedService.IsActive);
    }

    [Fact]
    public async Task GetByBusinessIdAsync_Should_Return_All_Services_For_Business()
    {
        // Arrange
        InitializeRepositories();
        var business = await CreateTestBusinessAsync();
        await _businessRepository.AddAsync(business);

        var service1 = Service.Create("Service 1", "Description 1", null, null, business.Id).Value;
        var service2 = Service.Create("Service 2", "Description 2", null, null, business.Id).Value;
        service2.Deactivate(); // Make one inactive

        await _repository.AddAsync(service1);
        await _repository.AddAsync(service2);
        await _context.CommitAsync();

        // Act
        var services = await _repository.GetByBusinessIdAsync(business.Id);

        // Assert
        Assert.Equal(2, services.Count);
        Assert.Contains(services, s => s.Name == "Service 1" && s.IsActive);
        Assert.Contains(services, s => s.Name == "Service 2" && !s.IsActive);
    }

    [Fact]
    public async Task GetActiveByBusinessIdAsync_Should_Return_Only_Active_Services()
    {
        // Arrange
        InitializeRepositories();
        var business = await CreateTestBusinessAsync();
        await _businessRepository.AddAsync(business);

        var activeService = Service.Create("Active Service", "Description", null, null, business.Id).Value;
        var inactiveService = Service.Create("Inactive Service", "Description", null, null, business.Id).Value;
        inactiveService.Deactivate();

        await _repository.AddAsync(activeService);
        await _repository.AddAsync(inactiveService);
        await _context.CommitAsync();

        // Act
        var services = await _repository.GetActiveByBusinessIdAsync(business.Id);

        // Assert
        Assert.Single(services);
        Assert.Equal("Active Service", services.First().Name);
        Assert.True(services.First().IsActive);
    }

    [Fact]
    public async Task GetByBusinessIdAndNameAsync_Should_Return_Matching_Service()
    {
        // Arrange
        InitializeRepositories();
        var business = await CreateTestBusinessAsync();
        await _businessRepository.AddAsync(business);

        var service = Service.Create("Unique Service", "Description", null, null, business.Id).Value;
        await _repository.AddAsync(service);
        await _context.CommitAsync();

        // Act
        var foundService = await _repository.GetByBusinessIdAndNameAsync(business.Id, "Unique Service");

        // Assert
        Assert.NotNull(foundService);
        Assert.Equal("Unique Service", foundService.Name);
        Assert.Equal(business.Id, foundService.BusinessId);
    }

    [Fact]
    public async Task SearchByNameAsync_Should_Return_Services_Matching_Name()
    {
        // Arrange
        InitializeRepositories();
        var business = await CreateTestBusinessAsync();
        await _businessRepository.AddAsync(business);

        var service1 = Service.Create("Hair Cut Service", "Description", null, null, business.Id).Value;
        var service2 = Service.Create("Hair Styling", "Description", null, null, business.Id).Value;
        var service3 = Service.Create("Massage", "Description", null, null, business.Id).Value;

        await _repository.AddAsync(service1);
        await _repository.AddAsync(service2);
        await _repository.AddAsync(service3);
        await _context.CommitAsync();

        // Act
        var services = await _repository.SearchByNameAsync("hair");

        // Assert
        Assert.Equal(2, services.Count);
        Assert.All(services, s => Assert.Contains("hair", s.Name.ToLower()));
        Assert.All(services, s => Assert.True(s.IsActive));
    }

    [Fact]
    public async Task GetByPriceRangeAsync_Should_Return_Services_In_Price_Range()
    {
        // Arrange
        InitializeRepositories();
        var business = await CreateTestBusinessAsync();
        await _businessRepository.AddAsync(business);

        var service1 = Service.Create("Service 1", "Description", Money.Create(50, Currency.LKR).Value, null, business.Id).Value;
        var service2 = Service.Create("Service 2", "Description", Money.Create(100, Currency.LKR).Value, null, business.Id).Value;
        var service3 = Service.Create("Service 3", "Description", Money.Create(200, Currency.LKR).Value, null, business.Id).Value;

        await _repository.AddAsync(service1);
        await _repository.AddAsync(service2);
        await _repository.AddAsync(service3);
        await _context.CommitAsync();

        // Act
        var services = await _repository.GetByPriceRangeAsync(
            Money.Create(40, Currency.LKR).Value,
            Money.Create(150, Currency.LKR).Value);

        // Assert
        Assert.Equal(2, services.Count);
        Assert.Contains(services, s => s.Price?.Amount == 50);
        Assert.Contains(services, s => s.Price?.Amount == 100);
        Assert.DoesNotContain(services, s => s.Price?.Amount == 200);
    }

    [Fact]
    public async Task GetFreeServicesAsync_Should_Return_Services_Without_Price()
    {
        // Arrange
        InitializeRepositories();
        var business = await CreateTestBusinessAsync();
        await _businessRepository.AddAsync(business);

        var freeService = Service.Create("Free Service", "Description", null, null, business.Id).Value;
        var zeroAmountService = Service.Create("Zero Service", "Description", Money.Create(0, Currency.LKR).Value, null, business.Id).Value;
        var paidService = Service.Create("Paid Service", "Description", Money.Create(100, Currency.LKR).Value, null, business.Id).Value;

        await _repository.AddAsync(freeService);
        await _repository.AddAsync(zeroAmountService);
        await _repository.AddAsync(paidService);
        await _context.CommitAsync();

        // Act
        var services = await _repository.GetFreeServicesAsync();

        // Assert
        Assert.Equal(2, services.Count);
        Assert.Contains(services, s => s.Name == "Free Service");
        Assert.Contains(services, s => s.Name == "Zero Service");
        Assert.DoesNotContain(services, s => s.Name == "Paid Service");
    }

    [Fact]
    public async Task GetServiceCountByBusinessIdAsync_Should_Return_Total_Count()
    {
        // Arrange
        InitializeRepositories();
        var business = await CreateTestBusinessAsync();
        await _businessRepository.AddAsync(business);

        var service1 = Service.Create("Service 1", "Description", null, null, business.Id).Value;
        var service2 = Service.Create("Service 2", "Description", null, null, business.Id).Value;
        service2.Deactivate();

        await _repository.AddAsync(service1);
        await _repository.AddAsync(service2);
        await _context.CommitAsync();

        // Act
        var count = await _repository.GetServiceCountByBusinessIdAsync(business.Id);

        // Assert
        Assert.Equal(2, count);
    }

    [Fact]
    public async Task GetActiveServiceCountByBusinessIdAsync_Should_Return_Active_Count_Only()
    {
        // Arrange
        InitializeRepositories();
        var business = await CreateTestBusinessAsync();
        await _businessRepository.AddAsync(business);

        var activeService = Service.Create("Active Service", "Description", null, null, business.Id).Value;
        var inactiveService = Service.Create("Inactive Service", "Description", null, null, business.Id).Value;
        inactiveService.Deactivate();

        await _repository.AddAsync(activeService);
        await _repository.AddAsync(inactiveService);
        await _context.CommitAsync();

        // Act
        var count = await _repository.GetActiveServiceCountByBusinessIdAsync(business.Id);

        // Assert
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task DeactivateAllByBusinessIdAsync_Should_Deactivate_All_Services()
    {
        // Arrange
        InitializeRepositories();
        var business = await CreateTestBusinessAsync();
        await _businessRepository.AddAsync(business);

        var service1 = Service.Create("Service 1", "Description", null, null, business.Id).Value;
        var service2 = Service.Create("Service 2", "Description", null, null, business.Id).Value;

        await _repository.AddAsync(service1);
        await _repository.AddAsync(service2);
        await _context.CommitAsync();

        // Act
        await _repository.DeactivateAllByBusinessIdAsync(business.Id);

        // Assert
        var services = await _repository.GetByBusinessIdAsync(business.Id);
        Assert.All(services, s => Assert.False(s.IsActive));
    }

    private Task<Business> CreateTestBusinessAsync()
    {
        // Create BusinessProfile
        var profileResult = Domain.Business.ValueObjects.BusinessProfile.Create(
            "Test Business",
            "Test business description",
            "https://www.testbusiness.com",
            null,
            new List<string> { "Service 1", "Service 2" },
            new List<string> { "Specialization 1" });
        if (!profileResult.IsSuccess)
            throw new InvalidOperationException($"Failed to create BusinessProfile: {profileResult.Error}");

        // Create Address
        var addressResult = Domain.Business.ValueObjects.Address.Create(
            "123 Test Street",
            "Test City",
            "Test State",
            "12345",
            "Test Country");
        if (!addressResult.IsSuccess)
            throw new InvalidOperationException($"Failed to create Address: {addressResult.Error}");

        // Create GeoCoordinate
        var coordinatesResult = Domain.Business.ValueObjects.GeoCoordinate.Create(6.9271m, 79.8612m);
        if (!coordinatesResult.IsSuccess)
            throw new InvalidOperationException($"Failed to create GeoCoordinate: {coordinatesResult.Error}");

        // Create BusinessLocation
        var locationResult = Domain.Business.ValueObjects.BusinessLocation.Create(addressResult.Value, coordinatesResult.Value);
        if (!locationResult.IsSuccess)
            throw new InvalidOperationException($"Failed to create BusinessLocation: {locationResult.Error}");

        // Create Email and Phone
        var email = EmailTestDataBuilder.CreateValidEmail("test@business.com");
        var phoneResult = PhoneNumber.Create("+94771234567");
        if (!phoneResult.IsSuccess)
            throw new InvalidOperationException($"Failed to create PhoneNumber: {phoneResult.Error}");

        // Create ContactInformation
        var contactInfoResult = Domain.Business.ValueObjects.ContactInformation.Create(
            email: email.Value,
            phoneNumber: phoneResult.Value.Value,
            website: "https://www.testbusiness.com");
        if (!contactInfoResult.IsSuccess)
            throw new InvalidOperationException($"Failed to create ContactInformation: {contactInfoResult.Error}");

        // Create BusinessHours
        var hoursResult = Domain.Business.ValueObjects.BusinessHours.Create(new Dictionary<DayOfWeek, (TimeOnly? open, TimeOnly? close)>
        {
            { DayOfWeek.Monday, (new TimeOnly(9, 0), new TimeOnly(17, 0)) },
            { DayOfWeek.Tuesday, (new TimeOnly(9, 0), new TimeOnly(17, 0)) },
            { DayOfWeek.Wednesday, (new TimeOnly(9, 0), new TimeOnly(17, 0)) },
            { DayOfWeek.Thursday, (new TimeOnly(9, 0), new TimeOnly(17, 0)) },
            { DayOfWeek.Friday, (new TimeOnly(9, 0), new TimeOnly(17, 0)) },
            { DayOfWeek.Saturday, (null, null) },
            { DayOfWeek.Sunday, (null, null) }
        });
        if (!hoursResult.IsSuccess)
            throw new InvalidOperationException($"Failed to create BusinessHours: {hoursResult.Error}");

        // Create Business
        var businessResult = Business.Create(
            profileResult.Value,
            locationResult.Value,
            contactInfoResult.Value,
            hoursResult.Value,
            BusinessCategory.Restaurant,
            Guid.NewGuid());
        if (!businessResult.IsSuccess)
            throw new InvalidOperationException($"Failed to create Business: {businessResult.Error}");

        return Task.FromResult(businessResult.Value);
    }
}