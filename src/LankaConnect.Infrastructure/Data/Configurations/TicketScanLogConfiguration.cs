using LankaConnect.Products.LankaEvents.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LankaConnect.Infrastructure.Data.Configurations;

/// <summary>
/// Phase 6A.141 — EF config for the ticket-scan audit log.
///
/// Table name follows the existing PascalCase-plural convention (matches Tickets,
/// EventPasses, SupportTickets — public schema, no prefix).
///
/// Column-name strategy mirrors the Phase 6A.24 TicketConfiguration: EF default
/// PascalCase for the standard fields (Id, EventId, CreatedAt) and explicit
/// HasColumnName("snake_case") for the Phase 6A.141 fields. Yes the result is
/// hybrid; yes that's consistent with the rest of the schema.
/// </summary>
public class TicketScanLogConfiguration : IEntityTypeConfiguration<TicketScanLog>
{
    public void Configure(EntityTypeBuilder<TicketScanLog> builder)
    {
        builder.ToTable("TicketScanLogs");

        builder.HasKey(l => l.Id);
        builder.Property(l => l.Id).ValueGeneratedNever();

        // Optional FK to Tickets — soft because failed-signature attempts have no ticket
        // to point at. Keep the column indexed so dispute-resolution queries by ticket
        // remain fast even though the FK is nullable.
        builder.Property(l => l.TicketId)
            .HasColumnName("ticket_id");
        builder.Property(l => l.EventId)
            .HasColumnName("event_id")
            .IsRequired();
        builder.Property(l => l.TicketCode)
            .HasColumnName("ticket_code")
            .HasMaxLength(20); // matches LC-YYYY-XXXXXX = 14 chars; gives 6 chars buffer

        builder.Property(l => l.ScannerUserId)
            .HasColumnName("scanner_user_id")
            .IsRequired();
        builder.Property(l => l.ScannerName)
            .HasColumnName("scanner_name")
            .HasMaxLength(200);

        builder.Property(l => l.ScanResult)
            .HasColumnName("scan_result")
            .HasMaxLength(32)
            .IsRequired();
        // F10: bumped from 64 to 128 to leave room for future reason codes without a
        // widen-migration.
        builder.Property(l => l.RejectionReason)
            .HasColumnName("rejection_reason")
            .HasMaxLength(128);
        builder.Property(l => l.EntryMethod)
            .HasColumnName("entry_method")
            .HasMaxLength(16)
            .IsRequired();

        builder.Property(l => l.VerifiedWithPreviousKey)
            .HasColumnName("verified_with_previous_key")
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(l => l.ClientIp)
            .HasColumnName("client_ip")
            .HasMaxLength(45); // IPv6 max literal length

        builder.Property(l => l.UserAgent)
            .HasColumnName("user_agent")
            .HasMaxLength(500);

        builder.Property(l => l.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();
        builder.Property(l => l.UpdatedAt)
            .HasColumnName("updated_at");

        // Indices per the architect plan
        builder.HasIndex(l => l.TicketId)
            .HasDatabaseName("IX_TicketScanLogs_TicketId");
        builder.HasIndex(l => new { l.EventId, l.CreatedAt })
            .HasDatabaseName("IX_TicketScanLogs_EventId_CreatedAt");
        builder.HasIndex(l => new { l.ScannerUserId, l.CreatedAt })
            .HasDatabaseName("IX_TicketScanLogs_ScannerUserId_CreatedAt");

        // FK to Ticket — soft (DeleteBehavior.SetNull) so a later ticket delete leaves
        // the audit row intact with TicketId = null. forensics > referential strict.
        builder.HasOne<Ticket>()
            .WithMany()
            .HasForeignKey(l => l.TicketId)
            .OnDelete(DeleteBehavior.SetNull);

        // FK to Event — Restrict (events shouldn't get deleted often; if they do, we
        // want to fail loudly rather than silently orphan the audit trail).
        builder.HasOne<LankaConnect.Products.LankaEvents.Domain.Event>()
            .WithMany()
            .HasForeignKey(l => l.EventId)
            .OnDelete(DeleteBehavior.Restrict);

        // FK to User (scanner) — Restrict for the same reason. If we ever soft-delete a
        // user, the audit log still references them by ID; the denormalized scanner_name
        // is the forensic display value.
        builder.HasOne<LankaConnect.Modules.Identity.Domain.Entities.User>()
            .WithMany()
            .HasForeignKey(l => l.ScannerUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
