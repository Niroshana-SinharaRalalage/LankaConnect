namespace LankaConnect.Products.LankaEvents.Domain.Enums;

/// <summary>
/// Polymorphic discriminator for a tier assignment's target.
/// A tier can be assigned to a zone (theater-style) or a table (banquet-style);
/// this enum distinguishes which kind of entity the <see cref="Guid"/> AssignableId refers to.
/// </summary>
public enum AssignableKind
{
    /// <summary>Assignment targets a VenueZone (rectangular / curved / polygonal seat area).</summary>
    Zone = 0,

    /// <summary>Assignment targets a VenueTable (round / square / rectangular dining table).</summary>
    Table = 1
}
