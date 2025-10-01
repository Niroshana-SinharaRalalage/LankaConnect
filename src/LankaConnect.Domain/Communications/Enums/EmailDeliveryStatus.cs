namespace LankaConnect.Domain.Communications.Enums;

/// <summary>
/// Represents the delivery status of an email message
/// This is used for tracking email lifecycle states for Analytics & Tracking
/// </summary>
public enum EmailDeliveryStatus
{
    Pending = 1,
    Queued = 2,
    Sending = 3,
    Sent = 4,
    Delivered = 5,
    Failed = 6,
    Bounced = 7,
    Rejected = 8
}