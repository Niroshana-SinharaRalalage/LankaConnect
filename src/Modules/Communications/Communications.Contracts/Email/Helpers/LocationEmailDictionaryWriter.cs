using LankaConnect.Shared.Email.Contracts;

namespace LankaConnect.Shared.Email.Helpers;

/// <summary>
/// Phase 7C.2: Writes the 8 decomposed Phase 7C.2 location keys (plus the legacy
/// <c>EventLocation</c> fallback) into a template parameter dictionary.
///
/// Single writer so every template-specific <c>*EmailParams.ToDictionary()</c> emits
/// the exact same key set. If a new location key is needed, add it here once instead
/// of touching ~13 classes.
/// </summary>
public static class LocationEmailDictionaryWriter
{
    /// <summary>
    /// Writes the 8 decomposed keys AND the legacy <c>EventLocation</c> key from a
    /// <see cref="LocationEmailProjection"/> into <paramref name="dict"/>. Overwrites
    /// any existing value for those keys so callers don't have to clear them first.
    /// </summary>
    public static void WriteTo(IDictionary<string, object> dict, LocationEmailProjection projection)
    {
        if (dict == null)
            throw new ArgumentNullException(nameof(dict));
        if (projection == null)
            throw new ArgumentNullException(nameof(projection));

        dict[EmailTemplateContract.Event.LocationName] = projection.LocationName;
        dict[EmailTemplateContract.Event.LocationAddress] = projection.LocationAddress;
        dict[EmailTemplateContract.Event.HasLocationName] = projection.HasLocationName;
        dict[EmailTemplateContract.Event.HasSecondaryLocation] = projection.HasSecondaryLocation;
        dict[EmailTemplateContract.Event.SecondaryLocationLabel] = projection.SecondaryLocationLabel;
        dict[EmailTemplateContract.Event.SecondaryLocationName] = projection.SecondaryLocationName;
        dict[EmailTemplateContract.Event.HasSecondaryLocationName] = projection.HasSecondaryLocationName;
        dict[EmailTemplateContract.Event.SecondaryLocationAddress] = projection.SecondaryLocationAddress;
        dict[EmailTemplateContract.Event.EventLocation] = projection.LegacyFlatString;
    }
}
