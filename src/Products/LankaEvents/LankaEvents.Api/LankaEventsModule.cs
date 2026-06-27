using Microsoft.Extensions.DependencyInjection;

namespace LankaConnect.Products.LankaEvents.Api;

/// <summary>
/// Composition-root DI extension for the LankaEvents product. Wave 5.0
/// (2026-06-26) skeleton — empty seam. Subsequent waves populate it as
/// the Event family migrates:
///
///  W5.1–W5.7: physical Domain moves (Enums/VOs/SubAggregates/Event itself)
///  W5.8: Application handler migrations (MediatR scan registration)
///  W5.9: Controller migrations (auto-discovered via attribute routing)
///  W5.10: ArchTest hardening + STAGING-VERIFIED
///
/// The Event family's repositories + DbContext registrations currently live
/// in LankaConnect.Infrastructure.DependencyInjection.AddInfrastructure();
/// they migrate here in W5.7+ alongside the Domain move.
/// </summary>
public static class LankaEventsModule
{
    public static IServiceCollection AddLankaEventsModule(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        // Wave 5.0: no registrations yet. Populated in W5.7+ as repositories
        // + Application handlers + domain services migrate from
        // LankaConnect.Infrastructure / LankaConnect.Application.

        return services;
    }
}
