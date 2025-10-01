using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using LankaConnect.Domain.Events;
using LankaConnect.Domain.Events.Enums;

namespace LankaConnect.Infrastructure.Data.Configurations;

public class RegistrationConfiguration : IEntityTypeConfiguration<Registration>
{
    public void Configure(EntityTypeBuilder<Registration> builder)
    {
        builder.HasKey(r => r.Id);
        
        builder.Property(r => r.Id)
            .ValueGeneratedNever();

        // Configure foreign keys
        builder.Property(r => r.EventId)
            .IsRequired();

        builder.Property(r => r.UserId)
            .IsRequired();

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

        builder.HasIndex(r => new { r.EventId, r.UserId })
            .IsUnique()
            .HasDatabaseName("ix_registrations_event_user_unique");

        builder.HasIndex(r => new { r.UserId, r.Status })
            .HasDatabaseName("ix_registrations_user_status");
    }
}