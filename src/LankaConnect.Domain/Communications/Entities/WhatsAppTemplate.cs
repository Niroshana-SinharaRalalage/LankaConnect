using LankaConnect.Domain.Common;
using LankaConnect.Domain.Communications.Enums;

namespace LankaConnect.Domain.Communications.Entities;

/// <summary>
/// Phase 7A: Local registry of pre-approved Meta WhatsApp templates.
/// Templates must be submitted to Meta Business Manager and approved before use.
/// Status/Category use enums per architect review D3.
/// </summary>
public class WhatsAppTemplate : BaseEntity
{
    public string TemplateName { get; private set; } = null!;
    public string DisplayName { get; private set; } = null!;
    public WhatsAppTemplateCategory Category { get; private set; }
    public string Language { get; private set; } = "en";
    public WhatsAppTemplateStatus Status { get; private set; }
    public string? HeaderText { get; private set; }
    public string BodyText { get; private set; } = null!;
    public string? FooterText { get; private set; }

    // JSONB stored as string — no ValueComparer needed (strings are immutable)
    // If changed to List<T> or Dictionary<K,V>, a ValueComparer MUST be added (see Phase 6A.129)
    public string? ButtonTypes { get; private set; }
    public string ParameterNames { get; private set; } = null!;
    public int ParameterCount { get; private set; }
    public string? MetaTemplateId { get; private set; }
    public string? SampleValues { get; private set; }

    // Approval tracking
    public DateTime? ApprovedAt { get; private set; }
    public DateTime? RejectedAt { get; private set; }
    public string? RejectionReason { get; private set; }

    // Computed
    public bool IsApproved => Status == WhatsAppTemplateStatus.Approved;
    public bool IsUsable => IsApproved && !string.IsNullOrEmpty(MetaTemplateId);

    // EF Core constructor
    private WhatsAppTemplate() { }

    /// <summary>
    /// Factory method to create a new WhatsApp template record.
    /// </summary>
    public static WhatsAppTemplate Create(
        string templateName,
        string displayName,
        WhatsAppTemplateCategory category,
        string bodyText,
        List<string> parameterNames,
        string language = "en",
        string? headerText = null,
        string? footerText = null,
        string? buttonTypesJson = null,
        string? sampleValuesJson = null)
    {
        return new WhatsAppTemplate
        {
            TemplateName = templateName,
            DisplayName = displayName,
            Category = category,
            BodyText = bodyText,
            ParameterNames = System.Text.Json.JsonSerializer.Serialize(parameterNames),
            ParameterCount = parameterNames.Count,
            Language = language,
            Status = WhatsAppTemplateStatus.Pending,
            HeaderText = headerText,
            FooterText = footerText,
            ButtonTypes = buttonTypesJson,
            SampleValues = sampleValuesJson
        };
    }

    /// <summary>
    /// Mark template as approved by Meta with the assigned template ID.
    /// </summary>
    public void MarkApproved(string metaTemplateId)
    {
        if (string.IsNullOrWhiteSpace(metaTemplateId))
            throw new ArgumentException("Meta template ID is required for approval", nameof(metaTemplateId));

        Status = WhatsAppTemplateStatus.Approved;
        MetaTemplateId = metaTemplateId;
        ApprovedAt = DateTime.UtcNow;
        RejectedAt = null;
        RejectionReason = null;
        MarkAsUpdated();
    }

    /// <summary>
    /// Mark template as rejected by Meta with the reason.
    /// </summary>
    public void MarkRejected(string reason)
    {
        Status = WhatsAppTemplateStatus.Rejected;
        RejectionReason = reason;
        RejectedAt = DateTime.UtcNow;
        ApprovedAt = null;
        MetaTemplateId = null;
        MarkAsUpdated();
    }

    /// <summary>
    /// Resubmit a rejected template (resets to Pending).
    /// </summary>
    public void Resubmit()
    {
        Status = WhatsAppTemplateStatus.Pending;
        RejectedAt = null;
        RejectionReason = null;
        ApprovedAt = null;
        MetaTemplateId = null;
        MarkAsUpdated();
    }

    /// <summary>
    /// Get the ordered parameter names for this template.
    /// </summary>
    public List<string> GetParameterNames()
    {
        return System.Text.Json.JsonSerializer.Deserialize<List<string>>(ParameterNames)
            ?? new List<string>();
    }
}
