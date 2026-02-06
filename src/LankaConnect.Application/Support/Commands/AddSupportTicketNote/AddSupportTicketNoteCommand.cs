using LankaConnect.Application.Common.Interfaces;

namespace LankaConnect.Application.Support.Commands.AddSupportTicketNote;

/// <summary>
/// Command to add an internal note to a support ticket (admin only, not visible to submitter)
/// Phase 6A.90: Support/Feedback System
/// </summary>
public record AddSupportTicketNoteCommand : ICommand
{
    public Guid TicketId { get; init; }
    public string Content { get; init; } = string.Empty;
    public string? IpAddress { get; init; }
    public string? UserAgent { get; init; }

    public AddSupportTicketNoteCommand(Guid ticketId, string content, string? ipAddress = null, string? userAgent = null)
    {
        TicketId = ticketId;
        Content = content;
        IpAddress = ipAddress;
        UserAgent = userAgent;
    }
}
