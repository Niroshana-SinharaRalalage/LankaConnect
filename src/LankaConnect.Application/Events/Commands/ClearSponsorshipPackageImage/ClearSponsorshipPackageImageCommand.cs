using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace LankaConnect.Application.Events.Commands.ClearSponsorshipPackageImage;

/// <summary>
/// Phase 6A.156 — clears the display image from a sponsorship package.
/// Idempotent — succeeds (no-op) when the package has no image set. The
/// blob deletion is best-effort: a forensic gap of an orphan blob is
/// acceptable, an inconsistent DB row is not.
/// </summary>
public record ClearSponsorshipPackageImageCommand : IRequest<Result>
{
    public Guid EventId { get; init; }
    public Guid PackageId { get; init; }
}

public class ClearSponsorshipPackageImageCommandHandler : IRequestHandler<ClearSponsorshipPackageImageCommand, Result>
{
    private readonly ISponsorshipPackageRepository _packageRepository;
    private readonly IImageService _imageService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ClearSponsorshipPackageImageCommandHandler> _logger;

    public ClearSponsorshipPackageImageCommandHandler(
        ISponsorshipPackageRepository packageRepository,
        IImageService imageService,
        IUnitOfWork unitOfWork,
        ILogger<ClearSponsorshipPackageImageCommandHandler> logger)
    {
        _packageRepository = packageRepository;
        _imageService = imageService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result> Handle(ClearSponsorshipPackageImageCommand request, CancellationToken cancellationToken)
    {
        using (LogContext.PushProperty("Operation", "ClearSponsorshipPackageImage"))
        using (LogContext.PushProperty("EventId", request.EventId))
        using (LogContext.PushProperty("PackageId", request.PackageId))
        {
            _logger.LogInformation("ClearSponsorshipPackageImage START");

            try
            {
                var package = await _packageRepository.GetByIdAsync(request.PackageId, cancellationToken);
                if (package is null || package.EventId != request.EventId)
                {
                    _logger.LogWarning(
                        "ClearSponsorshipPackageImage: package not found or wrong event");
                    return Result.NotFound(
                        $"Sponsorship package {request.PackageId} not found for event {request.EventId}");
                }

                var oldImageUrl = package.ImageUrl;
                var oldBlobName = package.ImageBlobName;

                var clearResult = package.ClearImage();
                if (!clearResult.IsSuccess)
                    return Result.Failure(clearResult.Errors);

                _packageRepository.Update(package);
                await _unitOfWork.CommitAsync(cancellationToken);

                // Best-effort blob delete after commit
                if (!string.IsNullOrEmpty(oldImageUrl))
                {
                    try
                    {
                        await _imageService.DeleteImageAsync(oldImageUrl, cancellationToken);
                        _logger.LogInformation(
                            "ClearSponsorshipPackageImage: blob deleted - Blob={Blob}",
                            oldBlobName);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex,
                            "ClearSponsorshipPackageImage: failed to delete blob - Blob={Blob}. DB row cleared.",
                            oldBlobName);
                    }
                }

                _logger.LogInformation("ClearSponsorshipPackageImage SUCCESS");
                return Result.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ClearSponsorshipPackageImage FAILED");
                throw;
            }
        }
    }
}
