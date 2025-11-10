using LankaConnect.Application.Common.Interfaces;

namespace LankaConnect.Application.Users.Commands.UpdatePreferredMetroAreas;

/// <summary>
/// Command to update user's preferred metro areas (0-10 allowed, optional)
/// Empty list clears all preferences (privacy choice)
/// Phase 5A: User Preferred Metro Areas
/// Architecture: Following ADR-008
/// </summary>
public record UpdateUserPreferredMetroAreasCommand : ICommand
{
    public Guid UserId { get; init; }
    public List<Guid> MetroAreaIds { get; init; } = new();
}
