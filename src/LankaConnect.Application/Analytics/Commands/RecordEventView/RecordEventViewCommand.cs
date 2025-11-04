using LankaConnect.Application.Common.Interfaces;

namespace LankaConnect.Application.Analytics.Commands.RecordEventView;

/// <summary>
/// Command to record a view of an event
/// Supports both authenticated (with UserId) and anonymous (IP-only) views
/// Implements fire-and-forget pattern for non-blocking analytics
/// </summary>
public record RecordEventViewCommand(
    Guid EventId,
    Guid? UserId,
    string IpAddress,
    string? UserAgent = null
) : ICommand;
