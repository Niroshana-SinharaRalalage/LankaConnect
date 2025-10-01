namespace LankaConnect.Domain.Common.Enums;

/// <summary>
/// Consolidated cultural event types with traffic predictions and cultural significance.
/// Architect recommendation: Rich domain models with strong typing for cultural contexts
/// </summary>
public enum CulturalEventType
{
    /// <summary>
    /// No specific cultural event type
    /// </summary>
    None = 0,

    /// <summary>
    /// Vesak Day - Most sacred Buddhist observance (5x traffic multiplier, 95% prediction accuracy)
    /// </summary>
    VesakDayBuddhist = 1,

    /// <summary>
    /// Diwali - Major Hindu festival of lights (4.5x traffic multiplier, 90% prediction accuracy)
    /// </summary>
    DiwaliHindu = 2,

    /// <summary>
    /// Eid al-Fitr - Sacred Islamic celebration (4x traffic multiplier, 88% prediction accuracy with lunar variation)
    /// </summary>
    EidAlFitrIslamic = 3,

    /// <summary>
    /// Eid al-Adha - Islamic festival of sacrifice
    /// </summary>
    EidAlAdhaIslamic = 4,

    /// <summary>
    /// Guru Nanak Jayanti - Sikh founder's birthday (3.5x traffic multiplier)
    /// </summary>
    GuruNanakJayanti = 5,

    /// <summary>
    /// Thaipusam - Tamil Hindu festival (3x traffic multiplier for Tamil communities)
    /// </summary>
    ThaipusamTamil = 6,

    /// <summary>
    /// Chinese New Year - Major celebration for Chinese diaspora (3.5x traffic multiplier)
    /// </summary>
    ChineseNewYear = 7,

    /// <summary>
    /// Sinhala and Tamil New Year - National celebration (4x traffic multiplier in Sri Lanka)
    /// </summary>
    SinhalaNewYear = 8,

    /// <summary>
    /// Holi - Hindu festival of colors (2.5x traffic multiplier)
    /// </summary>
    Holi = 9,

    /// <summary>
    /// Navaratri - Nine-night Hindu festival (2x traffic multiplier)
    /// </summary>
    Navaratri = 10,

    /// <summary>
    /// Poson Poya - Commemoration of Buddhism's arrival in Sri Lanka
    /// </summary>
    PosonPoya = 11,

    /// <summary>
    /// Esala Perahera - Sacred tooth relic procession in Kandy
    /// </summary>
    EsalaPerahera = 12,

    /// <summary>
    /// Christmas - Christian celebration (2.5x traffic multiplier)
    /// </summary>
    Christmas = 13,

    /// <summary>
    /// Good Friday - Christian observance
    /// </summary>
    GoodFriday = 14,

    /// <summary>
    /// Tamil Heritage Month - Cultural celebration period
    /// </summary>
    TamilHeritageMonth = 15,

    /// <summary>
    /// Maha Shivaratri - Hindu festival dedicated to Lord Shiva
    /// </summary>
    MahaShivaratri = 16,

    /// <summary>
    /// Buddha Purnima - Buddha's birthday celebration
    /// </summary>
    BuddhaPurnima = 17,

    /// <summary>
    /// Unduvap Poya - Commemorates Sanghamitta Theri's arrival
    /// </summary>
    UnduvapPoya = 18,

    /// <summary>
    /// Thai Pusam - South Indian Tamil festival
    /// </summary>
    ThaiPusam = 19,

    /// <summary>
    /// Kataragama Festival - Multi-religious pilgrimage
    /// </summary>
    KataragamaFestival = 20,

    // Additional Buddhist/Sri Lankan Events
    /// <summary>
    /// Poyaday - Monthly Buddhist observance (Level10Sacred = 10 for most sacred events)
    /// </summary>
    Poyaday = 21,

    /// <summary>
    /// Vesak Poya - Most sacred Buddhist full moon day (Level10Sacred = 10)
    /// </summary>
    VesakPoya = 22,


    // Additional Hindu Events
    /// <summary>
    /// Deepavali - Festival of lights, same as Diwali (Level10Sacred = 10)
    /// </summary>
    Deepavali = 25,

    /// <summary>
    /// Thaipusam - Tamil Hindu festival, variant spelling (Level10Sacred = 10)
    /// </summary>
    Thaipusam = 26,

    /// <summary>
    /// Tamil New Year - Tamil community celebration (Level10Sacred = 10)
    /// </summary>
    TamilNewYear = 27,


    // Additional Islamic Events
    /// <summary>
    /// Eid-ul-Fitr - End of Ramadan celebration (Level10Sacred = 10)
    /// </summary>
    EidulFitr = 28,

    /// <summary>
    /// Eid-ul-Adha - Festival of sacrifice (Level10Sacred = 10)
    /// </summary>
    EidulAdha = 29,

    // Additional Christian Events
    /// <summary>
    /// Easter - Christian resurrection celebration (Level10Sacred = 10)
    /// </summary>
    Easter = 30,

    // Generic categories for extensibility
    Religious = 100,
    Cultural = 101,
    National = 102,
    Regional = 103,
    Community = 104,
    Traditional = 105,
    Seasonal = 106,
    Historical = 107,
    Spiritual = 108,
    Educational = 109,

    // Additional backward compatibility values
    ReligiousFestival = 110,
    NationalHoliday = 111
}