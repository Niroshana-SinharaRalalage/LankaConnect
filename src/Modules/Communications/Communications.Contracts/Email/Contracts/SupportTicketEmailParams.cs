namespace LankaConnect.Modules.Communications.Contracts.Email.Contracts;

/// <summary>
/// Phase 6A.87 Week 5: Template-specific typed parameters for support ticket emails.
/// Templates: template-support-ticket-confirmation, template-support-ticket-reply
///
/// This replaces Dictionary&lt;string, object&gt; in CreateSupportTicketCommandHandler and
/// ReplySupportTicketCommandHandler with compile-time type-safe parameters.
/// </summary>
public class SupportTicketEmailParams : IEmailParameters
{
    private string _templateName = "template-support-ticket-confirmation";

    /// <summary>
    /// The template name for support ticket email.
    /// </summary>
    public string TemplateName => _templateName;

    /// <summary>
    /// Recipient email address.
    /// </summary>
    public string RecipientEmail => UserEmail;

    /// <summary>
    /// Recipient name.
    /// </summary>
    public string RecipientName => UserName;

    #region Core Properties

    /// <summary>
    /// User identifier.
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// User's display name.
    /// </summary>
    public string UserName { get; set; } = string.Empty;

    /// <summary>
    /// User's email address.
    /// </summary>
    public string UserEmail { get; set; } = string.Empty;

    /// <summary>
    /// Support ticket identifier.
    /// </summary>
    public Guid TicketId { get; set; }

    /// <summary>
    /// Ticket reference number (for display).
    /// </summary>
    public string TicketNumber { get; set; } = string.Empty;

    /// <summary>
    /// Ticket subject/title.
    /// </summary>
    public string Subject { get; set; } = string.Empty;

    /// <summary>
    /// Ticket category (e.g., "Technical Issue", "Billing", "General").
    /// </summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>
    /// Ticket priority (e.g., "Low", "Medium", "High", "Urgent").
    /// </summary>
    public string Priority { get; set; } = "Medium";

    /// <summary>
    /// Ticket status.
    /// </summary>
    public string Status { get; set; } = "Open";

    /// <summary>
    /// Original message content.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Reply content (for reply emails).
    /// </summary>
    public string ReplyMessage { get; set; } = string.Empty;

    /// <summary>
    /// Support agent name (for reply emails).
    /// </summary>
    public string AgentName { get; set; } = string.Empty;

    /// <summary>
    /// Date/time ticket was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Date/time of reply (for reply emails).
    /// </summary>
    public DateTime? RepliedAt { get; set; }

    /// <summary>
    /// Expected response time.
    /// </summary>
    public string ExpectedResponseTime { get; set; } = "24-48 hours";

    /// <summary>
    /// URL to view ticket in portal.
    /// </summary>
    public string TicketUrl { get; set; } = string.Empty;

    /// <summary>
    /// Support email address.
    /// </summary>
    public string SupportEmail { get; set; } = "lankaconnect.app@gmail.com";

    /// <summary>
    /// Company name for branding.
    /// </summary>
    public string CompanyName { get; set; } = "LankaConnect";

    #endregion

    #region Template Type Methods

    /// <summary>
    /// Sets the template to use for ticket confirmation emails.
    /// </summary>
    public SupportTicketEmailParams AsConfirmation()
    {
        _templateName = "template-support-ticket-confirmation";
        return this;
    }

    /// <summary>
    /// Sets the template to use for ticket reply emails.
    /// </summary>
    public SupportTicketEmailParams AsReply()
    {
        _templateName = "template-support-ticket-reply";
        return this;
    }

    #endregion

    #region IEmailParameters Implementation

