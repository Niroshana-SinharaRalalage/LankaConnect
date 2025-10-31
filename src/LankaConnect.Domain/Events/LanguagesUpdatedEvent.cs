using LankaConnect.Domain.Common;
using LankaConnect.Domain.Users.ValueObjects;

namespace LankaConnect.Domain.Events;

/// <summary>
/// Domain event raised when user's language preferences are updated
/// Architecture: Always raised when languages are updated (1-5 required)
/// </summary>
public sealed record LanguagesUpdatedEvent(
    Guid UserId,
    IReadOnlyCollection<LanguagePreference> Languages
) : DomainEvent;
