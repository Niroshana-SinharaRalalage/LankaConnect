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
            // Add XOR constraint: either UserId OR AttendeeInfo must be present, but not both
            t.HasCheckConstraint(
                "ck_registrations_user_xor_attendee",
                "(user_id IS NOT NULL AND attendee_info IS NULL) OR (user_id IS NULL AND attendee_info IS NOT NULL)"
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