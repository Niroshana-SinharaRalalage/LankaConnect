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

        // Configure EventLocation value object (Epic 2 Phase 1)
        builder.OwnsOne(e => e.Location, location =>
        {
            // Required to prevent EF Core optional dependent error
            location.Property<bool>("_hasValue")
                .HasColumnName("has_location")
                .HasDefaultValue(true)
                .IsRequired();

            // Configure Address as nested owned entity
            location.OwnsOne(l => l.Address, address =>
            {
                address.Property(a => a.Street)
                    .HasColumnName("address_street")
                    .HasMaxLength(200)
                    .IsRequired();

                address.Property(a => a.City)
                    .HasColumnName("address_city")
                    .HasMaxLength(100)
                    .IsRequired();

                address.Property(a => a.State)
                    .HasColumnName("address_state")
                    .HasMaxLength(100)
                    .IsRequired();

                address.Property(a => a.ZipCode)
                    .HasColumnName("address_zip_code")
                    .HasMaxLength(20)
                    .IsRequired();

                address.Property(a => a.Country)
                    .HasColumnName("address_country")
                    .HasMaxLength(100)
                    .IsRequired();
            });

            // Configure GeoCoordinate as nested owned entity (nullable)
            location.OwnsOne(l => l.Coordinates, coordinates =>
            {
                coordinates.Property(c => c.Latitude)
                    .HasColumnName("coordinates_latitude")
                    .HasPrecision(10, 7); // Precision for GPS coordinates

                coordinates.Property(c => c.Longitude)
                    .HasColumnName("coordinates_longitude")
                    .HasPrecision(10, 7); // Precision for GPS coordinates
            });
        });

        // Indexes for location-based searches will be added via raw SQL in migration
        // due to nested owned entity limitations with EF Core indexing
    }
}