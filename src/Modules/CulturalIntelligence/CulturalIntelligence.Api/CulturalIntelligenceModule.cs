using LankaConnect.Modules.CulturalIntelligence.Infrastructure;
using LankaConnect.Modules.CulturalIntelligence.Infrastructure.Services;
using LankaConnect.Modules.CulturalIntelligence.Contracts.Services;
using Microsoft.Extensions.DependencyInjection;

namespace LankaConnect.Modules.CulturalIntelligence.Api;

/// <summary>
/// Composition-root DI extension for the CulturalIntelligence module.
///
/// Wave 4.9 (2026-06-26): registered the initial 3 cultural service stubs.
/// Wave 8.5 GAP-1 Part A (2026-07-19) D-13 Option A: <c>ICulturalCalendar</c> promoted
/// to <see cref="LankaConnect.Modules.CulturalIntelligence.Contracts.Services.ICulturalCalendar"/>;
/// interface + supporting VOs (CulturalAppropriateness, DiasporaFriendliness,
/// EventNature, FestivalPeriod, SignificantDate, CalendarValidationResult) live in
/// CulturalIntelligence.Contracts.
/// Wave 8.5 GAP-1 Part B (2026-07-19): <c>StubCulturalCalendar</c> RETIRED — replaced
/// with real seed-file-backed <see cref="PoyaCalendarService"/> covering Sri Lankan
/// poya days for 2026-2028. Refresh seed file annually.
///
/// IUserPreferences + IGeographicProximityService remain LankaEvents.Domain.Services
/// types until their own Contracts promotion.
/// </summary>
public static class CulturalIntelligenceModule
{
    public static IServiceCollection AddCulturalIntelligenceModule(
        this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<ICulturalCalendar, PoyaCalendarService>();
        services.AddScoped<LankaConnect.Products.LankaEvents.Domain.Services.IUserPreferences, StubUserPreferences>();
        services.AddScoped<LankaConnect.Products.LankaEvents.Domain.Services.IGeographicProximityService, StubGeographicProximityService>();

        return services;
    }
}
