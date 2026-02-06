using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using LankaConnect.Domain.Events.Entities;
using LankaConnect.Domain.Events.Enums;

namespace LankaConnect.Infrastructure.Data.Configurations;

public class SignUpItemConfiguration : IEntityTypeConfiguration<SignUpItem>
{
    public void Configure(EntityTypeBuilder<SignUpItem> builder)
    {
        builder.HasKey(si => si.Id);

        builder.Property(si => si.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(si => si.SignUpListId)
            .HasColumnName("sign_up_list_id")
            .IsRequired();

        builder.Property(si => si.ItemDescription)
            .HasColumnName("item_description")
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(si => si.Quantity)
            .HasColumnName("quantity")
            .IsRequired();

        builder.Property(si => si.RemainingQuantity)
            .HasColumnName("remaining_quantity")
            .IsRequired();

        builder.Property(si => si.ItemCategory)
            .HasColumnName("item_category")
            .IsRequired()
            .HasConversion<int>(); // Store as int: 0=Mandatory, 1=Preferred, 2=Suggested, 3=Open

        builder.Property(si => si.Notes)
            .HasColumnName("notes")
            .HasMaxLength(500);

        // Phase 6A.27: User who created Open items (null for organizer-created items)
        builder.Property(si => si.CreatedByUserId)
            .HasColumnName("created_by_user_id")
            .IsRequired(false);

        // Shadow properties for BaseEntity
        builder.Property<DateTime>("CreatedAt")
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property<DateTime?>("UpdatedAt")
            .HasColumnName("updated_at");

        // Configure relationship with SignUpList
        builder.HasOne<SignUpList>()
            .WithMany(sl => sl.Items)
            .HasForeignKey(si => si.SignUpListId)
            .OnDelete(DeleteBehavior.Cascade);

        // Configure relationship with SignUpCommitments
        builder.HasMany(si => si.Commitments)
            .WithOne()
            .HasForeignKey(sc => sc.SignUpItemId)
            .OnDelete(DeleteBehavior.Cascade);

        // Phase 6A.28 Fix: Configure navigation to use backing field "_commitments"
        // CRITICAL: This enables EF Core to populate the private readonly List backing field
        // Without this, EF Core cannot write to IReadOnlyList<> property → commitments stay empty
        // Same pattern successfully used in SignUpListConfiguration.cs line 83-84
        builder.Navigation(si => si.Commitments)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        // Indexes
        builder.HasIndex(si => si.SignUpListId)
            .HasDatabaseName("ix_sign_up_items_list_id");

        builder.HasIndex(si => si.ItemCategory)
            .HasDatabaseName("ix_sign_up_items_category");
    }
}
