using LankaConnect.BuildingBlocks.Application.Common.Interfaces;
using LankaConnect.BuildingBlocks.Domain;
using LankaConnect.Products.LankaEvents.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;
using Serilog.Context;
namespace LankaConnect.Products.LankaEvents.Application.Commands.SetSponsorBrochure;

/// <summary>
/// Phase 6A.162 — uploads (or replaces) a sponsor's brochure/flyer image
/// (the OPTIONAL second slot, sibling to the logo set by
/// <see cref="LankaConnect.Products.LankaEvents.Application.Commands.SetSponsorImage.SetSponsorImageCommand"/>).
///
/// Mirrors SetSponsorImageCommand byte-for-byte — same validation, same
/// blob-upload-first-then-domain-write recovery pattern, same authz at
/// the controller layer. Touching the brochure slot does NOT mutate the
/// logo slot (pinned by SponsorTests independence invariants from [1/6]).
/// </summary>
public record SetSponsorBrochureCommand : IRequest<Result<SetSponsorBrochureResult>>
{
    public Guid EventId { get; init; }
    public Guid SponsorId { get; init; }
    public byte[] ImageData { get; init; } = Array.Empty<byte>();
    public string FileName { get; init; } = string.Empty;
}

public record SetSponsorBrochureResult(string BrochureUrl, string BrochureBlobName);

public class SetSponsorBrochureCommandHandler
    : IRequestHandler<SetSponsorBrochureCommand, Result<SetSponsorBrochureResult>>
{
    private readonly ISponsorRepository _sponsorRepository;
    private readonly IImageService _imageService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<SetSponsorBrochureCommandHandler> _logger;

    public SetSponsorBrochureCommandHandler(
        ISponsorRepository sponsorRepository,
        IImageService imageService,
        IUnitOfWork unitOfWork,
        ILogger<SetSponsorBrochureCommandHandler> logger)
    {
        _sponsorRepository = sponsorRepository;
        _imageService = imageService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<SetSponsorBrochureResult>> Handle(
        SetSponsorBrochureCommand request, CancellationToken cancellationToken)
    {
        using (LogContext.PushProperty("Operation", "SetSponsorBrochure"))
        using (LogContext.PushProperty("EventId", request.EventId))
        using (LogContext.PushProperty("SponsorId", request.SponsorId))
        {
            _logger.LogInformation(
                "SetSponsorBrochure START: FileName={FileName}, SizeBytes={SizeBytes}",
                request.FileName, request.ImageData?.Length ?? 0);

            try
            {
                if (request.ImageData is null || request.ImageData.Length == 0)
                    return Result<SetSponsorBrochureResult>.Failure("Brochure image data is required.");

                // 1. Validate image (size + content-type sniff) — same guards as logo
                var validation = _imageService.ValidateImage(request.ImageData, request.FileName);
                if (!validation.IsSuccess)
                {
                    _logger.LogWarning(
                        "SetSponsorBrochure: validation failed - {Errors}",
                        string.Join("; ", validation.Errors));
                    return Result<SetSponsorBrochureResult>.Failure(validation.Errors);
                }

                // 2. Load sponsor
                var sponsor = await _sponsorRepository.GetByIdAsync(request.SponsorId, cancellationToken);
                if (sponsor is null || sponsor.EventId != request.EventId)
                {
                    _logger.LogWarning("SetSponsorBrochure: sponsor not found or wrong event");
                    return Result<SetSponsorBrochureResult>.NotFound(
                        $"Sponsor {request.SponsorId} not found for event {request.EventId}");
                }

                // 3. Remember prior brochure blob for cleanup after the new one persists.
                var oldBlobName = sponsor.BrochureBlobName;
                var oldImageUrl = sponsor.BrochureUrl;

                // 4. Upload new blob (uses EventId as the businessId partition key —
                //    same pattern as logo handler).
                var upload = await _imageService.UploadImageAsync(
                    request.ImageData!, request.FileName, request.EventId, cancellationToken);
                if (!upload.IsSuccess)
                {
                    _logger.LogError(
                        "SetSponsorBrochure: blob upload failed - {Errors}",
                        string.Join("; ", upload.Errors));
                    return Result<SetSponsorBrochureResult>.Failure(upload.Errors);
                }

                // 5. Persist on entity; rollback the upload on domain failure.
                var setResult = sponsor.SetBrochure(upload.Value.Url, upload.Value.BlobName);
                if (!setResult.IsSuccess)
                {
                    await _imageService.DeleteImageAsync(upload.Value.Url, cancellationToken);
                    _logger.LogWarning(
                        "SetSponsorBrochure: domain SetBrochure failed - {Error}. Rolled back uploaded blob.",
                        setResult.Error);
                    return Result<SetSponsorBrochureResult>.Failure(setResult.Errors);
                }

                await _unitOfWork.CommitAsync(cancellationToken);

                // 6. Best-effort delete old blob after commit. Failure is logged but
                //    non-fatal — new brochure is in place; stale blob is a forensic gap.
                if (!string.IsNullOrEmpty(oldImageUrl))
                {
                    try
                    {
                        await _imageService.DeleteImageAsync(oldImageUrl, cancellationToken);
                        _logger.LogInformation(
                            "SetSponsorBrochure: old blob deleted - OldBlob={OldBlob}", oldBlobName);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex,
                            "SetSponsorBrochure: failed to delete old blob - OldBlob={OldBlob}. " +
                            "Forensic gap acceptable; new brochure in place.",
                            oldBlobName);
                    }
                }

                _logger.LogInformation(
                    "SetSponsorBrochure SUCCESS: NewUrl={Url}, NewBlob={BlobName}",
                    upload.Value.Url, upload.Value.BlobName);

                return Result<SetSponsorBrochureResult>.Success(
                    new SetSponsorBrochureResult(upload.Value.Url, upload.Value.BlobName));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SetSponsorBrochure FAILED");
                throw;
            }
        }
    }
}
