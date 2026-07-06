using LankaConnect.BuildingBlocks.Infrastructure.Idempotency;
using LankaConnect.BuildingBlocks.Infrastructure.Outbox;
using LankaConnect.Modules.Communications.Domain.Entities;
using LankaConnect.Modules.Communications.Infrastructure.Configurations;
using Microsoft.EntityFrameworkCore;

namespace LankaConnect.Modules.Communications.Infrastructure.Data;

/// <summary>
/// Module-owned <see cref="DbContext"/> for the Communications bounded context.
/// Owns the physical <c>communications.*</c> schema (email_messages,
/// email_templates, user_email_preferences).
/// </summary>
/// <remarks>
/// <para>
/// <b>Day 4 slot C sub-slice 4C.c</b> (Consult #14 PASS B, 2026-07-06). Created
/// to unblock IApplicationDbContext teardown — the 3 email DbSets that lived
/// on that god-context move here. Physical tables unchanged; ownership seam
/// moves from AppDbContext to this per-module context. Callers rewrite in
/// sub-slice 4C.f.
/// </para>
/// <para>
/// <b>Default schema</b>: <c>communications</c>. Matches the existing physical
/// schema set by AppDbContext migrations (<see cref="AppDbContext"/> line 475
/// pins <c>ToTable("email_messages", "communications")</c>).
/// </para>
/// <para>
/// <b>Inheritance</b>: derives directly from <c>DbContext</c> (not
/// <c>BaseDbContext</c>) mirroring NotificationsDbContext — Communications
/// entities pending audit-interceptor adoption.
/// </para>
/// </remarks>
public sealed class CommunicationsDbContext : DbContext
{
    /// <summary>Postgres schema all entities map into by default.</summary>
    public const string SchemaName = "communications";

    public CommunicationsDbContext(DbContextOptions<CommunicationsDbContext> options)
        : base(options)
    {
    }

    public DbSet<EmailMessage> EmailMessages => Set<EmailMessage>();
    public DbSet<EmailTemplate> EmailTemplates => Set<EmailTemplate>();
    public DbSet<UserEmailPreferences> UserEmailPreferences => Set<UserEmailPreferences>();

    /// <summary>Per-module outbox table (<c>communications.outbox</c>).</summary>
    public DbSet<OutboxMessage> Outbox => Set<OutboxMessage>();

    /// <summary>Per-module dead-letter table (<c>communications.outbox_dead_letter</c>).</summary>
    public DbSet<DeadLetterMessage> OutboxDeadLetter => Set<DeadLetterMessage>();

    /// <summary>Per-module idempotency-keys table (<c>communications.idempotency_keys</c>).</summary>
    public DbSet<IdempotencyKey> IdempotencyKeys => Set<IdempotencyKey>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        modelBuilder.HasDefaultSchema(SchemaName);

        // 3 Email configs relocated from src/LankaConnect.Infrastructure/Data/
        // Configurations/ (Rule 5j config-relocation audit in commit body).
        modelBuilder.ApplyConfiguration(new EmailMessageConfiguration());
        modelBuilder.ApplyConfiguration(new EmailTemplateConfiguration());
        modelBuilder.ApplyConfiguration(new UserEmailPreferencesConfiguration());

        // Per-module operational tables.
        modelBuilder.ApplyConfiguration(new OutboxMessageConfiguration());
        modelBuilder.ApplyConfiguration(new DeadLetterMessageConfiguration());
        modelBuilder.ApplyConfiguration(new IdempotencyKeyConfiguration());

        base.OnModelCreating(modelBuilder);
    }
}
