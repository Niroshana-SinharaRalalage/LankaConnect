using LankaConnect.BuildingBlocks.Application.Abstractions;
using LankaConnect.BuildingBlocks.Domain;
using LankaConnect.BuildingBlocks.Infrastructure.Persistence;
using LankaConnect.SharedKernel.Money;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace LankaConnect.BuildingBlocks.Infrastructure.Tests;

/// <summary>Concrete DbContext for testing BaseDbContext behaviors.</summary>
public sealed class TestDbContext : BaseDbContext
{
    public DbSet<PlainEntity> Plain => Set<PlainEntity>();
    public DbSet<AuditableEntity> Auditable => Set<AuditableEntity>();
    public DbSet<SoftDeletableEntity> SoftDeletable => Set<SoftDeletableEntity>();
    public DbSet<AuditableAndSoftDeletableEntity> Both => Set<AuditableAndSoftDeletableEntity>();
    public DbSet<MoneyOwnerEntity> MoneyOwners => Set<MoneyOwnerEntity>();
    public DbSet<ConcurrencyTokenEntity> ConcurrencyTokens => Set<ConcurrencyTokenEntity>();

    public TestDbContext(DbContextOptions<TestDbContext> options, ICurrentActor currentActor, ILogger logger)
        : base(options, currentActor, logger) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<PlainEntity>().HasKey(e => e.Id);
        modelBuilder.Entity<AuditableEntity>().HasKey(e => e.Id);
        modelBuilder.Entity<SoftDeletableEntity>().HasKey(e => e.Id);
        modelBuilder.Entity<AuditableAndSoftDeletableEntity>().HasKey(e => e.Id);
        modelBuilder.Entity<ConcurrencyTokenEntity>().HasKey(e => e.Id);

        // Money owner configured via the helper under test
        modelBuilder.Entity<MoneyOwnerEntity>(b =>
        {
            b.HasKey(e => e.Id);
            b.ConfigureMoney(e => e.Price, columnPrefix: "price");
        });
    }
}

/// <summary>Entity without any markers — should pass through unchanged.</summary>
public sealed class PlainEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
}

public sealed class AuditableEntity : IAuditable
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
}

public sealed class SoftDeletableEntity : ISoftDeletable
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }
}

public sealed class AuditableAndSoftDeletableEntity : IAuditable, ISoftDeletable
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }
}

public sealed class MoneyOwnerEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public Money? Price { get; set; }
}

/// <summary>Entity that opts into optimistic concurrency control via IConcurrencyToken.</summary>
public sealed class ConcurrencyTokenEntity : IConcurrencyToken
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
}

/// <summary>Typed tenant identifier for multi-tenant tests. Mirrors the shape of StorefrontId/OrganizationId from SharedKernel.Identity (W1D).</summary>
public readonly record struct TestTenantId(Guid Value)
{
    public static TestTenantId New() => new(Guid.NewGuid());
}

/// <summary>Entity scoped to a tenant via IMultiTenant&lt;TestTenantId&gt;.</summary>
public sealed class MultiTenantEntity : IMultiTenant<TestTenantId>
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public TestTenantId TenantId { get; init; }
}

/// <summary>Returns the configured actor id; mutable for tests that vary actor mid-scenario.</summary>
public sealed class TestCurrentActor : ICurrentActor
{
    public string? ActorId { get; set; }
    public TestCurrentActor(string? actorId = null) { ActorId = actorId; }
}

/// <summary>Builds a fresh InMemory TestDbContext + matching actor + null logger.</summary>
public static class TestDbContextBuilder
{
    public static (TestDbContext db, TestCurrentActor actor) Build(string? actorId = null)
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        var actor = new TestCurrentActor(actorId);
        var db = new TestDbContext(options, actor, NullLogger.Instance);
        return (db, actor);
    }
}
