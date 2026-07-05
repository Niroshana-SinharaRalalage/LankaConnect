using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using LankaConnect.Products.LankaEvents.Domain;
using LankaConnect.Modules.Identity.Domain.DomainEvents;
using LankaConnect.Products.LankaEvents.Domain.Enums;
using LankaConnect.Products.LankaEvents.Domain.ValueObjects;
using System.Text.Json;
using System.Text.Json.Serialization;
namespace LankaConnect.Products.LankaEvents.Infrastructure.Configurations;

/// <summary>
/// EF Core configuration for RegistrationAddition entity.
/// Part of the Add-Only Attendees with Delta Payment feature.
/// </summary>
public class RegistrationAdditionConfiguration : IEntityTypeConfiguration<RegistrationAddition>
{
    // Phase 7F-D (architect-approved 2026-04-30): JSON config for the head_count_delta
    // jsonb column. Mirrors RegistrationConfiguration.HeadCountJsonOptions exactly so the
    // serialised shape is consistent across the registration's HeadCount + the addition's
    // HeadCountDelta. Custom ValueConverter + deep-copy ValueComparer (NOT OwnsOne.ToJson —
    // Phase 6A.130 IReadOnlyList rehydration trap).
    private static readonly JsonSerializerOptions HeadCountDeltaJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private static readonly ValueConverter<HeadCountBreakdown?, string?> HeadCountDeltaConverter = new(
        v => v == null ? null : JsonSerializer.Serialize(v, HeadCountDeltaJsonOptions),
        v => string.IsNullOrEmpty(v) ? null : JsonSerializer.Deserialize<HeadCountBreakdown>(v, HeadCountDeltaJsonOptions));

    private static readonly ValueComparer<HeadCountBreakdown?> HeadCountDeltaComparer = new(
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
        var json = JsonSerializer.Serialize(source, HeadCountDeltaJsonOptions);
        return JsonSerializer.Deserialize<HeadCountBreakdown>(json, HeadCountDeltaJsonOptions)!;
    }

