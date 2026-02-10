namespace LankaConnect.Shared.Email.Contracts;

/// <summary>
/// Phase 6A.100: Template-specific typed parameters for support ticket reply emails.
/// Template: template-support-ticket-reply
///
/// This replaces Dictionary&lt;string, object&gt; in ReplySupportTicketCommandHandler with
/// compile-time type-safe parameters.
///
/// Sent to user when admin replies to their support ticket.
/// </summary>
public class SupportTicketReplyEmailParams : IEmailParameters
{
    /// <summary>
    /// The template name for support ticket reply email.
    /// </summary>
    public string TemplateName => EmailTemplateContract.TemplateNames.SupportTicketReply;

    /// <summary>
    /// Recipient email address.
    /// </summary>
    public string RecipientEmail { get; set; } = string.Empty;

    /// <summary>
    /// Recipient name.
    /// </summary>
    public string RecipientName { get; set; } = string.Empty;

    #region Core Properties

    /// <summary>
    /// Ticket reference ID (e.g., "TICKET-12345").
    /// </summary>
    public string ReferenceId { get; set; } = string.Empty;

    /// <summary>
    /// Ticket subject.
    /// </summary>
    public string Subject { get; set; } = string.Empty;

    /// <summary>
    /// Reply content from admin.
    /// </summary>
    public string ReplyContent { get; set; } = string.Empty;

    /// <summary>
    /// Admin name who replied.
    /// </summary>
    public string AdminName { get; set; } = string.Empty;

    /// <summary>
    /// Date/time when reply was sent.
    /// </summary>
    public DateTime RepliedAt { get; set; }

    /// <summary>
    /// Support email address.
    /// </summary>
    public string SupportEmail { get; set; } = "lankaconnect.app@gmail.com";

    #endregion

    #region IEmailParameters Implementation

    /// <summary>
    /// Converts the typed parameters to a dictionary for template rendering.
    /// Uses EmailTemplateContract constants for all parameter names.
    /// </summary>
    public Dictionary<string, object> ToDictionary()
    {
        return new Dictionary<string, object>
        {
            { "Name", RecipientName },
            { "ReferenceId", ReferenceId },
            { "Subject", Subject },
            { "ReplyContent", ReplyContent },
            { EmailTemplateContract.AdminUser.AdminName, AdminName },
            { EmailTemplateContract.Common.SupportEmail, SupportEmail },
            // Template alias: template uses ReplyMessage instead of ReplyContent
            { "ReplyMessage", ReplyContent },
            // Template alias: template uses UserName instead of Name
            { EmailTemplateContract.Common.UserName, RecipientName },
            { EmailTemplateContract.Common.Year, DateTime.UtcNow.Year.ToString() },
            { "RepliedAt", RepliedAt.ToString("MMMM dd, yyyy h:mm tt") + " UTC" }
        };
    }

    /// <summary>
    /// Validates the email parameters.
    /// </summary>
    public bool Validate(out List<string> errors)
    {
        errors = new List<string>();

        if (string.IsNullOrWhiteSpace(RecipientEmail))
            errors.Add("RecipientEmail is required");

        if (string.IsNullOrWhiteSpace(RecipientName))
            errors.Add("RecipientName is required");

        if (string.IsNullOrWhiteSpace(ReferenceId))
            errors.Add("ReferenceId is required");

        if (string.IsNullOrWhiteSpace(Subject))
            errors.Add("Subject is required");

        if (string.IsNullOrWhiteSpace(ReplyContent))
            errors.Add("ReplyContent is required");

        if (string.IsNullOrWhiteSpace(AdminName))
            errors.Add("AdminName is required");

        if (RepliedAt == default)
            errors.Add("RepliedAt is required");

        return errors.Count == 0;
    }

    #endregion

    #region Factory Methods

    /// <summary>
    /// Creates a new SupportTicketReplyEmailParams with required fields.
    /// </summary>
    public static SupportTicketReplyEmailParams Create(
        string recipientEmail,
        string recipientName,
        string referenceId,
        string subject,
        string replyContent,
        string adminName,
        DateTime repliedAt)
    {
        return new SupportTicketReplyEmailParams
        {
            RecipientEmail = recipientEmail,
            RecipientName = recipientName,
            ReferenceId = referenceId,
            Subject = subject,
            ReplyContent = replyContent,
            AdminName = adminName,
            RepliedAt = repliedAt
        };
    }

    #endregion
}
