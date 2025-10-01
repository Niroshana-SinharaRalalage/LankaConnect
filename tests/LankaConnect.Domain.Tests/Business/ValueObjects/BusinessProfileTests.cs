using LankaConnect.Domain.Business.ValueObjects;

namespace LankaConnect.Domain.Tests.Business.ValueObjects;

public class BusinessProfileTests
{
    [Fact]
    public void Create_WithValidData_ShouldReturnSuccess()
    {
        var name = "Rajitha's Restaurant";
        var description = "Authentic Sri Lankan cuisine with traditional recipes";
        var website = "https://rajithas.com";
        var socialMedia = CreateValidSocialMediaLinks();
        var services = new List<string> { "Dine-in", "Takeaway", "Catering" };
        var specializations = new List<string> { "Rice & Curry", "Kottu", "Hoppers" };

        var result = BusinessProfile.Create(name, description, website, socialMedia, services, specializations);

        Assert.True(result.IsSuccess);
        var profile = result.Value;
        Assert.Equal(name, profile.Name);
        Assert.Equal(description, profile.Description);
        Assert.Equal(website, profile.Website);
        Assert.Equal(socialMedia, profile.SocialMedia);
        Assert.Equal(services, profile.Services);
        Assert.Equal(specializations, profile.Specializations);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Create_WithInvalidName_ShouldReturnFailure(string name)
    {
        var result = BusinessProfile.Create(
            name,
            "Valid description",
            null,
            null,
            new List<string> { "Service" },
            new List<string> { "Specialization" });

        Assert.True(result.IsFailure);
        Assert.Contains("Business name is required", result.Errors);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Create_WithInvalidDescription_ShouldReturnFailure(string description)
    {
        var result = BusinessProfile.Create(
            "Valid Name",
            description,
            null,
            null,
            new List<string> { "Service" },
            new List<string> { "Specialization" });

        Assert.True(result.IsFailure);
        Assert.Contains("Business description is required", result.Errors);
    }

    [Fact]
    public void Create_WithTooLongName_ShouldReturnFailure()
    {
        var name = new string('a', 256);

        var result = BusinessProfile.Create(
            name,
            "Valid description",
            null,
            null,
            new List<string> { "Service" },
            new List<string> { "Specialization" });

        Assert.True(result.IsFailure);
        Assert.Contains("Business name cannot exceed 255 characters", result.Errors);
    }

    [Fact]
    public void Create_WithMaxLengthName_ShouldReturnSuccess()
    {
        var name = new string('a', 255);

        var result = BusinessProfile.Create(
            name,
            "Valid description",
            null,
            null,
            new List<string> { "Service" },
            new List<string> { "Specialization" });

        Assert.True(result.IsSuccess);
        Assert.Equal(name, result.Value.Name);
    }

    [Fact]
    public void Create_WithTooLongDescription_ShouldReturnFailure()
    {
        var description = new string('a', 2001);

        var result = BusinessProfile.Create(
            "Valid Name",
            description,
            null,
            null,
            new List<string> { "Service" },
            new List<string> { "Specialization" });

        Assert.True(result.IsFailure);
        Assert.Contains("Business description cannot exceed 2000 characters", result.Errors);
    }

    [Fact]
    public void Create_WithMaxLengthDescription_ShouldReturnSuccess()
    {
        var description = new string('a', 2000);

        var result = BusinessProfile.Create(
            "Valid Name",
            description,
            null,
            null,
            new List<string> { "Service" },
            new List<string> { "Specialization" });

        Assert.True(result.IsSuccess);
        Assert.Equal(description, result.Value.Description);
    }

    [Theory]
    [InlineData("not-a-url")]
    [InlineData("ftp://invalid.com")]
    [InlineData("invalid-protocol://test.com")]
    public void Create_WithInvalidWebsiteUrl_ShouldReturnFailure(string website)
    {
        var result = BusinessProfile.Create(
            "Valid Name",
            "Valid description",
            website,
            null,
            new List<string> { "Service" },
            new List<string> { "Specialization" });

        Assert.True(result.IsFailure);
        Assert.Contains("Invalid website URL format", result.Errors);
    }

    [Theory]
    [InlineData("https://example.com")]
    [InlineData("http://example.com")]
    [InlineData("https://sub.example.com/path")]
    [InlineData("http://example.com/path?query=value")]
    public void Create_WithValidWebsiteUrl_ShouldReturnSuccess(string website)
    {
        var result = BusinessProfile.Create(
            "Valid Name",
            "Valid description",
            website,
            null,
            new List<string> { "Service" },
            new List<string> { "Specialization" });

        Assert.True(result.IsSuccess);
        Assert.Equal(website, result.Value.Website);
    }

    [Fact]
    public void Create_WithNullWebsite_ShouldReturnSuccess()
    {
        var result = BusinessProfile.Create(
            "Valid Name",
            "Valid description",
            null,
            null,
            new List<string> { "Service" },
            new List<string> { "Specialization" });

        Assert.True(result.IsSuccess);
        Assert.Null(result.Value.Website);
    }

    [Fact]
    public void Create_WithEmptyServices_ShouldReturnFailure()
    {
        var result = BusinessProfile.Create(
            "Valid Name",
            "Valid description",
            null,
            null,
            new List<string>(),
            new List<string> { "Specialization" });

        Assert.True(result.IsFailure);
        Assert.Contains("At least one service must be provided", result.Errors);
    }

    [Fact]
    public void Create_WithNullServices_ShouldReturnFailure()
    {
        var result = BusinessProfile.Create(
            "Valid Name",
            "Valid description",
            null,
            null,
            null!,
            new List<string> { "Specialization" });

        Assert.True(result.IsFailure);
        Assert.Contains("At least one service must be provided", result.Errors);
    }

    [Fact]
    public void Create_WithEmptySpecializations_ShouldReturnFailure()
    {
        var result = BusinessProfile.Create(
            "Valid Name",
            "Valid description",
            null,
            null,
            new List<string> { "Service" },
            new List<string>());

        Assert.True(result.IsFailure);
        Assert.Contains("At least one specialization must be provided", result.Errors);
    }

    [Fact]
    public void Create_WithNullSpecializations_ShouldReturnFailure()
    {
        var result = BusinessProfile.Create(
            "Valid Name",
            "Valid description",
            null,
            null,
            new List<string> { "Service" },
            null!);

        Assert.True(result.IsFailure);
        Assert.Contains("At least one specialization must be provided", result.Errors);
    }

    [Fact]
    public void Create_WithWhitespaceOnlyServices_ShouldFilterThem()
    {
        var services = new List<string> { "Valid Service", "  ", "", null!, "Another Service" };

        var result = BusinessProfile.Create(
            "Valid Name",
            "Valid description",
            null,
            null,
            services,
            new List<string> { "Specialization" });

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.Services.Count);
        Assert.Contains("Valid Service", result.Value.Services);
        Assert.Contains("Another Service", result.Value.Services);
    }

    [Fact]
    public void Create_WithWhitespaceOnlySpecializations_ShouldFilterThem()
    {
        var specializations = new List<string> { "Valid Spec", "  ", "", null!, "Another Spec" };

        var result = BusinessProfile.Create(
            "Valid Name",
            "Valid description",
            null,
            null,
            new List<string> { "Service" },
            specializations);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.Specializations.Count);
        Assert.Contains("Valid Spec", result.Value.Specializations);
        Assert.Contains("Another Spec", result.Value.Specializations);
    }

