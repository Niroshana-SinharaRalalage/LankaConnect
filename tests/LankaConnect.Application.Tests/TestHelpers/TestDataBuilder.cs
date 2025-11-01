using LankaConnect.Application.Businesses.Commands.CreateBusiness;
using LankaConnect.Application.Businesses.Commands.UpdateBusiness;
using LankaConnect.Application.Businesses.Commands.AddService;
using LankaConnect.Application.Businesses.Common;
using LankaConnect.Application.Users.Commands.CreateUser;
using LankaConnect.Application.Users.DTOs;
using LankaConnect.Domain.Business;
using LankaConnect.Domain.Business.Enums;
using LankaConnect.Domain.Business.ValueObjects;
using LankaConnect.Domain.Users;
using LankaConnect.Domain.Users.ValueObjects;
using LankaConnect.Domain.Shared.ValueObjects;
using AutoFixture;

namespace LankaConnect.Application.Tests.TestHelpers;

public static class TestDataBuilder
{
    private static readonly Fixture _fixture = new();
    private static readonly Random _random = new();
    
    // US Cities for test data
    private static readonly string[] UsCities = { "New York", "Los Angeles", "Chicago", "Houston", "Phoenix", "Philadelphia", "San Antonio", "San Diego", "Dallas", "San Jose" };
    private static readonly string[] UsStates = { "NY", "CA", "IL", "TX", "AZ", "PA", "TX", "CA", "TX", "CA" };
    private static readonly string[] UsZipCodes = { "10001", "90210", "60601", "77001", "85001", "19101", "78201", "92101", "75201", "95101" };
    private static readonly string[] UsStreets = { "123 Main St", "456 Oak Ave", "789 Pine Rd", "321 Elm Dr", "654 Maple Ln" };

    public static CreateUserCommand CreateValidUserCommand()
    {
        var firstName = _fixture.Create<string>();
        var lastName = _fixture.Create<string>();
        var bio = _fixture.Create<string>();
        
        return new CreateUserCommand
        {
            Email = GenerateUsEmail(),
            FirstName = firstName.Length > 10 ? firstName[..10] : firstName, // Limit length for realistic data
            LastName = lastName.Length > 10 ? lastName[..10] : lastName,
            PhoneNumber = GenerateUsPhoneNumber(),
            Bio = bio.Length > 200 ? bio[..200] : bio // Limit bio length
        };
    }
    
    public static CreateUserCommand CreateUserCommandWithLongValues()
    {
        return new CreateUserCommand
        {
            Email = new string('a', 250) + "@test.com", // Exceeds 255 char limit
            FirstName = new string('A', 101), // Exceeds 100 char limit
            LastName = new string('B', 101), // Exceeds 100 char limit
            PhoneNumber = GenerateUsPhoneNumber(),
            Bio = new string('C', 1001) // Exceeds 1000 char limit
        };
    }

    public static CreateUserCommand CreateUserCommandWithInvalidEmail()
    {
        var firstName = _fixture.Create<string>();
        var lastName = _fixture.Create<string>();
        
        return new CreateUserCommand
        {
            Email = "invalid-email",
            FirstName = firstName.Length > 10 ? firstName[..10] : firstName,
            LastName = lastName.Length > 10 ? lastName[..10] : lastName
        };
    }
    
    public static CreateUserCommand CreateUserCommandWithInvalidUsPhoneNumber()
    {
        var firstName = _fixture.Create<string>();
        var lastName = _fixture.Create<string>();
        
        return new CreateUserCommand
        {
            Email = GenerateUsEmail(),
            FirstName = firstName.Length > 10 ? firstName[..10] : firstName,
            LastName = lastName.Length > 10 ? lastName[..10] : lastName,
            PhoneNumber = "invalid-phone" // Invalid US phone format
        };
    }

