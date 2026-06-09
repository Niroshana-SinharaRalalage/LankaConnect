using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using LankaConnect.Modules.Forms.Domain.Entities;
using LankaConnect.Modules.Forms.Domain.Enums;
using LankaConnect.Domain.Events.ValueObjects;

namespace LankaConnect.Modules.Forms.Infrastructure.Data.Configurations;

public class FormQuestionConfiguration : IEntityTypeConfiguration<FormQuestion>
{
    public void Configure(EntityTypeBuilder<FormQuestion> builder)
    {
        builder.HasKey(q => q.Id);

        builder.Property(q => q.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(q => q.EventFormId)
            .HasColumnName("event_form_id")
            .IsRequired();

        builder.Property(q => q.QuestionText)
            .HasColumnName("question_text")
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(q => q.QuestionType)
            .HasColumnName("question_type")
            .HasConversion<int>()
            .IsRequired();

        builder.Property(q => q.IsRequired)
            .HasColumnName("is_required")
            .HasDefaultValue(false);

        builder.Property(q => q.SortOrder)
            .HasColumnName("sort_order")
            .IsRequired();

        builder.Property(q => q.HelpText)
            .HasColumnName("help_text")
            .HasMaxLength(300);

        // Phase 6A.129: Options stored as JSONB - structured objects with Guid Id + Text + SortOrder
        // ValueComparer required to detect in-place mutations (Clear + AddRange) on the backing field.
        // QuestionOption extends ValueObject with GetEqualityComponents() for proper equality.
        builder.Property(q => q.Options)
            .HasColumnName("options")
            .HasColumnType("jsonb")
            .Metadata.SetValueComparer(new ValueComparer<IReadOnlyList<QuestionOption>>(
                (c1, c2) => c1 != null && c2 != null
                    ? c1.SequenceEqual(c2)
                    : ReferenceEquals(c1, c2),
                c => c != null
                    ? c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode()))
                    : 0,
                c => c != null
                    ? c.ToList().AsReadOnly()
                    : (IReadOnlyList<QuestionOption>)new List<QuestionOption>().AsReadOnly()));

        // Shadow properties for BaseEntity
        builder.Property<DateTime>("CreatedAt")
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property<DateTime?>("UpdatedAt")
            .HasColumnName("updated_at");

        // Wave4.9.2.10c.a Phase 1.10c.a (2026-06-09): physical CreatedBy/UpdatedBy.
        builder.Property<string>("CreatedBy").HasColumnName("created_by").HasColumnType("text");
        builder.Property<string>("UpdatedBy").HasColumnName("updated_by").HasColumnType("text");

        // Indexes
        builder.HasIndex(q => q.EventFormId)
            .HasDatabaseName("ix_form_questions_event_form_id");

        builder.HasIndex(q => q.SortOrder)
            .HasDatabaseName("ix_form_questions_sort_order");
    }
}
