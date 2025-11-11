using LankaConnect.Application.Common.Interfaces;

namespace LankaConnect.Application.Users.Commands.UpdatePreferredMetroAreas;

/// <summary>
/// Command to update user's preferred metro areas (0-20 allowed, optional)
/// Empty list clears all preferences (privacy choice)
/// Phase 5B: User Preferred Metro Areas - Expanded to 20 max limit
/// Architecture: Following ADR-008
/// </summary>
public record UpdateUserPreferredMetroAreasCommand : ICommand
{
    public Guid UserId { get; init; }
    public List<Guid> MetroAreaIds { get; init; } = new();
}
