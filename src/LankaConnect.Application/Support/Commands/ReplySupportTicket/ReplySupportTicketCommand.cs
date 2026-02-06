using LankaConnect.Application.Common.Interfaces;

namespace LankaConnect.Application.Support.Commands.ReplySupportTicket;

/// <summary>
/// Command to reply to a support ticket (admin action)
/// Phase 6A.90: Support/Feedback System
/// </summary>
public record ReplySupportTicketCommand : ICommand
{
    public Guid TicketId { get; init; }
    public string Content { get; init; } = string.Empty;
    public string? IpAddress { get; init; }
    public string? UserAgent { get; init; }

    public ReplySupportTicketCommand(Guid ticketId, string content, string? ipAddress = null, string? userAgent = null)
    {
        TicketId = ticketId;
        Content = content;
        IpAddress = ipAddress;
        UserAgent = userAgent;
    }
}
