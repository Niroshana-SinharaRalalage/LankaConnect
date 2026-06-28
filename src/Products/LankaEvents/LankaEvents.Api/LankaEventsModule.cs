using System.Reflection;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using LankaConnect.Products.LankaEvents.Application.Services;

namespace LankaConnect.Products.LankaEvents.Api;

/// <summary>
/// Composition-root DI extension for the LankaEvents product. Wave 5.0
/// (2026-06-26) skeleton; populated as the Event family migrates:
///
///  W5.1   physical Domain moves (SHIPPED 47e14ef9 + 59ed4483)
///  W5.2.a HasMany config fix (SHIPPED 9d9c2e78)
///  W5.2.a-fix EventPass/PassPurchase feature deletion (SHIPPED 918b0f6d)
///  W5.2.b Application Command handlers move + MediatR scan registration (SHIPPED 7e040d5b)
///  W5.2.c Application Query handlers move (SHIPPED 7eb8f71f)
///  W5.2.d BackgroundJobs + EventHandlers + Repositories + Services + Common stragglers (THIS)
///  W5.3+  Infrastructure carve-out + Repositories + DbContext partition
///  W5.10  ArchTest hardening + STAGING-VERIFIED
/// </summary>
public static class LankaEventsModule
{
    public static IServiceCollection AddLankaEventsModule(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        // Wave 5.2.b (2026-06-28): register MediatR handlers from the LankaEvents.Application
        // assembly so the 225+ Commands + 101+ Queries moved out of
        // LankaConnect.Application/Events/ remain discoverable.
        var assembly = typeof(LankaConnect.Products.LankaEvents.Application.AssemblyMarker).Assembly;
        services.AddMediatR(config =>
        {
            config.RegisterServicesFromAssembly(assembly);
        });

        // Wave 5.2.b: FluentValidation validators in the moved Commands assembly need
        // their own registration sweep -- AddValidatorsFromAssembly is per-assembly.
        services.AddValidatorsFromAssembly(assembly);

        // Wave 5.2.d-fix (2026-06-28): AutoMapper profile scan for the moved
        // EventMappingProfile + GroupPricingTierMappingProfile (now in
        // Products.LankaEvents.Application.Common). Without this, every Query that
        // calls IMapper.Map<EventDto>(event) throws AutoMapperMappingException at
        // runtime -- read endpoints return 500 (POST works because CreateEvent
        // command returns just the Guid; reads use AutoMapper). This is the third
        // assembly-scan system the architect warned about for W5.2 -- MediatR +
        // FluentValidation + AutoMapper each scan a single assembly.
        services.AddAutoMapper(assembly);

        // Wave 5.2.d (2026-06-28): Event-specific service registrations relocated from
        // LankaConnect.Application.DependencyInjection.AddApplication(). These were
        // scoped service interfaces in LankaConnect.Application.Events.Services that
        // moved to Products.LankaEvents.Application.Services in W5.2.d; the legacy
        // AddApplication() cannot reference Products (would cycle).
        services.AddScoped<ILayoutAuthorizationService, LayoutAuthorizationService>();
        services.AddScoped<IStructuralEditGuard, StructuralEditGuard>();
        services.AddScoped<ISeatAssignmentValidator, SeatAssignmentValidator>();
        services.AddScoped<ILayoutMetrics, LayoutMetrics>();
        services.AddScoped<ISeatHoldMetrics, SeatHoldMetrics>();

        return services;
    }
}
