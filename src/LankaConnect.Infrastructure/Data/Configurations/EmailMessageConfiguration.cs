using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using LankaConnect.Domain.Communications.Entities;
using LankaConnect.Domain.Communications.Enums;
using System.Text.Json;

namespace LankaConnect.Infrastructure.Data.Configurations;

public class EmailMessageConfiguration : IEntityTypeConfiguration<EmailMessage>
{
    public void Configure(EntityTypeBuilder<EmailMessage> builder)
    {
        builder.ToTable("email_messages", "communications");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id)
            .ValueGeneratedNever();

        // Configure FromEmail value object (OwnsOne pattern)
        // Note: No foreign key to Users table - loose coupling approach
        builder.OwnsOne(e => e.FromEmail, fromEmail =>
        {
            fromEmail.Property(f => f.Value)
                .HasColumnName("from_email")
                .HasMaxLength(255)
                .IsRequired();
        });

        // Configure Subject value object (OwnsOne pattern)
        builder.OwnsOne(e => e.Subject, subject =>
        {
            subject.Property(s => s.Value)
                .HasColumnName("subject")
                .HasMaxLength(200)
                .IsRequired();
        });

        // Configure email collections as JSON using backing fields
        builder.Property("_recipients")
            .HasColumnName("to_emails")
            .HasColumnType("jsonb")
            .IsRequired();
            
        builder.Property("_ccRecipients")
            .HasColumnName("cc_emails")
            .HasColumnType("jsonb")
            .IsRequired();
            
        builder.Property("_bccRecipients")
            .HasColumnName("bcc_emails")
            .HasColumnType("jsonb")
            .IsRequired();

        // Content properties
        builder.Property(e => e.TextContent)
            .HasColumnName("text_content")
            .HasColumnType("text")
            .IsRequired();

        builder.Property(e => e.HtmlContent)
            .HasColumnName("html_content")
            .HasColumnType("text");

        // Enum properties with string conversion
        builder.Property(e => e.Type)
            .HasColumnName("type")
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(e => e.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        // Tracking timestamps
        builder.Property(e => e.SentAt)
            .HasColumnName("sent_at")
            .IsRequired(false);

        builder.Property(e => e.DeliveredAt)
            .HasColumnName("delivered_at")
            .IsRequired(false);

        builder.Property(e => e.OpenedAt)
            .HasColumnName("opened_at")
            .IsRequired(false);

        builder.Property(e => e.ClickedAt)
            .HasColumnName("clicked_at")
            .IsRequired(false);

        builder.Property(e => e.FailedAt)
            .HasColumnName("failed_at")
            .IsRequired(false);

        builder.Property(e => e.NextRetryAt)
            .HasColumnName("next_retry_at")
            .IsRequired(false);

        // Error tracking
        builder.Property(e => e.ErrorMessage)
            .HasColumnName("error_message")
            .HasMaxLength(1000);

        builder.Property(e => e.RetryCount)
            .HasColumnName("retry_count")
            .IsRequired();

        builder.Property(e => e.MaxRetries)
            .HasColumnName("max_retries")
            .IsRequired();

        builder.Property(e => e.Priority)
            .HasColumnName("priority")
            .IsRequired();

        // Template properties
        builder.Property(e => e.TemplateName)
            .HasColumnName("template_name")
            .HasMaxLength(100);

        builder.Property(e => e.TemplateData)
            .HasColumnName("template_data")
            .HasColumnType("jsonb")
            .HasConversion(
                v => v != null ? JsonSerializer.Serialize(v, (JsonSerializerOptions?)null) : null,
                v => v != null ? JsonSerializer.Deserialize<Dictionary<string, object>>(v, (JsonSerializerOptions?)null) : null)
            .IsRequired(false);

        // Configure RecipientStatuses as JSON
        builder.Property(e => e.RecipientStatuses)
            .HasColumnName("recipient_statuses")
            .HasColumnType("jsonb")
            .HasConversion(
                v => v != null ? JsonSerializer.Serialize(v, (JsonSerializerOptions?)null) : null,
                v => v != null ? JsonSerializer.Deserialize<Dictionary<string, EmailDeliveryStatus>>(v, (JsonSerializerOptions?)null) : null)
            .IsRequired(false);

        // Configure CulturalContext as JSON (complex value object with multiple properties)
        builder.Property(e => e.CulturalContext)
            .HasColumnName("cultural_context")
            .HasColumnType("jsonb")
            .HasConversion(
                v => v != null ? JsonSerializer.Serialize(v, (JsonSerializerOptions?)null) : null,
                v => v != null ? JsonSerializer.Deserialize<Domain.Communications.ValueObjects.CulturalContext>(v, (JsonSerializerOptions?)null) : null)
            .IsRequired(false);

        // Performance indexes for email queue processing
        builder.HasIndex(e => e.Status)
            .HasDatabaseName("IX_EmailMessages_Status");

        builder.HasIndex(e => new { e.Status, e.NextRetryAt })
            .HasDatabaseName("IX_EmailMessages_Status_NextRetryAt");

        builder.HasIndex(e => e.CreatedAt)
            .HasDatabaseName("IX_EmailMessages_CreatedAt");

        builder.HasIndex(e => e.Type)
            .HasDatabaseName("IX_EmailMessages_Type");

        builder.HasIndex(e => new { e.RetryCount, e.Status })
            .HasDatabaseName("IX_EmailMessages_RetryCount_Status");

        builder.HasIndex(e => e.Priority)
            .HasDatabaseName("IX_EmailMessages_Priority");
    }
}