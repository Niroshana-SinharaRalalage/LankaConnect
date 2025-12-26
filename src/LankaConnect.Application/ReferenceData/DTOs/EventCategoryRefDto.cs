namespace LankaConnect.Application.ReferenceData.DTOs;

/// <summary>
/// DTO for Event Category reference data
/// Phase 6A.47: Database-driven event categories
/// </summary>
public class EventCategoryRefDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public string? IconUrl { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; }
}
