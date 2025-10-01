namespace LankaConnect.Domain.Common.Enums;

/// <summary>
/// Client segment classification for business analytics and customer profiling
/// Defines different customer segments for targeted services and marketing
/// </summary>
public enum ClientSegment
{
    /// <summary>
    /// Individual diaspora members - personal users
    /// </summary>
    Individual = 1,

    /// <summary>
    /// Family units - households using family features
    /// </summary>
    Family = 2,

    /// <summary>
    /// Small business - businesses with 1-50 employees
    /// </summary>
    SmallBusiness = 3,

    /// <summary>
    /// Medium business - businesses with 51-500 employees
    /// </summary>
    MediumBusiness = 4,

    /// <summary>
    /// Large enterprise - businesses with 500+ employees
    /// </summary>
    LargeEnterprise = 5,

    /// <summary>
    /// Cultural organization - temples, cultural centers, associations
    /// </summary>
    CulturalOrganization = 6,

    /// <summary>
    /// Educational institution - schools, universities, learning centers
    /// </summary>
    EducationalInstitution = 7,

    /// <summary>
    /// Government agency - government departments and services
    /// </summary>
    GovernmentAgency = 8,

    /// <summary>
    /// Non-profit organization - charitable and volunteer organizations
    /// </summary>
    NonProfitOrganization = 9,

    /// <summary>
    /// Media organization - news outlets, broadcasters, publishers
    /// </summary>
    MediaOrganization = 10,

    /// <summary>
    /// Technology company - IT services and technology providers
    /// </summary>
    TechnologyCompany = 11,

    /// <summary>
    /// Healthcare organization - medical practices and health services
    /// </summary>
    HealthcareOrganization = 12,

    /// <summary>
    /// Financial services - banks, investment firms, financial advisors
    /// </summary>
    FinancialServices = 13,

    /// <summary>
    /// Tourism and hospitality - travel, hotels, tourism services
    /// </summary>
    TourismHospitality = 14,

    /// <summary>
    /// Retail and commerce - shops, restaurants, commercial businesses
    /// </summary>
    RetailCommerce = 15
}