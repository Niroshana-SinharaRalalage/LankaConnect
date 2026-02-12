using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using LankaConnect.Domain.Events.Entities;

namespace LankaConnect.Infrastructure.Data.Configurations;

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

        // SelectedOptionIds stored as JSONB List<Guid>
        // EF Core auto-discovers _selectedOptionIds backing field by convention
        builder.Property(a => a.SelectedOptionIds)
            .HasColumnName("selected_option_ids")
            .HasColumnType("jsonb");

        // SelectedOptionTextSnapshots stored as JSONB List<string>
        // EF Core auto-discovers _selectedOptionTextSnapshots backing field by convention
        builder.Property(a => a.SelectedOptionTextSnapshots)
            .HasColumnName("selected_option_text_snapshots")
            .HasColumnType("jsonb");

        builder.Property(a => a.BooleanValue)
            .HasColumnName("boolean_value");

        // Shadow properties for BaseEntity
        builder.Property<DateTime>("CreatedAt")
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property<DateTime?>("UpdatedAt")
            .HasColumnName("updated_at");

        // Indexes
        builder.HasIndex(a => a.FormResponseId)
            .HasDatabaseName("ix_form_answers_form_response_id");

        builder.HasIndex(a => a.FormQuestionId)
            .HasDatabaseName("ix_form_answers_form_question_id");
    }
}
