using LankaConnect.Domain.Business.ValueObjects;

namespace LankaConnect.Domain.Tests.TestHelpers;

public static class BusinessLocationBuilder
{
    public static BusinessLocation Create()
    {
        var result = BusinessLocation.Create(
            "123 Main Street",
            "Colombo",
            "Western Province",
            "00100",
            "Sri Lanka",
            6.9271m, // Colombo latitude
            79.8612m  // Colombo longitude
        );

        return result.Value;
    }

    public static BusinessLocation CreateWithoutCoordinates()
    {
        var result = BusinessLocation.Create(
            "456 Test Street",
            "Kandy",
            "Central Province",
            "20000",
            "Sri Lanka"
        );

        return result.Value;
    }

    public static BusinessLocation CreateAtCoordinates(decimal latitude, decimal longitude)
    {
        var result = BusinessLocation.Create(
            "Test Address",
            "Test City",
            "Test State",
            "12345",
            "Test Country",
            latitude,
            longitude
        );

        return result.Value;
    }
}