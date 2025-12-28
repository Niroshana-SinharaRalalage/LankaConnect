namespace LankaConnect.Application.ReferenceData.DTOs;

/// <summary>
/// DTO for Cultural Interest value object
/// Maps from Domain.Users.ValueObjects.CulturalInterest
/// </summary>
public sealed class CulturalInterestDto
{
    /// <summary>
    /// Cultural interest code (e.g., "SL_CUISINE")
    /// </summary>
    public required string Code { get; init; }

    /// <summary>
    /// Display name (e.g., "Sri Lankan Cuisine")
    /// </summary>
    public required string Name { get; init; }
}
