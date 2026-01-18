using LankaConnect.Domain.Communications.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LankaConnect.Infrastructure.Data.Configurations;

/// <summary>
/// Phase 6A.74 Part 13 Issue #1: EF Core configuration for NewsletterEmailHistory entity
/// </summary>
public class NewsletterEmailHistoryConfiguration : IEntityTypeConfiguration<NewsletterEmailHistory>
{
    public void Configure(EntityTypeBuilder<NewsletterEmailHistory> builder)
    {
        builder.ToTable("newsletter_email_history", "communications");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasColumnName("id")
            .IsRequired();

        builder.Property(e => e.NewsletterId)
            .HasColumnName("newsletter_id")
            .IsRequired();

        builder.Property(e => e.SentAt)
            .HasColumnName("sent_at")
            .IsRequired();

        builder.Property(e => e.TotalRecipientCount)
            .HasColumnName("total_recipient_count")
            .IsRequired();

        builder.Property(e => e.EmailGroupRecipientCount)
            .HasColumnName("email_group_recipient_count")
            .IsRequired();

        builder.Property(e => e.SubscriberRecipientCount)
            .HasColumnName("subscriber_recipient_count")
            .IsRequired();

        builder.Property(e => e.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(e => e.UpdatedAt)
            .HasColumnName("updated_at");

        // Foreign key relationship to Newsletter
        builder.HasOne(e => e.Newsletter)
            .WithMany()
            .HasForeignKey(e => e.NewsletterId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(e => e.NewsletterId)
            .HasDatabaseName("ix_newsletter_email_history_newsletter_id");

        builder.HasIndex(e => e.SentAt)
            .HasDatabaseName("ix_newsletter_email_history_sent_at");
    }
}
