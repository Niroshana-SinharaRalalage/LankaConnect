using LankaConnect.Domain.Business.ValueObjects;

namespace LankaConnect.Domain.Tests.TestHelpers;

public static class BusinessProfileBuilder
{
    public static BusinessProfile Create(
        string name = "Test Restaurant",
        string description = "A great place to eat")
    {
        var services = new List<string> { "Dine-in", "Takeaway" };
        var specializations = new List<string> { "Sri Lankan Cuisine", "Seafood" };
        var website = "https://testrestaurant.lk";

        var result = BusinessProfile.Create(
            name,
            description,
            website,
            null, // SocialMediaLinks
            services,
            specializations
        );

        return result.Value;
    }

    public static BusinessProfile CreateWithoutWebsite()
    {
        var services = new List<string> { "Service 1" };
        var specializations = new List<string> { "Specialization 1" };

        var result = BusinessProfile.Create(
            "Test Business",
            "Test Description",
            null, // No website
            null,
            services,
            specializations
        );

        return result.Value;
    }
}