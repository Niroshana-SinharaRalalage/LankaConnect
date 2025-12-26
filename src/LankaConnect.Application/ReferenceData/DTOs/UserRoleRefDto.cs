namespace LankaConnect.Application.ReferenceData.DTOs;

/// <summary>
/// DTO for User Role reference data
/// Phase 6A.47: Database-driven user roles with business logic flags
/// </summary>
public class UserRoleRefDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; }

    // Business logic flags
    public bool CanManageUsers { get; set; }
    public bool CanCreateEvents { get; set; }
    public bool CanModerateContent { get; set; }
    public bool CanCreateBusinessProfile { get; set; }
    public bool CanCreatePosts { get; set; }
    public bool RequiresSubscription { get; set; }
    public decimal MonthlyPrice { get; set; }
    public bool RequiresApproval { get; set; }
}
