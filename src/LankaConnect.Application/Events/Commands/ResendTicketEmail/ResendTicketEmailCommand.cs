using LankaConnect.Application.Common.Interfaces;

namespace LankaConnect.Application.Events.Commands.ResendTicketEmail;

/// <summary>
/// Phase 6A.24: Command to resend ticket email to user
/// Authorization: Only the registration owner can resend their ticket
/// </summary>
public record ResendTicketEmailCommand(
    Guid RegistrationId,
    Guid UserId  // For authorization - only owner can resend
) : ICommand;
