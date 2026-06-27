using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;
using LankaConnect.Products.LankaEvents.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace LankaConnect.Application.Events.Commands.ClearSponsorBrochure;

/// <summary>
/// Phase 6A.162 — clears a sponsor's brochure/flyer image. Idempotent
/// (succeeds when no brochure set). Mirrors
/// <see cref="LankaConnect.Application.Events.Commands.ClearSponsorImage.ClearSponsorImageCommand"/>
/// byte-for-byte; the logo slot is untouched.
/// </summary>
public record ClearSponsorBrochureCommand : IRequest<Result>
{
    public Guid EventId { get; init; }
    public Guid SponsorId { get; init; }
}

public class ClearSponsorBrochureCommandHandler : IRequestHandler<ClearSponsorBrochureCommand, Result>
{
    private readonly ISponsorRepository _sponsorRepository;
    private readonly IImageService _imageService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ClearSponsorBrochureCommandHandler> _logger;

    public ClearSponsorBrochureCommandHandler(
        ISponsorRepository sponsorRepository,
        IImageService imageService,
        IUnitOfWork unitOfWork,
        ILogger<ClearSponsorBrochureCommandHandler> logger)
    {
        _sponsorRepository = sponsorRepository;
        _imageService = imageService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result> Handle(ClearSponsorBrochureCommand request, CancellationToken cancellationToken)
    {
        using (LogContext.PushProperty("Operation", "ClearSponsorBrochure"))
        using (LogContext.PushProperty("EventId", request.EventId))
        using (LogContext.PushProperty("SponsorId", request.SponsorId))
        {
            _logger.LogInformation("ClearSponsorBrochure START");

            try
            {
                var sponsor = await _sponsorRepository.GetByIdAsync(request.SponsorId, cancellationToken);
                if (sponsor is null || sponsor.EventId != request.EventId)
                {
                    _logger.LogWarning("ClearSponsorBrochure: sponsor not found or wrong event");
                    return Result.NotFound(
                        $"Sponsor {request.SponsorId} not found for event {request.EventId}");
                }

                var oldBlobName = sponsor.BrochureBlobName;
                var oldImageUrl = sponsor.BrochureUrl;

                var clearResult = sponsor.ClearBrochure();
                if (!clearResult.IsSuccess)
                {
                    _logger.LogWarning("ClearSponsorBrochure: domain clear failed - {Error}", clearResult.Error);
                    return clearResult;
                }

                await _unitOfWork.CommitAsync(cancellationToken);

                // Best-effort blob delete after the entity is updated.
                if (!string.IsNullOrEmpty(oldImageUrl))
                {
                    try
                    {
                        await _imageService.DeleteImageAsync(oldImageUrl, cancellationToken);
                        _logger.LogInformation(
                            "ClearSponsorBrochure: blob deleted - OldBlob={OldBlob}", oldBlobName);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex,
                            "ClearSponsorBrochure: blob delete failed - OldBlob={OldBlob}. " +
                            "Entity already cleared; stale blob is a forensic gap.",
                            oldBlobName);
                    }
                }

                _logger.LogInformation("ClearSponsorBrochure SUCCESS");
                return Result.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ClearSponsorBrochure FAILED");
                throw;
            }
        }
    }
}
