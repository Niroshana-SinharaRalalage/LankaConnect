using LankaConnect.Domain.Business;
using LankaConnect.Domain.Business.ValueObjects;
using LankaConnect.Domain.Business.Enums;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Shared.ValueObjects;
using LankaConnect.Domain.Tests.TestHelpers;

namespace LankaConnect.Domain.Tests.Business;

public class BusinessTests
{
    [Fact]
    public void Create_WithValidData_ShouldReturnSuccess()
    {
        var profile = TestDataFactory.ValidBusinessProfile();
        var location = TestDataFactory.ValidBusinessLocation();
        var contactInfo = TestDataFactory.ValidContactInformation();
        var hours = TestDataFactory.ValidBusinessHours();
        var category = BusinessCategory.Restaurant;
        var ownerId = Guid.NewGuid();

        var result = LankaConnect.Domain.Business.Business.Create(profile, location, contactInfo, hours, category, ownerId);

        Assert.True(result.IsSuccess);
        var business = result.Value;
        Assert.Equal(profile, business.Profile);
        Assert.Equal(location, business.Location);
        Assert.Equal(contactInfo, business.ContactInfo);
        Assert.Equal(hours, business.Hours);
        Assert.Equal(category, business.Category);
        Assert.Equal(ownerId, business.OwnerId);
        Assert.Equal(BusinessStatus.PendingApproval, business.Status);
        Assert.Null(business.Rating);
        Assert.Equal(0, business.ReviewCount);
        Assert.False(business.IsVerified);
        Assert.Null(business.VerifiedAt);
        Assert.NotEqual(Guid.Empty, business.Id);
    }

    [Fact]
    public void Create_WithNullProfile_ShouldReturnFailure()
    {
        var result = LankaConnect.Domain.Business.Business.Create(null!, TestDataFactory.ValidBusinessLocation(), 
            TestDataFactory.ValidContactInformation(), TestDataFactory.ValidBusinessHours(), 
            BusinessCategory.Restaurant, Guid.NewGuid());

        Assert.True(result.IsFailure);
        Assert.Contains("Business profile is required", result.Errors);
    }

    [Fact]
    public void Create_WithNullLocation_ShouldReturnFailure()
    {
        var result = LankaConnect.Domain.Business.Business.Create(TestDataFactory.ValidBusinessProfile(), null!,
            TestDataFactory.ValidContactInformation(), TestDataFactory.ValidBusinessHours(),
            BusinessCategory.Restaurant, Guid.NewGuid());

        Assert.True(result.IsFailure);
        Assert.Contains("Business location is required", result.Errors);
    }

    [Fact]
    public void Create_WithNullContactInfo_ShouldReturnFailure()
    {
        var result = LankaConnect.Domain.Business.Business.Create(TestDataFactory.ValidBusinessProfile(), TestDataFactory.ValidBusinessLocation(),
            null!, TestDataFactory.ValidBusinessHours(),
            BusinessCategory.Restaurant, Guid.NewGuid());

        Assert.True(result.IsFailure);
        Assert.Contains("Contact information is required", result.Errors);
    }

    [Fact]
    public void Create_WithNullHours_ShouldReturnFailure()
    {
        var result = LankaConnect.Domain.Business.Business.Create(TestDataFactory.ValidBusinessProfile(), TestDataFactory.ValidBusinessLocation(),
            TestDataFactory.ValidContactInformation(), null!,
            BusinessCategory.Restaurant, Guid.NewGuid());

        Assert.True(result.IsFailure);
        Assert.Contains("Business hours are required", result.Errors);
    }

    [Fact]
    public void Create_WithEmptyOwnerId_ShouldReturnFailure()
    {
        var result = LankaConnect.Domain.Business.Business.Create(TestDataFactory.ValidBusinessProfile(), TestDataFactory.ValidBusinessLocation(),
            TestDataFactory.ValidContactInformation(), TestDataFactory.ValidBusinessHours(),
            BusinessCategory.Restaurant, Guid.Empty);

        Assert.True(result.IsFailure);
        Assert.Contains("Owner ID is required", result.Errors);
    }

    [Fact]
    public void UpdateProfile_WithValidProfile_ShouldUpdateSuccessfully()
    {
        var business = TestDataFactory.ValidBusiness();
        var newProfile = BusinessProfile.Create(
            "Updated Business Name",
            "Updated description of the business",
            "https://updated-website.com",
            null,
            new List<string> { "Updated Service" },
            new List<string> { "Updated Specialization" }).Value;

        var result = business.UpdateProfile(newProfile);

        Assert.True(result.IsSuccess);
        Assert.Equal(newProfile, business.Profile);
        Assert.NotNull(business.UpdatedAt);
    }

    [Fact]
    public void UpdateProfile_WithNullProfile_ShouldReturnFailure()
    {
        var business = TestDataFactory.ValidBusiness();

        var result = business.UpdateProfile(null!);

        Assert.True(result.IsFailure);
        Assert.Contains("Business profile is required", result.Errors);
    }

