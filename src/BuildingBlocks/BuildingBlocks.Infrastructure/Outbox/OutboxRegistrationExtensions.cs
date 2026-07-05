using LankaConnect.BuildingBlocks.Contracts.IntegrationEvents;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
namespace LankaConnect.BuildingBlocks.Infrastructure.Outbox;

/// <summary>
/// Wave 6.5.a: DI registration helpers for per-module outbox wiring.
/// Every module owning a <see cref="DbContext"/> with <c>DbSet&lt;OutboxMessage&gt;</c>
/// + <c>DbSet&lt;DeadLetterMessage&gt;</c> calls
/// <see cref="AddModuleOutbox{TDbContext}"/> from its module registration
/// extension (e.g. <c>MediaModule.AddMediaModule()</c>).
/// </summary>
/// <remarks>
/// <para>
/// Registrations added by <see cref="AddModuleOutbox{TDbContext}"/>:
/// </para>
/// <list type="bullet">
///   <item><description><see cref="OutboxIntegrationEventDispatcher{TDbContext}"/>
///   as <c>Scoped</c> — the producer-side outbox writer.</description></item>
///   <item><description><see cref="OutboxProcessor{TDbContext}"/>
///   as <c>IHostedService</c> (singleton via <c>AddHostedService</c>) — the
///   consumer-side poll + dispatch loop.</description></item>
/// </list>
/// <para>
/// Handlers inject <c>IIntegrationEventOutbox&lt;TDbContext&gt;</c> to enqueue
/// events. The adapter <c>LankaConnect.BuildingBlocks.Infrastructure.Outbox.IntegrationEventOutbox&lt;TDbContext&gt;</c>
/// bridges the Application-layer interface to the concrete
/// <see cref="OutboxIntegrationEventDispatcher{TDbContext}"/> — and IS registered
/// separately by the composition root because it depends on
/// <c>LankaConnect.Application</c> which BuildingBlocks cannot reference.
/// </para>
/// </remarks>
public static class OutboxRegistrationExtensions
{
    /// <summary>
    /// Registers the outbox producer + consumer for the given module DbContext.
    /// Idempotent: calling twice with the same <typeparamref name="TDbContext"/>
    /// is a no-op after the first registration.
    /// </summary>
    /// <typeparam name="TDbContext">Module DbContext exposing
    /// <c>DbSet&lt;OutboxMessage&gt;</c> + <c>DbSet&lt;DeadLetterMessage&gt;</c>.</typeparam>
    /// <param name="services">Service collection to register into.</param>
    /// <param name="pollInterval">Optional override for the processor's poll
    /// interval. Defaults to <see cref="OutboxProcessor{TDbContext}.DefaultPollInterval"/>
    /// (5 seconds).</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddModuleOutbox<TDbContext>(
        this IServiceCollection services,
        TimeSpan? pollInterval = null)
        where TDbContext : DbContext
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddScoped<OutboxIntegrationEventDispatcher<TDbContext>>();

        services.AddHostedService(sp =>
            new OutboxProcessor<TDbContext>(
                sp,
                sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<OutboxProcessor<TDbContext>>>(),
                pollInterval));

        // MediatRIntegrationEventDispatcher is the process-side consumer that
        // OutboxProcessor uses to fan out payloads to INotificationHandler<T>.
        // Register once — it's TDbContext-agnostic — and the ServiceCollection
        // TryAdd semantics ensure no duplicate registration.
        services.TryAddSingletonMediatRIntegrationEventDispatcher();

        return services;
    }

    /// <summary>
    /// Adds the MediatR-backed integration event dispatcher exactly once. Multiple
    /// <see cref="AddModuleOutbox{TDbContext}"/> calls share the same instance.
    /// </summary>
    private static void TryAddSingletonMediatRIntegrationEventDispatcher(this IServiceCollection services)
    {
        var alreadyRegistered = services.Any(d => d.ServiceType == typeof(IIntegrationEventDispatcher));
        if (!alreadyRegistered)
        {
            services.AddSingleton<IIntegrationEventDispatcher, MediatRIntegrationEventDispatcher>();
        }
    }
}
