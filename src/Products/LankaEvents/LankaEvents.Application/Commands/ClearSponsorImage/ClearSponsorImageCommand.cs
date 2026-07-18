using LankaConnect.BuildingBlocks.Application.Common.Interfaces;
using LankaConnect.BuildingBlocks.Domain;
using LankaConnect.Products.LankaEvents.Domain.Repositories;
using LankaConnect.Products.LankaEvents.Infrastructure.Data; // Wave 8.5.g
using MediatR;
using Microsoft.EntityFrameworkCore; // Wave 8.5.g
using Microsoft.Extensions.Logging;
using Serilog.Context;
using LankaConnect.Modules.Media.Contracts.Services; // 4C.h Day 5: IImageService promoted
namespace LankaConnect.Products.LankaEvents.Application.Commands.ClearSponsorImage;

/// <summary>
/// Phase 6A.145 — clears a sponsor's image. Idempotent (succeeds when no image set).
/// Deletes the blob from storage best-effort; the entity update is the authoritative state.
/// </summary>
public record ClearSponsorImageCommand : IRequest<Result>
{
    public Guid EventId { get; init; }
    public Guid SponsorId { get; init; }
}

public class ClearSponsorImageCommandHandler : IRequestHandler<ClearSponsorImageCommand, Result>
{
    private readonly ISponsorRepository _sponsorRepository;
    private readonly IImageService _imageService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly LankaEventsDbContext _dbContext; // Wave 8.5.g direct-SaveChanges
    private readonly ILogger<ClearSponsorImageCommandHandler> _logger;

    public ClearSponsorImageCommandHandler(
        ISponsorRepository sponsorRepository,
        IImageService imageService,
        IUnitOfWork unitOfWork,
        LankaEventsDbContext dbContext,
        ILogger<ClearSponsorImageCommandHandler> logger)
    {
        _sponsorRepository = sponsorRepository;
        _imageService = imageService;
        _unitOfWork = unitOfWork;
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<Result> Handle(ClearSponsorImageCommand request, CancellationToken cancellationToken)
    {
        using (LogContext.PushProperty("Operation", "ClearSponsorImage"))
        using (LogContext.PushProperty("EventId", request.EventId))
        using (LogContext.PushProperty("SponsorId", request.SponsorId))
        {
            _logger.LogInformation("ClearSponsorImage START");

            try
            {
                var sponsor = await _sponsorRepository.GetByIdAsync(request.SponsorId, cancellationToken);
                if (sponsor is null || sponsor.EventId != request.EventId)
                {
                    _logger.LogWarning("ClearSponsorImage: sponsor not found or wrong event");
                    return Result.NotFound(
                        $"Sponsor {request.SponsorId} not found for event {request.EventId}");
                }

                var oldBlobName = sponsor.ImageBlobName;
                var oldImageUrl = sponsor.ImageUrl;

                var clearResult = sponsor.ClearImage();
                if (!clearResult.IsSuccess)
                {
                    _logger.LogWarning("ClearSponsorImage: domain clear failed - {Error}", clearResult.Error);
                    return clearResult;
                }

                await _dbContext.SaveChangesAsync(cancellationToken); // Wave 8.5.g: direct-SaveChanges on LankaEventsDbContext (was IUnitOfWork = 0 changes on AppDbContext)

                // Best-effort blob delete after the entity is updated.
                if (!string.IsNullOrEmpty(oldImageUrl))
                {
                    try
                    {
                        await _imageService.DeleteImageAsync(oldImageUrl, cancellationToken);
                        _logger.LogInformation(
                            "ClearSponsorImage: blob deleted - OldBlob={OldBlob}", oldBlobName);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex,
                            "ClearSponsorImage: blob delete failed - OldBlob={OldBlob}. " +
                            "Entity already cleared; stale blob is a forensic gap.",
                            oldBlobName);
                    }
                }

                _logger.LogInformation("ClearSponsorImage SUCCESS");
                return Result.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ClearSponsorImage FAILED");
                throw;
            }
        }
    }
}
