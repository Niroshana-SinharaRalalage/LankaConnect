using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;
using LankaConnect.Products.LankaEvents.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;
using Serilog.Context;
namespace LankaConnect.Products.LankaEvents.Application.Commands.SetSponsorshipPackageImage;

/// <summary>
/// Phase 6A.156 — uploads (or replaces) the display image for a sponsorship
/// package. Mirrors <c>SetAddOnDefinitionImageCommand</c> with rollback-on-
/// failure semantics: if the domain SetImage rejects the URL/blob (shouldn't
/// happen with valid uploader output, but defense-in-depth), the freshly
/// uploaded blob is deleted before returning the failure.
/// </summary>
public record SetSponsorshipPackageImageCommand : IRequest<Result<SetSponsorshipPackageImageResult>>
{
    public Guid EventId { get; init; }
    public Guid PackageId { get; init; }
    public byte[] ImageData { get; init; } = Array.Empty<byte>();
    public string FileName { get; init; } = string.Empty;
}

public record SetSponsorshipPackageImageResult(string ImageUrl, string ImageBlobName);

public class SetSponsorshipPackageImageCommandHandler
    : IRequestHandler<SetSponsorshipPackageImageCommand, Result<SetSponsorshipPackageImageResult>>
{
    private readonly ISponsorshipPackageRepository _packageRepository;
    private readonly IImageService _imageService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<SetSponsorshipPackageImageCommandHandler> _logger;

    public SetSponsorshipPackageImageCommandHandler(
        ISponsorshipPackageRepository packageRepository,
        IImageService imageService,
        IUnitOfWork unitOfWork,
        ILogger<SetSponsorshipPackageImageCommandHandler> logger)
    {
        _packageRepository = packageRepository;
        _imageService = imageService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<SetSponsorshipPackageImageResult>> Handle(
        SetSponsorshipPackageImageCommand request, CancellationToken cancellationToken)
    {
        using (LogContext.PushProperty("Operation", "SetSponsorshipPackageImage"))
        using (LogContext.PushProperty("EventId", request.EventId))
        using (LogContext.PushProperty("PackageId", request.PackageId))
        {
            _logger.LogInformation(
                "SetSponsorshipPackageImage START: FileName={FileName}, SizeBytes={SizeBytes}",
                request.FileName, request.ImageData?.Length ?? 0);

            try
            {
                if (request.ImageData is null || request.ImageData.Length == 0)
                    return Result<SetSponsorshipPackageImageResult>.Failure("Image data is required.");

                // 1. Validate image content (size, MIME sniff)
                var validation = _imageService.ValidateImage(request.ImageData, request.FileName);
                if (!validation.IsSuccess)
                {
                    _logger.LogWarning(
                        "SetSponsorshipPackageImage: validation failed - {Errors}",
                        string.Join("; ", validation.Errors));
                    return Result<SetSponsorshipPackageImageResult>.Failure(validation.Errors);
                }

                // 2. Load entity
                var package = await _packageRepository.GetByIdAsync(request.PackageId, cancellationToken);
                if (package is null || package.EventId != request.EventId)
                {
                    _logger.LogWarning(
                        "SetSponsorshipPackageImage: package not found or wrong event");
                    return Result<SetSponsorshipPackageImageResult>.NotFound(
                        $"Sponsorship package {request.PackageId} not found for event {request.EventId}");
                }

                var oldBlobName = package.ImageBlobName;
                var oldImageUrl = package.ImageUrl;

                // 3. Upload new blob (EventId as partition key — same pattern as add-on)
                var upload = await _imageService.UploadImageAsync(
                    request.ImageData!, request.FileName, request.EventId, cancellationToken);
                if (!upload.IsSuccess)
                {
                    _logger.LogError(
                        "SetSponsorshipPackageImage: blob upload failed - {Errors}",
                        string.Join("; ", upload.Errors));
                    return Result<SetSponsorshipPackageImageResult>.Failure(upload.Errors);
                }

                // 4. Persist URL + blob name; rollback the blob upload if domain rejects
                var setResult = package.SetImage(upload.Value.Url, upload.Value.BlobName);
                if (!setResult.IsSuccess)
                {
                    await _imageService.DeleteImageAsync(upload.Value.Url, cancellationToken);
                    _logger.LogWarning(
                        "SetSponsorshipPackageImage: domain SetImage failed - {Error}. Rolled back uploaded blob.",
                        setResult.Error);
                    return Result<SetSponsorshipPackageImageResult>.Failure(setResult.Errors);
                }

                _packageRepository.Update(package);
                await _unitOfWork.CommitAsync(cancellationToken);

                // 5. Best-effort delete old blob AFTER commit. A failed cleanup is logged
                // and swallowed — a stale blob is acceptable; a failed commit is not.
                if (!string.IsNullOrEmpty(oldImageUrl))
                {
                    try
                    {
                        await _imageService.DeleteImageAsync(oldImageUrl, cancellationToken);
                        _logger.LogInformation(
                            "SetSponsorshipPackageImage: old blob deleted - OldBlob={OldBlob}",
                            oldBlobName);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex,
                            "SetSponsorshipPackageImage: failed to delete old blob - OldBlob={OldBlob}. " +
                            "Forensic gap acceptable; new image in place.",
                            oldBlobName);
                    }
                }

                _logger.LogInformation(
                    "SetSponsorshipPackageImage SUCCESS: NewUrl={Url}, NewBlob={BlobName}",
                    upload.Value.Url, upload.Value.BlobName);

                return Result<SetSponsorshipPackageImageResult>.Success(
                    new SetSponsorshipPackageImageResult(upload.Value.Url, upload.Value.BlobName));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SetSponsorshipPackageImage FAILED");
                throw;
            }
        }
    }
}
