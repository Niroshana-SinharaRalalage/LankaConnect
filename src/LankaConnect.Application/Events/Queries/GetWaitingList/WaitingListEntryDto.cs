namespace LankaConnect.Application.Events.Queries.GetWaitingList;

/// <summary>
/// DTO for waiting list entry with user position
/// </summary>
public record WaitingListEntryDto
{
    public Guid UserId { get; init; }
    public int Position { get; init; }
    public DateTime JoinedAt { get; init; }
}
