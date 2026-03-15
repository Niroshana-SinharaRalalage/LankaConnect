using LankaConnect.Application.Common.Interfaces;

namespace LankaConnect.Application.Events.Commands.UpdateAddOnConfig;

/// <summary>
/// Updates the add-on configuration for an event.
/// Organizer-facing command to enable/disable and configure add-on settings.
/// </summary>
public record UpdateAddOnConfigCommand(
    Guid EventId,
    bool IsEnabled,
    bool AvailableDuringRegistration,
    bool AvailableStandalone,
    string? AddOnMessage
) : ICommand;
