using LankaConnect.BuildingBlocks.Infrastructure.Idempotency;
using LankaConnect.BuildingBlocks.Infrastructure.Outbox;
using LankaConnect.Modules.Forms.Domain;
using LankaConnect.Modules.Forms.Domain.Entities;
using LankaConnect.Modules.Forms.Infrastructure.Data.Configurations;
using Microsoft.EntityFrameworkCore;

namespace LankaConnect.Modules.Forms.Infrastructure.Data;

/// <summary>
/// Module-owned <see cref="DbContext"/> for the Forms bounded context. Maps
/// <c>forms.event_forms</c> + <c>forms.form_questions</c> + <c>forms.form_responses</c>
/// + <c>forms.form_answers</c> (renamed from the legacy <c>events.*</c> schema
/// by Wave 4.9.4 architect ruling 2026-06-10 via <c>ALTER TABLE ... SET SCHEMA</c>).
/// </summary>
/// <remarks>
/// Wave 4.9.4 (2026-06-10) closed the W4.3 cross-schema deferral: all four
/// legacy aggregate tables physically moved from <c>events</c> to <c>forms</c>.
/// The previous <c>LegacyTableSchema</c> constant + four
/// <c>ToTable(..., LegacyTableSchema)</c> overrides have been removed; the
/// four entities now live under the default schema (<c>forms</c>, per
/// <see cref="HasDefaultSchema"/>).
/// </remarks>
// Wave 6.5.d (2026-07-03): unsealed so FormCommandsTests can subclass the DbContext
// to override SaveChangesAsync (see FormCommandsTestDouble in the test project).
// The InMemory EF provider cannot validate the FormsDbContext model (FormResponse
// uses jsonb-backed IReadOnlyList<Guid> — not a primitive collection), so a
// Moq-mockable path was preferred. FormsDbContext was previously sealed as an
// idiomatic safeguard; no production consumer subclasses it.
public class FormsDbContext : DbContext
{
    /// <summary>Postgres schema for all Forms-owned tables (post-Wave 4.9.4).</summary>
    public const string SchemaName = "forms";

    public FormsDbContext(DbContextOptions<FormsDbContext> options)
        : base(options)
    {
    }

    public DbSet<Form> EventForms => Set<Form>();
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

        modelBuilder.ApplyConfiguration(new FormConfiguration());
        modelBuilder.ApplyConfiguration(new FormQuestionConfiguration());
        modelBuilder.ApplyConfiguration(new FormResponseConfiguration());
        modelBuilder.ApplyConfiguration(new FormAnswerConfiguration());

        // Wave 4.9.4 (2026-06-10): legacy events.event_forms / form_questions /
        // form_responses / form_answers physically moved to the forms schema.
        // Schema arg dropped — entities now use the default schema (`forms`)
        // via HasDefaultSchema above. Table names retained explicitly because
        // EF's PascalCase pluralization would otherwise produce "EventForms",
        // "FormQuestions", etc.
        modelBuilder.Entity<Form>().ToTable("event_forms");
        modelBuilder.Entity<FormQuestion>().ToTable("form_questions");
        modelBuilder.Entity<FormResponse>().ToTable("form_responses");
        modelBuilder.Entity<FormAnswer>().ToTable("form_answers");

        modelBuilder.ApplyConfiguration(new OutboxMessageConfiguration());
        modelBuilder.ApplyConfiguration(new DeadLetterMessageConfiguration());
        modelBuilder.ApplyConfiguration(new IdempotencyKeyConfiguration());

        // Wave4.9.2.10c.a Phase 1.10c.a (2026-06-09): physical CreatedBy/UpdatedBy
        // columns landed on events.event_forms / form_questions / form_responses /
        // form_answers via Phase1_10c_a_AddCreatedByUpdatedByToFormsTables migration
        // (FormsDbContext-owned). Per-entity configs now map both to snake_case
        // columns; the prior Ignore() hotfix has been removed.

        base.OnModelCreating(modelBuilder);
    }
}
