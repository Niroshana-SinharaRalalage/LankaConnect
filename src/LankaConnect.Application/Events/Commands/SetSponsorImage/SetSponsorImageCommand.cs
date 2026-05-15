using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events;
using LankaConnect.Domain.Events.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace LankaConnect.Application.Events.Commands.SetSponsorImage;

/// <summary>
/// Phase 6A.145 — uploads (or replaces) a sponsor's image. Threshold-gated:
/// only sponsors whose contribution amount meets the event's
/// <c>SponsorConfiguration.MinAmountForSponsorImage</c> can upload an image,
/// UNLESS the caller is an organizer (organizer override per architect E-1).
///
/// For Money sponsors, the threshold checks <c>Amount.Amount</c>.
/// For Item sponsors, it checks <c>EstimatedValue</c>; items without an
/// EstimatedValue are denied.
///
/// Validation + upload + persistence flow mirrors <see cref="SetAddOnDefinitionImage.SetAddOnDefinitionImageCommand"/>.
/// </summary>
public record SetSponsorImageCommand : IRequest<Result<SetSponsorImageResult>>
{
    public Guid EventId { get; init; }
    public Guid SponsorId { get; init; }
    public byte[] ImageData { get; init; } = Array.Empty<byte>();
    public string FileName { get; init; } = string.Empty;
    /// <summary>True when the caller is the event organizer — bypasses the threshold check.</summary>
    public bool IsOrganizer { get; init; }
}

public record SetSponsorImageResult(string ImageUrl, string ImageBlobName);

public class SetSponsorImageCommandHandler
    : IRequestHandler<SetSponsorImageCommand, Result<SetSponsorImageResult>>
{
    private readonly ISponsorRepository _sponsorRepository;
    private readonly IEventRepository _eventRepository;
    private readonly IImageService _imageService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<SetSponsorImageCommandHandler> _logger;

    public SetSponsorImageCommandHandler(
        ISponsorRepository sponsorRepository,
        IEventRepository eventRepository,
        IImageService imageService,
        IUnitOfWork unitOfWork,
        ILogger<SetSponsorImageCommandHandler> logger)
    {
        _sponsorRepository = sponsorRepository;
        _eventRepository = eventRepository;
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
        using (LogContext.PushProperty("IsOrganizer", request.IsOrganizer))
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

                // 3. Threshold check — load Event for its SponsorConfiguration.
                //    Organizer override bypasses this check entirely.
                if (!request.IsOrganizer)
                {
                    var @event = await _eventRepository.GetByIdAsync(request.EventId, cancellationToken);
                    if (@event is null || @event.SponsorConfig is null)
                    {
                        _logger.LogWarning("SetSponsorImage: event/sponsor config not found");
                        return Result<SetSponsorImageResult>.Failure(
                            "Sponsor image upload is not enabled for this event.");
                    }
                    var contribution = sponsor.Amount?.Amount ?? sponsor.EstimatedValue;
                    if (!@event.SponsorConfig.QualifiesForImage(contribution))
                    {
                        _logger.LogWarning(
                            "SetSponsorImage: threshold not met - Contribution={Contribution}, Threshold={Threshold}",
                            contribution, @event.SponsorConfig.MinAmountForSponsorImage);
                        return Result<SetSponsorImageResult>.Failure(
                            "Your sponsorship amount does not meet this event's image upload threshold.");
                    }
                }

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
