namespace LankaConnect.Domain.Communications.Enums;

/// <summary>
/// Status values for WhatsApp Business API messages with cultural intelligence workflow support
/// </summary>
public enum WhatsAppMessageStatus
{
    /// <summary>
    /// Message is being composed and not yet sent
    /// </summary>
    Draft = 1,
    
    /// <summary>
    /// Message is scheduled for future delivery with cultural timing optimization
    /// </summary>
    Scheduled = 2,
    
    /// <summary>
    /// Message is undergoing cultural appropriateness validation
    /// </summary>
    PendingValidation = 3,
    
    /// <summary>
    /// Message failed cultural validation and requires revision
    /// </summary>
    ValidationFailed = 4,
    
    /// <summary>
    /// Message is in the process of being sent to WhatsApp Business API
    /// </summary>
    Sending = 5,
    
    /// <summary>
    /// Message has been successfully sent to WhatsApp Business API
    /// </summary>
    Sent = 6,
    
    /// <summary>
    /// Message has been delivered to recipient's device
    /// </summary>
    Delivered = 7,
    
    /// <summary>
    /// Message has been read by the recipient (if read receipts enabled)
    /// </summary>
    Read = 8,
    
    /// <summary>
    /// Message delivery failed and may be eligible for retry
    /// </summary>
    Failed = 9,
    
    /// <summary>
    /// Message has reached maximum retry attempts and cannot be sent
    /// </summary>
    Expired = 10,
    
    /// <summary>
    /// Message was cancelled before sending (user action)
    /// </summary>
    Cancelled = 11,
    
    /// <summary>
    /// Message was rejected by cultural intelligence validation
    /// </summary>
    Rejected = 12
}