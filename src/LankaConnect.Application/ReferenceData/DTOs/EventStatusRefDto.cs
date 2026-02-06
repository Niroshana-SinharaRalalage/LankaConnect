namespace LankaConnect.Application.ReferenceData.DTOs;

/// <summary>
/// DTO for Event Status reference data
/// Phase 6A.47: Database-driven event statuses
/// </summary>
public class EventStatusRefDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; }
    public bool AllowsRegistration { get; set; }
    public bool IsFinalState { get; set; }
}
