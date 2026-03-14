using LankaConnect.Application.Common.Interfaces;

namespace LankaConnect.Application.Events.Commands.UpdateEventOrganizerContact;

public record UpdateEventOrganizerContactCommand(
    Guid EventId,
    bool PublishOrganizerContact,
    List<OrganizerContactRequest> Contacts
) : ICommand;

/// <summary>
/// Request model for a single organizer contact
/// </summary>
public record OrganizerContactRequest(
    string ContactName,
    string? ContactEmail = null,
    string? ContactPhone = null,
    bool IsPrimary = false,
    Guid? LinkedUserId = null);