    [Fact]
    public void UpdateLocation_WithValidLocation_ShouldUpdateSuccessfully()
    {
        var business = TestDataFactory.ValidBusiness();
        var newLocation = BusinessLocation.Create(
            Address.Create("456 New Street", "New City", "Western", "12345", "Sri Lanka").Value,
            GeoCoordinate.Create(7.0m, 80.0m).Value).Value;

        var result = business.UpdateLocation(newLocation);

        Assert.True(result.IsSuccess);
        Assert.Equal(newLocation, business.Location);
        Assert.NotNull(business.UpdatedAt);
    }

    [Fact]
    public void UpdateLocation_WithNullLocation_ShouldReturnFailure()
    {
        var business = TestDataFactory.ValidBusiness();

        var result = business.UpdateLocation(null!);

        Assert.True(result.IsFailure);
        Assert.Contains("Business location is required", result.Errors);
    }

    [Fact]
    public void UpdateContactInfo_WithValidContactInfo_ShouldUpdateSuccessfully()
    {
        var business = TestDataFactory.ValidBusiness();
        var newContactInfo = ContactInformation.Create(
            "+94-77-999-8888",
            "updated@example.com",
            "+94-11-999-8888").Value;

        var result = business.UpdateContactInfo(newContactInfo);

        Assert.True(result.IsSuccess);
        Assert.Equal(newContactInfo, business.ContactInfo);
        Assert.NotNull(business.UpdatedAt);
    }

    [Fact]
    public void UpdateContactInfo_WithNullContactInfo_ShouldReturnFailure()
    {
        var business = TestDataFactory.ValidBusiness();

        var result = business.UpdateContactInfo(null!);

        Assert.True(result.IsFailure);
        Assert.Contains("Contact information is required", result.Errors);
    }

    [Fact]
    public void UpdateHours_WithValidHours_ShouldUpdateSuccessfully()
    {
        var business = TestDataFactory.ValidBusiness();
        var newHours = BusinessHours.Create(
            new Dictionary<DayOfWeek, (TimeOnly? open, TimeOnly? close)>
            {
                [DayOfWeek.Monday] = (new TimeOnly(10, 0), new TimeOnly(22, 0)),
                [DayOfWeek.Tuesday] = (new TimeOnly(10, 0), new TimeOnly(22, 0)),
                [DayOfWeek.Wednesday] = (new TimeOnly(10, 0), new TimeOnly(22, 0)),
                [DayOfWeek.Thursday] = (new TimeOnly(10, 0), new TimeOnly(22, 0)),
                [DayOfWeek.Friday] = (new TimeOnly(10, 0), new TimeOnly(22, 0)),
                [DayOfWeek.Saturday] = (new TimeOnly(10, 0), new TimeOnly(22, 0)),
                [DayOfWeek.Sunday] = (new TimeOnly(10, 0), new TimeOnly(22, 0))
            }).Value;

        var result = business.UpdateHours(newHours);

        Assert.True(result.IsSuccess);
        Assert.Equal(newHours, business.Hours);
        Assert.NotNull(business.UpdatedAt);
    }

    [Fact]
    public void UpdateHours_WithNullHours_ShouldReturnFailure()
    {
        var business = TestDataFactory.ValidBusiness();

        var result = business.UpdateHours(null!);

        Assert.True(result.IsFailure);
        Assert.Contains("Business hours are required", result.Errors);
    }

    [Fact]
    public void UpdateCategory_WithValidCategory_ShouldUpdateSuccessfully()
    {
        var business = TestDataFactory.ValidBusiness();
        var newCategory = BusinessCategory.Technology;

        var result = business.UpdateCategory(newCategory);

        Assert.True(result.IsSuccess);
        Assert.Equal(newCategory, business.Category);
        Assert.NotNull(business.UpdatedAt);
    }

    [Fact]
    public void AddService_WithValidService_ShouldAddSuccessfully()
    {
        var business = TestDataFactory.ValidBusiness();
        var service = Service.Create("New Service", "Description of new service").Value;

        var result = business.AddService(service);

        Assert.True(result.IsSuccess);
        Assert.Contains(service, business.Services);
        Assert.NotNull(business.UpdatedAt);
    }

    [Fact]
    public void AddService_WithNullService_ShouldReturnFailure()
    {
        var business = TestDataFactory.ValidBusiness();

        var result = business.AddService(null!);

        Assert.True(result.IsFailure);
        Assert.Contains("Service is required", result.Errors);
    }

    [Fact]
    public void AddService_WithDuplicateName_ShouldReturnFailure()
    {
        var business = TestDataFactory.ValidBusiness();
        var service1 = Service.Create("Duplicate Service", "First service").Value;
        var service2 = Service.Create("Duplicate Service", "Second service").Value;

        business.AddService(service1);
        var result = business.AddService(service2);

        Assert.True(result.IsFailure);
        Assert.Contains("Service with this name already exists", result.Errors);
    }

