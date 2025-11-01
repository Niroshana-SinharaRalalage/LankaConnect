namespace LankaConnect.Application.Users.DTOs;

/// <summary>
/// User location data transfer object
/// </summary>
public record UserLocationDto
{
    public string? City { get; init; }
    public string? State { get; init; }
    public string? ZipCode { get; init; }
    public string? Country { get; init; }
}
