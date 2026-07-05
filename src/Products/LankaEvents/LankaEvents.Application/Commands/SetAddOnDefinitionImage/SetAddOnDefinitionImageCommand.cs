using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;
using LankaConnect.Products.LankaEvents.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;
using Serilog.Context;
namespace LankaConnect.Products.LankaEvents.Application.Commands.SetAddOnDefinitionImage;

/// <summary>
/// Phase 6A.143 — uploads (or replaces) the display image for a single add-on
/// definition. Validates content, uploads the new blob, deletes the previous blob
/// (if any) on success, and persists the URL + blob name on the entity.
///
/// Returns the newly stored URL + blob name so the caller can update its cache.
/// </summary>
public record SetAddOnDefinitionImageCommand : IRequest<Result<SetAddOnDefinitionImageResult>>
{
    public Guid EventId { get; init; }
    public Guid DefinitionId { get; init; }
    public byte[] ImageData { get; init; } = Array.Empty<byte>();
    public string FileName { get; init; } = string.Empty;
}

public record SetAddOnDefinitionImageResult(string ImageUrl, string ImageBlobName);

public class SetAddOnDefinitionImageCommandHandler
    : IRequestHandler<SetAddOnDefinitionImageCommand, Result<SetAddOnDefinitionImageResult>>
{
    private readonly IAddOnDefinitionRepository _definitionRepository;
    private readonly IImageService _imageService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<SetAddOnDefinitionImageCommandHandler> _logger;

    public SetAddOnDefinitionImageCommandHandler(
        IAddOnDefinitionRepository definitionRepository,
        IImageService imageService,
        IUnitOfWork unitOfWork,
        ILogger<SetAddOnDefinitionImageCommandHandler> logger)
    {
        _definitionRepository = definitionRepository;
        _imageService = imageService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<SetAddOnDefinitionImageResult>> Handle(
        SetAddOnDefinitionImageCommand request, CancellationToken cancellationToken)
    {
        using (LogContext.PushProperty("Operation", "SetAddOnDefinitionImage"))
        using (LogContext.PushProperty("EventId", request.EventId))
        using (LogContext.PushProperty("DefinitionId", request.DefinitionId))
        {
            _logger.LogInformation(
                "SetAddOnDefinitionImage START: FileName={FileName}, SizeBytes={SizeBytes}",
                request.FileName, request.ImageData?.Length ?? 0);

            try
            {
                if (request.ImageData is null || request.ImageData.Length == 0)
                    return Result<SetAddOnDefinitionImageResult>.Failure("Image data is required.");

                // 1. Validate image (size, content-type sniff)
                var validation = _imageService.ValidateImage(request.ImageData, request.FileName);
                if (!validation.IsSuccess)
                {
                    _logger.LogWarning(
                        "SetAddOnDefinitionImage: validation failed - {Errors}",
                        string.Join("; ", validation.Errors));
                    return Result<SetAddOnDefinitionImageResult>.Failure(validation.Errors);
                }

                // 2. Load entity
                var definition = await _definitionRepository.GetByIdAsync(request.DefinitionId, cancellationToken);
                if (definition is null || definition.EventId != request.EventId)
                {
                    _logger.LogWarning(
                        "SetAddOnDefinitionImage: definition not found or wrong event");
                    return Result<SetAddOnDefinitionImageResult>.NotFound(
                        $"Add-on definition {request.DefinitionId} not found for event {request.EventId}");
                }

                // Remember the prior blob so we can clean it up after the new one persists.
                var oldBlobName = definition.ImageBlobName;
                var oldImageUrl = definition.ImageUrl;

                // 3. Upload new blob (uses EventId as the "businessId" partition key — same
                // pattern as AddImageToEventCommandHandler at line 51).
                var upload = await _imageService.UploadImageAsync(
                    request.ImageData!, request.FileName, request.EventId, cancellationToken);
                if (!upload.IsSuccess)
                {
                    _logger.LogError(
                        "SetAddOnDefinitionImage: blob upload failed - {Errors}",
                        string.Join("; ", upload.Errors));
                    return Result<SetAddOnDefinitionImageResult>.Failure(upload.Errors);
                }

                // 4. Persist URL + blob name on the entity. Rollback the upload if the
                // domain method rejects (mirrors AddImageToEventCommandHandler:62).
                var setResult = definition.SetImage(upload.Value.Url, upload.Value.BlobName);
                if (!setResult.IsSuccess)
                {
                    await _imageService.DeleteImageAsync(upload.Value.Url, cancellationToken);
                    _logger.LogWarning(
                        "SetAddOnDefinitionImage: domain SetImage failed - {Error}. Rolled back uploaded blob.",
                        setResult.Error);
                    return Result<SetAddOnDefinitionImageResult>.Failure(setResult.Errors);
                }

                await _unitOfWork.CommitAsync(cancellationToken);

                // 5. Best-effort delete the old blob AFTER successful commit. If delete
                // fails we log + swallow — the door doesn't close on a stale blob.
                if (!string.IsNullOrEmpty(oldImageUrl))
                {
                    try
                    {
                        await _imageService.DeleteImageAsync(oldImageUrl, cancellationToken);
                        _logger.LogInformation(
                            "SetAddOnDefinitionImage: old blob deleted - OldBlob={OldBlob}",
                            oldBlobName);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex,
                            "SetAddOnDefinitionImage: failed to delete old blob - OldBlob={OldBlob}. " +
                            "Forensic gap acceptable; new image in place.",
                            oldBlobName);
                    }
                }

                _logger.LogInformation(
                    "SetAddOnDefinitionImage SUCCESS: NewUrl={Url}, NewBlob={BlobName}",
                    upload.Value.Url, upload.Value.BlobName);

                return Result<SetAddOnDefinitionImageResult>.Success(
                    new SetAddOnDefinitionImageResult(upload.Value.Url, upload.Value.BlobName));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SetAddOnDefinitionImage FAILED");
                throw;
            }
        }
    }
}
