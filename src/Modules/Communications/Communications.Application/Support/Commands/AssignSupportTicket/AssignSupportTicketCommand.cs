using LankaConnect.BuildingBlocks.Application.Common.Interfaces;
namespace LankaConnect.Modules.Communications.Application.Support.Commands.AssignSupportTicket;

/// <summary>
/// Command to assign support ticket to an admin user
/// Phase 6A.90: Support/Feedback System
/// </summary>
public record AssignSupportTicketCommand : ICommand
{
    public Guid TicketId { get; init; }
    public Guid AssignToUserId { get; init; }
    public string? IpAddress { get; init; }
    public string? UserAgent { get; init; }

    public AssignSupportTicketCommand(Guid ticketId, Guid assignToUserId, string? ipAddress = null, string? userAgent = null)
    {
        TicketId = ticketId;
        AssignToUserId = assignToUserId;
        IpAddress = ipAddress;
        UserAgent = userAgent;
    }
}
