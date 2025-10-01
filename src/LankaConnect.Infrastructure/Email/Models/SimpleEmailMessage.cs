namespace LankaConnect.Infrastructure.Email.Models;

/// <summary>
/// Simple email message model for sending emails via SMTP
/// </summary>
public class SimpleEmailMessage
{
    public required string To { get; set; }
    public string? ToDisplayName { get; set; }
    public string? From { get; set; }
    public string? FromDisplayName { get; set; }
    public required string Subject { get; set; }
    public required string TextBody { get; set; }
    public string? HtmlBody { get; set; }
    public Dictionary<string, string>? Attachments { get; set; }
    public Dictionary<string, object>? Metadata { get; set; }
    public int Priority { get; set; } = 3; // 1 = highest, 5 = lowest
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string? MessageId { get; set; }
    public bool IsHtml => !string.IsNullOrEmpty(HtmlBody);
}