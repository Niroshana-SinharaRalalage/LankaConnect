using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using LankaConnect.Domain.Communications.Entities;

namespace LankaConnect.Infrastructure.Data.Configurations;

/// <summary>
/// EF Core configuration for EmailGroup entity
/// Phase 6A.25: Email Groups Management
/// </summary>
public class EmailGroupConfiguration : IEntityTypeConfiguration<EmailGroup>
{
    public void Configure(EntityTypeBuilder<EmailGroup> builder)
    {
        builder.ToTable("email_groups", "communications");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id)
            .ValueGeneratedNever();

        // Name - required, max 200 chars
        builder.Property(e => e.Name)
            .HasColumnName("name")
            .HasMaxLength(200)
            .IsRequired();

        // Description - optional, max 500 chars
        builder.Property(e => e.Description)
            .HasColumnName("description")
            .HasMaxLength(500);

        // Owner ID - required, links to Users table
        builder.Property(e => e.OwnerId)
            .HasColumnName("owner_id")
            .IsRequired();

        // Email Addresses - stored as TEXT for unlimited comma-separated emails
        builder.Property(e => e.EmailAddresses)
            .HasColumnName("email_addresses")
            .HasColumnType("text")
            .IsRequired();

        // Is Active - for soft delete
        builder.Property(e => e.IsActive)
            .HasColumnName("is_active")
            .IsRequired()
            .HasDefaultValue(true);

        // Audit fields from BaseEntity
        builder.Property(e => e.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired()
            .HasDefaultValueSql("NOW()");

        builder.Property(e => e.UpdatedAt)
            .HasColumnName("updated_at");

        // Indexes for performance
        builder.HasIndex(e => e.OwnerId)
            .HasDatabaseName("IX_EmailGroups_OwnerId");

        // Unique constraint: same owner cannot have two groups with same name
        builder.HasIndex(e => new { e.OwnerId, e.Name })
            .IsUnique()
            .HasDatabaseName("IX_EmailGroups_Owner_Name");

        builder.HasIndex(e => e.IsActive)
            .HasDatabaseName("IX_EmailGroups_IsActive");

        builder.HasIndex(e => new { e.OwnerId, e.IsActive })
            .HasDatabaseName("IX_EmailGroups_Owner_IsActive");

        // Ignore domain events collection (not persisted)
        builder.Ignore(e => e.DomainEvents);
    }
}
