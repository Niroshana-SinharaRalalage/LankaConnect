using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using LankaConnect.Modules.Forms.Domain;
using LankaConnect.Modules.Forms.Domain.Entities;
using LankaConnect.Modules.Forms.Domain.Enums;

namespace LankaConnect.Modules.Forms.Infrastructure.Data.Configurations;

public class FormConfiguration : IEntityTypeConfiguration<Form>
{
    public void Configure(EntityTypeBuilder<Form> builder)
    {
        builder.HasKey(f => f.Id);

        builder.Property(f => f.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(f => f.EventId)
            .HasColumnName("event_id")
            .IsRequired();

        // Wave 5.2b (2026-06-11): OwnerEntityId + OwnerEntityType are CLR-only
        // transitional shims. Ignored by EF for this wave so the snapshot stays
        // clean and Gate G1 (empty-migration probe after the rename) passes.
        // Wave 5.2c removes these Ignore() calls, adds owner_entity_id +
        // owner_entity_type columns, and backfills from event_id. Wave 5.2d
        // drops event_id after staging soak.
        builder.Ignore(f => f.OwnerEntityId);
        builder.Ignore(f => f.OwnerEntityType);

        builder.Property(f => f.Title)
            .HasColumnName("title")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(f => f.Description)
            .HasColumnName("description")
            .HasMaxLength(2000);

        builder.Property(f => f.Status)
            .HasColumnName("status")
            .HasConversion<int>()
            .IsRequired();

        builder.Property(f => f.AllowMultipleResponses)
            .HasColumnName("allow_multiple_responses")
            .HasDefaultValue(false);

        builder.Property(f => f.ResponseDeadline)
            .HasColumnName("response_deadline");

        builder.Property(f => f.MaxResponses)
            .HasColumnName("max_responses");

        builder.Property(f => f.HasResponses)
            .HasColumnName("has_responses")
            .HasDefaultValue(false);

        // Phase 6A.146: organizer-controlled toggle that allows event visitors to
        // view all submitted responses (with respondent PII stripped at the
        // projection layer). Default false preserves status-quo privacy for all
        // existing forms.
        builder.Property(f => f.AllowAttendeesToViewResponses)
            .HasColumnName("allow_attendees_to_view_responses")
            .IsRequired()
            .HasDefaultValue(false);

        // Shadow properties for BaseEntity
        builder.Property<DateTime>("CreatedAt")
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property<DateTime?>("UpdatedAt")
            .HasColumnName("updated_at");

        // Wave4.9.2.10c.a Phase 1.10c.a (2026-06-09): physical CreatedBy/UpdatedBy.
        builder.Property<string>("CreatedBy").HasColumnName("created_by").HasColumnType("text");
        builder.Property<string>("UpdatedBy").HasColumnName("updated_by").HasColumnType("text");

        // Configure relationship to FormQuestions
        builder.HasMany(f => f.Questions)
            .WithOne()
            .HasForeignKey(q => q.EventFormId)
            .OnDelete(DeleteBehavior.Cascade);

        // CRITICAL: Use backing field "_questions" for EF Core change tracking
        builder.Navigation(f => f.Questions)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        // Optimistic concurrency via PostgreSQL xmin
#pragma warning disable CS0618 // UseXminAsConcurrencyToken is the correct PostgreSQL approach
        builder.UseXminAsConcurrencyToken();
#pragma warning restore CS0618

        // Indexes
        builder.HasIndex(f => f.EventId)
            .HasDatabaseName("ix_event_forms_event_id");

        builder.HasIndex(f => f.Status)
            .HasDatabaseName("ix_event_forms_status");
    }
}
