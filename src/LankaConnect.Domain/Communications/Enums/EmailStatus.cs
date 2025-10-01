namespace LankaConnect.Domain.Communications.Enums;

public enum EmailStatus
{
    Pending = 1,
    Queued = 2,
    Sending = 3,
    Sent = 4,
    Delivered = 5,
    Failed = 6,
    Bounced = 7,
    Rejected = 8,
    QueuedWithCulturalDelay = 9,
    PermanentlyFailed = 10,
    CulturalEventNotification = 11
}