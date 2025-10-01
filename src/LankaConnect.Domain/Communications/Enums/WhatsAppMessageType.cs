namespace LankaConnect.Domain.Communications.Enums;

/// <summary>
/// Types of WhatsApp Business API messages for cultural intelligence platform
/// </summary>
public enum WhatsAppMessageType
{
    /// <summary>
    /// Regular text message for personal communication
    /// </summary>
    Text = 1,
    
    /// <summary>
    /// Template message using pre-approved WhatsApp templates
    /// </summary>
    Template = 2,
    
    /// <summary>
    /// Media message with images, audio, or video content
    /// </summary>
    Media = 3,
    
    /// <summary>
    /// Interactive message with buttons or lists
    /// </summary>
    Interactive = 4,
    
    /// <summary>
    /// Broadcast message to multiple recipients simultaneously
    /// Requires cultural intelligence validation for diaspora communities
    /// </summary>
    Broadcast = 5,
    
    /// <summary>
    /// Event notification message with RSVP capabilities
    /// Integrated with cultural calendar awareness
    /// </summary>
    EventNotification = 6,
    
    /// <summary>
    /// Festival greeting message with cultural appropriateness validation
    /// </summary>
    FestivalGreeting = 7,
    
    /// <summary>
    /// Community announcement for diaspora groups
    /// </summary>
    CommunityAnnouncement = 8,
    
    /// <summary>
    /// Reminder message with cultural timing optimization
    /// </summary>
    Reminder = 9,
    
    /// <summary>
    /// Location-based message for diaspora community events
    /// </summary>
    LocationBased = 10
}