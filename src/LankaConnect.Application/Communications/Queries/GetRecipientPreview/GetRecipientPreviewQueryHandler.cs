using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Communications.Common;
using LankaConnect.Application.Communications.Services;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Communications;
using Microsoft.Extensions.Logging;

namespace LankaConnect.Application.Communications.Queries.GetRecipientPreview;

/// <summary>
/// Handler for GetRecipientPreviewQuery
/// Phase 6A.74: Recipient preview with location-based filtering
/// </summary>
public class GetRecipientPreviewQueryHandler : IQueryHandler<GetRecipientPreviewQuery, RecipientPreviewDto>
{
    private readonly INewsletterRepository _newsletterRepository;
    private readonly INewsletterRecipientService _recipientService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<GetRecipientPreviewQueryHandler> _logger;

    public GetRecipientPreviewQueryHandler(
        INewsletterRepository newsletterRepository,
        INewsletterRecipientService recipientService,
        ICurrentUserService currentUserService,
        ILogger<GetRecipientPreviewQueryHandler> logger)
    {
        _newsletterRepository = newsletterRepository;
        _recipientService = recipientService;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<Result<RecipientPreviewDto>> Handle(GetRecipientPreviewQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "[Phase 6A.74] GetRecipientPreviewQuery STARTED - Newsletter {NewsletterId}, User {UserId}",
            request.NewsletterId, _currentUserService.UserId);

        try
        {
            // Retrieve newsletter
            var newsletter = await _newsletterRepository.GetByIdAsync(request.NewsletterId, cancellationToken);
            
            if (newsletter == null)
                return Result<RecipientPreviewDto>.Failure("Newsletter not found");

            // Authorization: Only creator or admin can preview recipients
            if (newsletter.CreatedByUserId != _currentUserService.UserId && !_currentUserService.IsAdmin)
                return Result<RecipientPreviewDto>.Failure("You do not have permission to preview recipients for this newsletter");

            // Resolve recipients using location-based targeting
            var recipientPreview = await _recipientService.ResolveRecipientsAsync(request.NewsletterId, cancellationToken);

            _logger.LogInformation(
                "[Phase 6A.74] Recipient preview completed - Newsletter {NewsletterId}, Total Recipients: {TotalRecipients}",
                request.NewsletterId, recipientPreview.TotalRecipients);

            return Result<RecipientPreviewDto>.Success(recipientPreview);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[Phase 6A.74] ERROR previewing recipients - Newsletter {NewsletterId}",
                request.NewsletterId);
            throw;
        }
    }
}
