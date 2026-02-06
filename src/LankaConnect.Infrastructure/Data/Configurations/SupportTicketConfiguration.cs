using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using LankaConnect.Domain.Support;
using LankaConnect.Domain.Support.Enums;

namespace LankaConnect.Infrastructure.Data.Configurations;

/// <summary>
/// EF Core configuration for SupportTicket entity
/// Phase 6A.89: Support/Feedback System
/// </summary>
public class SupportTicketConfiguration : IEntityTypeConfiguration<SupportTicket>
{
    public void Configure(EntityTypeBuilder<SupportTicket> builder)
    {
        builder.ToTable("support_tickets", "support");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id)
            .ValueGeneratedNever();

        // ReferenceId - required, unique, max 50 chars
        builder.Property(e => e.ReferenceId)
            .HasColumnName("reference_id")
            .HasMaxLength(50)
            .IsRequired();

        builder.HasIndex(e => e.ReferenceId)
            .IsUnique()
            .HasDatabaseName("IX_SupportTickets_ReferenceId");

        // Name - required, max 100 chars
        builder.Property(e => e.Name)
            .HasColumnName("name")
            .HasMaxLength(100)
            .IsRequired();

        // Email - value object stored as string
        builder.OwnsOne(e => e.Email, email =>
        {
            email.Property(e => e.Value)
                .HasColumnName("email")
                .HasMaxLength(255)
                .IsRequired();
        });

        // Subject - required, max 200 chars
        builder.Property(e => e.Subject)
            .HasColumnName("subject")
            .HasMaxLength(200)
            .IsRequired();

        // Message - required, text type for longer content
        builder.Property(e => e.Message)
            .HasColumnName("message")
            .HasColumnType("text")
            .IsRequired();

        // Status - stored as integer
        builder.Property(e => e.Status)
            .HasColumnName("status")
            .IsRequired()
            .HasDefaultValue(SupportTicketStatus.New);

        // Priority - stored as integer
        builder.Property(e => e.Priority)
            .HasColumnName("priority")
            .IsRequired()
            .HasDefaultValue(SupportTicketPriority.Normal);

        // AssignedToUserId - optional
        builder.Property(e => e.AssignedToUserId)
            .HasColumnName("assigned_to_user_id");

        // Audit fields from BaseEntity
        builder.Property(e => e.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired()
            .HasDefaultValueSql("NOW()");

        builder.Property(e => e.UpdatedAt)
            .HasColumnName("updated_at");

        // Configure Replies collection (owned entities)
        builder.OwnsMany(e => e.Replies, reply =>
        {
            reply.ToTable("support_ticket_replies", "support");

            reply.Property<Guid>("Id")
                .HasColumnName("id")
                .ValueGeneratedNever();
            reply.HasKey("Id");

            reply.Property(r => r.Content)
                .HasColumnName("content")
                .HasColumnType("text")
                .IsRequired();

            reply.Property(r => r.RepliedByUserId)
                .HasColumnName("replied_by_user_id")
                .IsRequired();

            reply.Property(r => r.CreatedAt)
                .HasColumnName("created_at")
                .IsRequired()
                .HasDefaultValueSql("NOW()");

            reply.WithOwner().HasForeignKey("SupportTicketId");

            // Index for querying replies by ticket
            reply.HasIndex("SupportTicketId")
                .HasDatabaseName("IX_SupportTicketReplies_TicketId");
        });

        // CRITICAL: Use backing field for EF Core change tracking
        builder.Navigation(e => e.Replies)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        // Configure Notes collection (owned entities)
        builder.OwnsMany(e => e.Notes, note =>
        {
            note.ToTable("support_ticket_notes", "support");

            note.Property<Guid>("Id")
                .HasColumnName("id")
                .ValueGeneratedNever();
            note.HasKey("Id");

            note.Property(n => n.Content)
                .HasColumnName("content")
                .HasColumnType("text")
                .IsRequired();

            note.Property(n => n.CreatedByUserId)
                .HasColumnName("created_by_user_id")
                .IsRequired();

            note.Property(n => n.CreatedAt)
                .HasColumnName("created_at")
                .IsRequired()
                .HasDefaultValueSql("NOW()");

            note.WithOwner().HasForeignKey("SupportTicketId");

            // Index for querying notes by ticket
            note.HasIndex("SupportTicketId")
                .HasDatabaseName("IX_SupportTicketNotes_TicketId");
        });

        // CRITICAL: Use backing field for EF Core change tracking
        builder.Navigation(e => e.Notes)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        // Performance indexes as per architect recommendations
        builder.HasIndex(e => new { e.Status, e.CreatedAt })
            .HasDatabaseName("IX_SupportTickets_Status_CreatedAt");

        builder.HasIndex(e => e.Status)
            .HasDatabaseName("IX_SupportTickets_Status");

        builder.HasIndex(e => e.AssignedToUserId)
            .HasDatabaseName("IX_SupportTickets_AssignedTo");

        builder.HasIndex(e => e.CreatedAt)
            .HasDatabaseName("IX_SupportTickets_CreatedAt");

        // Ignore domain events collection (not persisted)
        builder.Ignore(e => e.DomainEvents);
    }
}
