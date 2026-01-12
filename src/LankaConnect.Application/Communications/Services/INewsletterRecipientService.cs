using LankaConnect.Application.Communications.Common;

namespace LankaConnect.Application.Communications.Services;

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
