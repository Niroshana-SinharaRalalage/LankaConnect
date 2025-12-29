using System.Security.Cryptography;
using System.Text;
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

        // Phase 6A.47 Part 2: Only seed configurable reference data
        // Code enums (EventStatus, UserRole) are kept in code for type safety
        SeedEventCategories(builder);
        // EventStatus and UserRole removed - kept as code enums only
    }

    private void SeedEventCategories(EntityTypeBuilder<ReferenceValue> builder)
    {
        var categories = new[]
        {
            ReferenceValue.Create(GenerateDeterministicGuid("EventCategory", "Religious"), "EventCategory", "Religious", 0, "Religious", 1, metadata: new Dictionary<string, object> { ["iconUrl"] = "" }),
            ReferenceValue.Create(GenerateDeterministicGuid("EventCategory", "Cultural"), "EventCategory", "Cultural", 1, "Cultural", 2, metadata: new Dictionary<string, object> { ["iconUrl"] = "" }),
            ReferenceValue.Create(GenerateDeterministicGuid("EventCategory", "Community"), "EventCategory", "Community", 2, "Community", 3, metadata: new Dictionary<string, object> { ["iconUrl"] = "" }),
            ReferenceValue.Create(GenerateDeterministicGuid("EventCategory", "Educational"), "EventCategory", "Educational", 3, "Educational", 4, metadata: new Dictionary<string, object> { ["iconUrl"] = "" }),
            ReferenceValue.Create(GenerateDeterministicGuid("EventCategory", "Social"), "EventCategory", "Social", 4, "Social", 5, metadata: new Dictionary<string, object> { ["iconUrl"] = "" }),
            ReferenceValue.Create(GenerateDeterministicGuid("EventCategory", "Business"), "EventCategory", "Business", 5, "Business", 6, metadata: new Dictionary<string, object> { ["iconUrl"] = "" }),
            ReferenceValue.Create(GenerateDeterministicGuid("EventCategory", "Charity"), "EventCategory", "Charity", 6, "Charity", 7, metadata: new Dictionary<string, object> { ["iconUrl"] = "" }),
            ReferenceValue.Create(GenerateDeterministicGuid("EventCategory", "Entertainment"), "EventCategory", "Entertainment", 7, "Entertainment", 8, metadata: new Dictionary<string, object> { ["iconUrl"] = "" }),
            // Phase 6A.47 Part 1: Add 4 new EventCategory values from EventType
            ReferenceValue.Create(GenerateDeterministicGuid("EventCategory", "Workshop"), "EventCategory", "Workshop", 8, "Workshop", 9, metadata: new Dictionary<string, object> { ["iconUrl"] = "" }),
            ReferenceValue.Create(GenerateDeterministicGuid("EventCategory", "Festival"), "EventCategory", "Festival", 9, "Festival", 10, metadata: new Dictionary<string, object> { ["iconUrl"] = "" }),
            ReferenceValue.Create(GenerateDeterministicGuid("EventCategory", "Ceremony"), "EventCategory", "Ceremony", 10, "Ceremony", 11, metadata: new Dictionary<string, object> { ["iconUrl"] = "" }),
            ReferenceValue.Create(GenerateDeterministicGuid("EventCategory", "Celebration"), "EventCategory", "Celebration", 11, "Celebration", 12, metadata: new Dictionary<string, object> { ["iconUrl"] = "" })
        };

        builder.HasData(categories);
    }

    // Phase 6A.47 Part 2: EventStatus and UserRole removed from seed data
    // These enums are kept in code for type safety (state machines, authorization)
    // Frontend should NOT use these in dropdowns - they are backend-only enums

    /// <summary>
    /// Generates a deterministic GUID based on enum type and code
    /// This ensures seed data GUIDs remain stable across migrations
    /// Phase 6A.47: Fix for EF Core GUID regeneration issue
    /// </summary>
    private static Guid GenerateDeterministicGuid(string enumType, string code)
    {
        var input = $"LankaConnect.ReferenceData.{enumType}.{code}";
        var hash = MD5.HashData(Encoding.UTF8.GetBytes(input));
        return new Guid(hash);
    }
}
