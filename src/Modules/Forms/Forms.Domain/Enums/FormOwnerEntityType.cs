namespace LankaConnect.Modules.Forms.Domain.Enums;

/// <summary>
/// Polymorphic discriminator for what kind of entity owns this Form.
///
/// Introduced by Wave 5.2b (2026-06-11) to support Forms being owned by entities
/// other than Events (e.g. future Marketplace listings, Newsletters, etc.). Today
/// only <see cref="Event"/> is a valid value; the persistence column lands in
/// Wave 5.2c (EF migration). Wave 5.2d drops the legacy <c>event_id</c> column
/// after staging soak.
///
/// Persisted as <c>text</c> via <c>HasConversion&lt;string&gt;()</c> for
/// human-readable backfill SQL (<c>UPDATE forms.event_forms SET owner_entity_type = 'Event'</c>).
/// </summary>
public enum FormOwnerEntityType
{
    /// <summary>
    /// Form is owned by an Event. <see cref="LankaConnect.Modules.Forms.Domain.Form.OwnerEntityId"/>
    /// holds the Event's primary key.
    /// </summary>
    Event = 1,
}
