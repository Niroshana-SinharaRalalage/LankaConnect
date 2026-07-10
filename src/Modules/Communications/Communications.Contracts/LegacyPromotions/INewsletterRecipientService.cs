namespace LankaConnect.Modules.Communications.Contracts.LegacyPromotions; // Wave 6.5.f mirror (2026-07-09 Day 4): promoted from Communications.Application per Consult #15 PASS C.

/// <summary>
/// Service for resolving newsletter recipients
/// Phase 6A.74: Newsletter recipient resolution with location targeting
/// </summary>
public interface INewsletterRecipientService
{
    /// <summary>
    /// Resolves all recipients for a newsletter
    /// Includes email groups + newsletter subscribers (with location matching)
    /// </summary>
    /// <param name="newsletterId">Newsletter ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Deduplicated list of email addresses with breakdown</returns>
    Task<RecipientPreviewDto> ResolveRecipientsAsync(Guid newsletterId, CancellationToken cancellationToken = default);
}