    public static CreateBusinessCommand CreateValidUsBusinessCommand(Guid? ownerId = null)
    {
        var cityIndex = _random.Next(UsCities.Length);
        return new CreateBusinessCommand(
            Name: GenerateUsBusinessName(),
            Description: "Authentic Sri Lankan cuisine in the heart of America",
            ContactPhone: GenerateUsPhoneNumber(),
            ContactEmail: GenerateUsEmail(),
            Website: "https://" + GenerateUsBusinessName().Replace(" ", "").ToLower() + ".com",
            Address: UsStreets[_random.Next(UsStreets.Length)],
            City: UsCities[cityIndex],
            Province: UsStates[cityIndex], // US uses State instead of Province
            PostalCode: UsZipCodes[cityIndex],
            Latitude: GenerateUsLatitude(cityIndex),
            Longitude: GenerateUsLongitude(cityIndex),
            Category: GetRandomUsBusinessCategory(),
            OwnerId: ownerId ?? Guid.NewGuid(),
            Categories: GenerateUsBusinessCategories(),
            Tags: GenerateUsBusinessTags()
        );
    }
    
    public static CreateBusinessCommand CreateValidBusinessCommand() => CreateValidUsBusinessCommand();

    public static User CreateValidUser()
    {
        var email = LankaConnect.Domain.Shared.ValueObjects.Email.Create($"test{_fixture.Create<int>()}@test.com").Value;
        return User.Create(email, _fixture.Create<string>(), _fixture.Create<string>()).Value;
    }

    // Epic 1 Phase 3: Test Helpers for Profile Enhancement
    public static User CreateUserWithProfilePhoto()
    {
        var user = CreateValidUser();
        user.UpdateProfilePhoto("https://example.com/photo.jpg", "test-photo.jpg");
        return user;
    }

    public static User CreateUserWithLocation()
    {
        var user = CreateValidUser();
        var location = LankaConnect.Domain.Users.ValueObjects.UserLocation.Create("New York", "NY", "10001", "USA").Value;
        user.UpdateLocation(location);
        return user;
    }

    public static User CreateUserWithCulturalInterests()
    {
        var user = CreateValidUser();
        var interests = new List<LankaConnect.Domain.Users.ValueObjects.CulturalInterest>
        {
            LankaConnect.Domain.Users.ValueObjects.CulturalInterest.SriLankanCuisine,
            LankaConnect.Domain.Users.ValueObjects.CulturalInterest.BuddhistFestivals,
            LankaConnect.Domain.Users.ValueObjects.CulturalInterest.CricketCulture
        };
        user.UpdateCulturalInterests(interests);
        return user;
    }

    public static User CreateUserWithLanguages()
    {
        var user = CreateValidUser();
        var languages = new List<LankaConnect.Domain.Users.ValueObjects.LanguagePreference>
        {
            LankaConnect.Domain.Users.ValueObjects.LanguagePreference.Create(
                LankaConnect.Domain.Users.ValueObjects.LanguageCode.English,
                LankaConnect.Domain.Users.Enums.ProficiencyLevel.Advanced).Value,
            LankaConnect.Domain.Users.ValueObjects.LanguagePreference.Create(
                LankaConnect.Domain.Users.ValueObjects.LanguageCode.Sinhala,
                LankaConnect.Domain.Users.Enums.ProficiencyLevel.Native).Value
        };
        user.UpdateLanguages(languages);
        return user;
    }

