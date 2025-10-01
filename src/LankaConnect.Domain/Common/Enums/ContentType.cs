namespace LankaConnect.Domain.Common.Enums;

/// <summary>
/// Represents different types of content in the platform
/// </summary>
public enum ContentType
{
    /// <summary>
    /// Unknown or unspecified content type
    /// </summary>
    Unknown = 0,
    
    /// <summary>
    /// Sacred religious texts and content
    /// </summary>
    SacredText = 1,
    
    /// <summary>
    /// Cultural events and celebrations
    /// </summary>
    CulturalEvent = 2,
    
    /// <summary>
    /// Business listings and services
    /// </summary>
    BusinessListing = 3,
    
    /// <summary>
    /// Community forum posts and discussions
    /// </summary>
    CommunityPost = 4,
    
    /// <summary>
    /// Educational and informational content
    /// </summary>
    Educational = 5,
    
    /// <summary>
    /// News and announcements
    /// </summary>
    News = 6,
    
    /// <summary>
    /// Cultural traditions and practices
    /// </summary>
    Cultural = 7,
    
    /// <summary>
    /// User-generated content
    /// </summary>
    UserGenerated = 8
}