using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Npgsql.EntityFrameworkCore.PostgreSQL;  // Issue #56 FIX: For UseXminAsConcurrencyToken()
using System.Text.Json;
using System.Text.Json.Serialization;
using LankaConnect.Domain.Events;
using LankaConnect.Domain.Events.Enums;
using LankaConnect.Domain.Events.ValueObjects;

namespace LankaConnect.Infrastructure.Data.Configurations;

public class RegistrationConfiguration : IEntityTypeConfiguration<Registration>
{
    // Phase 7E: JSON serialisation options for the head_count JSONB column.
    // CamelCase to match common JSONB conventions; ignore null props on write to keep stored
    // documents compact (mode B1 has no Demographics; non-tier events have no TierCounts).
    private static readonly JsonSerializerOptions HeadCountJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    // Phase 7E: Custom ValueConverter for HeadCountBreakdown <-> JSONB string.
    // Architect decision: NOT OwnsOne(...).ToJson() — that path has the Phase 6A.130
    // IReadOnlyList rehydration trap. Custom converter sidesteps it entirely.
    private static readonly ValueConverter<HeadCountBreakdown?, string?> HeadCountConverter = new(
        v => v == null ? null : JsonSerializer.Serialize(v, HeadCountJsonOptions),
        v => string.IsNullOrEmpty(v) ? null : JsonSerializer.Deserialize<HeadCountBreakdown>(v, HeadCountJsonOptions));

    // Phase 7E: Deep-copy ValueComparer.
    // - Equality: ValueObject's structural equality via GetEqualityComponents (Total + Demographics + each TierCount).
    // - Snapshot: deep clone via JSON round-trip — defends against any future mutable field
    //   replicating the Phase 6A.129 mutate-in-place-defeats-snapshot trap.
    // - Hash: ValueObject's hash combine.
    //
    // Phase 7F-C (2026-04-30): TierCount gained nullable AdultCount/ChildCount fields.
    // No comparer change required — the JSON-roundtrip snapshot inherently picks up new
    // fields, and TierCount.GetEqualityComponents already yields them so structural equality
    // detects per-tier-age changes. Verified by round-trip test in
    // Phase7FCTierAgeMatrixPricingTests.HeadCountBreakdown_JsonRoundTrip_PreservesAgeSplit.
    //
    // Phase 7F-E.7 (2026-05-04): TierCount additionally gained nullable
    // AdultMaleCount/AdultFemaleCount/ChildMaleCount/ChildFemaleCount fields. Same pattern
    // applies — the JsonConstructor takes them as nullable parameters with default null,
    // so legacy JSONB rows deserialise cleanly with all 4 null. GetEqualityComponents was
    // also extended (TierCount.cs) so structural equality detects per-tier 4-leaf changes.
    // Verified by round-trip test in Phase7FE7TierCount4LeafJsonRoundTripTests (companion
    // file under tests/LankaConnect.Application.Tests/Events/Domain).
    private static readonly ValueComparer<HeadCountBreakdown?> HeadCountComparer = new(
        (a, b) => HeadCountStructuralEquals(a, b),
        v => v == null ? 0 : v.GetHashCode(),
        v => v == null ? null : DeepCloneHeadCount(v));

    private static bool HeadCountStructuralEquals(HeadCountBreakdown? a, HeadCountBreakdown? b)
    {
        if (ReferenceEquals(a, b)) return true;
        if (a is null || b is null) return false;
        return a.Equals(b);
    }

    private static HeadCountBreakdown DeepCloneHeadCount(HeadCountBreakdown source)
    {
        var json = JsonSerializer.Serialize(source, HeadCountJsonOptions);
        return JsonSerializer.Deserialize<HeadCountBreakdown>(json, HeadCountJsonOptions)!;
    }

