using LankaConnect.Domain.Events.Entities;
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

        builder.Property(t => t.CreatedAt)
            .IsRequired();

        builder.Property(t => t.UpdatedAt);

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
