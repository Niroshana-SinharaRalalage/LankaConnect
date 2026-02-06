using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using LankaConnect.Domain.ReferenceData.Entities;

namespace LankaConnect.Infrastructure.Data.Configurations.ReferenceData;

/// <summary>
/// EF Core configuration for UserRoleRef entity
/// Maps to reference_data.user_roles table
/// Phase 6A.47: Reference data migration from hardcoded enums
/// </summary>
public class UserRoleRefConfiguration : IEntityTypeConfiguration<UserRoleRef>
{
    public void Configure(EntityTypeBuilder<UserRoleRef> builder)
    {
        builder.ToTable("user_roles", "reference_data");

        builder.HasKey(u => u.Id);
        builder.Property(u => u.Id)
            .HasColumnName("id")
            .ValueGeneratedNever(); // UUIDs are generated externally

        builder.Property(u => u.Code)
            .HasColumnName("code")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(u => u.Name)
            .HasColumnName("name")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(u => u.Description)
            .HasColumnName("description")
            .HasMaxLength(500)
            .IsRequired(false);

        builder.Property(u => u.DisplayOrder)
            .HasColumnName("display_order")
            .IsRequired();

        builder.Property(u => u.IsActive)
            .HasColumnName("is_active")
            .IsRequired()
            .HasDefaultValue(true);

        // Business logic flags
        builder.Property(u => u.CanManageUsers)
            .HasColumnName("can_manage_users")
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(u => u.CanCreateEvents)
            .HasColumnName("can_create_events")
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(u => u.CanModerateContent)
            .HasColumnName("can_moderate_content")
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(u => u.CanCreateBusinessProfile)
            .HasColumnName("can_create_business_profile")
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(u => u.CanCreatePosts)
            .HasColumnName("can_create_posts")
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(u => u.RequiresSubscription)
            .HasColumnName("requires_subscription")
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(u => u.MonthlyPrice)
            .HasColumnName("monthly_price")
            .HasPrecision(10, 2)
            .IsRequired()
            .HasDefaultValue(0.00m);

        builder.Property(u => u.RequiresApproval)
            .HasColumnName("requires_approval")
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(u => u.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(u => u.UpdatedAt)
            .HasColumnName("updated_at")
            .IsRequired(false);

        // Indexes
        builder.HasIndex(u => u.Code)
            .IsUnique()
            .HasDatabaseName("idx_user_roles_code");

        builder.HasIndex(u => u.IsActive)
            .HasDatabaseName("idx_user_roles_is_active");

        builder.HasIndex(u => u.DisplayOrder)
            .HasDatabaseName("idx_user_roles_display_order");

        builder.HasIndex(u => u.CanCreateEvents)
            .HasDatabaseName("idx_user_roles_can_create_events");

        // Ignore domain events
        builder.Ignore(u => u.DomainEvents);
    }
}
