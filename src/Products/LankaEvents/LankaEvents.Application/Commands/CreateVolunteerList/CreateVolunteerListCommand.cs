using LankaConnect.Application.Common.Interfaces;
namespace LankaConnect.Products.LankaEvents.Application.Commands.CreateVolunteerList;

/// <summary>
/// Phase 7D.1: Role-oriented wrapper for creating a volunteer recruitment list.
/// Thin command that maps to SignUpList.CreateVolunteerList — each role is a
/// slot-based item where every slot = 1 volunteer.
/// </summary>
public record CreateVolunteerListCommand(
    Guid EventId,
    string Category,
    string Description,
    List<VolunteerRoleDto> Roles
) : ICommand<Guid>;

/// <summary>
/// A single volunteer role: name, headcount, optional notes.
/// VolunteersNeeded is the total cap for the role; SuggestedPerSlot is the hint
/// shown to signers (default 1 — one volunteer per commitment).
/// </summary>
public record VolunteerRoleDto(
    string RoleName,
    int VolunteersNeeded,
    int? SuggestedPerSlot = null,
    string? Notes = null);
