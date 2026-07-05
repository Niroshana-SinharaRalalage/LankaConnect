namespace LankaConnect.SharedKernel.Cultural.Enums;

/// <summary>
/// Narrow Sri Lankan operational language set (Sinhala/Tamil/English).
/// Used for cultural email optimization, in-app UI selection for SL users,
/// and any flow that needs to know "which of the 3 official SL languages
/// is this user's primary".
/// </summary>
/// <remarks>
/// Distinct from <c>SouthAsianLanguage</c> (broad 19+-value diaspora
/// routing set) — see ADR-008 + cultural-type-inventory.md §B.6 for the
/// narrow-vs-broad rationale. Both live in SharedKernel.Cultural.
/// </remarks>
public enum SriLankanLanguage
{
    Sinhala = 1,
    Tamil = 2,
    English = 3
}