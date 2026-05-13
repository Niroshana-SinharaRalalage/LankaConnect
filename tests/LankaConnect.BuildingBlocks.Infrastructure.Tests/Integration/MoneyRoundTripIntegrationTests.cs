using LankaConnect.BuildingBlocks.Application.Abstractions;
using LankaConnect.BuildingBlocks.Domain;
using LankaConnect.BuildingBlocks.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace LankaConnect.BuildingBlocks.Infrastructure.Tests.Integration;

/// <summary>
/// Verifies that the W2.5a <see cref="MoneyConfigurationExtensions.ConfigureMoney"/>
/// helper round-trips Money through a real Postgres backend via the documented
/// two-column persistence (per ADR-005): <c>price_amount</c> (decimal) +
/// <c>price_currency</c> (varchar(3)) with <see cref="Currency.FromCode"/>
/// converter.
/// </summary>
[Trait("Category", "Integration")]
public sealed class MoneyRoundTripIntegrationTests : IClassFixture<PostgresFixture>
{
    private readonly PostgresFixture _postgres;

    public MoneyRoundTripIntegrationTests(PostgresFixture postgres)
    {
        _postgres = postgres;
    }

    private MoneyTestDbContext NewDb()
    {
        var options = new DbContextOptionsBuilder<MoneyTestDbContext>()
            .UseNpgsql(_postgres.ConnectionString)
            .Options;
        return new MoneyTestDbContext(options, new TestCurrentActor("test-actor"), NullLogger.Instance);
    }

    [Fact]
    public async Task Money_RoundTrip_AcrossSupportedCurrencies()
    {
        await using var dbCreate = NewDb();
        await dbCreate.Database.EnsureDeletedAsync();
        await dbCreate.Database.EnsureCreatedAsync();

        // Seed one ticket per supported currency
        var seeded = Currency.All.Select((c, idx) => new MoneyEntity
        {
            Id = Guid.NewGuid(),
            Name = $"ticket-{c.Code}",
            Price = new Money(100m + idx, c),
        }).ToList();
        dbCreate.MoneyEntities.AddRange(seeded);
        await dbCreate.SaveChangesAsync();

        // Re-fetch in a fresh context, verify the round-trip
        await using var dbReload = NewDb();
        var reloaded = await dbReload.MoneyEntities.AsNoTracking().ToListAsync();

        reloaded.Should().HaveCount(Currency.All.Count);

        // Look up by Id so ordering doesn't matter (seeded list isn't sorted
        // and Postgres doesn't guarantee insertion order on SELECT without ORDER BY).
        var reloadedById = reloaded.ToDictionary(e => e.Id);
        foreach (var original in seeded)
        {
            reloadedById.Should().ContainKey(original.Id);
            var loaded = reloadedById[original.Id];
            loaded.Price.Should().NotBeNull();
            loaded.Price!.Amount.Should().Be(original.Price!.Amount);
            loaded.Price.Currency.Code.Should().Be(original.Price.Currency.Code);
        }
    }

    [Fact]
    public async Task Money_NullPrice_PersistsAsNull()
    {
        await using var dbCreate = NewDb();
        await dbCreate.Database.EnsureDeletedAsync();
        await dbCreate.Database.EnsureCreatedAsync();

        dbCreate.MoneyEntities.Add(new MoneyEntity
        {
            Id = Guid.NewGuid(),
            Name = "free-item",
            Price = null,
        });
        await dbCreate.SaveChangesAsync();

        await using var dbReload = NewDb();
        var loaded = await dbReload.MoneyEntities.AsNoTracking().SingleAsync(e => e.Name == "free-item");
        loaded.Price.Should().BeNull();
    }

    [Fact]
    public async Task Money_UpdateChangesBothColumns()
    {
        // Verifies that changing Currency along with Amount updates BOTH the
        // _amount AND _currency columns — important because if EF only tracked
        // the amount column, currency changes would silently fail to persist.
        await using var dbCreate = NewDb();
        await dbCreate.Database.EnsureDeletedAsync();
        await dbCreate.Database.EnsureCreatedAsync();

        var entity = new MoneyEntity
        {
            Id = Guid.NewGuid(),
            Name = "convertible",
            Price = new Money(100m, Currency.USD),
        };
        dbCreate.MoneyEntities.Add(entity);
        await dbCreate.SaveChangesAsync();

        await using (var dbUpdate = NewDb())
        {
            var loaded = await dbUpdate.MoneyEntities.SingleAsync(e => e.Id == entity.Id);
            loaded.Price = new Money(35000m, Currency.LKR);
            await dbUpdate.SaveChangesAsync();
        }

        await using var dbReload = NewDb();
        var reloaded = await dbReload.MoneyEntities.AsNoTracking().SingleAsync(e => e.Id == entity.Id);
        reloaded.Price!.Amount.Should().Be(35000m);
        reloaded.Price.Currency.Should().Be(Currency.LKR);
    }
}

public sealed class MoneyTestDbContext : BaseDbContext
{
    public DbSet<MoneyEntity> MoneyEntities => Set<MoneyEntity>();

    public MoneyTestDbContext(DbContextOptions<MoneyTestDbContext> options, ICurrentActor currentActor, Microsoft.Extensions.Logging.ILogger logger)
        : base(options, currentActor, logger) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<MoneyEntity>(b =>
        {
            b.HasKey(e => e.Id);
            b.Property(e => e.Name).HasMaxLength(100).IsRequired();
            b.ConfigureMoney(e => e.Price, columnPrefix: "price");
        });
    }
}

public sealed class MoneyEntity
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public Money? Price { get; set; }
}
