namespace LankaConnect.Application.Businesses.Common;

public record ServiceDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public decimal Price { get; init; }
    public string Duration { get; init; } = string.Empty;
    public bool IsAvailable { get; init; }
    public Guid BusinessId { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
}