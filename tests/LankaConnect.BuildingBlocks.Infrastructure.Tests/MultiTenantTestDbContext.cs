using LankaConnect.BuildingBlocks.Application.Abstractions;
using LankaConnect.BuildingBlocks.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LankaConnect.BuildingBlocks.Infrastructure.Tests;

/// <summary>
/// Per-Capability-style DbContext that opts into the multi-tenant query filter
/// via <see cref="BaseDbContext.ApplyMultiTenantFilter{TTenantId}"/>. Used by
/// W1B BaseDbContextMultiTenantTests to verify automatic per-tenant row scoping.
/// </summary>
public sealed class MultiTenantTestDbContext : BaseDbContext
{
    /// <summary>
    /// Per-instance tenant ID, set once at construction. The filter expression
    /// references this PROPERTY (not a captured field) so EF Core treats it
    /// as a per-DbContext parameter and re-evaluates per query — surviving
    /// the per-type model cache.
    /// </summary>
    public TestTenantId CurrentTenantId { get; }

    public DbSet<MultiTenantEntity> Tenants => Set<MultiTenantEntity>();

    public MultiTenantTestDbContext(
        DbContextOptions<MultiTenantTestDbContext> options,
        ICurrentActor currentActor,
        ILogger logger,
        TestTenantId currentTenantId)
        : base(options, currentActor, logger)
    {
        CurrentTenantId = currentTenantId;
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<MultiTenantEntity>().HasKey(e => e.Id);

        ApplyMultiTenantFilter<TestTenantId>(modelBuilder, () => CurrentTenantId);
    }
}

/// <summary>
/// Builds a fresh InMemory MultiTenantTestDbContext scoped to a specific tenant.
/// For tenant-flip scenarios, build a second DbContext with the new tenant
/// against the SAME in-memory database name (per-DbContext tenant capture is
/// the production pattern; mid-instance flips are not supported by design).
/// </summary>
public static class MultiTenantTestDbContextBuilder
{
    public static MultiTenantTestDbContext Build(TestTenantId currentTenant, string? databaseName = null)
    {
        var options = new DbContextOptionsBuilder<MultiTenantTestDbContext>()
            .UseInMemoryDatabase(databaseName: databaseName ?? Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        var actor = new TestCurrentActor("test");
        return new MultiTenantTestDbContext(options, actor, Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance, currentTenant);
    }
}
