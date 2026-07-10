using System.Security.Claims;

namespace LankaConnect.API.Extensions;

// Day 4 slot C sub-slice 4C.d.vi (2026-07-06): local copy of GetUserId
// extension. Duplicate of LankaConnect.API.Extensions.ClaimsPrincipalExtensions;
// Media.Api cannot ProjectReference LankaConnect.API (cycle - Host references
// Media.Api). Post-sprint: move this extension to BuildingBlocks.Web +
// delete both copies.
internal static class ClaimsPrincipalExtensions
{
    internal static Guid GetUserId(this ClaimsPrincipal user)
    {
        var raw = user.FindFirstValue(ClaimTypes.NameIdentifier)
                  ?? user.FindFirstValue("sub");
        return Guid.TryParse(raw, out var id) ? id : Guid.Empty;
    }

    internal static Guid? TryGetUserId(this ClaimsPrincipal user)
    {
        var raw = user.FindFirstValue(ClaimTypes.NameIdentifier)
                  ?? user.FindFirstValue("sub");
        return Guid.TryParse(raw, out var id) ? id : null;
    }
}
