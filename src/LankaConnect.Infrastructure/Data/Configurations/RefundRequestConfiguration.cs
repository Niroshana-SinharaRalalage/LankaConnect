using LankaConnect.Domain.Events;
using LankaConnect.Modules.Identity.Domain.DomainEvents;
using LankaConnect.Modules.Forms.Domain;
using LankaConnect.Modules.Forms.Domain.Entities;
using LankaConnect.Modules.Forms.Domain.Enums;
using LankaConnect.Modules.Forms.Domain.DomainEvents;
using LankaConnect.Modules.Forms.Domain.Repositories;
using LankaConnect.Domain.Events.Entities;
using LankaConnect.Domain.Events.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Npgsql.EntityFrameworkCore.PostgreSQL;

namespace LankaConnect.Infrastructure.Data.Configurations;

/// <summary>
/// Phase 6A.148 — EF config for refund_requests table.
///
/// Owned by the Registration aggregate via <c>HasMany</c> in RegistrationConfiguration,
/// but lives in its own table so the organizer queue can be queried with AsNoTracking
/// projections without loading the full Registration graph.
///
/// Concurrency: Postgres <c>xmin</c> (per architect F3 — matches project convention,
/// see RegistrationConfiguration.cs:327).
///
/// FK behavior: <c>ON DELETE RESTRICT</c> on the Registration FK so a registration with
/// an in-flight or completed refund request cannot be hard-deleted (audit-trail safety).
/// </summary>
public class RefundRequestConfiguration : IEntityTypeConfiguration<RefundRequest>
{
    public void Configure(EntityTypeBuilder<RefundRequest> builder)
    {
        builder.ToTable("refund_requests");

        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).ValueGeneratedNever();

        builder.Property(r => r.RegistrationId)
            .HasColumnName("registration_id")
            .IsRequired();

        builder.Property(r => r.RequestedByUserId)
            .HasColumnName("requested_by_user_id")
            .IsRequired();

        builder.Property(r => r.IsOrganizerInitiated)
            .HasColumnName("is_organizer_initiated")
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(r => r.RequestedAt)
            .HasColumnName("requested_at")
            .IsRequired();

        builder.Property(r => r.Status)
            .HasColumnName("status")
            .HasConversion<int>()
            .IsRequired();

        builder.Property(r => r.RequesterReason)
            .HasColumnName("requester_reason")
            .HasMaxLength(1000);

        builder.Property(r => r.ReviewedByUserId)
            .HasColumnName("reviewed_by_user_id");

        builder.Property(r => r.ReviewedAt)
            .HasColumnName("reviewed_at");

        builder.Property(r => r.OrganizerNotes)
            .HasColumnName("organizer_notes")
            .HasMaxLength(2000);

        builder.Property(r => r.RejectionReason)
            .HasColumnName("rejection_reason")
            .HasMaxLength(1000);

        builder.Property(r => r.CompletedAt)
            .HasColumnName("completed_at");

        builder.Property(r => r.ScanGuardOverridden)
            .HasColumnName("scan_guard_overridden")
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(r => r.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(r => r.UpdatedAt)
            .HasColumnName("updated_at");

        // Wave4.9.2.10d Phase 1.10d (2026-06-09): physical CreatedBy/UpdatedBy.
        builder.Property(r => r.CreatedBy).HasColumnName("created_by").HasColumnType("text");
        builder.Property(r => r.UpdatedBy).HasColumnName("updated_by").HasColumnType("text");

        // Architect F3: Postgres xmin optimistic concurrency. Matches project convention
        // (RegistrationConfiguration.cs:327, FormConfiguration.cs:70).
#pragma warning disable CS0618 // Type or member is obsolete
        builder.UseXminAsConcurrencyToken();
#pragma warning restore CS0618

        // Line items — one-to-many. Use the private backing field; expose via IReadOnlyList.
        builder.HasMany(r => r.LineItems)
            .WithOne()
            .HasForeignKey(li => li.RefundRequestId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(r => r.LineItems)
            .UsePropertyAccessMode(PropertyAccessMode.Field)
            .HasField("_lineItems");

        // Indices for organizer queue + reconciliation lookups.
        builder.HasIndex(r => r.RegistrationId)
            .HasDatabaseName("ix_refund_requests_registration_id");
        builder.HasIndex(r => r.Status)
            .HasDatabaseName("ix_refund_requests_status");
        // Compound for the most common organizer query: list-by-event-status-by-date.
        builder.HasIndex(r => new { r.Status, r.RequestedAt })
            .HasDatabaseName("ix_refund_requests_status_requested_at");
    }
}
