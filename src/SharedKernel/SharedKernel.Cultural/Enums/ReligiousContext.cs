namespace LankaConnect.SharedKernel.Cultural.Enums;

/// <summary>
/// Religious contexts for cultural timing optimization (Buddhist Poyaday,
/// Ramadan, Hindu festivals, etc.). Drives reminder scheduling, communication
/// content selection, and respectful-quiet-period gating across modules.
/// </summary>
/// <remarks>
/// Moved from <c>LankaConnect.Modules.Communications.Domain.Enums</c> to
/// <c>SharedKernel.Cultural</c> in W2C.3 (2026-06-05) per ADR-008.
/// </remarks>
public enum ReligiousContext
{
    None = 0,
    BuddhistPoyaday = 1,
    Ramadan = 2,
    HinduFestival = 3,
    ChristianSabbath = 4,
    VesakDay = 5,
    Deepavali = 6,
    Eid = 7,
    Christmas = 8,
    GeneralReligiousObservance = 9
}