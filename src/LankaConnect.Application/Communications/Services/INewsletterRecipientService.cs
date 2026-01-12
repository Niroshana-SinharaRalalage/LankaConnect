using LankaConnect.Application.Communications.Common;

namespace LankaConnect.Application.Communications.Services;

/// <summary>
/// Service for resolving newsletter recipients with location-based filtering
/// Phase 6A.74 Enhancement 1: Location targeting for non-event newsletters
/// </summary>
public interface INewsletterRecipientService
{
    /// <summary>
    /// Resolves all unique recipient email addresses for a newsletter
    /// </summary>
    Task<RecipientPreviewDto> ResolveRecipientsAsync(
        Guid newsletterId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets preview of recipients based on configuration
    /// </summary>
    Task<RecipientPreviewDto> GetRecipientPreviewAsync(
        List<Guid> emailGroupIds,
        bool includeNewsletterSubscribers,
        Guid? eventId,
        List<Guid>? metroAreaIds,
        bool targetAllLocations,
        CancellationToken cancellationToken = default);
}
