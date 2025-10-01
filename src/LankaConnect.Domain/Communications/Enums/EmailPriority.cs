namespace LankaConnect.Domain.Communications.Enums;

/// <summary>
/// Represents the priority level of an email message
/// Used for queue processing and delivery optimization
/// </summary>
public enum EmailPriority
{
    Low = 1,
    Normal = 5,
    High = 10,
    Critical = 15
}