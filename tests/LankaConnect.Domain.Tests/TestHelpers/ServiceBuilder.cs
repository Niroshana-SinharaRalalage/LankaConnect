using LankaConnect.Domain.Business;
using LankaConnect.Domain.Shared.Enums;
using LankaConnect.Domain.Shared.ValueObjects;

namespace LankaConnect.Domain.Tests.TestHelpers;

public static class ServiceBuilder
{
    public static Service Create(string name = "Test Service")
    {
        var price = Money.Create(1000, Currency.LKR).Value;
        
        var result = Service.Create(
            name,
            "Test service description",
            price,
            "1 hour"
        );

        return result.Value;
    }

    public static Service CreateWithoutPrice(string name = "Test Service")
    {
        var result = Service.Create(
            name,
            "Test service description"
        );

        return result.Value;
    }

    public static Service CreateExpensive(string name = "Expensive Service")
    {
        var price = Money.Create(10000, Currency.LKR).Value;
        
        var result = Service.Create(
            name,
            "Expensive service description",
            price,
            "1 day"
        );

        return result.Value;
    }
}