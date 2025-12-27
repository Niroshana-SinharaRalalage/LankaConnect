namespace LankaConnect.Application.ReferenceData.DTOs;

/// <summary>
/// Unified reference data DTO for all enum types
/// Phase 6A.47: Unified Reference Data Architecture
/// </summary>
public class ReferenceValueDto
{
    public Guid Id { get; set; }
    public string EnumType { get; set; } = null!;
    public string Code { get; set; } = null!;
    public int IntValue { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; }
    public Dictionary<string, object>? Metadata { get; set; }
}
