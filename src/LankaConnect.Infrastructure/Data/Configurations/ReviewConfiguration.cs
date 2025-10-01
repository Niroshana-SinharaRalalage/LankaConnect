using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using LankaConnect.Domain.Business;
using LankaConnect.Domain.Business.ValueObjects;
using LankaConnect.Domain.Business.Enums;
using System.Text.Json;

namespace LankaConnect.Infrastructure.Data.Configurations;

public class ReviewConfiguration : IEntityTypeConfiguration<Review>
{
    public void Configure(EntityTypeBuilder<Review> builder)
    {
        builder.HasKey(r => r.Id);
        
        builder.Property(r => r.Id)
            .ValueGeneratedNever();

        // Configure Rating value object
        builder.OwnsOne(r => r.Rating, rating =>
        {
            rating.Property(rt => rt.Value)
                .HasColumnName("rating")
                .IsRequired();
        });

        // Configure ReviewContent value object
        builder.OwnsOne(r => r.Content, content =>
        {
            content.Property(c => c.Title)
                .HasColumnName("title")
                .HasMaxLength(200)
                .IsRequired();
                
            content.Property(c => c.Content)
                .HasColumnName("content")
                .HasMaxLength(2000)
                .IsRequired();

            // Store Pros and Cons as JSON arrays
            content.Property(c => c.Pros)
                .HasColumnName("pros")
                .HasConversion(
                    v => v == null ? null : JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => v == null ? null : JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions?)null))
                .HasColumnType("jsonb");

            content.Property(c => c.Cons)
                .HasColumnName("cons")
                .HasConversion(
                    v => v == null ? null : JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => v == null ? null : JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions?)null))
                .HasColumnType("jsonb");
        });

        // Configure enum properties
        builder.Property(r => r.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasDefaultValue(ReviewStatus.Pending);

        // Configure scalar properties
        builder.Property(r => r.BusinessId)
            .IsRequired();
            
        builder.Property(r => r.ReviewerId)
            .IsRequired();
            
        builder.Property(r => r.ApprovedAt);
        
        builder.Property(r => r.ModerationNotes)
            .HasMaxLength(1000);

        // Configure audit fields
        builder.Property(r => r.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("NOW()");

        builder.Property(r => r.UpdatedAt);

        // Configure relationship
        builder.HasOne(r => r.Business)
            .WithMany(b => b.Reviews)
            .HasForeignKey(r => r.BusinessId)
            .OnDelete(DeleteBehavior.Cascade);

        // Configure indexes
        builder.HasIndex(r => r.BusinessId)
            .HasDatabaseName("ix_reviews_business_id");
            
        builder.HasIndex(r => r.ReviewerId)
            .HasDatabaseName("ix_reviews_reviewer_id");
            
        builder.HasIndex(r => r.Status)
            .HasDatabaseName("ix_reviews_status");
            
        builder.HasIndex(r => r.CreatedAt)
            .HasDatabaseName("ix_reviews_created_at");

        // Composite indexes for common queries
        builder.HasIndex(r => new { r.BusinessId, r.Status })
            .HasDatabaseName("ix_reviews_business_status");
            
        builder.HasIndex(r => new { r.ReviewerId, r.Status })
            .HasDatabaseName("ix_reviews_reviewer_status");
            
        builder.HasIndex(r => new { r.BusinessId, r.Status, r.CreatedAt })
            .HasDatabaseName("ix_reviews_business_status_created");

        // Prevent duplicate reviews from same user to same business
        builder.HasIndex(r => new { r.BusinessId, r.ReviewerId })
            .IsUnique()
            .HasDatabaseName("ix_reviews_business_reviewer_unique");

        // Note: Full-text search indexes will be added via custom SQL migration
    }
}