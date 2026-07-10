namespace LankaConnect.Modules.Identity.Application.DTOs;
using LankaConnect.Products.LankaEvents.Domain.ValueObjects;

/// <summary>
/// Phase 6A.133: Minimal user DTO for co-organizer search results.
/// Privacy-conscious — only exposes what's needed to identify a user.
/// </summary>
public record UserSearchResultDto
{
    public Guid Id { get; init; }
    public string DisplayName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string? ProfilePhotoUrl { get; init; }
}
