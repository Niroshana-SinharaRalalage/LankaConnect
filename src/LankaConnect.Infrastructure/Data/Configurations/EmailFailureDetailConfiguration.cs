using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using LankaConnect.Domain.Communications.Entities;

namespace LankaConnect.Infrastructure.Data.Configurations;

/// <summary>
/// Phase 6A.99: EF Core configuration for EmailFailureDetail entity.
///
/// Table: communications.email_failure_details
/// Purpose: Persist email failure details for admin debugging that survives container restarts
///
/// GDPR Notes:
/// - recipient_email_encrypted column stores AES-256 encrypted email addresses
/// - 30-day retention policy enforced via expires_at column
/// - Background service cleans up expired records daily
/// </summary>
public class EmailFailureDetailConfiguration : IEntityTypeConfiguration<EmailFailureDetail>
{
    public void Configure(EntityTypeBuilder<EmailFailureDetail> builder)
    {
        builder.ToTable("email_failure_details", "communications");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        // Timestamp when failure occurred
        builder.Property(e => e.Timestamp)
            .HasColumnName("timestamp")
            .IsRequired();

        // Template name
        builder.Property(e => e.TemplateName)
            .HasColumnName("template_name")
            .HasMaxLength(100)
            .IsRequired();

        // Encrypted email (longer due to Base64 encoding of encrypted data)
        builder.Property(e => e.RecipientEmailEncrypted)
            .HasColumnName("recipient_email_encrypted")
            .HasMaxLength(500);

        // Error message (can be long, use text type)
        builder.Property(e => e.ErrorMessage)
            .HasColumnName("error_message")
            .HasColumnType("text")
            .IsRequired();

        // Handler name (optional)
        builder.Property(e => e.HandlerName)
            .HasColumnName("handler_name")
            .HasMaxLength(200);

        // Correlation ID for tracing (optional)
        builder.Property(e => e.CorrelationId)
            .HasColumnName("correlation_id")
            .HasMaxLength(100);

        // Expiration date for retention policy
        builder.Property(e => e.ExpiresAt)
            .HasColumnName("expires_at")
            .IsRequired();

        // Audit fields from BaseEntity
        builder.Property(e => e.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired()
            .HasDefaultValueSql("NOW()");

        builder.Property(e => e.UpdatedAt)
            .HasColumnName("updated_at");

        // Performance indexes
        // Primary query: order by timestamp descending for recent failures
        builder.HasIndex(e => e.Timestamp)
            .IsDescending()
            .HasDatabaseName("IX_EmailFailureDetails_Timestamp");

        // Filter by template name
        builder.HasIndex(e => e.TemplateName)
            .HasDatabaseName("IX_EmailFailureDetails_Template");

        // Cleanup query: find expired records
        builder.HasIndex(e => e.ExpiresAt)
            .HasDatabaseName("IX_EmailFailureDetails_ExpiresAt");

        // Correlation ID lookup for tracing
        builder.HasIndex(e => e.CorrelationId)
            .HasDatabaseName("IX_EmailFailureDetails_CorrelationId");

        // Ignore domain events (not persisted)
        builder.Ignore(e => e.DomainEvents);
    }
}