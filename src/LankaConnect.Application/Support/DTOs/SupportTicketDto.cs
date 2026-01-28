namespace LankaConnect.Application.Support.DTOs;

/// <summary>
/// Phase 6A.90: DTO for support ticket list view
/// </summary>
public record SupportTicketDto
{
    public Guid Id { get; init; }
    public string ReferenceId { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string Subject { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public string Priority { get; init; } = string.Empty;
    public Guid? AssignedToUserId { get; init; }
    public string? AssignedToName { get; init; }
    public int ReplyCount { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}

/// <summary>
/// Phase 6A.90: DTO for detailed support ticket view
/// </summary>
public record SupportTicketDetailsDto
{
    public Guid Id { get; init; }
    public string ReferenceId { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string Subject { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public string Priority { get; init; } = string.Empty;
    public Guid? AssignedToUserId { get; init; }
    public string? AssignedToName { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
    public IReadOnlyList<SupportTicketReplyDto> Replies { get; init; } = Array.Empty<SupportTicketReplyDto>();
    public IReadOnlyList<SupportTicketNoteDto> Notes { get; init; } = Array.Empty<SupportTicketNoteDto>();
}

/// <summary>
/// Phase 6A.90: DTO for support ticket reply
/// </summary>
public record SupportTicketReplyDto
{
    public Guid Id { get; init; }
    public string Content { get; init; } = string.Empty;
    public Guid AdminUserId { get; init; }
    public string AdminUserName { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
}

/// <summary>
/// Phase 6A.90: DTO for support ticket internal note
/// </summary>
public record SupportTicketNoteDto
{
    public Guid Id { get; init; }
    public string Content { get; init; } = string.Empty;
    public Guid AdminUserId { get; init; }
    public string AdminUserName { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
}

/// <summary>
/// Phase 6A.90: DTO for support ticket statistics
/// </summary>
public record SupportTicketStatisticsDto
{
    public int TotalTickets { get; init; }
    public int NewTickets { get; init; }
    public int InProgressTickets { get; init; }
    public int WaitingForResponseTickets { get; init; }
    public int ResolvedTickets { get; init; }
    public int ClosedTickets { get; init; }
    public int UnassignedTickets { get; init; }
    public Dictionary<string, int> TicketsByPriority { get; init; } = new();
}
