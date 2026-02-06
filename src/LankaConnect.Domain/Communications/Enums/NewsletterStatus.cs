namespace LankaConnect.Domain.Communications.Enums;

/// <summary>
/// Status of a newsletter in its lifecycle
/// Phase 6A.74: Newsletter status workflow
/// </summary>
public enum NewsletterStatus
{
    /// <summary>
    /// Newsletter is in draft state, can be edited
    /// </summary>
    Draft = 0,

    /// <summary>
    /// Newsletter is published and visible, can send emails
    /// </summary>
    Active = 2,

    /// <summary>
    /// Newsletter has expired after 7 days
    /// </summary>
    Inactive = 3,

    /// <summary>
    /// Newsletter email has been sent (final state)
    /// </summary>
    Sent = 4
}
