using LankaConnect.BuildingBlocks.Infrastructure.Idempotency;
using LankaConnect.BuildingBlocks.Infrastructure.Outbox;
using LankaConnect.Modules.Notifications.Domain;
using LankaConnect.Modules.Notifications.Infrastructure.Data.Configurations;
using Microsoft.EntityFrameworkCore;

namespace LankaConnect.Modules.Notifications.Infrastructure.Data;

/// <summary>
/// Module-owned <see cref="DbContext"/> for the Notifications bounded context.
/// Maps the <c>notifications.notifications</c> table (existing physical table
/// originally created by <c>AppDbContext</c>'s <c>Phase 6A.6</c> migration on
/// 2025-11-11) and is the target of the W3.5 baseline migration.
/// </summary>
/// <remarks>
/// <para>
/// <b>Phase A status</b>: this context is REGISTERED in DI but dormant — the
/// active <see cref="LankaConnect.Modules.Notifications.Infrastructure.Repositories.NotificationRepository"/>
/// continues to inject <c>AppDbContext</c> through the legacy
/// <c>Repository&lt;T&gt;</c> base for backward compatibility with existing
/// handler chains. The W3.6 / W3.7 feature-flag pivot will route new dispatch
/// through this context as the modular path; the legacy path stays available
/// for rollback throughout the 7-day soak window.
/// </para>
/// <para>
/// <b>Default schema</b>: <c>notifications</c> (matches the existing table's
/// physical schema, set by migration <c>20251111172127_AddNotificationsTable</c>).
/// Setting it here ensures W3.5 <c>dotnet ef migrations add Baseline_Notifications</c>
/// generates DDL against the correct schema even though <c>Up()</c> is later
/// emptied (history-row-only baseline pattern).
/// </para>
/// <para>
/// <b>Inheritance</b>: derives directly from <c>DbContext</c> rather than
/// <c>BuildingBlocks.Infrastructure.Persistence.BaseDbContext</c> because the
/// Notification aggregate doesn't yet implement <see cref="BuildingBlocks.Domain.IAuditable"/>
/// / <see cref="BuildingBlocks.Domain.ISoftDeletable"/>. Refactoring the
/// aggregate to those abstractions is a follow-up alongside the BuildingBlocks
/// elevation pass for <c>BaseEntity</c> / <c>IRepository&lt;T&gt;</c>.
/// </para>
/// </remarks>
public sealed class NotificationsDbContext : DbContext
{
    /// <summary>Postgres schema all entities map into by default.</summary>
    public const string SchemaName = "notifications";

    public NotificationsDbContext(DbContextOptions<NotificationsDbContext> options)
        : base(options)
    {
    }

    public DbSet<Notification> Notifications => Set<Notification>();

    /// <summary>Per-module outbox table (<c>notifications.outbox</c>).</summary>
    public DbSet<OutboxMessage> Outbox => Set<OutboxMessage>();

    /// <summary>Per-module dead-letter table (<c>notifications.outbox_dead_letter</c>).</summary>
    public DbSet<DeadLetterMessage> OutboxDeadLetter => Set<DeadLetterMessage>();

    /// <summary>Per-module idempotency-keys table (<c>notifications.idempotency_keys</c>).</summary>
    public DbSet<IdempotencyKey> IdempotencyKeys => Set<IdempotencyKey>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        modelBuilder.HasDefaultSchema(SchemaName);
        modelBuilder.ApplyConfiguration(new NotificationConfiguration());

        // Match the physical table name created by the legacy AppDbContext
        // migration `20251111172127_AddNotificationsTable` (lowercase "notifications"
        // in schema "notifications"). Without this, EF would derive PascalCase
        // "Notifications" from the DbSet property name + drift from production.
        modelBuilder.Entity<Notification>().ToTable("notifications", SchemaName);

        // W3.5b operational tables — shared shape across all modules, per-schema isolation.
        modelBuilder.ApplyConfiguration(new OutboxMessageConfiguration());
        modelBuilder.ApplyConfiguration(new DeadLetterMessageConfiguration());
        modelBuilder.ApplyConfiguration(new IdempotencyKeyConfiguration());

        base.OnModelCreating(modelBuilder);
    }
}
