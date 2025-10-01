namespace LankaConnect.Domain.Communications.Enums;

/// <summary>
/// Calendar systems for multi-cultural South Asian diaspora communities
/// Supports diverse calendar calculations for global cultural expansion
/// </summary>
public enum CalendarSystem
{
    // Existing Sri Lankan Systems
    SriLankanBuddhist = 1,
    SriLankanTamil = 2,
    
    // Indian Calendar Systems
    HinduLunar = 10,
    HinduSolar = 11,
    HinduLunisolar = 12,
    BengaliCalendar = 13,
    TamilCalendar = 14,
    malayalamCalendar = 15,
    TeluguCalendar = 16,
    KannadaCalendar = 17,
    GujaratiCalendar = 18,
    MarathiCalendar = 19,
    
    // Islamic Calendar Systems
    IslamicHijri = 20,
    IslamicCalendar = 21, // Missing property causing CS1061 error
    IslamicPakistani = 22,
    IslamicBangladeshi = 23,
    
    // Sikh Calendar System
    NanakshahiCalendar = 30,
    
    // Other South Asian Systems
    NepaleseVikramSambat = 40,
    BhutaneseBhutaneseCalendar = 41,
    
    // Western Integration
    GregorianCalendar = 50,
    
    // Multi-Cultural Hybrid
    DiasporaHybrid = 60
}