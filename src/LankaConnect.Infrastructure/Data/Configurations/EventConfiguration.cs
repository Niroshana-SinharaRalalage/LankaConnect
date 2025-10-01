using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using LankaConnect.Domain.Events;
using LankaConnect.Domain.Events.ValueObjects;
using LankaConnect.Domain.Events.Enums;

namespace LankaConnect.Infrastructure.Data.Configurations;

public class EventConfiguration : IEntityTypeConfiguration<Event>
{
    public void Configure(EntityTypeBuilder<Event> builder)
    {
        builder.HasKey(e => e.Id);
        
        builder.Property(e => e.Id)
            .ValueGeneratedNever();

        // Configure EventTitle value object
        builder.OwnsOne(e => e.Title, title =>
        {
            title.Property(t => t.Value)
                .HasColumnName("title")
                .HasMaxLength(200)
                .IsRequired();
        });

        // Configure EventDescription value object
        builder.OwnsOne(e => e.Description, description =>
        {
            description.Property(d => d.Value)
                .HasColumnName("description")
                .HasMaxLength(2000)
                .IsRequired();
        });

        // Configure basic properties
        builder.Property(e => e.StartDate)
            .IsRequired()
            .HasColumnType("timestamp with time zone");

        builder.Property(e => e.EndDate)
            .IsRequired()
            .HasColumnType("timestamp with time zone");

        builder.Property(e => e.OrganizerId)
            .IsRequired();

        builder.Property(e => e.Capacity)
            .IsRequired();

        // Configure enum
        builder.Property(e => e.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired()
            .HasDefaultValue(EventStatus.Draft);

        builder.Property(e => e.CancellationReason)
            .HasMaxLength(500);

        // Configure audit fields
        builder.Property(e => e.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("NOW()");

        builder.Property(e => e.UpdatedAt);

        // Configure relationships
        builder.HasMany(e => e.Registrations)
            .WithOne()
            .HasForeignKey("EventId")
            .OnDelete(DeleteBehavior.Cascade);

        // Configure indexes
        builder.HasIndex(e => e.StartDate)
            .HasDatabaseName("ix_events_start_date");

        builder.HasIndex(e => e.OrganizerId)
            .HasDatabaseName("ix_events_organizer_id");

        builder.HasIndex(e => e.Status)
            .HasDatabaseName("ix_events_status");

        builder.HasIndex(e => new { e.Status, e.StartDate })
            .HasDatabaseName("ix_events_status_start_date");
    }
}