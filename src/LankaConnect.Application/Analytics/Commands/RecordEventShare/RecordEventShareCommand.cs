using LankaConnect.Application.Common.Interfaces;

namespace LankaConnect.Application.Analytics.Commands.RecordEventShare;

/// <summary>
/// Command to record a social share of an event
/// Tracks share count for analytics and engagement metrics
/// </summary>
public record RecordEventShareCommand(
    Guid EventId,
    Guid? UserId = null,
    string? Platform = null
) : ICommand;
