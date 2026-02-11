using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using LankaConnect.Domain.Events.Entities;
using LankaConnect.Domain.Events.Enums;

namespace LankaConnect.Infrastructure.Data.Configurations;

public class EventFormConfiguration : IEntityTypeConfiguration<EventForm>
{
    public void Configure(EntityTypeBuilder<EventForm> builder)
    {
        builder.HasKey(f => f.Id);

        builder.Property(f => f.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(f => f.EventId)
            .HasColumnName("event_id")
            .IsRequired();

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

        // Shadow properties for BaseEntity
        builder.Property<DateTime>("CreatedAt")
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property<DateTime?>("UpdatedAt")
            .HasColumnName("updated_at");

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
