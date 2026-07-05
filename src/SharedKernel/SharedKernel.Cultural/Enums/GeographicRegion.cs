namespace LankaConnect.SharedKernel.Cultural.Enums;

/// <summary>
/// Comprehensive geographic regions for Sri Lankan diaspora communities and local provinces.
/// 35 values spanning Sri Lankan provinces, major diaspora regions, and significant cities.
/// </summary>
/// <remarks>
/// W2C.5 (2026-06-05): moved from LankaConnect.Domain.Common.Enums to SharedKernel.Cultural per
/// ADR-008. Two deprecated empty-stub variants (Communications/Enums/GeographicRegion.cs and
/// Events/Enums/GeographicRegion.cs) deleted. The Billing/BillingSupportingTypes.cs CLASS
/// variant (distinct open-text tax-jurisdiction VO) renamed to <c>BillingRegion</c> in the
/// same commit per architect Q3 ruling — different concept that shared a name.
/// </remarks>
/// <summary>Consolidated from multiple domain areas to avoid duplication</summary>
/// </summary>
public enum GeographicRegion
{
    // Sri Lankan Provinces (from Communications.Enums)
    SriLanka = 1,
    WesternProvince = 2,
    CentralProvince = 3,
    SouthernProvince = 4,
    NorthernProvince = 5,
    EasternProvince = 6,
    NorthCentralProvince = 7,
    NorthWesternProvince = 8,
    SabaragamuwaProvince = 9,
    UvaProvince = 10,
    
    // Major Diaspora Regions (from Communications.Enums)
    UnitedStates = 11,
    Canada = 12,
    UnitedKingdom = 13,
    Australia = 14,
    Germany = 15,
    France = 16,
    Italy = 17,
    Norway = 18,
    Sweden = 19,
    Switzerland = 20,
    
    // Major Cities with Sri Lankan Communities (from Communications.Enums)
    Toronto = 21,
    London = 22,
    Melbourne = 23,
    Sydney = 24,
    NewYork = 25,
    LosAngeles = 26,
    Paris = 27,
    Zurich = 28,
    
    // Additional regions from Events.Enums
    NorthAmerica = 29,
    BayArea = 30,
    California = 31,
    Europe = 32,
    MiddleEast = 33,
    
    // Missing regions causing CS0117 errors
    SanFranciscoBayArea = 34,
    
    Other = 99
}