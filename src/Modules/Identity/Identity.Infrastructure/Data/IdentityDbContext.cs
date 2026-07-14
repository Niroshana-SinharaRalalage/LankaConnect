using LankaConnect.BuildingBlocks.Infrastructure.Idempotency;
using LankaConnect.BuildingBlocks.Infrastructure.Outbox;
using LankaConnect.Modules.Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;
namespace LankaConnect.Modules.Identity.Infrastructure.Data;

/// <summary>
/// Module-owned <see cref="DbContext"/> for the Identity bounded context.
/// Maps the <see cref="User"/> aggregate + Identity operational tables in the
/// <c>identity</c> schema. Sub-slice 4C.e (2026-07-08) per Consult #14 PASS B.
/// </summary>
/// <remarks>
/// <para>
/// <b>Dual mapping (intentional and temporary)</b>: <see cref="User"/> is ALSO
/// mapped by <see cref="LankaConnect.Infrastructure.Data.AppDbContext"/> via the
/// <c>UserConfiguration</c> physically relocated to
/// <c>LankaConnect.Modules.Identity.Infrastructure.Data.Configurations</c>. Both
/// DbContexts thus produce the same model shape while ~18 caller sites cut over
/// site-by-site in sub-slice 4C.e.2. Mirrors the Wave 6.5.e LankaEventsDbContext
/// pattern that landed dormant then completed cutover in Wave 6.5.f.
/// </para>
/// <para>
/// <b>Default schema</b>: <c>identity</c>. Every Identity-owned business table
/// (<c>users</c>, <c>user_refresh_tokens</c>, <c>external_logins</c>,
/// <c>user_preferred_metro_areas</c>) already physically lives in the
/// <c>identity</c> schema (established by earlier Phase 6A migrations). The
/// OwnsMany <c>UserCulturalInterest</c> / <c>UserLanguage</c> child tables land
/// in a legacy <c>users</c> schema, kept via per-config <c>ToTable</c> overrides
/// until a Phase B realignment migration collapses them into <c>identity</c>.
/// </para>
/// <para>
/// <b>Cross-module principal — MetroArea</b>: <see cref="User"/> has a many-to-many
/// junction to <see cref="LankaConnect.Products.LankaEvents.Domain.MetroArea"/>
/// (<c>user_preferred_metro_areas</c>). MetroArea is owned by the LankaEvents
/// product and NOT re-mapped here. Following the LankaEventsDbContext pattern
/// (which <c>Ignore</c>s User), we <c>Ignore&lt;MetroArea&gt;()</c> so the
/// junction FK columns remain scalar. Cross-module hydration happens at the
/// application layer via <c>IMetroAreaRepository</c> per Blueprint §7.8.
/// </para>
/// <para>
/// <b>Operational tables</b>: <c>identity.outbox</c>, <c>identity.outbox_dead_letter</c>,
/// <c>identity.idempotency_keys</c>. Materialised via the shared BuildingBlocks
/// configs; the physical table + <c>__EFMigrationsHistory</c> creation happens
/// in the empty-Up() migration that follows this commit (sub-slice 4C.e.3).
/// </para>
/// </remarks>
public sealed class IdentityDbContext : DbContext
{
    /// <summary>Postgres schema all Identity-owned tables map into by default.</summary>
    public const string SchemaName = "identity";

    public IdentityDbContext(DbContextOptions<IdentityDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();

    /// <summary>Per-module outbox table (<c>identity.outbox</c>).</summary>
    public DbSet<OutboxMessage> Outbox => Set<OutboxMessage>();

    /// <summary>Per-module dead-letter table (<c>identity.outbox_dead_letter</c>).</summary>
    public DbSet<DeadLetterMessage> OutboxDeadLetter => Set<DeadLetterMessage>();

    /// <summary>Per-module idempotency-keys table (<c>identity.idempotency_keys</c>).</summary>
    public DbSet<IdempotencyKey> IdempotencyKeys => Set<IdempotencyKey>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        modelBuilder.HasDefaultSchema(SchemaName);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(IdentityDbContext).Assembly);

        // Sprint-Day 7 (2026-07-14) hotfix: UserConfiguration declares HasMany<MetroArea>("_preferredMetroAreaEntities")
        // to create the junction navigation. Under IdentityDbContext (which Ignore<MetroArea>()s below),
        // the shadow-nav is still registered but its target is gone → `_dbSet.AddAsync(user)` on a NEW user
        // throws "Cannot create a DbSet for 'MetroArea' because this type is not included in the model".
        // Blocked POST /api/Auth/register + downstream 7 AdminUsers throwaway-user creation.
        // Fix: explicitly Ignore the shadow nav here. Persistence of user_preferred_metro_areas rows
        // for register moves to a raw-SQL insert in RegisterUserHandler (same pattern as
        // UpdateUserPreferredMetroAreasCommandHandler landed today).
        modelBuilder.Entity<LankaConnect.Modules.Identity.Domain.Entities.User>()
            .Ignore("_preferredMetroAreaEntities");

        // Module-owned operational tables. Rule 5i.2: shared BuildingBlocks configs
        // (OutboxMessageConfiguration, DeadLetterMessageConfiguration,
        // IdempotencyKeyConfiguration) use single-arg .ToTable(name); owning
        // per-module DbContext pins the schema after ApplyConfiguration.
        modelBuilder.ApplyConfiguration(new OutboxMessageConfiguration());
        modelBuilder.ApplyConfiguration(new DeadLetterMessageConfiguration());
        modelBuilder.ApplyConfiguration(new IdempotencyKeyConfiguration());
        modelBuilder.Entity<OutboxMessage>().ToTable("outbox", SchemaName);
        modelBuilder.Entity<DeadLetterMessage>().ToTable("outbox_dead_letter", SchemaName);
        modelBuilder.Entity<IdempotencyKey>().ToTable("idempotency_keys", SchemaName);

        // Cross-module principal cleanup. UserConfiguration has a many-to-many
        // junction (user_preferred_metro_areas) to MetroArea, which is owned by
        // the LankaEvents product. When run under IdentityDbContext, EF Core 8
        // would pull MetroArea + its owned graph in without its own config.
        // Ignore keeps the junction FK columns scalar; cross-module hydration
        // routes through IMetroAreaRepository per Blueprint §7.8. Mirrors
        // LankaEventsDbContext.Ignore<User>() pattern from Wave 6.5.e.
        modelBuilder.Ignore<LankaConnect.Products.LankaEvents.Domain.MetroArea>();

        base.OnModelCreating(modelBuilder);
    }
}
