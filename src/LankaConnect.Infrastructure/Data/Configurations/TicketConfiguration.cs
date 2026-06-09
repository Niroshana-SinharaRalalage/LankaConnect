using LankaConnect.Domain.Events.Entities;
using LankaConnect.Domain.Events.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LankaConnect.Infrastructure.Data.Configurations;

/// <summary>
/// Phase 6A.24: EF Core configuration for Ticket entity
/// </summary>
public class TicketConfiguration : IEntityTypeConfiguration<Ticket>
{
    public void Configure(EntityTypeBuilder<Ticket> builder)
    {
        builder.ToTable("Tickets");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Id)
            .ValueGeneratedNever();

        builder.Property(t => t.RegistrationId)
            .IsRequired();

        builder.Property(t => t.EventId)
            .IsRequired();

        builder.Property(t => t.UserId);

        builder.Property(t => t.TicketCode)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(t => t.QrCodeData)
            .IsRequired();

        builder.Property(t => t.PdfBlobUrl)
            .HasMaxLength(500);

        builder.Property(t => t.IsValid)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(t => t.ValidatedAt);

        builder.Property(t => t.ExpiresAt)
            .IsRequired();

        // Multi-tier ticketing fields
        builder.Property(t => t.TicketTierName)
            .HasColumnName("ticket_tier_name")
            .HasMaxLength(100);

        builder.Property(t => t.TicketCategory)
            .HasColumnName("ticket_category")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired()
            .HasDefaultValue(TicketCategory.Standard);

        builder.Property(t => t.AttendeeIndex)
            .HasColumnName("attendee_index");

        builder.Property(t => t.AttendeeNames)
            .HasColumnName("attendee_names")
            .HasMaxLength(2000);

        builder.Property(t => t.CreatedAt)
            .IsRequired();

        builder.Property(t => t.UpdatedAt);

        // Wave4.9.2.10c.c Phase 1.10c.c (2026-06-09): physical CreatedBy/UpdatedBy.
        builder.Property(t => t.CreatedBy).HasColumnName("created_by").HasColumnType("text");
        builder.Property(t => t.UpdatedBy).HasColumnName("updated_by").HasColumnType("text");

        // Indexes
        builder.HasIndex(t => t.TicketCode)
            .IsUnique();

        builder.HasIndex(t => t.RegistrationId);

        builder.HasIndex(t => t.EventId);

        builder.HasIndex(t => t.UserId);

        // Relationships
        builder.HasOne<LankaConnect.Domain.Events.Registration>()
            .WithMany()
            .HasForeignKey(t => t.RegistrationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<LankaConnect.Domain.Events.Event>()
            .WithMany()
            .HasForeignKey(t => t.EventId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<LankaConnect.Domain.Users.User>()
            .WithMany()
            .HasForeignKey(t => t.UserId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
