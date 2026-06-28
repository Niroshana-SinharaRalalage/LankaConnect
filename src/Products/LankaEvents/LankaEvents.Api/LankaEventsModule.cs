using System.Reflection;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace LankaConnect.Products.LankaEvents.Api;

/// <summary>
/// Composition-root DI extension for the LankaEvents product. Wave 5.0
/// (2026-06-26) skeleton; populated as the Event family migrates:
///
///  W5.1   physical Domain moves (SHIPPED 47e14ef9 + 59ed4483)
///  W5.2.a HasMany config fix (SHIPPED 9d9c2e78)
///  W5.2.a-fix EventPass/PassPurchase feature deletion (SHIPPED 918b0f6d)
///  W5.2.b Application Command handlers move + MediatR scan registration (THIS)
///  W5.2.c Application Query handlers move
///  W5.2.d BackgroundJobs + Services + Common stragglers
///  W5.3+  Infrastructure carve-out + Repositories + DbContext partition
///  W5.10  ArchTest hardening + STAGING-VERIFIED
/// </summary>
public static class LankaEventsModule
{
    public static IServiceCollection AddLankaEventsModule(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        // Wave 5.2.b (2026-06-28): register MediatR handlers from the LankaEvents.Application
        // assembly so the 225 Commands moved out of LankaConnect.Application/Events/Commands/
        // remain discoverable. The legacy LankaConnect.Application registration in
        // DependencyInjection.AddApplication() still scans its own assembly for handlers
        // that haven't moved yet (Queries / BackgroundJobs / Services until W5.2.c/d).
        var assembly = typeof(LankaConnect.Products.LankaEvents.Application.AssemblyMarker).Assembly;
        services.AddMediatR(config =>
        {
            config.RegisterServicesFromAssembly(assembly);
        });

        // Wave 5.2.b: FluentValidation validators in the moved Commands assembly need
        // their own registration sweep -- AddValidatorsFromAssembly is per-assembly.
        services.AddValidatorsFromAssembly(assembly);

        return services;
    }
}
