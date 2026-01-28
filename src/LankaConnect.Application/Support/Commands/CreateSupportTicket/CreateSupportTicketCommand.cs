using LankaConnect.Application.Common.Interfaces;

namespace LankaConnect.Application.Support.Commands.CreateSupportTicket;

/// <summary>
/// Command to create a new support ticket from contact form submission
/// Phase 6A.90: Support/Feedback System
/// </summary>
public record CreateSupportTicketCommand : ICommand<Guid>
{
    public string Name { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string Subject { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
}
