using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using LankaConnect.Domain.Events;
using LankaConnect.Domain.Events.ValueObjects;
using LankaConnect.Domain.Events.Enums;
using LankaConnect.Domain.Communications.Entities; // Phase 6A.32: Email groups relationship

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

        // Phase 6A.46: PublishedAt timestamp for "New" label calculation
        builder.Property(e => e.PublishedAt)
            .HasColumnType("timestamp with time zone")
            .IsRequired(false); // Nullable for draft events

        // Configure Category enum (Epic 2 Phase 2)
        builder.Property(e => e.Category)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired()
            .HasDefaultValue(EventCategory.Community);

        // Phase 6A.X: Event Organizer Contact Details - Optional contact information for event inquiries
        builder.Property(e => e.PublishOrganizerContact)
            .HasColumnName("publish_organizer_contact")
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(e => e.OrganizerContactName)
            .HasColumnName("organizer_contact_name")
            .HasMaxLength(200)
            .IsRequired(false); // Nullable - only set when PublishOrganizerContact is true

        builder.Property(e => e.OrganizerContactPhone)
            .HasColumnName("organizer_contact_phone")
            .HasMaxLength(20)
            .IsRequired(false); // Nullable - optional if email provided

        builder.Property(e => e.OrganizerContactEmail)
            .HasColumnName("organizer_contact_email")
            .HasMaxLength(255)
            .IsRequired(false); // Nullable - optional if phone provided

        // Configure TicketPrice as JSONB for consistency with Pricing (Epic 2 Phase 2 - legacy single pricing)
        // Converted from separate columns to ToJson to resolve EF Core shared-type conflict with Pricing.AdultPrice
        builder.OwnsOne(e => e.TicketPrice, money =>
        {
            money.ToJson("ticket_price");  // Store as JSONB column
        });

        // Session 21 + Phase 6D: Configure Pricing as JSONB for dual/group pricing
        // ToJson() automatically serializes Type, AdultPrice, ChildPrice, ChildAgeLimit, and GroupTiers
        builder.OwnsOne(e => e.Pricing, pricing =>
        {
            pricing.ToJson("pricing");  // Store entire Pricing as JSONB column

            // Explicitly configure nested Money types to prevent EF Core shared-type conflict
            pricing.OwnsOne(p => p.AdultPrice);
            pricing.OwnsOne(p => p.ChildPrice);

            // Configure GroupTiers collection
            pricing.OwnsMany(p => p.GroupTiers, tier =>
            {
                tier.OwnsOne(t => t.PricePerPerson);
            });
        });

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

        // CRITICAL: Use backing field "_registrations" for EF Core change tracking
        builder.Navigation(e => e.Registrations)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        // Configure Images relationship (Epic 2 Phase 2)
        builder.HasMany(e => e.Images)
            .WithOne()
            .HasForeignKey(ei => ei.EventId)
            .OnDelete(DeleteBehavior.Cascade);

        // CRITICAL: Use backing field "_images" for EF Core change tracking
        // This ensures EF Core populates the private _images field when loading
        // Required for SetPrimaryImage and other image management operations
        builder.Navigation(e => e.Images)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        // Configure Videos relationship (Epic 2 Phase 2)
        builder.HasMany(e => e.Videos)
            .WithOne()
            .HasForeignKey(ev => ev.EventId)
            .OnDelete(DeleteBehavior.Cascade);

        // CRITICAL: Use backing field "_videos" for EF Core change tracking
        // This ensures EF Core populates the private _videos field when loading
        builder.Navigation(e => e.Videos)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        // Configure SignUpLists relationship (Phase 6A: Sign-up lists for volunteers/items)
        builder.HasMany(e => e.SignUpLists)
            .WithOne()
            .HasForeignKey("EventId")
            .OnDelete(DeleteBehavior.Cascade);

        // CRITICAL: Use backing field "_signUpLists" for EF Core change tracking
        builder.Navigation(e => e.SignUpLists)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        // Configure WaitingList relationship (Epic 2: Waiting List)
        builder.OwnsMany(e => e.WaitingList, waitingList =>
        {
            waitingList.ToTable("event_waiting_list");
            waitingList.Property<Guid>("Id").ValueGeneratedOnAdd();
            waitingList.HasKey("Id");

            waitingList.Property(w => w.UserId)
                .HasColumnName("user_id")
                .IsRequired();

            waitingList.Property(w => w.JoinedAt)
                .HasColumnName("joined_at")
                .HasColumnType("timestamp with time zone")
                .IsRequired();

            waitingList.Property(w => w.Position)
                .HasColumnName("position")
                .IsRequired();

            // Create composite unique index to prevent duplicate user entries
            waitingList.HasIndex("EventId", nameof(WaitingListEntry.UserId))
                .IsUnique()
                .HasDatabaseName("ix_event_waiting_list_event_user");

            // Index for position ordering
            waitingList.HasIndex("EventId", nameof(WaitingListEntry.Position))
                .HasDatabaseName("ix_event_waiting_list_event_position");
        });

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

        // Phase 6A.32: Email Groups - Many-to-Many Relationship
        // Fix #1: Junction table ONLY, no JSONB denormalization
        // Fix #2: Cascade delete on BOTH FKs (safe with soft delete pattern)
        builder
            .HasMany<EmailGroup>("_emailGroupEntities")
            .WithMany()
            .UsingEntity<Dictionary<string, object>>(
                "event_email_groups",
                j => j
                    .HasOne<EmailGroup>()
                    .WithMany()
                    .HasForeignKey("email_group_id")
                    .OnDelete(DeleteBehavior.Cascade), // Fix #2: Safe with soft delete
                j => j
                    .HasOne<Event>()
                    .WithMany()
                    .HasForeignKey("event_id")
                    .OnDelete(DeleteBehavior.Cascade), // Fix #2: Safe with soft delete
                j =>
                {
                    j.ToTable("event_email_groups");
                    j.HasKey("event_id", "email_group_id"); // Composite primary key
                    j.Property<DateTime>("assigned_at")
                        .HasDefaultValueSql("CURRENT_TIMESTAMP");

                    // Indexes for query performance
                    j.HasIndex("event_id");
                    j.HasIndex("email_group_id");
                });
    }
}