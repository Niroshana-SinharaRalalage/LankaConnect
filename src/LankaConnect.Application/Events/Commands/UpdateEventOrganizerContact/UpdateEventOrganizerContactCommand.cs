using LankaConnect.Application.Common.Interfaces;

namespace LankaConnect.Application.Events.Commands.UpdateEventOrganizerContact;

public record UpdateEventOrganizerContactCommand(
    Guid EventId,
    bool PublishOrganizerContact,
    string? OrganizerContactName = null,
    string? OrganizerContactPhone = null,
    string? OrganizerContactEmail = null
) : ICommand;
