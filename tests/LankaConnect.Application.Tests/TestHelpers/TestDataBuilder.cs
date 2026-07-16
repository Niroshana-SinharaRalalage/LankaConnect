using LankaConnect.Modules.Identity.Application.Commands.Users.CreateUser;
using LankaConnect.Modules.Identity.Application.DTOs;
using LankaConnect.Modules.Identity.Domain.Entities;
using AutoFixture;

namespace LankaConnect.Application.Tests.TestHelpers;

// Wave 8.5.k (2026-07-16): Business builders (CreateValidUsBusinessCommand /
// CreateValidBusinessCommand / CreateValidBusiness / CreateValidUpdateBusinessCommand /
// CreateValidAddServiceCommand / CreateValidBusinessDto + supporting
// GenerateUsBusinessName / GetRandomUsBusinessCategory / GenerateUsBusinessCategories /
// GenerateUsBusinessTags / GenerateUsLatitude / GenerateUsLongitude) removed
// alongside Businesses controller retirement per founder direction. Restore
// alongside LankaBusiness product re-add in Phase B.
public static class TestDataBuilder
{
    private static readonly Fixture _fixture = new();
    private static readonly Random _random = new();

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

    public static User CreateValidUser()
    {
        var email = LankaConnect.BuildingBlocks.Domain.Shared.ValueObjects.Email.Create($"test{_fixture.Create<int>()}@test.com").Value;
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
        var location = LankaConnect.Modules.Identity.Domain.ValueObjects.UserLocation.Create("New York", "NY", "10001", "USA").Value;
        user.UpdateLocation(location);
        return user;
    }

    public static User CreateUserWithCulturalInterests()
    {
        var user = CreateValidUser();
        var interests = new List<LankaConnect.Modules.Identity.Domain.ValueObjects.CulturalInterest>
        {
            LankaConnect.Modules.Identity.Domain.ValueObjects.CulturalInterest.SriLankanCuisine,
            LankaConnect.Modules.Identity.Domain.ValueObjects.CulturalInterest.BuddhistFestivals,
            LankaConnect.Modules.Identity.Domain.ValueObjects.CulturalInterest.CricketCulture
        };
        user.UpdateCulturalInterests(interests);
        return user;
    }

    public static User CreateUserWithLanguages()
    {
        var user = CreateValidUser();
        var languages = new List<LankaConnect.Modules.Identity.Domain.ValueObjects.LanguagePreference>
        {
            LankaConnect.Modules.Identity.Domain.ValueObjects.LanguagePreference.Create(
                LankaConnect.Modules.Identity.Domain.ValueObjects.LanguageCode.English,
                LankaConnect.Modules.Identity.Domain.Enums.ProficiencyLevel.Advanced).Value,
            LankaConnect.Modules.Identity.Domain.ValueObjects.LanguagePreference.Create(
                LankaConnect.Modules.Identity.Domain.ValueObjects.LanguageCode.Sinhala,
                LankaConnect.Modules.Identity.Domain.Enums.ProficiencyLevel.Native).Value
        };
        user.UpdateLanguages(languages);
        return user;
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
}
