namespace LankaConnect.Application.Communications.Common;

/// <summary>
/// Preview of newsletter recipients before sending
/// </summary>
public class RecipientPreviewDto
{
    public int TotalUniqueRecipients { get; set; }
    public int EmailGroupRecipients { get; set; }
    public int NewsletterSubscribers { get; set; }
    public List<string> EmailGroupNames { get; set; } = new();
    public List<string> MetroAreaNames { get; set; } = new();
    public bool TargetsAllLocations { get; set; }
}
