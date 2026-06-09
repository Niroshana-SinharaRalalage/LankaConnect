using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using LankaConnect.Modules.Forms.Domain.Entities;

namespace LankaConnect.Modules.Forms.Infrastructure.Data.Configurations;

public class FormAnswerConfiguration : IEntityTypeConfiguration<FormAnswer>
{
    public void Configure(EntityTypeBuilder<FormAnswer> builder)
    {
        builder.HasKey(a => a.Id);

        builder.Property(a => a.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(a => a.FormResponseId)
            .HasColumnName("form_response_id")
            .IsRequired();

        builder.Property(a => a.FormQuestionId)
            .HasColumnName("form_question_id")
            .IsRequired();

        builder.Property(a => a.QuestionTextSnapshot)
            .HasColumnName("question_text_snapshot")
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(a => a.TextValue)
            .HasColumnName("text_value")
            .HasColumnType("text");

        // Phase 6A.129: SelectedOptionIds stored as JSONB List<Guid>
        // CRITICAL: ValueComparer required for mutable in-place collection updates.
        // Without it, EF Core's change tracker uses reference equality on the snapshot,
        // which fails to detect Clear() + AddRange() mutations on the same List instance.
        // The domain's FormAnswer.Update() mutates _selectedOptionIds in-place (readonly field).
        builder.Property(a => a.SelectedOptionIds)
            .HasColumnName("selected_option_ids")
            .HasColumnType("jsonb")
            .Metadata.SetValueComparer(new ValueComparer<IReadOnlyList<Guid>>(
                (c1, c2) => c1 != null && c2 != null
                    ? c1.SequenceEqual(c2)
                    : ReferenceEquals(c1, c2),
                c => c != null
                    ? c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode()))
                    : 0,
                c => c != null
                    ? c.ToList().AsReadOnly()
                    : (IReadOnlyList<Guid>)new List<Guid>().AsReadOnly()));

        // Phase 6A.129: SelectedOptionTextSnapshots stored as JSONB List<string>
        // Same ValueComparer pattern to detect in-place mutations.
        builder.Property(a => a.SelectedOptionTextSnapshots)
            .HasColumnName("selected_option_text_snapshots")
            .HasColumnType("jsonb")
            .Metadata.SetValueComparer(new ValueComparer<IReadOnlyList<string>>(
                (c1, c2) => c1 != null && c2 != null
                    ? c1.SequenceEqual(c2)
                    : ReferenceEquals(c1, c2),
                c => c != null
                    ? c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode()))
                    : 0,
                c => c != null
                    ? c.ToList().AsReadOnly()
                    : (IReadOnlyList<string>)new List<string>().AsReadOnly()));

        builder.Property(a => a.BooleanValue)
            .HasColumnName("boolean_value");

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
        builder.HasIndex(a => a.FormResponseId)
            .HasDatabaseName("ix_form_answers_form_response_id");

        builder.HasIndex(a => a.FormQuestionId)
            .HasDatabaseName("ix_form_answers_form_question_id");
    }
}
