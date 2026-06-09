using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using LankaConnect.Modules.Forms.Domain.Entities;

namespace LankaConnect.Modules.Forms.Infrastructure.Data.Configurations;

public class FormResponseConfiguration : IEntityTypeConfiguration<FormResponse>
{
    public void Configure(EntityTypeBuilder<FormResponse> builder)
    {
        builder.HasKey(r => r.Id);

        builder.Property(r => r.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(r => r.EventFormId)
            .HasColumnName("event_form_id")
            .IsRequired();

        builder.Property(r => r.EventId)
            .HasColumnName("event_id")
            .IsRequired();

        builder.Property(r => r.AccessTokenHash)
            .HasColumnName("access_token_hash")
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(r => r.RespondentEmail)
            .HasColumnName("respondent_email")
            .HasMaxLength(255);

        builder.Property(r => r.RespondentName)
            .HasColumnName("respondent_name")
            .HasMaxLength(200);

        builder.Property(r => r.RespondentUserId)
            .HasColumnName("respondent_user_id");

        builder.Property(r => r.SubmittedAt)
            .HasColumnName("submitted_at")
            .IsRequired();

        // Shadow properties for BaseEntity
        builder.Property<DateTime>("CreatedAt")
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property<DateTime?>("UpdatedAt")
            .HasColumnName("updated_at");

        // Wave4.9.2.10c.a Phase 1.10c.a (2026-06-09): physical CreatedBy/UpdatedBy.
        builder.Property<string>("CreatedBy").HasColumnName("created_by").HasColumnType("text");
        builder.Property<string>("UpdatedBy").HasColumnName("updated_by").HasColumnType("text");

        // Configure relationship to FormAnswers
        builder.HasMany(r => r.Answers)
            .WithOne()
            .HasForeignKey(a => a.FormResponseId)
            .OnDelete(DeleteBehavior.Cascade);

        // CRITICAL: Use backing field "_answers" for EF Core change tracking
        builder.Navigation(r => r.Answers)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        // Optimistic concurrency via PostgreSQL xmin
#pragma warning disable CS0618 // UseXminAsConcurrencyToken is the correct PostgreSQL approach
        builder.UseXminAsConcurrencyToken();
#pragma warning restore CS0618

        // Indexes
        builder.HasIndex(r => r.EventFormId)
            .HasDatabaseName("ix_form_responses_event_form_id");

        builder.HasIndex(r => r.EventId)
            .HasDatabaseName("ix_form_responses_event_id");

        builder.HasIndex(r => r.AccessTokenHash)
            .IsUnique()
            .HasDatabaseName("ix_form_responses_access_token_hash");

        builder.HasIndex(r => r.RespondentEmail)
            .HasDatabaseName("ix_form_responses_respondent_email");

        builder.HasIndex(r => r.RespondentUserId)
            .HasDatabaseName("ix_form_responses_respondent_user_id");

        builder.HasIndex(r => r.SubmittedAt)
            .HasDatabaseName("ix_form_responses_submitted_at");

        // Phase 6A.111: Composite index for faster logged-in user response lookups
        // Used by GetByFormAndUserAsync query (frequent operation during edit/update)
        builder.HasIndex(r => new { r.EventFormId, r.RespondentUserId })
            .HasDatabaseName("ix_form_responses_event_form_id_respondent_user_id");
    }
}
