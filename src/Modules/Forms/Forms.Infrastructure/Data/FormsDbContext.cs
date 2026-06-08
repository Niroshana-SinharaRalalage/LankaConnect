using LankaConnect.BuildingBlocks.Infrastructure.Idempotency;
using LankaConnect.BuildingBlocks.Infrastructure.Outbox;
using LankaConnect.Modules.Forms.Domain;
using LankaConnect.Modules.Forms.Domain.Entities;
using LankaConnect.Modules.Forms.Infrastructure.Data.Configurations;
using Microsoft.EntityFrameworkCore;

namespace LankaConnect.Modules.Forms.Infrastructure.Data;

/// <summary>
/// Module-owned <see cref="DbContext"/> for the Forms bounded context. Maps
/// the <c>events.event_forms</c> + <c>events.form_questions</c> +
/// <c>events.form_responses</c> + <c>events.form_answers</c> tables (physically
/// owned by legacy AppDbContext migrations).
/// </summary>
/// <remarks>
/// W4.3 cross-schema pattern (architect ruling 2026-06-06): legacy aggregate
/// tables remain on the <c>events</c> physical schema via explicit
/// <c>ToTable(..., "events")</c> overrides; module-owned operational tables
/// (<c>forms.outbox</c>, <c>forms.outbox_dead_letter</c>, <c>forms.idempotency_keys</c>,
/// <c>forms.__EFMigrationsHistory</c>) live in the <c>forms</c> schema. Schema
/// realignment to <c>forms.event_forms</c> etc. deferred to Wave 4.9.
/// </remarks>
public sealed class FormsDbContext : DbContext
{
    /// <summary>Postgres schema for module-owned operational tables.</summary>
    public const string SchemaName = "forms";

    /// <summary>Physical schema for the legacy aggregate tables.</summary>
    public const string LegacyTableSchema = "events";

    public FormsDbContext(DbContextOptions<FormsDbContext> options)
        : base(options)
    {
    }

    public DbSet<EventForm> EventForms => Set<EventForm>();
    public DbSet<FormQuestion> FormQuestions => Set<FormQuestion>();
    public DbSet<FormResponse> FormResponses => Set<FormResponse>();
    public DbSet<FormAnswer> FormAnswers => Set<FormAnswer>();

    public DbSet<OutboxMessage> Outbox => Set<OutboxMessage>();
    public DbSet<DeadLetterMessage> OutboxDeadLetter => Set<DeadLetterMessage>();
    public DbSet<IdempotencyKey> IdempotencyKeys => Set<IdempotencyKey>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        modelBuilder.HasDefaultSchema(SchemaName);

        modelBuilder.ApplyConfiguration(new EventFormConfiguration());
        modelBuilder.ApplyConfiguration(new FormQuestionConfiguration());
        modelBuilder.ApplyConfiguration(new FormResponseConfiguration());
        modelBuilder.ApplyConfiguration(new FormAnswerConfiguration());

        modelBuilder.Entity<EventForm>().ToTable("event_forms", LegacyTableSchema);
        modelBuilder.Entity<FormQuestion>().ToTable("form_questions", LegacyTableSchema);
        modelBuilder.Entity<FormResponse>().ToTable("form_responses", LegacyTableSchema);
        modelBuilder.Entity<FormAnswer>().ToTable("form_answers", LegacyTableSchema);

        modelBuilder.ApplyConfiguration(new OutboxMessageConfiguration());
        modelBuilder.ApplyConfiguration(new DeadLetterMessageConfiguration());
        modelBuilder.ApplyConfiguration(new IdempotencyKeyConfiguration());

        // 2026-06-08 hotfix (matches AppDbContext.IgnoreAuditByActorPropertiesUntilPhase1):
        // Phase 1.10 of Wave 4.9 will add physical CreatedBy/UpdatedBy columns to the
        // event_forms / form_questions / form_responses / form_answers tables; until
        // then, ignore the auto-mapped properties.
        modelBuilder.Entity<EventForm>().Ignore("CreatedBy").Ignore("UpdatedBy");
        modelBuilder.Entity<FormQuestion>().Ignore("CreatedBy").Ignore("UpdatedBy");
        modelBuilder.Entity<FormResponse>().Ignore("CreatedBy").Ignore("UpdatedBy");
        modelBuilder.Entity<FormAnswer>().Ignore("CreatedBy").Ignore("UpdatedBy");

        base.OnModelCreating(modelBuilder);
    }
}
