namespace LankaConnect.Application.Common.DTOs;

/// <summary>
/// Phase 6A.100: Email DTOs for direct email sending operations.
/// Moved from IEmailService.cs after interface removal.
/// Used by AzureEmailService for internal email operations.
/// </summary>

/// <summary>
/// Represents an email message to be sent (DTO for service layer)
/// </summary>
public class EmailMessageDto
{
    public string ToEmail { get; set; } = string.Empty;
    public string ToName { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string HtmlBody { get; set; } = string.Empty;
    public string? PlainTextBody { get; set; }
    public string? FromEmail { get; set; }
    public string? FromName { get; set; }
    public List<EmailAttachment>? Attachments { get; set; }
    public Dictionary<string, string>? Headers { get; set; }
    public int Priority { get; set; } = 1; // 1 = High, 2 = Normal, 3 = Low
}

/// <summary>
/// Represents an email attachment
/// Phase 6A.35: Added ContentId for CID inline image embedding in emails
/// </summary>
public class EmailAttachment
{
    public string FileName { get; set; } = string.Empty;
    public byte[] Content { get; set; } = Array.Empty<byte>();
    public string ContentType { get; set; } = "application/octet-stream";

    /// <summary>
    /// Content-ID for inline attachments (CID embedding).
    /// When set, the attachment can be referenced in HTML using: src="cid:{ContentId}"
    /// This ensures images display immediately without user action in email clients.
    /// </summary>
    public string? ContentId { get; set; }

    /// <summary>
    /// Indicates if this is an inline attachment (embedded in email body) vs regular attachment
    /// </summary>
    public bool IsInline => !string.IsNullOrEmpty(ContentId);
}

/// <summary>
/// Result of bulk email sending operation
/// </summary>
public class BulkEmailResult
{
    public int TotalEmails { get; set; }
    public int SuccessfulSends { get; set; }
    public int FailedSends { get; set; }
    public List<string> Errors { get; set; } = new();
}
