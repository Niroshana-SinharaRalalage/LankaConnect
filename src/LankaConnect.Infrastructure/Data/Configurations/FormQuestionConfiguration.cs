using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using LankaConnect.Domain.Events.Entities;
using LankaConnect.Domain.Events.Enums;
using LankaConnect.Domain.Events.ValueObjects;

namespace LankaConnect.Infrastructure.Data.Configurations;

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

        // Options stored as JSONB - structured objects with Guid Id + Text + SortOrder
        // EF Core auto-discovers _options backing field by convention
        builder.Property(q => q.Options)
            .HasColumnName("options")
            .HasColumnType("jsonb");

        // Shadow properties for BaseEntity
        builder.Property<DateTime>("CreatedAt")
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property<DateTime?>("UpdatedAt")
            .HasColumnName("updated_at");

        // Indexes
        builder.HasIndex(q => q.EventFormId)
            .HasDatabaseName("ix_form_questions_event_form_id");

        builder.HasIndex(q => q.SortOrder)
            .HasDatabaseName("ix_form_questions_sort_order");
    }
}
