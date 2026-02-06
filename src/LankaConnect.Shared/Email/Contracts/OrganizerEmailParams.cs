namespace LankaConnect.Shared.Email.Contracts;

/// <summary>
/// Phase 6A.87: Base parameter contract for organizer contact information in emails.
///
/// Used by email templates that display event organizer contact details.
/// Provides strongly-typed access with conditional validation.
///
/// Common parameters:
/// - HasOrganizerContact: Flag indicating if organizer info should be displayed
/// - OrganizerContactName: Organizer's display name
/// - OrganizerContactEmail: Organizer's email address
/// - OrganizerContactPhone: Organizer's phone number
///
/// Templates using these parameters:
/// - Event registration confirmation
/// - Event reminder
/// - Event cancellation notification
/// - Payment completed confirmation
/// - Event published notification
///
/// Note: The "OrganizerContact" prefix matches database template expectations.
/// Earlier parameter mismatches (OrganizerName vs OrganizerContactName) caused
/// literal {{OrganizerContactName}} to appear in production emails.
/// </summary>
public class OrganizerEmailParams
{
    /// <summary>
    /// Flag indicating whether organizer contact information should be displayed.
    /// When false, templates should hide the organizer contact section.
    /// </summary>
    public bool HasOrganizerContact { get; set; } = false;

    /// <summary>
    /// Organizer's display name (e.g., "Jane Smith").
    /// Only required when HasOrganizerContact is true.
    /// </summary>
    public string OrganizerContactName { get; set; } = string.Empty;

    /// <summary>
    /// Organizer's email address for contact purposes.
    /// Only required when HasOrganizerContact is true.
    /// </summary>
    public string OrganizerContactEmail { get; set; } = string.Empty;

    /// <summary>
    /// Organizer's phone number (e.g., "555-123-4567").
    /// Optional even when HasOrganizerContact is true.
    /// </summary>
    public string OrganizerContactPhone { get; set; } = string.Empty;

    /// <summary>
    /// Converts to dictionary for backward compatibility with existing email system.
    /// Uses exact parameter names expected by database templates.
    /// </summary>
    /// <returns>Dictionary with all organizer parameters</returns>
    public Dictionary<string, object> ToDictionary()
    {
        return new Dictionary<string, object>
        {
            { "HasOrganizerContact", HasOrganizerContact },
            { "OrganizerContactName", OrganizerContactName },
            { "OrganizerContactEmail", OrganizerContactEmail },
            { "OrganizerContactPhone", OrganizerContactPhone }
        };
    }

    /// <summary>
    /// Validates organizer parameters based on HasOrganizerContact flag.
    /// When HasOrganizerContact is true, OrganizerContactName is required.
    /// When HasOrganizerContact is false, all fields can be empty.
    /// </summary>
    /// <param name="errors">List of validation errors if validation fails</param>
    /// <returns>True if valid, false otherwise</returns>
    public bool Validate(out List<string> errors)
    {
        errors = new List<string>();

        // When HasOrganizerContact is true, OrganizerContactName is required
        if (HasOrganizerContact && string.IsNullOrWhiteSpace(OrganizerContactName))
        {
            errors.Add("OrganizerContactName is required when HasOrganizerContact is true");
        }

        // OrganizerContactEmail and OrganizerContactPhone are optional
        // even when HasOrganizerContact is true

        return errors.Count == 0;
    }

    /// <summary>
    /// Creates an OrganizerEmailParams with no contact information.
    /// Use when event doesn't have organizer contact details.
    /// </summary>
    /// <returns>OrganizerEmailParams with HasOrganizerContact = false</returns>
    public static OrganizerEmailParams NoContact()
    {
        return new OrganizerEmailParams
        {
            HasOrganizerContact = false,
            OrganizerContactName = "",
            OrganizerContactEmail = "",
            OrganizerContactPhone = ""
        };
    }

    /// <summary>
    /// Creates an OrganizerEmailParams from event organizer data.
    /// Automatically sets HasOrganizerContact based on whether name is provided.
    /// </summary>
    /// <param name="name">Organizer name (required for contact to be shown)</param>
    /// <param name="email">Organizer email (optional)</param>
    /// <param name="phone">Organizer phone (optional)</param>
    /// <returns>OrganizerEmailParams with contact information</returns>
    public static OrganizerEmailParams FromContact(string? name, string? email = null, string? phone = null)
    {
        var hasContact = !string.IsNullOrWhiteSpace(name);

        return new OrganizerEmailParams
        {
            HasOrganizerContact = hasContact,
            OrganizerContactName = name ?? "",
            OrganizerContactEmail = email ?? "",
            OrganizerContactPhone = phone ?? ""
        };
    }
}
