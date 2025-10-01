using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using LankaConnect.Domain.Community;
using LankaConnect.Domain.Community.ValueObjects;
using LankaConnect.Domain.Community.Enums;

namespace LankaConnect.Infrastructure.Data.Configurations;

public class ForumTopicConfiguration : IEntityTypeConfiguration<ForumTopic>
{
    public void Configure(EntityTypeBuilder<ForumTopic> builder)
    {
        builder.HasKey(t => t.Id);
        
        builder.Property(t => t.Id)
            .ValueGeneratedNever();

        // Configure ForumTitle value object
        builder.OwnsOne(t => t.Title, title =>
        {
            title.Property(tt => tt.Value)
                .HasColumnName("title")
                .HasMaxLength(100)
                .IsRequired();
        });

        // Configure PostContent value object
        builder.OwnsOne(t => t.Content, content =>
        {
            content.Property(c => c.Value)
                .HasColumnName("content")
                .HasMaxLength(10000)
                .IsRequired();
        });

        // Configure properties
        builder.Property(t => t.AuthorId)
            .IsRequired();

        builder.Property(t => t.ForumId)
            .IsRequired();

        // Configure enums
        builder.Property(t => t.Category)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(t => t.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired()
            .HasDefaultValue(TopicStatus.Active);

        // Configure boolean properties
        builder.Property(t => t.IsPinned)
            .IsRequired()
            .HasDefaultValue(false);

        // Configure numeric properties
        builder.Property(t => t.ViewCount)
            .IsRequired()
            .HasDefaultValue(0);

        // Configure optional properties
        builder.Property(t => t.LockReason)
            .HasMaxLength(500);

        // Configure audit fields
        builder.Property(t => t.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("NOW()");

        builder.Property(t => t.UpdatedAt);

        // Configure relationships
        builder.HasMany(t => t.Replies)
            .WithOne()
            .HasForeignKey("TopicId")
            .OnDelete(DeleteBehavior.Cascade);

        // Configure indexes
        builder.HasIndex(t => t.ForumId)
            .HasDatabaseName("ix_topics_forum_id");

        builder.HasIndex(t => t.AuthorId)
            .HasDatabaseName("ix_topics_author_id");

        builder.HasIndex(t => t.Category)
            .HasDatabaseName("ix_topics_category");

        builder.HasIndex(t => t.Status)
            .HasDatabaseName("ix_topics_status");

        builder.HasIndex(t => new { t.ForumId, t.Status, t.UpdatedAt })
            .HasDatabaseName("ix_topics_forum_status_updated");

        builder.HasIndex(t => new { t.IsPinned, t.UpdatedAt })
            .HasDatabaseName("ix_topics_pinned_updated");

        // Note: Full text search would be implemented using PostgreSQL specific features
        // This would require raw SQL or database-specific extensions
    }
}