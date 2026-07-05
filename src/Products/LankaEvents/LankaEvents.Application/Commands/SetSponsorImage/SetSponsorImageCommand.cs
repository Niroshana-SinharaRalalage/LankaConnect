using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;
using LankaConnect.Products.LankaEvents.Domain;
using LankaConnect.Modules.Identity.Domain.DomainEvents;
using LankaConnect.Products.LankaEvents.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;
using Serilog.Context;
namespace LankaConnect.Products.LankaEvents.Application.Commands.SetSponsorImage;

/// <summary>
/// Phase 6A.145 — uploads (or replaces) a sponsor's image.
///
/// Phase 6A.145 Commit 6 — threshold gate REMOVED per UAT feedback. Any sponsor
/// (money or item, any amount) can attach an image. Authorization is still
/// enforced at the controller layer (sponsor ID knowledge for public callers,
/// organizer auth for management actions).
///
/// Validation + upload + persistence flow mirrors <see cref="SetAddOnDefinitionImage.SetAddOnDefinitionImageCommand"/>.
/// </summary>
public record SetSponsorImageCommand : IRequest<Result<SetSponsorImageResult>>
{
    public Guid EventId { get; init; }
    public Guid SponsorId { get; init; }
    public byte[] ImageData { get; init; } = Array.Empty<byte>();
    public string FileName { get; init; } = string.Empty;
}

public record SetSponsorImageResult(string ImageUrl, string ImageBlobName);

public class SetSponsorImageCommandHandler
    : IRequestHandler<SetSponsorImageCommand, Result<SetSponsorImageResult>>
{
    private readonly ISponsorRepository _sponsorRepository;
    private readonly IImageService _imageService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<SetSponsorImageCommandHandler> _logger;

    public SetSponsorImageCommandHandler(
        ISponsorRepository sponsorRepository,
        IImageService imageService,
        IUnitOfWork unitOfWork,
        ILogger<SetSponsorImageCommandHandler> logger)
    {
        _sponsorRepository = sponsorRepository;
        _imageService = imageService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<SetSponsorImageResult>> Handle(
        SetSponsorImageCommand request, CancellationToken cancellationToken)
    {
        using (LogContext.PushProperty("Operation", "SetSponsorImage"))
        using (LogContext.PushProperty("EventId", request.EventId))
        using (LogContext.PushProperty("SponsorId", request.SponsorId))
        {
            _logger.LogInformation(
                "SetSponsorImage START: FileName={FileName}, SizeBytes={SizeBytes}",
                request.FileName, request.ImageData?.Length ?? 0);

            try
            {
                if (request.ImageData is null || request.ImageData.Length == 0)
                    return Result<SetSponsorImageResult>.Failure("Image data is required.");

                // 1. Validate image (size + content-type sniff)
                var validation = _imageService.ValidateImage(request.ImageData, request.FileName);
                if (!validation.IsSuccess)
                {
                    _logger.LogWarning(
                        "SetSponsorImage: validation failed - {Errors}",
                        string.Join("; ", validation.Errors));
                    return Result<SetSponsorImageResult>.Failure(validation.Errors);
                }

                // 2. Load sponsor
                var sponsor = await _sponsorRepository.GetByIdAsync(request.SponsorId, cancellationToken);
                if (sponsor is null || sponsor.EventId != request.EventId)
                {
                    _logger.LogWarning("SetSponsorImage: sponsor not found or wrong event");
                    return Result<SetSponsorImageResult>.NotFound(
                        $"Sponsor {request.SponsorId} not found for event {request.EventId}");
                }

                // 3. Phase 6A.145 Commit 6 — threshold gate removed per UAT; any
                //    sponsor (money or item, any amount) can attach an image.

                // Remember prior blob for cleanup after the new one persists.
                var oldBlobName = sponsor.ImageBlobName;
                var oldImageUrl = sponsor.ImageUrl;

                // 4. Upload new blob (uses EventId as the businessId partition key —
                //    same pattern as add-on image handler).
                var upload = await _imageService.UploadImageAsync(
                    request.ImageData!, request.FileName, request.EventId, cancellationToken);
                if (!upload.IsSuccess)
                {
                    _logger.LogError(
                        "SetSponsorImage: blob upload failed - {Errors}",
                        string.Join("; ", upload.Errors));
                    return Result<SetSponsorImageResult>.Failure(upload.Errors);
                }

                // 5. Persist on entity; rollback the upload on domain failure.
                var setResult = sponsor.SetImage(upload.Value.Url, upload.Value.BlobName);
                if (!setResult.IsSuccess)
                {
                    await _imageService.DeleteImageAsync(upload.Value.Url, cancellationToken);
                    _logger.LogWarning(
                        "SetSponsorImage: domain SetImage failed - {Error}. Rolled back uploaded blob.",
                        setResult.Error);
                    return Result<SetSponsorImageResult>.Failure(setResult.Errors);
                }

                await _unitOfWork.CommitAsync(cancellationToken);

                // 6. Best-effort delete old blob after commit. Failure is logged but
                //    non-fatal — new image is in place; stale blob is a forensic gap.
                if (!string.IsNullOrEmpty(oldImageUrl))
                {
                    try
                    {
                        await _imageService.DeleteImageAsync(oldImageUrl, cancellationToken);
                        _logger.LogInformation(
                            "SetSponsorImage: old blob deleted - OldBlob={OldBlob}", oldBlobName);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex,
                            "SetSponsorImage: failed to delete old blob - OldBlob={OldBlob}. " +
                            "Forensic gap acceptable; new image in place.",
                            oldBlobName);
                    }
                }

                _logger.LogInformation(
                    "SetSponsorImage SUCCESS: NewUrl={Url}, NewBlob={BlobName}",
                    upload.Value.Url, upload.Value.BlobName);

                return Result<SetSponsorImageResult>.Success(
                    new SetSponsorImageResult(upload.Value.Url, upload.Value.BlobName));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SetSponsorImage FAILED");
                throw;
            }
        }
    }
}
