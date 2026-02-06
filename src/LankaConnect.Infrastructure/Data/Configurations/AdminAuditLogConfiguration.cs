using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using LankaConnect.Domain.Support;

namespace LankaConnect.Infrastructure.Data.Configurations;

/// <summary>
/// EF Core configuration for AdminAuditLog entity
/// Phase 6A.89: Security audit logging for admin actions
/// </summary>
public class AdminAuditLogConfiguration : IEntityTypeConfiguration<AdminAuditLog>
{
    public void Configure(EntityTypeBuilder<AdminAuditLog> builder)
    {
        builder.ToTable("admin_audit_logs", "support");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id)
            .ValueGeneratedNever();

        // AdminUserId - required, references Users table
        builder.Property(e => e.AdminUserId)
            .HasColumnName("admin_user_id")
            .IsRequired();

        // Action - required, max 100 chars (e.g., "USER_LOCKED")
        builder.Property(e => e.Action)
            .HasColumnName("action")
            .HasMaxLength(100)
            .IsRequired();

        // TargetUserId - optional, for user-related actions
        builder.Property(e => e.TargetUserId)
            .HasColumnName("target_user_id");

        // TargetEntityId - optional, for non-user actions
        builder.Property(e => e.TargetEntityId)
            .HasColumnName("target_entity_id");

        // TargetEntityType - optional, describes the entity type
        builder.Property(e => e.TargetEntityType)
            .HasColumnName("target_entity_type")
            .HasMaxLength(100);

        // Details - optional, JSON text for before/after state
        builder.Property(e => e.Details)
            .HasColumnName("details")
            .HasColumnType("text");

        // IpAddress - optional, max 50 chars (IPv4/IPv6)
        builder.Property(e => e.IpAddress)
            .HasColumnName("ip_address")
            .HasMaxLength(50);

        // UserAgent - optional, max 500 chars
        builder.Property(e => e.UserAgent)
            .HasColumnName("user_agent")
            .HasMaxLength(500);

        // Audit fields from BaseEntity
        builder.Property(e => e.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired()
            .HasDefaultValueSql("NOW()");

        builder.Property(e => e.UpdatedAt)
            .HasColumnName("updated_at");

        // Performance indexes
        builder.HasIndex(e => e.AdminUserId)
            .HasDatabaseName("IX_AdminAuditLogs_AdminUserId");

        builder.HasIndex(e => e.Action)
            .HasDatabaseName("IX_AdminAuditLogs_Action");

        builder.HasIndex(e => e.TargetUserId)
            .HasDatabaseName("IX_AdminAuditLogs_TargetUserId");

        builder.HasIndex(e => e.CreatedAt)
            .HasDatabaseName("IX_AdminAuditLogs_CreatedAt");

        // Composite index for common queries
        builder.HasIndex(e => new { e.AdminUserId, e.CreatedAt })
            .HasDatabaseName("IX_AdminAuditLogs_Admin_CreatedAt");

        builder.HasIndex(e => new { e.Action, e.CreatedAt })
            .HasDatabaseName("IX_AdminAuditLogs_Action_CreatedAt");

        // Ignore domain events collection (not persisted)
        builder.Ignore(e => e.DomainEvents);
    }
}