    public void Configure(EntityTypeBuilder<Registration> builder)
    {
        builder.ToTable("registrations", t =>
        {
            // Session 21: Updated constraint to support both legacy and new multi-attendee formats
            // Valid scenarios:
            // 1. Legacy authenticated: UserId NOT NULL, attendee_info NULL
            // 2. Legacy anonymous: UserId NULL, attendee_info NOT NULL
            // 3. New multi-attendee: attendees NOT NULL, contact NOT NULL (UserId optional)
            // Note: UserId uses PascalCase (no explicit column mapping), JSON columns use snake_case
            t.HasCheckConstraint(
                "ck_registrations_valid_format",
                @"(
                    (""UserId"" IS NOT NULL AND attendee_info IS NULL) OR
                    (""UserId"" IS NULL AND attendee_info IS NOT NULL) OR
                    (attendees IS NOT NULL AND contact IS NOT NULL)
                )"
            );
        });

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Id)
            .ValueGeneratedNever();

        // Configure foreign keys
        builder.Property(r => r.EventId)
            .IsRequired();

        builder.Property(r => r.UserId)
            .IsRequired(false);  // Nullable for anonymous registrations

        // Configure AttendeeInfo as JSONB for anonymous registrations
        builder.OwnsOne(r => r.AttendeeInfo, attendeeBuilder =>
        {
            attendeeBuilder.ToJson("attendee_info");

            // Configure nested Email value object
            attendeeBuilder.OwnsOne(a => a.Email, emailBuilder =>
            {
                emailBuilder.Property(e => e.Value)
                    .HasColumnName("email");
            });

            // Configure nested PhoneNumber value object
            attendeeBuilder.OwnsOne(a => a.PhoneNumber, phoneBuilder =>
            {
                phoneBuilder.Property(p => p.Value)
                    .HasColumnName("phone_number");
            });
        });

        // Session 21: Configure Attendees as JSONB array for multi-attendee registration
        // Phase 6A.43: Updated to use AgeCategory and Gender instead of Age
        // Phase 6A.55: Fixed shadow property issue for JSONB collections
        builder.OwnsMany(r => r.Attendees, attendeesBuilder =>
        {
            attendeesBuilder.ToJson("attendees");

            // Configure properties explicitly to help EF Core binding
            attendeesBuilder.Property(a => a.Name).HasColumnName("name");
            attendeesBuilder.Property(a => a.AgeCategory)
                .HasColumnName("age_category")
                .HasConversion<string>();
            attendeesBuilder.Property(a => a.Gender)
                .HasColumnName("gender")
                .HasConversion<string?>()
                .IsRequired(false);

            // Multi-tier ticketing: tier assignment per attendee
            attendeesBuilder.Property(a => a.TicketTierId)
                .HasColumnName("ticket_tier_id")
                .IsRequired(false);
            attendeesBuilder.Property(a => a.TicketTierName)
                .HasColumnName("ticket_tier_name")
                .HasMaxLength(100)
                .IsRequired(false);

            // Phase 8 S8.1: per-attendee seat binding for AssignedSeating events.
            // JSONB-schema-less so this addition needs no ALTER TABLE — existing
            // rows deserialise with null defaults, matching the WhatsApp opt-in
            // pattern at lines 152-153 of this file.
            attendeesBuilder.Property(a => a.SeatId)
                .HasColumnName("seat_id")
                .IsRequired(false);
            attendeesBuilder.Property(a => a.SeatLabel)
                .HasColumnName("seat_label")
                .HasMaxLength(50)
                .IsRequired(false);
        });

        // Phase 8 S8.2: pending seat-assignment stash for the RSVP-to-Stripe-checkout
        // window. Set during the RSVP handler before redirect to Stripe; read by the
        // checkout-completed webhook to drive ConfirmSeatAssignments. Cleared on
        // success or expiry. Backed by a real JSONB column on `events.registrations`
        // (NOT on the attendees JSONB — the stash is registration-scoped, not
        // attendee-scoped, and lives separately so cleanup doesn't touch the
        // attendee row).
        builder.OwnsMany(r => r.PendingSeatAssignments, pendingBuilder =>
        {
            pendingBuilder.ToJson("pending_seat_assignments");
            pendingBuilder.Property(p => p.AttendeeIndex).HasColumnName("attendee_index");
            pendingBuilder.Property(p => p.SeatId).HasColumnName("seat_id");
            pendingBuilder.Property(p => p.SeatLabel)
                .HasColumnName("seat_label")
                .HasMaxLength(50);
        });

        builder.Property(r => r.PendingSeatSessionId)
            .HasColumnName("pending_seat_session_id")
            .HasMaxLength(100)
            .IsRequired(false);

        // Session 21: Configure Contact as JSONB for shared contact information
        builder.OwnsOne(r => r.Contact, contactBuilder =>
        {
            contactBuilder.ToJson("contact");

            // Configure properties explicitly to help EF Core binding
            contactBuilder.Property(c => c.Email).HasColumnName("email");
            contactBuilder.Property(c => c.PhoneNumber).HasColumnName("phone_number");
            contactBuilder.Property(c => c.Address).HasColumnName("address");

            // Phase 7A.6D: WhatsApp opt-in fields (JSONB schema-less — no ALTER TABLE needed)
            // Existing rows deserialize with null/false defaults for these new fields
            contactBuilder.Property(c => c.WhatsAppPhoneNumber).HasColumnName("whats_app_phone_number");
            contactBuilder.Property(c => c.WhatsAppOptedIn).HasColumnName("whats_app_opted_in");
        });

        // Session 21: Configure TotalPrice as Money value object (separate columns)
        // The Money object itself is nullable, so we don't need IsRequired(false) on the properties
        builder.OwnsOne(r => r.TotalPrice, money =>
        {
            money.Property(m => m.Amount)
                .HasColumnName("total_price_amount")
                .HasPrecision(18, 2);

            money.Property(m => m.Currency)
                .HasColumnName("total_price_currency")
                .HasConversion<string>()
                .HasMaxLength(3); // ISO 4217 currency codes (USD, LKR, etc.)
        });

        // Phase 6A.X: Configure revenue breakdown Money value objects
        builder.OwnsOne(r => r.SalesTaxAmount, money =>
        {
            money.Property(m => m.Amount)
                .HasColumnName("sales_tax_amount")
                .HasPrecision(18, 2);

            money.Property(m => m.Currency)
                .HasColumnName("sales_tax_currency")
                .HasConversion<string>()
                .HasMaxLength(3);
        });

        builder.OwnsOne(r => r.StripeFeeAmount, money =>
        {
            money.Property(m => m.Amount)
                .HasColumnName("stripe_fee_amount")
                .HasPrecision(18, 2);

            money.Property(m => m.Currency)
                .HasColumnName("stripe_fee_currency")
                .HasConversion<string>()
                .HasMaxLength(3);
        });

        builder.OwnsOne(r => r.PlatformCommissionAmount, money =>
        {
            money.Property(m => m.Amount)
                .HasColumnName("platform_commission_amount")
                .HasPrecision(18, 2);

            money.Property(m => m.Currency)
                .HasColumnName("platform_commission_currency")
                .HasConversion<string>()
                .HasMaxLength(3);
        });

        builder.OwnsOne(r => r.OrganizerPayoutAmount, money =>
        {
            money.Property(m => m.Amount)
                .HasColumnName("organizer_payout_amount")
                .HasPrecision(18, 2);

            money.Property(m => m.Currency)
                .HasColumnName("organizer_payout_currency")
                .HasConversion<string>()
                .HasMaxLength(3);
        });

        builder.Property(r => r.SalesTaxRate)
            .HasColumnName("sales_tax_rate")
            .HasPrecision(5, 4)
            .IsRequired()
            .HasDefaultValue(0m);

        // Configure properties
        builder.Property(r => r.Quantity)
            .IsRequired()
            .HasDefaultValue(1);

        // Configure enum
        // Phase 6A.81 FIX: Removed HasDefaultValue to let application code control Status
        // Default value was overriding domain logic (Preliminary → Confirmed)
        builder.Property(r => r.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        // Phase 6A.81 FIX: Configure PaymentStatus without default value
        // Database had DEFAULT 0 (NotRequired) which overrode domain logic
        // Application code should control PaymentStatus (Pending for paid events, NotRequired for free)
        builder.Property(r => r.PaymentStatus)
            .HasConversion<int>()
            .IsRequired();

        // Phase 7E: Snapshot of Event.RegistrationMode at construction time.
        // Domain event handlers (cancellation, reminder) read this snapshot, not the live Event,
        // so historical email re-renders survive an organiser flipping the mode after the fact.
        // DB-level DEFAULT 0 ensures legacy rows materialise as DetailedAttendees (Phase 6A.123 lesson).
        builder.Property(r => r.RegistrationMode)
            .HasColumnName("registration_mode")
            .HasConversion<short>()
            .IsRequired()
            .HasDefaultValue(LankaConnect.Domain.Events.Enums.RegistrationMode.DetailedAttendees);

        // Phase 7E: Lead attendee name. Populated only for head-count modes (B1-B4).
        builder.Property(r => r.LeadAttendeeName)
            .HasColumnName("lead_attendee_name")
            .HasMaxLength(200)
            .IsRequired(false);

        // Phase 7E: Composite head-count breakdown (Total + Demographics? + TierCounts?).
        // Stored as flat JSONB string via custom converter — NOT OwnsOne(...).ToJson() (Phase 6A.130 trap).
        // Deep-copy ValueComparer protects against the Phase 6A.129 mutate-in-place snapshot trap.
        builder.Property(r => r.HeadCount)
            .HasColumnName("head_count")
            .HasColumnType("jsonb")
            .HasConversion(HeadCountConverter)
            .Metadata.SetValueComparer(HeadCountComparer);

        // Configure audit fields
        builder.Property(r => r.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("NOW()");

        builder.Property(r => r.UpdatedAt);

        // Issue #56 FIX: Add concurrency token for optimistic locking
        // This prevents race conditions where concurrent webhook requests both
        // successfully modify the same registration row.
        // Uses Npgsql's UseXminAsConcurrencyToken() which properly configures PostgreSQL's
        // xmin system column for EF Core concurrency detection.
        // Note: Suppressing CS0618 because the recommended IsRowVersion() approach
        // doesn't work correctly for PostgreSQL xmin system column.
#pragma warning disable CS0618 // Type or member is obsolete
        builder.UseXminAsConcurrencyToken();
#pragma warning restore CS0618

        // Phase 6A.148: refund_requests navigation. Private backing field on the aggregate;
        // FK ON DELETE RESTRICT so a registration with any refund request (even rejected/
        // withdrawn — kept for audit) cannot be hard-deleted. RefundRequestConfiguration
        // sets the FK column + concurrency token.
        builder.HasMany(r => r.RefundRequests)
            .WithOne()
            .HasForeignKey("RegistrationId")
            .OnDelete(DeleteBehavior.Restrict);

        builder.Navigation(r => r.RefundRequests)
            .UsePropertyAccessMode(PropertyAccessMode.Field)
            .HasField("_refundRequests");

        // Configure indexes
        builder.HasIndex(r => r.EventId)
            .HasDatabaseName("ix_registrations_event_id");

        builder.HasIndex(r => r.UserId)
            .HasDatabaseName("ix_registrations_user_id");

        // Phase 6A.XXX FIX: Conditional unique index for authenticated users
        // Prevents duplicate active registrations for the same user on the same event.
        // Filtered to exclude terminal/transient states so users can re-register after cancellation.
        builder.HasIndex(r => new { r.EventId, r.UserId })
            .HasDatabaseName("uix_registrations_event_user_active")
            .IsUnique()
            .HasFilter("\"UserId\" IS NOT NULL AND \"Status\" NOT IN ('Cancelled', 'Refunded', 'RefundRequested', 'Abandoned', 'Preliminary', 'Pending')");

        // Note: Conditional unique index on (EventId, contact->>'email') is added via raw SQL
        // in the migration file because EF Core doesn't support JSONB expression indexes natively.

        builder.HasIndex(r => new { r.UserId, r.Status })
            .HasDatabaseName("ix_registrations_user_status");
    }
}