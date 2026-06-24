namespace LankaConnect.Modules.Identity.Contracts;

/// <summary>
/// Cross-module mirror of <c>LankaConnect.Domain.Users.Enums.UserRole</c>.
/// Deliberately duplicated at the Contracts boundary per the W4.4 / W5.4
/// precedent - Contracts must not pull <c>LankaConnect.Domain</c>. Mapping is
/// 1:1 on the underlying byte value, so the projection is a pure cast at the
/// <c>Identity.Application.UserContractMappings</c> seam (added in 4.6.b).
/// </summary>
/// <remarks>
/// Wave 4.6.a (2026-06-24).
/// </remarks>
public enum UserRoleDto : byte
{
    GeneralUser = 1,
    BusinessOwner = 2,
    EventOrganizer = 3,
    EventOrganizerAndBusinessOwner = 4,
    Admin = 5,
    AdminManager = 6,
}