    [Fact]
    public void Create_ShouldTrimWhitespaceFromInputs()
    {
        var result = BusinessProfile.Create(
            "  Business Name  ",
            "  Business Description  ",
            "  https://example.com  ",
            null,
            new List<string> { "  Service 1  ", "  Service 2  " },
            new List<string> { "  Spec 1  ", "  Spec 2  " });

        Assert.True(result.IsSuccess);
        var profile = result.Value;
        Assert.Equal("Business Name", profile.Name);
        Assert.Equal("Business Description", profile.Description);
        Assert.Equal("https://example.com", profile.Website);
        Assert.Equal("Service 1", profile.Services[0]);
        Assert.Equal("Service 2", profile.Services[1]);
        Assert.Equal("Spec 1", profile.Specializations[0]);
        Assert.Equal("Spec 2", profile.Specializations[1]);
    }

    [Fact]
    public void Equality_WithSameValues_ShouldBeEqual()
    {
        var services = new List<string> { "Service 1", "Service 2" };
        var specializations = new List<string> { "Spec 1", "Spec 2" };

        var profile1 = BusinessProfile.Create("Name", "Description", "https://example.com", 
            null, services, specializations).Value;
        var profile2 = BusinessProfile.Create("Name", "Description", "https://example.com", 
            null, services, specializations).Value;

        Assert.Equal(profile1, profile2);
    }

    [Fact]
    public void Equality_WithDifferentNames_ShouldNotBeEqual()
    {
        var services = new List<string> { "Service" };
        var specializations = new List<string> { "Spec" };

        var profile1 = BusinessProfile.Create("Name 1", "Description", null, 
            null, services, specializations).Value;
        var profile2 = BusinessProfile.Create("Name 2", "Description", null, 
            null, services, specializations).Value;

        Assert.NotEqual(profile1, profile2);
    }

    [Fact]
    public void Equality_WithDifferentServices_ShouldNotBeEqual()
    {
        var specializations = new List<string> { "Spec" };

        var profile1 = BusinessProfile.Create("Name", "Description", null, 
            null, new List<string> { "Service 1" }, specializations).Value;
        var profile2 = BusinessProfile.Create("Name", "Description", null, 
            null, new List<string> { "Service 2" }, specializations).Value;

        Assert.NotEqual(profile1, profile2);
    }

    [Fact]
    public void GetHashCode_WithSameValues_ShouldBeEqual()
    {
        var services = new List<string> { "Service" };
        var specializations = new List<string> { "Spec" };

        var profile1 = BusinessProfile.Create("Name", "Description", null, 
            null, services, specializations).Value;
        var profile2 = BusinessProfile.Create("Name", "Description", null, 
            null, services, specializations).Value;

        Assert.Equal(profile1.GetHashCode(), profile2.GetHashCode());
    }

    private static SocialMediaLinks CreateValidSocialMediaLinks()
    {
        return SocialMediaLinks.Create(
            "https://facebook.com/business",
            "https://twitter.com/business",
            "https://instagram.com/business",
            "https://linkedin.com/company/business").Value;
    }
}