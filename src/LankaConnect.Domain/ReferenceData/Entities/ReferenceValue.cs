using System.Text.Json;
using LankaConnect.Domain.Common.Entities;

namespace LankaConnect.Domain.ReferenceData.Entities;

/// <summary>
/// Unified reference data entity for all enum types
/// Stores enum values with flexible JSONB metadata for type-specific properties
/// Phase 6A.47: Unified Reference Data Architecture
/// </summary>
public class ReferenceValue : EntityBase
{
    /// <summary>
    /// Type of enum (e.g., 'EventCategory', 'EventStatus', 'UserRole')
    /// </summary>
    public string EnumType { get; private set; } = null!;

    /// <summary>
    /// Enum code value (e.g., 'Religious', 'Draft', 'GeneralUser')
    /// </summary>
    public string Code { get; private set; } = null!;

    /// <summary>
    /// Integer value matching the C# enum for backward compatibility
    /// </summary>
    public int IntValue { get; private set; }

    /// <summary>
    /// Display name for UI
    /// </summary>
    public string Name { get; private set; } = null!;

    /// <summary>
    /// Optional description
    /// </summary>
    public string? Description { get; private set; }

    /// <summary>
    /// Display order for sorting
    /// </summary>
    public int DisplayOrder { get; private set; }

    /// <summary>
    /// Soft delete flag
    /// </summary>
    public bool IsActive { get; private set; } = true;

    /// <summary>
    /// Flexible JSONB metadata for enum-specific properties
    /// Examples:
    /// - EventCategory: {"iconUrl": "..."}
    /// - EventStatus: {"allowsRegistration": true, "isFinalState": false}
    /// - UserRole: {"canManageUsers": false, "monthlySubscriptionPrice": 10.00, ...}
    /// </summary>
    public Dictionary<string, object>? Metadata { get; private set; }

    // EF Core requires parameterless constructor
    private ReferenceValue() { }

    private ReferenceValue(
        Guid id,
        string enumType,
        string code,
        int intValue,
        string name,
        int displayOrder,
        string? description = null,
        Dictionary<string, object>? metadata = null)
        : base(id)
    {
        EnumType = enumType ?? throw new ArgumentNullException(nameof(enumType));
        Code = code ?? throw new ArgumentNullException(nameof(code));
        IntValue = intValue;
        Name = name ?? throw new ArgumentNullException(nameof(name));
        DisplayOrder = displayOrder;
        Description = description;
        Metadata = metadata;
    }

    public static ReferenceValue Create(
        Guid id,
        string enumType,
        string code,
        int intValue,
        string name,
        int displayOrder,
        string? description = null,
        Dictionary<string, object>? metadata = null)
    {
        return new ReferenceValue(id, enumType, code, intValue, name, displayOrder, description, metadata)
        {
            Id = id
        };
    }

    public void Deactivate()
    {
        IsActive = false;
    }

    public void Activate()
    {
        IsActive = true;
    }

    public void UpdateDisplayOrder(int displayOrder)
    {
        DisplayOrder = displayOrder;
    }

    public void UpdateMetadata(Dictionary<string, object>? metadata)
    {
        Metadata = metadata;
    }

    /// <summary>
    /// Gets a metadata value by key with type safety
    /// </summary>
    public T? GetMetadataValue<T>(string key)
    {
        if (Metadata == null || !Metadata.ContainsKey(key))
            return default;

        var value = Metadata[key];

        if (value == null)
            return default;

        // Handle JsonElement for values deserialized from database
        if (value is JsonElement jsonElement)
        {
            return JsonSerializer.Deserialize<T>(jsonElement.GetRawText());
        }

        // Direct conversion for in-memory values
        if (value is T typedValue)
            return typedValue;

        // Try conversion
        try
        {
            return (T)Convert.ChangeType(value, typeof(T));
        }
        catch
        {
            return default;
        }
    }

    /// <summary>
    /// Checks if a metadata boolean flag is true
    /// </summary>
    public bool GetMetadataFlag(string key)
    {
        return GetMetadataValue<bool>(key);
    }

    /// <summary>
    /// Gets a metadata decimal value (for prices, etc.)
    /// </summary>
    public decimal GetMetadataDecimal(string key)
    {
        return GetMetadataValue<decimal>(key);
    }

    /// <summary>
    /// Gets a metadata string value
    /// </summary>
    public string? GetMetadataString(string key)
    {
        return GetMetadataValue<string>(key);
    }
}
