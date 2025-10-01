using LankaConnect.Domain.Business.Enums;

namespace LankaConnect.Application.Businesses.Common;

public record BusinessDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string ContactPhone { get; init; } = string.Empty;
    public string ContactEmail { get; init; } = string.Empty;
    public string Website { get; init; } = string.Empty;
    public string Address { get; init; } = string.Empty;
    public string City { get; init; } = string.Empty;
    public string Province { get; init; } = string.Empty;
    public string PostalCode { get; init; } = string.Empty;
    public double Latitude { get; init; }
    public double Longitude { get; init; }
    public BusinessCategory Category { get; init; }
    public BusinessStatus Status { get; init; }
    public decimal? Rating { get; init; }
    public int ReviewCount { get; init; }
    public bool IsVerified { get; init; }
    public DateTime? VerifiedAt { get; init; }
    public Guid OwnerId { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
    public List<string> Categories { get; init; } = new();
    public List<string> Tags { get; init; } = new();
}