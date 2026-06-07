using LankaConnect.Modules.CulturalIntelligence.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace LankaConnect.Modules.CulturalIntelligence.Api;

/// <summary>
/// Composition-root DI extension for the CulturalIntelligence module.
/// W4.7 (2026-06-06): registers <see cref="StubCulturalCalendar"/> as the
/// concrete implementation of <c>LankaConnect.Domain.Events.Services.ICulturalCalendar</c>.
/// The interface stays in legacy Events.Services until Wave 5 Products
/// carve-out moves the EventRecommendationEngine consumer.
/// </summary>
public static class CulturalIntelligenceModule
{
    public static IServiceCollection AddCulturalIntelligenceModule(
        this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddScoped<LankaConnect.Domain.Events.Services.ICulturalCalendar, StubCulturalCalendar>();

        return services;
    }
}
