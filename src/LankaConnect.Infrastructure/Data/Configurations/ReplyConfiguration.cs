using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using LankaConnect.Domain.Community;
using LankaConnect.Domain.Community.ValueObjects;

namespace LankaConnect.Infrastructure.Data.Configurations;

public class ReplyConfiguration : IEntityTypeConfiguration<Reply>
{
    public void Configure(EntityTypeBuilder<Reply> builder)
    {
        builder.HasKey(r => r.Id);
        
        builder.Property(r => r.Id)
            .ValueGeneratedNever();

        // Configure foreign keys
        builder.Property(r => r.TopicId)
            .IsRequired();

        builder.Property(r => r.AuthorId)
            .IsRequired();

        builder.Property(r => r.ParentReplyId);

        // Configure PostContent value object
        builder.OwnsOne(r => r.Content, content =>
        {
            content.Property(c => c.Value)
                .HasColumnName("content")
                .HasMaxLength(10000)
                .IsRequired();
        });

        // Configure properties
        builder.Property(r => r.HelpfulVotes)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(r => r.IsMarkedAsSolution)
            .IsRequired()
            .HasDefaultValue(false);

        // Configure audit fields
        builder.Property(r => r.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("NOW()");

        builder.Property(r => r.UpdatedAt);

        // Configure self-referencing relationship for threaded replies
        builder.HasOne<Reply>()
            .WithMany()
            .HasForeignKey(r => r.ParentReplyId)
            .OnDelete(DeleteBehavior.Restrict); // Prevent cascade delete loops

        // Configure indexes
        builder.HasIndex(r => r.TopicId)
            .HasDatabaseName("ix_replies_topic_id");

        builder.HasIndex(r => r.AuthorId)
            .HasDatabaseName("ix_replies_author_id");

        builder.HasIndex(r => r.ParentReplyId)
            .HasDatabaseName("ix_replies_parent_id");

        builder.HasIndex(r => new { r.TopicId, r.CreatedAt })
            .HasDatabaseName("ix_replies_topic_created");

        builder.HasIndex(r => new { r.IsMarkedAsSolution, r.TopicId })
            .HasDatabaseName("ix_replies_solution_topic");

        builder.HasIndex(r => r.HelpfulVotes)
            .HasDatabaseName("ix_replies_helpful_votes");
    }
}