namespace LankaConnect.Modules.Forms.Contracts;

/// <summary>
/// Contracts mirror of <c>LankaConnect.Modules.Forms.Domain.Enums.FormOwnerEntityType</c>.
/// Introduced by Wave 5.2b (2026-06-11) on the Domain side; mirrored here in
/// Wave 5.3a (2026-06-11) so cross-module callers can name the discriminator
/// without depending on Forms.Domain.
/// </summary>
public enum FormOwnerEntityTypeDto
{
    /// <summary>
    /// Form is owned by an Event. <see cref="FormSummaryDto.OwnerEntityId"/>
    /// holds the Event's primary key.
    /// </summary>
    Event = 1,
}
