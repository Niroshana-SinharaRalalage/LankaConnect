namespace LankaConnect.Application.Contact.DTOs;

/// <summary>
/// Response model for contact form submission.
/// Phase 6A.76: Contact Us Feature
/// </summary>
public class ContactSubmissionResponse
{
    /// <summary>
    /// Indicates whether the submission was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// User-friendly message describing the result.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Unique reference ID for tracking the submission.
    /// Only provided on successful submission.
    /// </summary>
    public string? ReferenceId { get; set; }

    /// <summary>
    /// Error code for client-side handling.
    /// Only provided on failure.
    /// </summary>
    public string? ErrorCode { get; set; }

    /// <summary>
    /// Creates a successful response.
    /// </summary>
    public static ContactSubmissionResponse SuccessResponse(string referenceId) => new()
    {
        Success = true,
        Message = "Thank you for contacting us! We'll get back to you soon.",
        ReferenceId = referenceId
    };

    /// <summary>
    /// Creates a failure response.
    /// </summary>
    public static ContactSubmissionResponse FailureResponse(string message, string errorCode) => new()
    {
        Success = false,
        Message = message,
        ErrorCode = errorCode
    };
}
