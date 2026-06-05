using LankaConnect.BuildingBlocks.Domain;
using LankaConnect.BuildingBlocks.Infrastructure.Persistence;
using LankaConnect.SharedKernel.Money;
using Microsoft.EntityFrameworkCore;

namespace LankaConnect.BuildingBlocks.Infrastructure.Tests;

public sealed class MoneyConfigurationTests
{
    [Fact]
    public async Task Money_RoundTrip_PreservesAmountAndCurrency()
    {
        var (db, _) = TestDbContextBuilder.Build(actorId: null);
        var entity = new MoneyOwnerEntity
        {
            Name = "ticket",
            Price = new Money(123.45m, Currency.LKR),
        };

        db.MoneyOwners.Add(entity);
        await db.SaveChangesAsync();

        // Detach to force a real reload from the InMemory store
        db.Entry(entity).State = EntityState.Detached;
        var reloaded = await db.MoneyOwners.SingleAsync(e => e.Id == entity.Id);

        reloaded.Price.Should().NotBeNull();
        reloaded.Price!.Amount.Should().Be(123.45m);
        reloaded.Price.Currency.Should().Be(Currency.LKR);
    }

    [Fact]
    public async Task Money_DifferentCurrencies_RoundTripCorrectly()
    {
        var (db, _) = TestDbContextBuilder.Build(actorId: null);
        var ticketUsd = new MoneyOwnerEntity { Name = "usd-ticket", Price = new Money(10m, Currency.USD) };
        var ticketLkr = new MoneyOwnerEntity { Name = "lkr-ticket", Price = new Money(3500m, Currency.LKR) };
        var ticketInr = new MoneyOwnerEntity { Name = "inr-ticket", Price = new Money(800m, Currency.INR) };
        db.MoneyOwners.AddRange(ticketUsd, ticketLkr, ticketInr);
        await db.SaveChangesAsync();

        db.ChangeTracker.Clear();
        var loaded = await db.MoneyOwners.ToListAsync();

        loaded.Should().HaveCount(3);
        loaded.Single(e => e.Name == "usd-ticket").Price!.Currency.Should().Be(Currency.USD);
        loaded.Single(e => e.Name == "lkr-ticket").Price!.Currency.Should().Be(Currency.LKR);
        loaded.Single(e => e.Name == "inr-ticket").Price!.Currency.Should().Be(Currency.INR);
    }

    [Fact]
    public async Task Money_NullPrice_PersistsAsNull()
    {
        var (db, _) = TestDbContextBuilder.Build(actorId: null);
        var entity = new MoneyOwnerEntity { Name = "free-item", Price = null };

        db.MoneyOwners.Add(entity);
        await db.SaveChangesAsync();

        db.ChangeTracker.Clear();
        var reloaded = await db.MoneyOwners.SingleAsync(e => e.Id == entity.Id);
        reloaded.Price.Should().BeNull();
    }

    [Fact]
    public void ConfigureMoney_EmptyPrefix_Throws()
    {
        // Direct test of the helper's guard: empty prefix must fail fast at
        // model-build time (during OnModelCreating), not silently produce
        // mis-named columns.
        var options = new DbContextOptionsBuilder<BadPrefixContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        Action act = () =>
        {
            using var db = new BadPrefixContext(options);
            _ = db.Model; // trigger OnModelCreating
        };

        act.Should().Throw<ArgumentException>().WithMessage("*Column prefix must be non-empty*");
    }

    /// <summary>Context that intentionally misconfigures Money with an empty prefix to test the guard.</summary>
    private sealed class BadPrefixContext : DbContext
    {
        public BadPrefixContext(DbContextOptions<BadPrefixContext> options) : base(options) { }
        public DbSet<MoneyOwnerEntity> Owners => Set<MoneyOwnerEntity>();
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<MoneyOwnerEntity>(b =>
            {
                b.HasKey(e => e.Id);
                b.ConfigureMoney(e => e.Price, columnPrefix: ""); // <-- bad
            });
        }
    }
}
