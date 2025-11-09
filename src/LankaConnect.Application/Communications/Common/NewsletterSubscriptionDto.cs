using System.ComponentModel.DataAnnotations;

namespace LankaConnect.Application.Communications.Common;

/// <summary>
/// Newsletter subscription request DTO
/// </summary>
public class NewsletterSubscriptionDto
{
    /// <summary>
    /// Subscriber's email address
    /// </summary>
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    public required string Email { get; set; }

    /// <summary>
    /// Metro area ID for location-based subscriptions (null if receive all locations)
    /// </summary>
    public string? MetroAreaId { get; set; }

    /// <summary>
    /// Whether subscriber wants to receive events from all locations
    /// </summary>
    public bool ReceiveAllLocations { get; set; }

    /// <summary>
    /// Timestamp of subscription
    /// </summary>
    public DateTime Timestamp { get; set; }
}

/// <summary>
/// Newsletter subscription response DTO
/// </summary>
public class NewsletterSubscriptionResponseDto
{
    /// <summary>
    /// Whether the subscription was successful
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Success or error message
    /// </summary>
    public string? Message { get; set; }

    /// <summary>
    /// Subscriber ID (for successful subscriptions)
    /// </summary>
    public string? SubscriberId { get; set; }

    /// <summary>
    /// Error code (for failed subscriptions)
    /// </summary>
    public string? ErrorCode { get; set; }
}
