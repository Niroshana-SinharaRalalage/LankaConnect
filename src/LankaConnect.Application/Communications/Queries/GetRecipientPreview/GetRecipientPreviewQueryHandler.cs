using System.Diagnostics;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Communications.Common;
using LankaConnect.Application.Communications.Services;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Communications;
using Microsoft.Extensions.Logging;
using Serilog.Context;

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
        using (LogContext.PushProperty("Operation", "GetRecipientPreview"))
        using (LogContext.PushProperty("EntityType", "Newsletter"))
        using (LogContext.PushProperty("NewsletterId", request.NewsletterId))
        {
            var stopwatch = Stopwatch.StartNew();
            var userId = _currentUserService.UserId;
            var isAdmin = _currentUserService.IsAdmin;

            _logger.LogInformation(
                "GetRecipientPreview START: NewsletterId={NewsletterId}, RequesterId={RequesterId}, IsAdmin={IsAdmin}",
                request.NewsletterId, userId, isAdmin);

            try
            {
                // Validate request
                if (request.NewsletterId == Guid.Empty)
                {
                    stopwatch.Stop();

                    _logger.LogWarning(
                        "GetRecipientPreview FAILED: Invalid NewsletterId - NewsletterId={NewsletterId}, Duration={ElapsedMs}ms",
                        request.NewsletterId, stopwatch.ElapsedMilliseconds);

                    return Result<RecipientPreviewDto>.Failure("Newsletter ID is required");
                }

                // Retrieve newsletter
                var newsletter = await _newsletterRepository.GetByIdAsync(request.NewsletterId, cancellationToken);

                if (newsletter == null)
                {
                    stopwatch.Stop();

                    _logger.LogWarning(
                        "GetRecipientPreview FAILED: Newsletter not found - NewsletterId={NewsletterId}, Duration={ElapsedMs}ms",
                        request.NewsletterId, stopwatch.ElapsedMilliseconds);

                    return Result<RecipientPreviewDto>.Failure("Newsletter not found");
                }

                // Authorization: Only creator or admin can preview recipients
                if (newsletter.CreatedByUserId != userId && !isAdmin)
                {
                    stopwatch.Stop();

                    _logger.LogWarning(
                        "GetRecipientPreview FAILED: Access denied - NewsletterId={NewsletterId}, RequesterId={RequesterId}, CreatorId={CreatorId}, Duration={ElapsedMs}ms",
                        request.NewsletterId, userId, newsletter.CreatedByUserId, stopwatch.ElapsedMilliseconds);

                    return Result<RecipientPreviewDto>.Failure("You do not have permission to preview recipients for this newsletter");
                }

                // Resolve recipients using location-based targeting
                var recipientPreview = await _recipientService.ResolveRecipientsAsync(request.NewsletterId, cancellationToken);

                stopwatch.Stop();

                _logger.LogInformation(
                    "GetRecipientPreview COMPLETE: NewsletterId={NewsletterId}, TotalRecipients={TotalRecipients}, Duration={ElapsedMs}ms",
                    request.NewsletterId, recipientPreview.TotalRecipients, stopwatch.ElapsedMilliseconds);

                return Result<RecipientPreviewDto>.Success(recipientPreview);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _logger.LogError(ex,
                    "GetRecipientPreview FAILED: Exception occurred - NewsletterId={NewsletterId}, Duration={ElapsedMs}ms, Error={ErrorMessage}",
                    request.NewsletterId, stopwatch.ElapsedMilliseconds, ex.Message);

                throw;
            }
        }
    }
}
