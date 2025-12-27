using System.Text.Json;
using LankaConnect.Domain.ReferenceData.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LankaConnect.Infrastructure.Data.Configurations.ReferenceData;

/// <summary>
/// EF Core configuration for unified ReferenceValue entity
/// Phase 6A.47: Unified Reference Data Architecture
/// </summary>
public class ReferenceValueConfiguration : IEntityTypeConfiguration<ReferenceValue>
{
    public void Configure(EntityTypeBuilder<ReferenceValue> builder)
    {
        builder.ToTable("reference_values", "reference_data");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .ValueGeneratedNever(); // We generate GUIDs in domain

        builder.Property(x => x.EnumType)
            .HasColumnName("enum_type")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.Code)
            .HasColumnName("code")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.IntValue)
            .HasColumnName("int_value")
            .IsRequired();

        builder.Property(x => x.Name)
            .HasColumnName("name")
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(x => x.Description)
            .HasColumnName("description");

        builder.Property(x => x.DisplayOrder)
            .HasColumnName("display_order")
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(x => x.IsActive)
            .HasColumnName("is_active")
            .IsRequired()
            .HasDefaultValue(true);

        // JSONB metadata column with value converter and comparer
        builder.Property(x => x.Metadata)
            .HasColumnName("metadata")
            .HasColumnType("jsonb")
            .HasConversion(
                v => v == null ? null : JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => v == null ? null : JsonSerializer.Deserialize<Dictionary<string, object>>(v, (JsonSerializerOptions?)null))
            .Metadata.SetValueComparer(new ValueComparer<Dictionary<string, object>?>(
                (c1, c2) => JsonSerializer.Serialize(c1, (JsonSerializerOptions?)null) == JsonSerializer.Serialize(c2, (JsonSerializerOptions?)null),
                c => c == null ? 0 : JsonSerializer.Serialize(c, (JsonSerializerOptions?)null).GetHashCode(),
                c => c == null ? null : JsonSerializer.Deserialize<Dictionary<string, object>>(
                    JsonSerializer.Serialize(c, (JsonSerializerOptions?)null), (JsonSerializerOptions?)null)));

        builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired()
            .HasDefaultValueSql("NOW()");

        builder.Property(x => x.UpdatedAt)
            .HasColumnName("updated_at")
            .IsRequired()
            .HasDefaultValueSql("NOW()");

        // Indexes
        builder.HasIndex(x => x.EnumType)
            .HasDatabaseName("idx_reference_values_enum_type");

        builder.HasIndex(x => x.IsActive)
            .HasDatabaseName("idx_reference_values_is_active");

        builder.HasIndex(x => x.DisplayOrder)
            .HasDatabaseName("idx_reference_values_display_order");

        // Unique constraints
        builder.HasIndex(x => new { x.EnumType, x.IntValue })
            .IsUnique()
            .HasDatabaseName("uq_reference_values_type_int_value");

        builder.HasIndex(x => new { x.EnumType, x.Code })
            .IsUnique()
            .HasDatabaseName("uq_reference_values_type_code");

        // Seed data for existing 3 enums
        SeedEventCategories(builder);
        SeedEventStatuses(builder);
        SeedUserRoles(builder);
    }

    private void SeedEventCategories(EntityTypeBuilder<ReferenceValue> builder)
    {
        var categories = new[]
        {
            ReferenceValue.Create(Guid.NewGuid(), "EventCategory", "Religious", 0, "Religious", 1, metadata: new Dictionary<string, object> { ["iconUrl"] = "" }),
            ReferenceValue.Create(Guid.NewGuid(), "EventCategory", "Cultural", 1, "Cultural", 2, metadata: new Dictionary<string, object> { ["iconUrl"] = "" }),
            ReferenceValue.Create(Guid.NewGuid(), "EventCategory", "Community", 2, "Community", 3, metadata: new Dictionary<string, object> { ["iconUrl"] = "" }),
            ReferenceValue.Create(Guid.NewGuid(), "EventCategory", "Educational", 3, "Educational", 4, metadata: new Dictionary<string, object> { ["iconUrl"] = "" }),
            ReferenceValue.Create(Guid.NewGuid(), "EventCategory", "Social", 4, "Social", 5, metadata: new Dictionary<string, object> { ["iconUrl"] = "" }),
            ReferenceValue.Create(Guid.NewGuid(), "EventCategory", "Business", 5, "Business", 6, metadata: new Dictionary<string, object> { ["iconUrl"] = "" }),
            ReferenceValue.Create(Guid.NewGuid(), "EventCategory", "Charity", 6, "Charity", 7, metadata: new Dictionary<string, object> { ["iconUrl"] = "" }),
            ReferenceValue.Create(Guid.NewGuid(), "EventCategory", "Entertainment", 7, "Entertainment", 8, metadata: new Dictionary<string, object> { ["iconUrl"] = "" })
        };

        builder.HasData(categories);
    }

    private void SeedEventStatuses(EntityTypeBuilder<ReferenceValue> builder)
    {
        var statuses = new[]
        {
            ReferenceValue.Create(Guid.NewGuid(), "EventStatus", "Draft", 0, "Draft", 1, metadata: new Dictionary<string, object> { ["allowsRegistration"] = false, ["isFinalState"] = false }),
            ReferenceValue.Create(Guid.NewGuid(), "EventStatus", "Published", 1, "Published", 2, metadata: new Dictionary<string, object> { ["allowsRegistration"] = true, ["isFinalState"] = false }),
            ReferenceValue.Create(Guid.NewGuid(), "EventStatus", "Active", 2, "Active", 3, metadata: new Dictionary<string, object> { ["allowsRegistration"] = true, ["isFinalState"] = false }),
            ReferenceValue.Create(Guid.NewGuid(), "EventStatus", "Postponed", 3, "Postponed", 4, metadata: new Dictionary<string, object> { ["allowsRegistration"] = false, ["isFinalState"] = false }),
            ReferenceValue.Create(Guid.NewGuid(), "EventStatus", "Cancelled", 4, "Cancelled", 5, metadata: new Dictionary<string, object> { ["allowsRegistration"] = false, ["isFinalState"] = true }),
            ReferenceValue.Create(Guid.NewGuid(), "EventStatus", "Completed", 5, "Completed", 6, metadata: new Dictionary<string, object> { ["allowsRegistration"] = false, ["isFinalState"] = true }),
            ReferenceValue.Create(Guid.NewGuid(), "EventStatus", "Archived", 6, "Archived", 7, metadata: new Dictionary<string, object> { ["allowsRegistration"] = false, ["isFinalState"] = true }),
            ReferenceValue.Create(Guid.NewGuid(), "EventStatus", "UnderReview", 7, "Under Review", 8, metadata: new Dictionary<string, object> { ["allowsRegistration"] = false, ["isFinalState"] = false })
        };

        builder.HasData(statuses);
    }

    private void SeedUserRoles(EntityTypeBuilder<ReferenceValue> builder)
    {
        var roles = new[]
        {
            ReferenceValue.Create(Guid.NewGuid(), "UserRole", "GeneralUser", 1, "General User", 1, metadata: new Dictionary<string, object>
            {
                ["canManageUsers"] = false,
                ["canCreateEvents"] = false,
                ["canModerateContent"] = false,
                ["isEventOrganizer"] = false,
                ["isAdmin"] = false,
                ["requiresSubscription"] = false,
                ["canCreateBusinessProfile"] = false,
                ["canCreatePosts"] = false,
                ["monthlySubscriptionPrice"] = 0.00m
            }),
            ReferenceValue.Create(Guid.NewGuid(), "UserRole", "BusinessOwner", 2, "Business Owner", 2, metadata: new Dictionary<string, object>
            {
                ["canManageUsers"] = false,
                ["canCreateEvents"] = false,
                ["canModerateContent"] = false,
                ["isEventOrganizer"] = false,
                ["isAdmin"] = false,
                ["requiresSubscription"] = true,
                ["canCreateBusinessProfile"] = true,
                ["canCreatePosts"] = false,
                ["monthlySubscriptionPrice"] = 10.00m
            }),
            ReferenceValue.Create(Guid.NewGuid(), "UserRole", "EventOrganizer", 3, "Event Organizer", 3, metadata: new Dictionary<string, object>
            {
                ["canManageUsers"] = false,
                ["canCreateEvents"] = true,
                ["canModerateContent"] = false,
                ["isEventOrganizer"] = true,
                ["isAdmin"] = false,
                ["requiresSubscription"] = true,
                ["canCreateBusinessProfile"] = false,
                ["canCreatePosts"] = true,
                ["monthlySubscriptionPrice"] = 10.00m
            }),
            ReferenceValue.Create(Guid.NewGuid(), "UserRole", "EventOrganizerAndBusinessOwner", 4, "Event Organizer + Business Owner", 4, metadata: new Dictionary<string, object>
            {
                ["canManageUsers"] = false,
                ["canCreateEvents"] = true,
                ["canModerateContent"] = false,
                ["isEventOrganizer"] = false,
                ["isAdmin"] = false,
                ["requiresSubscription"] = true,
                ["canCreateBusinessProfile"] = true,
                ["canCreatePosts"] = true,
                ["monthlySubscriptionPrice"] = 15.00m
            }),
            ReferenceValue.Create(Guid.NewGuid(), "UserRole", "Admin", 5, "Administrator", 5, metadata: new Dictionary<string, object>
            {
                ["canManageUsers"] = true,
                ["canCreateEvents"] = true,
                ["canModerateContent"] = true,
                ["isEventOrganizer"] = false,
                ["isAdmin"] = true,
                ["requiresSubscription"] = false,
                ["canCreateBusinessProfile"] = true,
                ["canCreatePosts"] = true,
                ["monthlySubscriptionPrice"] = 0.00m
            }),
            ReferenceValue.Create(Guid.NewGuid(), "UserRole", "AdminManager", 6, "Admin Manager", 6, metadata: new Dictionary<string, object>
            {
                ["canManageUsers"] = true,
                ["canCreateEvents"] = true,
                ["canModerateContent"] = true,
                ["isEventOrganizer"] = false,
                ["isAdmin"] = true,
                ["requiresSubscription"] = false,
                ["canCreateBusinessProfile"] = true,
                ["canCreatePosts"] = true,
                ["monthlySubscriptionPrice"] = 0.00m
            })
        };

        builder.HasData(roles);
    }
}
