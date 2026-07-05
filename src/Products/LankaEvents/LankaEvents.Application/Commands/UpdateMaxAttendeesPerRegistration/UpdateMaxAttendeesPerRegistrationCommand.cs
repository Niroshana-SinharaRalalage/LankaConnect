using LankaConnect.Application.Common.Interfaces;
namespace LankaConnect.Products.LankaEvents.Application.Commands.UpdateMaxAttendeesPerRegistration;

/// <summary>
/// Issue #51: Command to update max attendees per registration for an event
/// This allows event organizers to configure how many attendees can be added in a single registration
/// </summary>
public record UpdateMaxAttendeesPerRegistrationCommand(Guid EventId, int MaxAttendeesPerRegistration) : ICommand;
