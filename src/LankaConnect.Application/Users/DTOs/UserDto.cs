namespace LankaConnect.Application.Users.DTOs;

public record UserDto
{
    public Guid Id { get; init; }
    public string Email { get; init; } = string.Empty;
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string FullName { get; init; } = string.Empty;
    public string? PhoneNumber { get; init; }
    public string? Bio { get; init; }
    public bool IsActive { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }

    // Epic 1 Phase 3: Profile Enhancement Fields
    public string? ProfilePhotoUrl { get; init; }
    public UserLocationDto? Location { get; init; }
    public List<string> CulturalInterests { get; init; } = new();
    public List<LanguageDto> Languages { get; init; } = new();

    // Phase 5B/6A.9: User Preferred Metro Areas (0-20 GUIDs)
    public List<Guid> PreferredMetroAreas { get; init; } = new();
}