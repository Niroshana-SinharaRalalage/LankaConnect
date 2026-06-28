using LankaConnect.Modules.CulturalIntelligence.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace LankaConnect.Modules.CulturalIntelligence.Api;

/// <summary>
/// Composition-root DI extension for the CulturalIntelligence module.
/// Wave 4.9 (2026-06-26): registers all 3 cultural service stubs
/// (<see cref="StubCulturalCalendar"/>, <see cref="StubUserPreferences"/>,
/// <see cref="StubGeographicProximityService"/>). The service interfaces stay
/// in legacy <c>LankaConnect.Products.LankaEvents.Domain.Services</c> until Wave 5 Products
/// carve-out moves the EventRecommendationEngine consumer.
/// </summary>
public static class CulturalIntelligenceModule
{
    public static IServiceCollection AddCulturalIntelligenceModule(
        this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddScoped<LankaConnect.Products.LankaEvents.Domain.Services.ICulturalCalendar, StubCulturalCalendar>();
        services.AddScoped<LankaConnect.Products.LankaEvents.Domain.Services.IUserPreferences, StubUserPreferences>();
        services.AddScoped<LankaConnect.Products.LankaEvents.Domain.Services.IGeographicProximityService, StubGeographicProximityService>();

        return services;
    }
}
