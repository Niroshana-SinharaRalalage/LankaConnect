using LankaConnect.BuildingBlocks.Domain;
using LankaConnect.Modules.Identity.Domain.ValueObjects;

namespace LankaConnect.Modules.Identity.Domain.DomainEvents;

/// <summary>
/// Domain event raised when user's language preferences are updated
/// Architecture: Always raised when languages are updated (1-5 required)
/// </summary>
public sealed record LanguagesUpdatedEvent(
    Guid UserId,
    IReadOnlyCollection<LanguagePreference> Languages
) : DomainEvent;
