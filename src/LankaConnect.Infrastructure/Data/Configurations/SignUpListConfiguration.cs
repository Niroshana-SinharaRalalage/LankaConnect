using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using LankaConnect.Domain.Events.Entities;
using LankaConnect.Domain.Events.Enums;

namespace LankaConnect.Infrastructure.Data.Configurations;

public class SignUpListConfiguration : IEntityTypeConfiguration<SignUpList>
{
    public void Configure(EntityTypeBuilder<SignUpList> builder)
    {
        builder.HasKey(s => s.Id);

        builder.Property(s => s.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(s => s.Category)
            .HasColumnName("category")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(s => s.Description)
            .HasColumnName("description")
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(s => s.SignUpType)
            .HasColumnName("sign_up_type")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        // New category flags
        builder.Property(s => s.HasMandatoryItems)
            .HasColumnName("has_mandatory_items")
            .HasDefaultValue(false);

        builder.Property(s => s.HasPreferredItems)
            .HasColumnName("has_preferred_items")
            .HasDefaultValue(false);

        builder.Property(s => s.HasSuggestedItems)
            .HasColumnName("has_suggested_items")
            .HasDefaultValue(false);

        // Phase 6A.27: Open items flag - allows users to add their own items
        builder.Property(s => s.HasOpenItems)
            .HasColumnName("has_open_items")
            .HasDefaultValue(false);

        // Store predefined items as JSON array (legacy)
        builder.Property<List<string>>("_predefinedItems")
            .HasColumnName("predefined_items")
            .HasColumnType("jsonb");

        // Shadow property for Event foreign key
        builder.Property<Guid>("EventId")
            .HasColumnName("event_id")
            .IsRequired();

        // Shadow properties for BaseEntity
        builder.Property<DateTime>("CreatedAt")
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property<DateTime?>("UpdatedAt")
            .HasColumnName("updated_at");

        // Configure relationship to SignUpCommitments (legacy)
        builder.HasMany(s => s.Commitments)
            .WithOne()
            .HasForeignKey("SignUpListId")
            .OnDelete(DeleteBehavior.Cascade);

        // Configure relationship to SignUpItems (new category-based model)
        builder.HasMany(s => s.Items)
            .WithOne()
            .HasForeignKey(i => i.SignUpListId)
            .OnDelete(DeleteBehavior.Cascade);

        // CRITICAL: Use backing field "_items" for EF Core change tracking
        builder.Navigation(s => s.Items)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        // Indexes
        builder.HasIndex("EventId")
            .HasDatabaseName("ix_sign_up_lists_event_id");

        builder.HasIndex(s => s.Category)
            .HasDatabaseName("ix_sign_up_lists_category");
    }
}