    /// <summary>
    /// Converts the typed parameters to a dictionary for template rendering.
    /// Phase 6A.87+ Fix: Added TicketReference alias and Year for footer.
    /// </summary>
    public Dictionary<string, object> ToDictionary()
    {
        var dict = new Dictionary<string, object>
        {
            { "UserName", UserName },
            { "TicketNumber", TicketNumber },
            { "TicketReference", TicketNumber },  // Alias for templates expecting TicketReference
            // Wave 9.h.10.6 F32: template-support-ticket-confirmation subject line contains
            // `Reference #{{ReferenceId}}` but ToDictionary previously never emitted a
            // ReferenceId key so the placeholder rendered unreplaced. Founder inbox proof:
            // "We Received Your Message - Reference #{{ReferenceId}}" — literal double-brace.
            { "ReferenceId", TicketNumber },
            { "Subject", Subject },
            { "Category", Category },
            { "Priority", Priority },
            { "Status", Status },
            { "Message", Message },
            { "CreatedAt", CreatedAt.ToString("MMMM dd, yyyy h:mm tt") },
            { "ExpectedResponseTime", ExpectedResponseTime },
            { "TicketUrl", TicketUrl },
            { "SupportEmail", SupportEmail },
            { "CompanyName", CompanyName },
            { "Year", DateTime.UtcNow.Year }  // Footer param
        };

        if (!string.IsNullOrWhiteSpace(ReplyMessage))
        {
            dict["ReplyMessage"] = ReplyMessage;
        }

        if (!string.IsNullOrWhiteSpace(AgentName))
        {
            dict["AgentName"] = AgentName;
        }

        if (RepliedAt.HasValue)
        {
            dict["RepliedAt"] = RepliedAt.Value.ToString("MMMM dd, yyyy h:mm tt");
        }

        return dict;
    }

    /// <summary>
    /// Validates the email parameters.
    /// </summary>
    public bool Validate(out List<string> errors)
    {
        errors = new List<string>();

        // Wave 9.h.10.5 F23 fix: UserId is OPTIONAL for anonymous contact-form
        // submissions. `ContactController.SubmitContact` is [AllowAnonymous] and
        // `CreateSupportTicketCommandHandler.cs:128` intentionally passes
        // `userId: Guid.Empty` when the submitter isn't authenticated (comment
        // there: "Contact form tickets may not have user IDs"). The previous
        // hard-reject on Guid.Empty here contradicted that domain rule and killed
        // every template-support-ticket-confirmation dispatch for anonymous
        // submissions (Pass 1 probe evidence: 4 VALIDATION-FAIL with errors=
        // "UserId is required"). ToDictionary does not emit "UserId" so template
        // rendering is unaffected -- the check was validator-only invariant.

        if (string.IsNullOrWhiteSpace(UserName))
            errors.Add("UserName is required");

        if (string.IsNullOrWhiteSpace(UserEmail))
            errors.Add("UserEmail is required");

        if (TicketId == Guid.Empty)
            errors.Add("TicketId is required");

        if (string.IsNullOrWhiteSpace(TicketNumber))
            errors.Add("TicketNumber is required");

        if (string.IsNullOrWhiteSpace(Subject))
            errors.Add("Subject is required");

        if (CreatedAt == default)
            errors.Add("CreatedAt is required");

        return errors.Count == 0;
    }

    #endregion

    #region Factory Methods

    /// <summary>
    /// Creates a new SupportTicketEmailParams for ticket confirmation.
    /// </summary>
    public static SupportTicketEmailParams CreateConfirmation(
        Guid userId,
        string userName,
        string userEmail,
        Guid ticketId,
        string ticketNumber,
        string subject,
        string category,
        string priority,
        string message,
        DateTime createdAt,
        string ticketUrl)
    {
        return new SupportTicketEmailParams
        {
            UserId = userId,
            UserName = userName,
            UserEmail = userEmail,
            TicketId = ticketId,
            TicketNumber = ticketNumber,
            Subject = subject,
            Category = category,
            Priority = priority,
            Status = "Open",
            Message = message,
            CreatedAt = createdAt,
            TicketUrl = ticketUrl
        }.AsConfirmation();
    }

    /// <summary>
    /// Creates a new SupportTicketEmailParams for ticket reply.
    /// </summary>
    public static SupportTicketEmailParams CreateReply(
        Guid userId,
        string userName,
        string userEmail,
        Guid ticketId,
        string ticketNumber,
        string subject,
        string originalMessage,
        string replyMessage,
        string agentName,
        DateTime createdAt,
        DateTime repliedAt,
        string ticketUrl,
        string status = "In Progress")
    {
        return new SupportTicketEmailParams
        {
            UserId = userId,
            UserName = userName,
            UserEmail = userEmail,
            TicketId = ticketId,
            TicketNumber = ticketNumber,
            Subject = subject,
            Status = status,
            Message = originalMessage,
            ReplyMessage = replyMessage,
            AgentName = agentName,
            CreatedAt = createdAt,
            RepliedAt = repliedAt,
            TicketUrl = ticketUrl
        }.AsReply();
    }

    #endregion
}