    [Fact]
    public void RemoveService_WithExistingService_ShouldRemoveSuccessfully()
    {
        var business = TestDataFactory.ValidBusiness();
        var service = Service.Create("Service to Remove", "Service description").Value;
        business.AddService(service);

        var result = business.RemoveService(service.Id);

        Assert.True(result.IsSuccess);
        Assert.DoesNotContain(service, business.Services);
        Assert.NotNull(business.UpdatedAt);
    }

    [Fact]
    public void RemoveService_WithNonExistentService_ShouldReturnFailure()
    {
        var business = TestDataFactory.ValidBusiness();

        var result = business.RemoveService(Guid.NewGuid());

        Assert.True(result.IsFailure);
        Assert.Contains("Service not found", result.Errors);
    }

    [Fact]
    public void Activate_WhenNotActive_ShouldActivateSuccessfully()
    {
        var business = TestDataFactory.ValidBusiness();

        var result = business.Activate();

        Assert.True(result.IsSuccess);
        Assert.Equal(BusinessStatus.Active, business.Status);
        Assert.NotNull(business.UpdatedAt);
    }

    [Fact]
    public void Activate_WhenAlreadyActive_ShouldReturnFailure()
    {
        var business = TestDataFactory.ValidBusiness();
        business.Activate();

        var result = business.Activate();

        Assert.True(result.IsFailure);
        Assert.Contains("Business is already active", result.Errors);
    }

    [Fact]
    public void Suspend_WhenNotSuspended_ShouldSuspendSuccessfully()
    {
        var business = TestDataFactory.ValidBusiness();

        var result = business.Suspend();

        Assert.True(result.IsSuccess);
        Assert.Equal(BusinessStatus.Suspended, business.Status);
        Assert.NotNull(business.UpdatedAt);
    }

    [Fact]
    public void Suspend_WhenAlreadySuspended_ShouldReturnFailure()
    {
        var business = TestDataFactory.ValidBusiness();
        business.Suspend();

        var result = business.Suspend();

        Assert.True(result.IsFailure);
        Assert.Contains("Business is already suspended", result.Errors);
    }

    [Fact]
    public void Deactivate_ShouldDeactivateSuccessfully()
    {
        var business = TestDataFactory.ValidBusiness();

        var result = business.Deactivate();

        Assert.True(result.IsSuccess);
        Assert.Equal(BusinessStatus.Inactive, business.Status);
        Assert.NotNull(business.UpdatedAt);
    }

    [Fact]
    public void Verify_WhenNotVerified_ShouldVerifySuccessfully()
    {
        var business = TestDataFactory.ValidBusiness();

        var result = business.Verify();

        Assert.True(result.IsSuccess);
        Assert.True(business.IsVerified);
        Assert.NotNull(business.VerifiedAt);
        Assert.NotNull(business.UpdatedAt);
    }

    [Fact]
    public void Verify_WhenAlreadyVerified_ShouldReturnFailure()
    {
        var business = TestDataFactory.ValidBusiness();
        business.Verify();

        var result = business.Verify();

        Assert.True(result.IsFailure);
        Assert.Contains("Business is already verified", result.Errors);
    }

    [Fact]
    public void Unverify_WhenVerified_ShouldUnverifySuccessfully()
    {
        var business = TestDataFactory.ValidBusiness();
        business.Verify();

        var result = business.Unverify();

        Assert.True(result.IsSuccess);
        Assert.False(business.IsVerified);
        Assert.Null(business.VerifiedAt);
        Assert.NotNull(business.UpdatedAt);
    }

    [Fact]
    public void Unverify_WhenNotVerified_ShouldReturnFailure()
    {
        var business = TestDataFactory.ValidBusiness();

        var result = business.Unverify();

        Assert.True(result.IsFailure);
        Assert.Contains("Business is not verified", result.Errors);
    }

    [Fact]
    public void IsOpenAt_WhenActiveAndWithinHours_ShouldReturnTrue()
    {
        var business = TestDataFactory.ValidBusiness();
        business.Activate();
        var testTime = DateTime.Today.AddHours(14); // 2 PM

        var isOpen = business.IsOpenAt(testTime);

        Assert.True(isOpen);
    }

    [Fact]
    public void IsOpenAt_WhenNotActive_ShouldReturnFalse()
    {
        var business = TestDataFactory.ValidBusiness();
        var testTime = DateTime.Today.AddHours(14);

        var isOpen = business.IsOpenAt(testTime);

        Assert.False(isOpen);
    }

    [Fact]
    public void DistanceTo_WithValidLocation_ShouldReturnDistance()
    {
        var business = TestDataFactory.ValidBusiness();
        var targetLocation = BusinessLocation.Create(
            Address.Create("456 Target Street", "Target City", "Western", "54321", "Sri Lanka").Value,
            GeoCoordinate.Create(7.0m, 80.0m).Value).Value;

        var distance = business.DistanceTo(targetLocation);

        Assert.NotNull(distance);
        Assert.True(distance >= 0);
    }

}