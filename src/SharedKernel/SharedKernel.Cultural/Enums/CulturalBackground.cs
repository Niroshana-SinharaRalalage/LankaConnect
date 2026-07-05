namespace LankaConnect.SharedKernel.Cultural.Enums;

/// <summary>
/// Cultural and religious backgrounds for Sri Lankan diaspora communities
/// (Sinhala Buddhist, Tamil Hindu, Tamil Sri Lankan, SL Muslim/Christian,
/// Burgher, Malay). Drives email cultural-context selection, in-app content
/// filtering, and respectful timing rules.
/// </summary>
/// <remarks>
/// Moved from <c>LankaConnect.Domain.Communications.Enums</c> to
/// <c>SharedKernel.Cultural</c> in W2C.4 (2026-06-05) per ADR-008.
///
/// This is the CANONICAL CulturalBackground. Two dead/orphan variants were
/// removed at the same time:
/// <list type="bullet">
///   <item>The 7-value generic enum in <c>Shared/CulturalTypes.cs</c> (no callers — dead code)</item>
///   <item>The 15-value broad regional enum in <c>Common/Database/MultiLanguageRoutingModels.cs</c>
///   was renamed to <c>SouthAsianCommunity</c> — it represents a different concept
///   (broad regional community granularity) and shouldn't share this name.</item>
/// </list>
/// </remarks>
public enum CulturalBackground
{
    SinhalaBuddhist = 1,
    TamilHindu = 2,
    TamilSriLankan = 3,
    SriLankanMuslim = 4,
    SriLankanChristian = 5,
    Burgher = 6,
    Malay = 7,
    Other = 8
}