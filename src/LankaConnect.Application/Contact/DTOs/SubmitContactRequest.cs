using System.ComponentModel.DataAnnotations;

namespace LankaConnect.Application.Contact.DTOs;

/// <summary>
/// Request model for contact form submission.
/// Phase 6A.76: Contact Us Feature
/// </summary>
public class SubmitContactRequest
{
    /// <summary>
    /// Name of the person submitting the contact form.
    /// </summary>
    [Required(ErrorMessage = "Name is required")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Name must be between 2 and 100 characters")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Email address for reply correspondence.
    /// </summary>
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    [StringLength(254, ErrorMessage = "Email cannot exceed 254 characters")]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Subject of the contact message.
    /// </summary>
    [Required(ErrorMessage = "Subject is required")]
    [StringLength(200, MinimumLength = 5, ErrorMessage = "Subject must be between 5 and 200 characters")]
    public string Subject { get; set; } = string.Empty;

    /// <summary>
    /// The message content.
    /// </summary>
    [Required(ErrorMessage = "Message is required")]
    [StringLength(5000, MinimumLength = 20, ErrorMessage = "Message must be between 20 and 5000 characters")]
    public string Message { get; set; } = string.Empty;
}
