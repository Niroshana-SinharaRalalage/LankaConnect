using LankaConnect.Application.Common.Interfaces;

namespace LankaConnect.Products.LankaEvents.Application.Commands.BatchLinkOrganizerContacts;

/// <summary>
/// Phase 6A.133: Batch link registered users to organizer contacts as co-organizers.
/// </summary>
public record BatchLinkOrganizerContactsCommand(
    Guid EventId,
    List<ContactUserLink> Links
) : ICommand;

public record ContactUserLink(Guid ContactId, Guid UserId);
