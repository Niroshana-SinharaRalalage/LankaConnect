using System.Reflection;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using LankaConnect.BuildingBlocks.Infrastructure.Outbox;
using LankaConnect.Products.LankaEvents.Application.Services;
using LankaConnect.Products.LankaEvents.Domain;
using LankaConnect.Products.LankaEvents.Infrastructure.Data;
using LankaConnect.Products.LankaEvents.Infrastructure.Repositories;
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
    /// <summary>Per-context migrations history table name (EF convention).</summary>
    public const string MigrationsHistoryTable = "__EFMigrationsHistory";

    public static IServiceCollection AddLankaEventsModule(this IServiceCollection services)
        => services.AddLankaEventsModule(configuration: null);

    public static IServiceCollection AddLankaEventsModule(
        this IServiceCollection services,
        IConfiguration? configuration)
    {
        ArgumentNullException.ThrowIfNull(services);

        // Wave 6.5.e (2026-07-03): register LankaEventsDbContext + per-product
        // outbox pipeline. Only wires when the caller supplies IConfiguration;
        // legacy no-arg call sites (early Wave 5.0-5.4 slice landings) fall
        // through to the MediatR / AutoMapper / repository-DI block below and
        // rely on AddInfrastructure() to keep AppDbContext registered.
        if (configuration is not null)
        {
            AddLankaEventsDbContext(services, configuration);
        }

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

        // W5.3.a1 (2026-06-28): first Infrastructure repo relocated to Products.
        // MetroAreaRepository moved from LankaConnect.Infrastructure.Data.Repositories
        // to Products/LankaEvents.Infrastructure/Repositories. Same AppDbContext, same
        // Repository<T> base, same SQL — only the assembly + DI registration site
        // changed. Proves the cross-module DI pattern for W5.3.a2 (bulk leaves) +
        // W5.3.b (finance) + W5.3.c (aggregate root + children).
        services.AddScoped<IMetroAreaRepository, MetroAreaRepository>();

        // W5.3.a2 (2026-06-28): bulk leaf-repo relocation. Three more Event-family
        // repositories shifted from LankaConnect.Infrastructure to Products following
        // the W5.3.a1 proof. Interfaces already lived in Products.* namespaces.
        // EventAnalytics + EventViewRecord intentionally deferred: their interfaces
        // remain in LankaConnect.Domain.Analytics and a separate slice (W5.3.b or
        // dedicated cleanup) will move them.
        services.AddScoped<LankaConnect.Products.LankaEvents.Application.Repositories.IEventReminderRepository, LankaConnect.Products.LankaEvents.Infrastructure.Repositories.EventReminderRepository>();
        services.AddScoped<LankaConnect.Products.LankaEvents.Application.Repositories.IEventNotificationHistoryRepository, LankaConnect.Products.LankaEvents.Infrastructure.Repositories.EventNotificationHistoryRepository>();
        services.AddScoped<LankaConnect.Products.LankaEvents.Domain.Repositories.ITicketScanLogRepository, LankaConnect.Products.LankaEvents.Infrastructure.Repositories.TicketScanLogRepository>();

        // W5.3.b (2026-06-28): 8 Event-finance repositories relocated to Products.
        // All interfaces already in LankaConnect.Products.LankaEvents.Domain.Repositories;
        // implementations now also in Products/LankaEvents.Infrastructure/Repositories.
        // RegistrationAddition + RegistrationPayment cover the add-on lifecycle write paths;
        // Donation / Collection / Sponsor / SponsorshipPackage / AddOnDefinition / AddOnPurchase
        // cover the rest of the Event finance domain.
        services.AddScoped<LankaConnect.Products.LankaEvents.Domain.Repositories.IRegistrationAdditionRepository, LankaConnect.Products.LankaEvents.Infrastructure.Repositories.RegistrationAdditionRepository>();
        services.AddScoped<LankaConnect.Products.LankaEvents.Domain.Repositories.IRegistrationPaymentRepository, LankaConnect.Products.LankaEvents.Infrastructure.Repositories.RegistrationPaymentRepository>();
        services.AddScoped<LankaConnect.Products.LankaEvents.Domain.Repositories.IDonationRepository, LankaConnect.Products.LankaEvents.Infrastructure.Repositories.DonationRepository>();
        services.AddScoped<LankaConnect.Products.LankaEvents.Domain.Repositories.ICollectionRepository, LankaConnect.Products.LankaEvents.Infrastructure.Repositories.CollectionRepository>();
        services.AddScoped<LankaConnect.Products.LankaEvents.Domain.Repositories.ISponsorRepository, LankaConnect.Products.LankaEvents.Infrastructure.Repositories.SponsorRepository>();
        services.AddScoped<LankaConnect.Products.LankaEvents.Domain.Repositories.IAddOnDefinitionRepository, LankaConnect.Products.LankaEvents.Infrastructure.Repositories.AddOnDefinitionRepository>();
        services.AddScoped<LankaConnect.Products.LankaEvents.Domain.Repositories.IAddOnPurchaseRepository, LankaConnect.Products.LankaEvents.Infrastructure.Repositories.AddOnPurchaseRepository>();
        services.AddScoped<LankaConnect.Products.LankaEvents.Domain.Repositories.ISponsorshipPackageRepository, LankaConnect.Products.LankaEvents.Infrastructure.Repositories.SponsorshipPackageRepository>();

        // W5.3.c1 (2026-06-28): 4 Event child-entity repositories relocated to Products.
        // VenueLayout + Seat (Hold + Reservation) + Ticket cover the seated-event +
        // ticket-issuance write paths. Architect-gated split from c2 (Event +
        // Registration aggregate roots) so blast radius bisects cleanly if a
        // regression appears in one or the other half.
        services.AddScoped<LankaConnect.Products.LankaEvents.Domain.Repositories.IVenueLayoutRepository, LankaConnect.Products.LankaEvents.Infrastructure.Repositories.VenueLayoutRepository>();
        services.AddScoped<LankaConnect.Products.LankaEvents.Domain.Repositories.ISeatHoldRepository, LankaConnect.Products.LankaEvents.Infrastructure.Repositories.SeatHoldRepository>();
        services.AddScoped<LankaConnect.Products.LankaEvents.Domain.Repositories.ISeatReservationRepository, LankaConnect.Products.LankaEvents.Infrastructure.Repositories.SeatReservationRepository>();
        services.AddScoped<LankaConnect.Products.LankaEvents.Domain.Repositories.ITicketRepository, LankaConnect.Products.LankaEvents.Infrastructure.Repositories.TicketRepository>();

        // W5.3.c2 (2026-06-28): Event + Registration aggregate-root repository registrations.
        // EventRepository (953 LOC) is the spine of the entire Events read+write surface;
        // RegistrationRepository (607 LOC) hosts the GetByIdAsync tracking override that
        // raises PaymentCompletedEvent + RegistrationConfirmedEvent through the dispatch
        // chain widened in Wave3-followup.B (1688aee9). Both interfaces in Products.LankaEvents.Domain.
        services.AddScoped<LankaConnect.Products.LankaEvents.Domain.IEventRepository, LankaConnect.Products.LankaEvents.Infrastructure.Repositories.EventRepository>();
        services.AddScoped<LankaConnect.Products.LankaEvents.Domain.IRegistrationRepository, LankaConnect.Products.LankaEvents.Infrastructure.Repositories.RegistrationRepository>();

        // W5.4.b (2026-06-29): Analytics repositories relocated to Products. Completes the
        // Event-family Repository carve-out begun in Wave 5.3. Interfaces moved in
        // Wave 5.4.a (ae50fb27); implementations + DI shift in this sub-slice.
        services.AddScoped<LankaConnect.Products.LankaEvents.Domain.Repositories.IEventAnalyticsRepository, LankaConnect.Products.LankaEvents.Infrastructure.Repositories.EventAnalyticsRepository>();
        services.AddScoped<LankaConnect.Products.LankaEvents.Domain.Repositories.IEventViewRecordRepository, LankaConnect.Products.LankaEvents.Infrastructure.Repositories.EventViewRecordRepository>();

        return services;
    }

    /// <summary>
    /// Wave 6.5.e (2026-07-03): registers <see cref="LankaEventsDbContext"/>
    /// against the same Postgres connection AppDbContext uses, plus the
    /// per-product outbox pipeline
    /// (<c>AddModuleOutbox&lt;LankaEventsDbContext&gt;()</c> — producer scoped +
    /// hosted <see cref="OutboxProcessor{TDbContext}"/>). The
    /// <c>IIntegrationEventOutbox&lt;LankaEventsDbContext&gt;</c> adapter is
    /// registered by the composition root
    /// (<c>LankaConnect.API.Program.cs</c>) because it depends on the legacy
    /// <c>LankaConnect.Infrastructure.Outbox.IntegrationEventOutbox&lt;T&gt;</c>
    /// concrete — matching the Media + Notifications pattern.
    /// </summary>
    private static void AddLankaEventsDbContext(IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "ConnectionStrings:DefaultConnection is required to register LankaEventsDbContext.");

        // Sprint Day 1 fix (2026-07-05): mirror AppDbContext's NpgsqlDataSourceBuilder
        // pattern with EnableDynamicJson(). SignUpListConfiguration maps a shadow
        // Property<List<string>>("_predefinedItems") to jsonb; Npgsql 8+ requires
        // opt-in for dynamic JSON serialization of List<T> types. Without this,
        // any query loading a SignUpList throws:
        //   System.NotSupportedException: Type 'List`1' required dynamic JSON
        //   serialization, which requires an explicit opt-in
        // Same construction as LankaConnect.Infrastructure.DependencyInjection.
        var dataSourceBuilder = new Npgsql.NpgsqlDataSourceBuilder(connectionString);
        dataSourceBuilder.EnableDynamicJson();
        // NOTE: NetTopologySuite is configured on npgsqlOptions inside UseNpgsql
        // (line ~185), not here on the data source builder — the extension method
        // does not exist on NpgsqlDataSourceBuilder in Npgsql 8.
        var dataSource = dataSourceBuilder.Build();

        services.AddDbContext<LankaEventsDbContext>(options =>
        {
            options.UseNpgsql(dataSource, npgsqlOptions =>
            {
                npgsqlOptions.MigrationsAssembly(typeof(LankaEventsDbContext).Assembly.GetName().Name);
                npgsqlOptions.MigrationsHistoryTable(MigrationsHistoryTable, LankaEventsDbContext.SchemaName);
                npgsqlOptions.UseNetTopologySuite(); // Event aggregate uses PostGIS spatial types
                npgsqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 3,
                    maxRetryDelay: TimeSpan.FromSeconds(5),
                    errorCodesToAdd: null);
                npgsqlOptions.CommandTimeout(30);
            });
        }, ServiceLifetime.Scoped);

        // Wave 6.5.e: per-product outbox wiring (producer scoped +
        // OutboxProcessor hosted). The IIntegrationEventOutbox<LankaEventsDbContext>
        // adapter is registered by the composition root (LankaConnect.API) —
        // see Program.cs.
        services.AddModuleOutbox<LankaEventsDbContext>();
    }
}
