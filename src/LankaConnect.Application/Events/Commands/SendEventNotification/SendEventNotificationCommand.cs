using LankaConnect.Application.Common;
using LankaConnect.Domain.Common;
using MediatR;

namespace LankaConnect.Application.Events.Commands.SendEventNotification;

/// <summary>
/// Phase 6A.61: Command to send manual event notification email to all attendees
/// Triggered by "Send Email" button in Communication tab
/// </summary>
public record SendEventNotificationCommand(Guid EventId) : IRequest<Result<int>>;
