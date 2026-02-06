using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Support.Enums;

namespace LankaConnect.Application.Support.Commands.UpdateSupportTicketStatus;

/// <summary>
/// Command to update support ticket status (admin action)
/// Phase 6A.90: Support/Feedback System
/// </summary>
public record UpdateSupportTicketStatusCommand : ICommand
{
    public Guid TicketId { get; init; }
    public SupportTicketStatus NewStatus { get; init; }
    public string? IpAddress { get; init; }
    public string? UserAgent { get; init; }

    public UpdateSupportTicketStatusCommand(Guid ticketId, SupportTicketStatus newStatus, string? ipAddress = null, string? userAgent = null)
    {
        TicketId = ticketId;
        NewStatus = newStatus;
        IpAddress = ipAddress;
        UserAgent = userAgent;
    }
}
