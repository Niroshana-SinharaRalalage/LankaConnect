using LankaConnect.BuildingBlocks.Application.Abstractions;
using LankaConnect.BuildingBlocks.Infrastructure.Outbox;
using LankaConnect.BuildingBlocks.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace LankaConnect.BuildingBlocks.Infrastructure.Tests;

/// <summary>DbContext that exposes outbox + dead-letter DbSets for tests.</summary>
public sealed class OutboxTestDbContext : BaseDbContext
{
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
    public DbSet<DeadLetterMessage> DeadLetterMessages => Set<DeadLetterMessage>();

    public OutboxTestDbContext(DbContextOptions<OutboxTestDbContext> options, ICurrentActor currentActor, ILogger logger)
        : base(options, currentActor, logger) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<OutboxMessage>().HasKey(m => m.Id);
        modelBuilder.Entity<DeadLetterMessage>().HasKey(m => m.Id);
    }
}

/// <summary>Dispatcher fake that records every dispatch + optionally throws.</summary>
public sealed class FakeIntegrationEventDispatcher : IIntegrationEventDispatcher
{
    public List<(string EventType, string Payload)> Dispatched { get; } = new();
    public Func<string, string, Task>? OnDispatch { get; set; }

    public Task DispatchAsync(string eventType, string payload, CancellationToken cancellationToken = default)
    {
        Dispatched.Add((eventType, payload));
        return OnDispatch?.Invoke(eventType, payload) ?? Task.CompletedTask;
    }
}

/// <summary>Builds an in-memory OutboxTestDbContext + matching service collection for processor tests.</summary>
public static class OutboxTestSetup
{
    public static (OutboxTestDbContext db, FakeIntegrationEventDispatcher dispatcher, IServiceProvider services) Build()
    {
        var dbName = Guid.NewGuid().ToString();

        var services = new ServiceCollection();
        services.AddSingleton<ICurrentActor>(new TestCurrentActor("test-actor"));
        services.AddSingleton<ILogger>(NullLogger.Instance);
        services.AddDbContext<OutboxTestDbContext>(opts => opts.UseInMemoryDatabase(dbName));

        var dispatcher = new FakeIntegrationEventDispatcher();
        services.AddSingleton<IIntegrationEventDispatcher>(dispatcher);

        var provider = services.BuildServiceProvider();
        var db = provider.GetRequiredService<OutboxTestDbContext>();

        return (db, dispatcher, provider);
    }
}
