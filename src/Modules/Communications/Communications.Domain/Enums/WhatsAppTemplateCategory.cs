namespace LankaConnect.Modules.Communications.Domain.Enums;

/// <summary>
/// Phase 7A: WhatsApp Business API template categories.
/// UTILITY templates are transactional (no opt-in required by Meta).
/// MARKETING templates require explicit user opt-in per Meta policy.
/// </summary>
public enum WhatsAppTemplateCategory
{
    /// <summary>
    /// Transactional messages (confirmations, reminders, updates).
    /// No additional opt-in required by Meta policy beyond our own preference system.
    /// </summary>
    Utility = 1,

    /// <summary>
    /// Promotional messages (new events, newsletters).
    /// Requires explicit user opt-in per Meta Business API policy.
    /// </summary>
    Marketing = 2
}
