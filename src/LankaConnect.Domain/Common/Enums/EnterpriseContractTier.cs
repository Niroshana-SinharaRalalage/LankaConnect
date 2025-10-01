namespace LankaConnect.Domain.Common.Enums;

/// <summary>
/// Enterprise contract tier classification for business customers
/// Defines service levels and feature access for enterprise clients
/// </summary>
public enum EnterpriseContractTier
{
    /// <summary>
    /// Starter tier - basic enterprise features for small organizations
    /// </summary>
    Starter = 1,

    /// <summary>
    /// Professional tier - enhanced features for medium organizations
    /// </summary>
    Professional = 2,

    /// <summary>
    /// Business tier - comprehensive features for large organizations
    /// </summary>
    Business = 3,

    /// <summary>
    /// Enterprise tier - full feature set for enterprise organizations
    /// </summary>
    Enterprise = 4,

    /// <summary>
    /// Premium tier - white-label and custom solutions
    /// </summary>
    Premium = 5,

    /// <summary>
    /// Fortune 500 tier - dedicated infrastructure and support
    /// </summary>
    Fortune500 = 6,

    /// <summary>
    /// Government tier - specialized compliance and security features
    /// </summary>
    Government = 7,

    /// <summary>
    /// Non-profit tier - discounted services for charitable organizations
    /// </summary>
    NonProfit = 8,

    /// <summary>
    /// Educational tier - specialized features for educational institutions
    /// </summary>
    Educational = 9,

    /// <summary>
    /// Cultural organization tier - features for cultural institutions
    /// </summary>
    CulturalOrganization = 10,

    /// <summary>
    /// Custom tier - bespoke contract with tailored features
    /// </summary>
    Custom = 11
}