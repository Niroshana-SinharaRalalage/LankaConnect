namespace LankaConnect.Domain.Communications.Services;

/// <summary>
/// Result record containing consolidated newsletter email recipients
/// Phase 6A.74: Newsletter/News Alert Feature
/// </summary>
/// <param name="EmailAddresses">Deduplicated set of email addresses (case-insensitive)</param>
/// <param name="Breakdown">Breakdown of recipient sources before deduplication</param>
public sealed record NewsletterRecipients(
    HashSet<string> EmailAddresses,
    RecipientBreakdown Breakdown);

/// <summary>
/// Breakdown of newsletter recipient counts by source
/// Phase 6A.74: Provides observability into recipient resolution
/// </summary>
/// <param name="EmailGroupCount">Count of emails from email groups</param>
/// <param name="SubscriberCount">Count of confirmed newsletter subscribers</param>
/// <param name="TotalUnique">Total unique emails after case-insensitive deduplication</param>
public sealed record RecipientBreakdown(
    int EmailGroupCount,
    int SubscriberCount,
    int TotalUnique);