    public static Business CreateValidBusiness(Guid ownerId)
    {
        var businessData = CreateValidUsBusinessCommand(ownerId);
        
        // Create Value Objects
        var profile = BusinessProfile.Create(
            businessData.Name,
            businessData.Description,
            businessData.Website,
            null, // socialMedia
            new List<string> { "Main Service" }, // services
            new List<string> { "Main Specialization" } // specializations
        ).Value;
        
        var location = BusinessLocation.Create(
            businessData.Address,
            businessData.City,
            businessData.Province,
            businessData.PostalCode,
            "USA", // country
            (decimal?)businessData.Latitude,
            (decimal?)businessData.Longitude
        ).Value;
        
        var contactInfo = ContactInformation.Create(
            email: businessData.ContactEmail,
            phoneNumber: businessData.ContactPhone,
            website: businessData.Website
        ).Value;
        
        var hours = BusinessHours.Create(
            new Dictionary<DayOfWeek, (TimeOnly? open, TimeOnly? close)>
            {
                { DayOfWeek.Monday, (new TimeOnly(9, 0, 0), new TimeOnly(17, 0, 0)) },
                { DayOfWeek.Tuesday, (new TimeOnly(9, 0, 0), new TimeOnly(17, 0, 0)) },
                { DayOfWeek.Wednesday, (new TimeOnly(9, 0, 0), new TimeOnly(17, 0, 0)) },
                { DayOfWeek.Thursday, (new TimeOnly(9, 0, 0), new TimeOnly(17, 0, 0)) },
                { DayOfWeek.Friday, (new TimeOnly(9, 0, 0), new TimeOnly(17, 0, 0)) }
            }
        ).Value;
        
        return Business.Create(
            profile,
            location,
            contactInfo,
            hours,
            businessData.Category,
            ownerId
        ).Value;
    }

    public static UserDto CreateValidUserDto()
    {
        var firstName = _fixture.Create<string>();
        var lastName = _fixture.Create<string>();
        var bio = _fixture.Create<string>();
        
        return new UserDto
        {
            Id = Guid.NewGuid(),
            Email = GenerateUsEmail(),
            FirstName = firstName.Length > 10 ? firstName[..10] : firstName,
            LastName = lastName.Length > 10 ? lastName[..10] : lastName,
            PhoneNumber = GenerateUsPhoneNumber(),
            Bio = bio.Length > 200 ? bio[..200] : bio,
            IsActive = true,
            CreatedAt = DateTime.UtcNow.AddDays(-_random.Next(365)),
            UpdatedAt = DateTime.UtcNow.AddDays(-_random.Next(30))
        };
    }
    
    // US-specific helper methods
    private static string GenerateUsEmail()
    {
        return $"test{_random.Next(1000, 9999)}@{GenerateUsDomain()}";
    }
    
    private static string GenerateUsDomain()
    {
        var domains = new[] { "gmail.com", "yahoo.com", "hotmail.com", "outlook.com", "aol.com" };
        return domains[_random.Next(domains.Length)];
    }
    
    private static string GenerateUsPhoneNumber()
    {
        // US phone format: +1-XXX-XXX-XXXX
        return $"+1-{_random.Next(200, 999)}-{_random.Next(200, 999)}-{_random.Next(1000, 9999)}";
    }
    
    private static string GenerateUsBusinessName()
    {
        var prefixes = new[] { "Ceylon", "Lanka", "Spice", "Golden", "Royal", "Authentic" };
        var suffixes = new[] { "Kitchen", "Restaurant", "Cafe", "Grill", "Palace", "House" };
        return $"{prefixes[_random.Next(prefixes.Length)]} {suffixes[_random.Next(suffixes.Length)]}";
    }
    
    private static BusinessCategory GetRandomUsBusinessCategory()
    {
        var categories = new[] { BusinessCategory.Restaurant, BusinessCategory.Retail, BusinessCategory.Healthcare, 
                               BusinessCategory.Services, BusinessCategory.Transportation, BusinessCategory.Beauty };
        return categories[_random.Next(categories.Length)];
    }
    
    private static List<string> GenerateUsBusinessCategories()
    {
        var allCategories = new[] { "Restaurant", "Take-out", "Catering", "Sri Lankan Food", "Asian Cuisine", 
                                   "Vegetarian", "Halal", "Family Dining", "Lunch", "Dinner" };
        var count = _random.Next(2, 5);
        return allCategories.OrderBy(x => _random.Next()).Take(count).ToList();
    }
    
