using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using LankaConnect.Domain.Events.Entities;
using LankaConnect.Domain.Events.Enums;

namespace LankaConnect.Infrastructure.Data.Configurations;

public class SignUpListConfiguration : IEntityTypeConfiguration<SignUpList>
{
    public void Configure(EntityTypeBuilder<SignUpList> builder)
    {
        builder.ToTable("sign_up_lists");

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

        // Store predefined items as JSON array
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

        // Configure relationship to SignUpCommitments
        builder.HasMany(s => s.Commitments)
            .WithOne()
            .HasForeignKey("SignUpListId")
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex("EventId")
            .HasDatabaseName("ix_sign_up_lists_event_id");

        builder.HasIndex(s => s.Category)
            .HasDatabaseName("ix_sign_up_lists_category");
    }
}
