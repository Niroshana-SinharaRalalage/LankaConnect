using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using LankaConnect.Modules.Forms.Domain.Entities;
using LankaConnect.Modules.Forms.Domain.Enums;
using LankaConnect.Products.LankaEvents.Domain.ValueObjects;

namespace LankaConnect.Modules.Forms.Infrastructure.Data.Configurations;

public class FormQuestionConfiguration : IEntityTypeConfiguration<FormQuestion>
{
    // 2026-06-12 BUG-FIX: missing converter caused
    // "Reading as 'IReadOnlyList<QuestionOption>' is not supported for fields
    // having DataTypeName 'jsonb'" 500s on any event whose FormQuestion had
    // Options data. Mirrors the W7E HeadCountConverter pattern in
    // RegistrationConfiguration -- a NOT-OwnsOne JSON serializer + System.Text.Json,
    // sidestepping the Phase 6A.130 IReadOnlyList rehydration trap. The
    // existing deep-copy ValueComparer (lines below) already guards against
    // the Phase 6A.129 mutate-in-place snapshot trap.
    private static readonly JsonSerializerOptions OptionsJsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    // CRITICAL: do NOT call .AsReadOnly() in the materializer -- the entity's
    // backing field is `List<QuestionOption> _options`, and EF Core needs to
    // write the deserialized collection into that mutable field. Returning a
    // ReadOnlyCollection<T> trips an InvalidCastException at hydration time.
    private static readonly ValueConverter<IReadOnlyList<QuestionOption>, string> OptionsConverter = new(
        v => JsonSerializer.Serialize(v, OptionsJsonOptions),
        v => string.IsNullOrEmpty(v)
            ? new List<QuestionOption>()
            : JsonSerializer.Deserialize<List<QuestionOption>>(v, OptionsJsonOptions) ?? new List<QuestionOption>());

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
            .HasConversion(OptionsConverter)
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
