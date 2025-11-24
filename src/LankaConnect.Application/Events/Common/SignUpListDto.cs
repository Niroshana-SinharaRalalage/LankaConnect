using LankaConnect.Domain.Events.Enums;

namespace LankaConnect.Application.Events.Common;

public class SignUpListDto
{
    public Guid Id { get; set; }
    public string Category { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public SignUpType SignUpType { get; set; }
    public List<string> PredefinedItems { get; set; } = new();
    public List<SignUpCommitmentDto> Commitments { get; set; } = new();
    public int CommitmentCount { get; set; }
}

public class SignUpCommitmentDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string ItemDescription { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public DateTime CommittedAt { get; set; }
}
