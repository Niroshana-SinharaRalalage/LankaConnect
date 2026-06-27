using LankaConnect.Modules.CulturalIntelligence.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace LankaConnect.Modules.CulturalIntelligence.Api;

/// <summary>
/// Composition-root DI extension for the CulturalIntelligence module.
/// Wave 4.9 (2026-06-26): registers all 3 cultural service stubs
/// (<see cref="StubCulturalCalendar"/>, <see cref="StubUserPreferences"/>,
/// <see cref="StubGeographicProximityService"/>). The service interfaces stay
/// in legacy <c>LankaConnect.Domain.Events.Services</c> until Wave 5 Products
/// carve-out moves the EventRecommendationEngine consumer.
/// </summary>
public static class CulturalIntelligenceModule
{
    public static IServiceCollection AddCulturalIntelligenceModule(
        this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddScoped<LankaConnect.Domain.Events.Services.ICulturalCalendar, StubCulturalCalendar>();
        services.AddScoped<LankaConnect.Domain.Events.Services.IUserPreferences, StubUserPreferences>();
        services.AddScoped<LankaConnect.Domain.Events.Services.IGeographicProximityService, StubGeographicProximityService>();

        return services;
    }
}
