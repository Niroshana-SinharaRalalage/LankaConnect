namespace LankaConnect.Domain.Communications.Enums;

/// <summary>
/// Phase 7A: Approval status of WhatsApp templates registered with Meta Business Manager.
/// Templates must be Approved before they can be used to send messages.
/// </summary>
public enum WhatsAppTemplateStatus
{
    /// <summary>
    /// Template submitted to Meta but not yet reviewed
    /// </summary>
    Pending = 1,

    /// <summary>
    /// Template approved by Meta and ready for use
    /// </summary>
    Approved = 2,

    /// <summary>
    /// Template rejected by Meta — see rejection reason
    /// </summary>
    Rejected = 3
}
