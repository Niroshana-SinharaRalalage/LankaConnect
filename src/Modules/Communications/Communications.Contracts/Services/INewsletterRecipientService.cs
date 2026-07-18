using LankaConnect.Modules.Communications.Contracts.DTOs;

namespace LankaConnect.Modules.Communications.Contracts.Services; // Wave 8.5.d (2026-07-18): split from LegacyPromotions/ per Consult #17 Q2 Day 10 debt. Originally promoted from Communications.Application (Wave 6.5.f mirror, 2026-07-09 Day 4) per Consult #15 PASS C.

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
