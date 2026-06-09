using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using LankaConnect.Domain.Events;
using LankaConnect.Domain.Events.ValueObjects;
using LankaConnect.Domain.Events.Enums;
using LankaConnect.Domain.Events.Entities;
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
                .HasMaxLength(10000)
                .IsRequired();
        });

        // Configure basic properties
        // Phase 8YA.1: dates are nullable for TBD events. Migration
        // Phase8YA1_AllowNullEventDates drops NOT NULL on both columns.
        builder.Property(e => e.StartDate)
            .HasColumnType("timestamp with time zone");

        builder.Property(e => e.EndDate)
            .HasColumnType("timestamp with time zone");

        builder.Property(e => e.OrganizerId)
            .IsRequired();

        builder.Property(e => e.Capacity)
            .IsRequired();

        // Issue #51: MaxAttendeesPerRegistration - configurable limit per single registration
        builder.Property(e => e.MaxAttendeesPerRegistration)
            .HasColumnName("max_attendees_per_registration")
            .IsRequired()
            .HasDefaultValue(10);  // Backward compatibility: default 10 for existing events

        // Phase 7E: Per-event RegistrationMode (DetailedAttendees / HeadCount* / NoRegistration).
        // Stored as smallint with DB-level DEFAULT 0 so legacy events materialise as DetailedAttendees
        // automatically — Phase 6A.123 lesson: NEVER rely on app-side defaults for NOT NULL columns.
        builder.Property(e => e.RegistrationMode)
            .HasColumnName("registration_mode")
            .HasConversion<short>()
            .IsRequired()
            .HasDefaultValue(LankaConnect.Domain.Events.Enums.RegistrationMode.DetailedAttendees);

        // Phase 8X — Per-event EventPaymentMode (Free / OnPlatformPaid / ExternalPaid).
        // Stored as smallint with DB-level DEFAULT 0 so legacy rows materialise as Free;
        // Phase 8X.2 migration runs a backfill UPDATE that flips paid rows to OnPlatformPaid
        // (with embedded RAISE EXCEPTION post-assertion per Phase 6A.122 lesson).
        builder.Property(e => e.PaymentMode)
            .HasColumnName("payment_mode")
            .HasConversion<short>()
            .IsRequired()
            .HasDefaultValue(EventPaymentMode.Free);

        // Phase 8X — ExternalRegistration value object: URL + optional instructions + optional vendor name,
        // mapped as 3 nullable scalar columns (no JSONB — no collection backing fields, so the Phase 6A.129
        // ValueComparer / Phase 6A.130 ToJson() IReadOnlyList concerns do not apply).
        builder.OwnsOne(e => e.ExternalRegistration, ext =>
        {
            ext.Property(x => x.Url)
                .HasColumnName("external_registration_url")
                .HasMaxLength(2048);
            ext.Property(x => x.Instructions)
                .HasColumnName("external_registration_instructions")
                .HasColumnType("text");
            ext.Property(x => x.VendorName)
                .HasColumnName("external_registration_vendor_name")
                .HasMaxLength(100);
        });

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

        // Phase 6A.154: Organizer-controlled vanity URL slug.
        //
        // Mapped as a SCALAR Property with HasConversion, NOT OwnsOne.
        // OwnsOne for a single-string VO causes EF Core 8's convention
        // scanner to walk the owned-entity-type fragment first, which
        // collides with the EventSlugAlias child entity discovery and
        // produces "first mapped explicitly and then ignored" — the
        // EventSlugAlias table then silently never makes it into the
        // migration. Scalar + HasConversion has none of that overhead:
        // the converter unwraps Value on write and rebuilds the VO via
        // its private ctor on read (no factory re-validation, so legacy
        // edge-case rows don't crash on materialization).
        builder.Property(e => e.VanitySlug)
            .HasColumnName("vanity_slug")
            .HasMaxLength(80)
            .HasConversion(
                v => v == null ? null : v.Value,
                v => MaterializeVanitySlug(v))
            .IsRequired(false);

        // Partial unique index — only enforced when vanity_slug IS NOT NULL.
        // Thousands of legacy events with null slugs don't trip the constraint.
        builder.HasIndex(e => e.VanitySlug)
            .IsUnique()
            .HasFilter("vanity_slug IS NOT NULL")
            .HasDatabaseName("ix_events_vanity_slug_unique");

        // Phase 6A.154: alias child collection. Mirrors the working
        // OrganizerContacts pattern below (lines ~172-178). EventSlugAlias
        // has its own EventSlugAliasConfiguration registered in AppDbContext
        // — applied AFTER EventConfiguration is fine (same as
        // EventOrganizerContactConfiguration; no ordering trick needed once
        // VanitySlug is a scalar Property rather than OwnsOne).
        builder.HasMany(e => e.SlugAliases)
            .WithOne()
            .HasForeignKey(a => a.EventId)
            .OnDelete(DeleteBehavior.Cascade);

        // Backing field "_slugAliases" — required for change tracking on
        // the IReadOnlyList collection (same pattern as _organizerContacts).
        builder.Navigation(e => e.SlugAliases)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        // Configure Category enum (Epic 2 Phase 2)
        builder.Property(e => e.Category)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired()
            .HasDefaultValue(EventCategory.Community);

        // Event Organizer Contact Details
        builder.Property(e => e.PublishOrganizerContact)
            .HasColumnName("publish_organizer_contact")
            .IsRequired()
            .HasDefaultValue(false);

        // Ignore backward-compat computed properties (they delegate to OrganizerContacts collection)
        builder.Ignore(e => e.OrganizerContactName);
        builder.Ignore(e => e.OrganizerContactPhone);
        builder.Ignore(e => e.OrganizerContactEmail);

        // Multiple organizer contacts (separate table)
        builder.HasMany(e => e.OrganizerContacts)
            .WithOne()
            .HasForeignKey(c => c.EventId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(e => e.OrganizerContacts)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

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

        // Phase 6A.X: Configure RevenueBreakdown as JSONB
        builder.OwnsOne(e => e.RevenueBreakdown, breakdown =>
        {
            breakdown.ToJson("revenue_breakdown");  // Store entire breakdown as JSONB

            // Explicitly configure nested Money types to prevent EF Core shared-type conflict
            breakdown.OwnsOne(b => b.GrossAmount);
            breakdown.OwnsOne(b => b.SalesTaxAmount);
            breakdown.OwnsOne(b => b.TaxableAmount);
            breakdown.OwnsOne(b => b.StripeFeeAmount);
            breakdown.OwnsOne(b => b.PlatformCommission);
            breakdown.OwnsOne(b => b.OrganizerPayout);
        });

        // Donation Configuration: JSONB value object (C5 Guard: flat primitives only, no nested Money)
        builder.OwnsOne(e => e.DonationConfig, donationConfig =>
        {
            donationConfig.ToJson("donation_config");

            // SuggestedAmounts is a List<decimal> — EF Core handles this automatically in JSONB
        });

        // Collection Configuration: JSONB value object (C5 Guard: flat primitives only)
        builder.OwnsOne(e => e.CollectionConfig, collectionConfig =>
        {
            collectionConfig.ToJson("collection_config");

            // SuggestedAmounts is a List<decimal> — EF Core handles this automatically in JSONB
        });

        // Sponsor Configuration: JSONB value object (C5 Guard: flat primitives only)
        builder.OwnsOne(e => e.SponsorConfig, sponsorConfig =>
        {
            sponsorConfig.ToJson("sponsor_config");
        });

        // Add-On Configuration: JSONB value object (C5 Guard: flat primitives only)
        builder.OwnsOne(e => e.AddOnConfig, addOnConfig =>
        {
            addOnConfig.ToJson("add_on_config");
        });

        // Multi-tier ticketing: TicketingMode enum
        builder.Property(e => e.TicketingMode)
            .HasColumnName("ticketing_mode")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired()
            .HasDefaultValue(TicketingMode.SingleTier);

        // Multi-tier ticketing: TicketTiers relationship
        builder.HasMany(e => e.TicketTiers)
            .WithOne()
            .HasForeignKey(t => t.EventId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(e => e.TicketTiers)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        // Ignore computed properties from partial class
        builder.Ignore(e => e.HasTicketTiers);

        // Phase 2: Seating properties (from Event.Seating.cs partial class)
        builder.Property(e => e.SeatingMode)
            .HasColumnName("seating_mode")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired()
            .HasDefaultValue(SeatingMode.GeneralAdmission);

        builder.Property(e => e.VenueLayoutId)
            .HasColumnName("venue_layout_id");

        // Ignore computed property from Event.Seating.cs
        builder.Ignore(e => e.HasAssignedSeating);

        // Configure audit fields
        builder.Property(e => e.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("NOW()");

        builder.Property(e => e.UpdatedAt);

        // Wave4.9.2.10a Phase 1.10a (2026-06-09): physical CreatedBy/UpdatedBy.
        builder.Property(e => e.CreatedBy).HasColumnName("created_by").HasColumnType("text");
        builder.Property(e => e.UpdatedBy).HasColumnName("updated_by").HasColumnType("text");

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

        // Configure EventLocation value object (Epic 2 Phase 1; Phase 7C.1 added Name)
        builder.OwnsOne(e => e.Location, location =>
        {
            // Required to prevent EF Core optional dependent error
            location.Property<bool>("_hasValue")
                .HasColumnName("has_location")
                .HasDefaultValue(true)
                .IsRequired();

            // Phase 7C.1: Optional venue name (e.g., "Grand Ballroom")
            location.Property(l => l.Name)
                .HasColumnName("location_name")
                .HasMaxLength(150)
                .IsRequired(false);

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

        // Phase 7C.1: Configure optional EventSecondaryLocation value object.
        // Parallel structure to primary Location with `has_secondary_location` discriminator.
        builder.OwnsOne(e => e.SecondaryLocation, secondary =>
        {
            // Discriminator flag so EF Core can tell "present with empty strings" from "absent"
            secondary.Property<bool>("_hasValue")
                .HasColumnName("has_secondary_location")
                .HasDefaultValue(false)
                .IsRequired();

            // SecondaryLocationType stored as string for enum-reorder safety.
            // Column is nullable at the DB level via the owned-type's `has_secondary_location` flag —
            // EF Core handles optional dependents, so we do NOT mark Type itself as optional
            // (non-nullable enum properties cannot be marked nullable).
            secondary.Property(s => s.Type)
                .HasColumnName("secondary_location_type")
                .HasConversion<string>()
                .HasMaxLength(50);

            // Inner EventLocation (owned-within-owned)
            secondary.OwnsOne(s => s.Location, location =>
            {
                location.Property(l => l.Name)
                    .HasColumnName("secondary_location_name")
                    .HasMaxLength(150)
                    .IsRequired(false);

                location.OwnsOne(l => l.Address, address =>
                {
                    address.Property(a => a.Street)
                        .HasColumnName("secondary_address_street")
                        .HasMaxLength(200)
                        .IsRequired(false);

                    address.Property(a => a.City)
                        .HasColumnName("secondary_address_city")
                        .HasMaxLength(100)
                        .IsRequired(false);

                    address.Property(a => a.State)
                        .HasColumnName("secondary_address_state")
                        .HasMaxLength(100)
                        .IsRequired(false);

                    address.Property(a => a.ZipCode)
                        .HasColumnName("secondary_address_zip_code")
                        .HasMaxLength(20)
                        .IsRequired(false);

                    address.Property(a => a.Country)
                        .HasColumnName("secondary_address_country")
                        .HasMaxLength(100)
                        .IsRequired(false);
                });

                location.OwnsOne(l => l.Coordinates, coordinates =>
                {
                    coordinates.Property(c => c.Latitude)
                        .HasColumnName("secondary_coordinates_latitude")
                        .HasPrecision(10, 7);

                    coordinates.Property(c => c.Longitude)
                        .HasColumnName("secondary_coordinates_longitude")
                        .HasPrecision(10, 7);
                });
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

    // Phase 6A.154: rebuild EventVanitySlug from its raw string column without
    // re-running the factory's validation. Materialization MUST be lenient —
    // if a legacy row holds an edge-case value (e.g. a slug that pre-dates a
    // tightened regex), failing here would 500 every event read. The VO's
    // private ctor accepts the raw value unconditionally; the factory is the
    // gate for *new* values. Reflection cost is one-time per slot — EF caches
    // the converter expression compile.
    private static readonly System.Reflection.ConstructorInfo VanitySlugCtor =
        typeof(LankaConnect.Domain.Events.ValueObjects.EventVanitySlug).GetConstructor(
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic,
            binder: null,
            types: new[] { typeof(string) },
            modifiers: null)
        ?? throw new System.InvalidOperationException(
            "EventVanitySlug private(string) ctor not found — VanitySlug converter cannot materialize values.");

    private static LankaConnect.Domain.Events.ValueObjects.EventVanitySlug? MaterializeVanitySlug(string? raw)
    {
        if (raw is null) return null;
        return (LankaConnect.Domain.Events.ValueObjects.EventVanitySlug)VanitySlugCtor.Invoke(new object[] { raw });
    }
}