    private static List<string> GenerateUsBusinessTags()
    {
        var allTags = new[] { "authentic", "family-owned", "fresh", "spicy", "traditional", "modern", 
                             "affordable", "premium", "take-out", "delivery", "catering" };
        var count = _random.Next(3, 7);
        return allTags.OrderBy(x => _random.Next()).Take(count).ToList();
    }
    
    private static double GenerateUsLatitude(int cityIndex)
    {
        // Approximate latitudes for test cities
        var latitudes = new[] { 40.7128, 34.0522, 41.8781, 29.7604, 33.4484, 39.9526, 29.4241, 32.7157, 32.7767, 37.3382 };
        return latitudes[cityIndex] + (_random.NextDouble() - 0.5) * 0.1; // Add small variation
    }
    
    private static double GenerateUsLongitude(int cityIndex)
    {
        // Approximate longitudes for test cities
        var longitudes = new[] { -74.0060, -118.2437, -87.6298, -95.3698, -112.0740, -75.1652, -98.4936, -117.1611, -96.7970, -121.8863 };
        return longitudes[cityIndex] + (_random.NextDouble() - 0.5) * 0.1; // Add small variation
    }
    
    public static UpdateBusinessCommand CreateValidUpdateBusinessCommand(Guid businessId)
    {
        var cityIndex = _random.Next(UsCities.Length);
        return new UpdateBusinessCommand(
            businessId,
            GenerateUsBusinessName(),
            "Updated authentic Sri Lankan cuisine",
            GenerateUsPhoneNumber(),
            GenerateUsEmail(),
            "https://updated-site.com",
            UsStreets[_random.Next(UsStreets.Length)],
            UsCities[cityIndex],
            UsStates[cityIndex],
            UsZipCodes[cityIndex],
            GenerateUsLatitude(cityIndex),
            GenerateUsLongitude(cityIndex),
            GenerateUsBusinessCategories(),
            GenerateUsBusinessTags()
        );
    }
    
    public static AddServiceCommand CreateValidAddServiceCommand(Guid businessId)
    {
        var services = new[] { "Lunch Buffet", "Dinner Service", "Catering", "Take-out", "Private Dining" };
        var descriptions = new[] { "All-you-can-eat lunch buffet", "Traditional Sri Lankan dinner", "Catering for events", "Quick take-out service", "Private dining experience" };
        var index = _random.Next(services.Length);
        
        return new AddServiceCommand(
            businessId,
            services[index],
            descriptions[index],
            _random.Next(15, 50), // Price between $15-50
            $"{_random.Next(30, 180)} minutes", // Duration string format
            true // IsAvailable
        );
    }

    public static BusinessDto CreateValidBusinessDto()
    {
        var cityIndex = _random.Next(UsCities.Length);
        return new BusinessDto
        {
            Id = Guid.NewGuid(),
            Name = GenerateUsBusinessName(),
            Description = "Premium Sri Lankan dining experience in America",
            ContactPhone = GenerateUsPhoneNumber(),
            ContactEmail = GenerateUsEmail(),
            Website = "https://test.com",
            Address = UsStreets[_random.Next(UsStreets.Length)],
            City = UsCities[cityIndex],
            Province = UsStates[cityIndex],
            PostalCode = UsZipCodes[cityIndex],
            Latitude = GenerateUsLatitude(cityIndex),
            Longitude = GenerateUsLongitude(cityIndex),
            Category = GetRandomUsBusinessCategory(),
            Status = BusinessStatus.Active,
            Rating = (decimal)(_random.NextDouble() * 4 + 1), // 1-5 rating
            ReviewCount = _random.Next(1, 100),
            IsVerified = _random.Next(2) == 1,
            VerifiedAt = DateTime.UtcNow.AddDays(-_random.Next(365)),
            OwnerId = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow.AddDays(-_random.Next(730)),
            UpdatedAt = DateTime.UtcNow.AddDays(-_random.Next(30)),
            Categories = GenerateUsBusinessCategories(),
            Tags = GenerateUsBusinessTags()
        };
    }
}