    public void Configure(EntityTypeBuilder<RegistrationAddition> builder)
    {
        builder.ToTable("registration_additions", "events");

        builder.HasKey(ra => ra.Id);

        builder.Property(ra => ra.Id)
            .ValueGeneratedNever();

        // Foreign keys
        builder.Property(ra => ra.RegistrationId)
            .HasColumnName("registration_id")
            .IsRequired();

        builder.Property(ra => ra.EventId)
            .HasColumnName("event_id")
            .IsRequired();

        // Configure NewAttendees as JSONB array
        builder.OwnsMany(ra => ra.NewAttendees, attendeesBuilder =>
        {
            attendeesBuilder.ToJson("new_attendees");

            attendeesBuilder.Property(a => a.Name).HasColumnName("name");
            attendeesBuilder.Property(a => a.AgeCategory)
                .HasColumnName("age_category")
                .HasConversion<string>();
            attendeesBuilder.Property(a => a.Gender)
                .HasColumnName("gender")
                .HasConversion<string?>()
                .IsRequired(false);
        });

        // Configure PreviousTotalPrice as Money value object
        builder.OwnsOne(ra => ra.PreviousTotalPrice, money =>
        {
            money.Property(m => m.Amount)
                .HasColumnName("previous_total_price_amount")
                .HasPrecision(18, 2)
                .IsRequired();

            money.Property(m => m.Currency)
                .HasColumnName("previous_total_price_currency")
                .HasConversion<string>()
                .HasMaxLength(3)
                .IsRequired();
        });

        // Configure NewTotalPrice as Money value object
        builder.OwnsOne(ra => ra.NewTotalPrice, money =>
        {
            money.Property(m => m.Amount)
                .HasColumnName("new_total_price_amount")
                .HasPrecision(18, 2)
                .IsRequired();

            money.Property(m => m.Currency)
                .HasColumnName("new_total_price_currency")
                .HasConversion<string>()
                .HasMaxLength(3)
                .IsRequired();
        });

        // Configure AdditionalAmount as Money value object
        builder.OwnsOne(ra => ra.AdditionalAmount, money =>
        {
            money.Property(m => m.Amount)
                .HasColumnName("additional_amount")
                .HasPrecision(18, 2)
                .IsRequired();

            money.Property(m => m.Currency)
                .HasColumnName("additional_amount_currency")
                .HasConversion<string>()
                .HasMaxLength(3)
                .IsRequired();
        });

        // Payment tracking
        builder.Property(ra => ra.StripeCheckoutSessionId)
            .HasColumnName("stripe_checkout_session_id")
            .HasMaxLength(500);

        builder.Property(ra => ra.StripePaymentIntentId)
            .HasColumnName("stripe_payment_intent_id")
            .HasMaxLength(200);

        // Status enum
        builder.Property(ra => ra.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        // Phase 7F-D: registration-mode snapshot at addition creation. NOT NULL with DB
        // default 0 (DetailedAttendees) so legacy rows materialise correctly post-migration
        // (memory 6A.123: NOT NULL columns need a DB default).
        builder.Property(ra => ra.RegistrationMode)
            .HasColumnName("registration_mode")
            .HasConversion<short>()
            .IsRequired()
            .HasDefaultValue(LankaConnect.Products.LankaEvents.Domain.Enums.RegistrationMode.DetailedAttendees);

        // Phase 7F-D: optional Mode-B head-count delta. jsonb with deep-copy ValueComparer
        // (memory 6A.129) so EF detects changes when the delta's nested TierCount or
        // Demographics fields are mutated.
        builder.Property(ra => ra.HeadCountDelta)
            .HasColumnName("head_count_delta")
            .HasColumnType("jsonb")
            .HasConversion(HeadCountDeltaConverter)
            .Metadata.SetValueComparer(HeadCountDeltaComparer);

        // Timestamps
        builder.Property(ra => ra.CheckoutExpiresAt)
            .HasColumnName("checkout_expires_at")
            .HasColumnType("timestamp with time zone");

        builder.Property(ra => ra.PaymentCompletedAt)
            .HasColumnName("payment_completed_at")
            .HasColumnType("timestamp with time zone");

        builder.Property(ra => ra.MergedAt)
            .HasColumnName("merged_at")
            .HasColumnType("timestamp with time zone");

        builder.Property(ra => ra.FailedAt)
            .HasColumnName("failed_at")
            .HasColumnType("timestamp with time zone");

        builder.Property(ra => ra.AbandonedAt)
            .HasColumnName("abandoned_at")
            .HasColumnType("timestamp with time zone");

        // Audit fields
        builder.Property(ra => ra.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired()
            .HasDefaultValueSql("NOW()");

        builder.Property(ra => ra.UpdatedAt)
            .HasColumnName("updated_at")
            .HasColumnType("timestamp with time zone");

        // Wave4.9.2.10d Phase 1.10d (2026-06-09): physical CreatedBy/UpdatedBy.
        builder.Property(ra => ra.CreatedBy).HasColumnName("created_by").HasColumnType("text");
        builder.Property(ra => ra.UpdatedBy).HasColumnName("updated_by").HasColumnType("text");

        // Indexes
        builder.HasIndex(ra => ra.RegistrationId)
            .HasDatabaseName("ix_registration_additions_registration_id");

        builder.HasIndex(ra => ra.EventId)
            .HasDatabaseName("ix_registration_additions_event_id");

        builder.HasIndex(ra => ra.Status)
            .HasDatabaseName("ix_registration_additions_status");

        builder.HasIndex(ra => ra.StripeCheckoutSessionId)
            .HasDatabaseName("ix_registration_additions_checkout_session");

        // Unique constraint: Only one pending addition per registration at a time
        builder.HasIndex(ra => new { ra.RegistrationId, ra.Status })
            .HasFilter("status = 'Pending'")
            .IsUnique()
            .HasDatabaseName("uq_registration_additions_one_pending_per_registration");

        // Foreign key relationships
        builder.HasOne<Registration>()
            .WithMany()
            .HasForeignKey(ra => ra.RegistrationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Event>()
            .WithMany()
            .HasForeignKey(ra => ra.EventId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
