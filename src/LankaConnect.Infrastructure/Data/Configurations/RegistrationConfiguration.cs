using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using LankaConnect.Domain.Events;
using LankaConnect.Domain.Events.Enums;
using LankaConnect.Domain.Events.ValueObjects;

namespace LankaConnect.Infrastructure.Data.Configurations;

public class RegistrationConfiguration : IEntityTypeConfiguration<Registration>
{
    public void Configure(EntityTypeBuilder<Registration> builder)
    {
        builder.ToTable("registrations", t =>
        {
            // Session 21: Updated constraint to support both legacy and new multi-attendee formats
            // Valid scenarios:
            // 1. Legacy authenticated: user_id NOT NULL, attendee_info NULL
            // 2. Legacy anonymous: user_id NULL, attendee_info NOT NULL
            // 3. New multi-attendee: attendees NOT NULL, contact NOT NULL (user_id optional)
            t.HasCheckConstraint(
                "ck_registrations_valid_format",
                @"(
                    (user_id IS NOT NULL AND attendee_info IS NULL) OR
                    (user_id IS NULL AND attendee_info IS NOT NULL) OR
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
        builder.OwnsMany(r => r.Attendees, attendeesBuilder =>
        {
            attendeesBuilder.ToJson("attendees");

            // Configure properties explicitly to help EF Core binding
            attendeesBuilder.Property(a => a.Name).HasColumnName("name");
            attendeesBuilder.Property(a => a.Age).HasColumnName("age");
        });

        // Session 21: Configure Contact as JSONB for shared contact information
        builder.OwnsOne(r => r.Contact, contactBuilder =>
        {
            contactBuilder.ToJson("contact");

            // Configure properties explicitly to help EF Core binding
            contactBuilder.Property(c => c.Email).HasColumnName("email");
            contactBuilder.Property(c => c.PhoneNumber).HasColumnName("phone_number");
            contactBuilder.Property(c => c.Address).HasColumnName("address");
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

        // Configure properties
        builder.Property(r => r.Quantity)
            .IsRequired()
            .HasDefaultValue(1);

        // Configure enum
        builder.Property(r => r.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired()
            .HasDefaultValue(RegistrationStatus.Confirmed);

        // Configure audit fields
        builder.Property(r => r.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("NOW()");

        builder.Property(r => r.UpdatedAt);

        // Configure indexes
        builder.HasIndex(r => r.EventId)
            .HasDatabaseName("ix_registrations_event_id");

        builder.HasIndex(r => r.UserId)
            .HasDatabaseName("ix_registrations_user_id");

        // Remove unique constraint on EventId+UserId since UserId can be null for anonymous
        // Anonymous users can register multiple times with different attendee info

        builder.HasIndex(r => new { r.UserId, r.Status })
            .HasDatabaseName("ix_registrations_user_status");
    }
}