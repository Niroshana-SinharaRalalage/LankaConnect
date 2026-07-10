using LankaConnect.BuildingBlocks.Application.Common;
using LankaConnect.BuildingBlocks.Domain;
using MediatR;
namespace LankaConnect.Products.LankaEvents.Application.Commands.SendEventNotification;

/// <summary>
/// Phase 6A.61: Command to send manual event notification email to all attendees
/// Triggered by "Send Email" button in Communication tab
/// </summary>
public record SendEventNotificationCommand(Guid EventId) : IRequest<Result<int>>;
