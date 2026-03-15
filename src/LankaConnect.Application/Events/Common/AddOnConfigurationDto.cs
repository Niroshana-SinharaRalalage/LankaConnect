namespace LankaConnect.Application.Events.Common;

public record AddOnConfigurationDto
{
    public bool IsEnabled { get; init; }
    public bool AvailableDuringRegistration { get; init; }
    public bool AvailableStandalone { get; init; }
    public string? AddOnMessage { get; init; }
}
