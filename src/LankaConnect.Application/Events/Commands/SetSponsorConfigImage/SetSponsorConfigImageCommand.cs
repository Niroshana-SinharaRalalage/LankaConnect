using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events;
using LankaConnect.Domain.Events.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace LankaConnect.Application.Events.Commands.SetSponsorConfigImage;

/// <summary>
/// Phase 6A.143 — uploads (or replaces) the sponsor banner image for an event.
/// Stored on the SponsorConfiguration value object inside the Event aggregate.
/// Requires sponsor config to exist + be enabled (the banner has nowhere to surface
/// otherwise — the SponsorSection on the public details page only renders when
/// sponsorConfig.isEnabled is true).
/// </summary>
public record SetSponsorConfigImageCommand : IRequest<Result<SetSponsorConfigImageResult>>
{
    public Guid EventId { get; init; }
    public byte[] ImageData { get; init; } = Array.Empty<byte>();
    public string FileName { get; init; } = string.Empty;
}

public record SetSponsorConfigImageResult(string ImageUrl, string ImageBlobName);

public class SetSponsorConfigImageCommandHandler
    : IRequestHandler<SetSponsorConfigImageCommand, Result<SetSponsorConfigImageResult>>
{
    private readonly IEventRepository _eventRepository;
    private readonly IImageService _imageService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<SetSponsorConfigImageCommandHandler> _logger;

    public SetSponsorConfigImageCommandHandler(
        IEventRepository eventRepository,
        IImageService imageService,
        IUnitOfWork unitOfWork,
        ILogger<SetSponsorConfigImageCommandHandler> logger)
    {
        _eventRepository = eventRepository;
        _imageService = imageService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<SetSponsorConfigImageResult>> Handle(
        SetSponsorConfigImageCommand request, CancellationToken cancellationToken)
    {
        using (LogContext.PushProperty("Operation", "SetSponsorConfigImage"))
        using (LogContext.PushProperty("EventId", request.EventId))
        {
            _logger.LogInformation(
                "SetSponsorConfigImage START: FileName={FileName}, SizeBytes={SizeBytes}",
                request.FileName, request.ImageData?.Length ?? 0);

            try
            {
                if (request.ImageData is null || request.ImageData.Length == 0)
                    return Result<SetSponsorConfigImageResult>.Failure("Image data is required.");

                var validation = _imageService.ValidateImage(request.ImageData, request.FileName);
                if (!validation.IsSuccess)
                {
                    _logger.LogWarning(
                        "SetSponsorConfigImage: validation failed - {Errors}",
                        string.Join("; ", validation.Errors));
                    return Result<SetSponsorConfigImageResult>.Failure(validation.Errors);
                }

                var @event = await _eventRepository.GetByIdAsync(request.EventId, cancellationToken);
                if (@event is null)
                    return Result<SetSponsorConfigImageResult>.NotFound($"Event {request.EventId} not found");

                if (@event.SponsorConfig is null || !@event.SponsorConfig.IsEnabled)
                {
                    _logger.LogWarning(
                        "SetSponsorConfigImage: sponsor config disabled or absent — banner has no surface to render on");
                    return Result<SetSponsorConfigImageResult>.Failure(
                        "Enable sponsorships for this event before uploading a sponsor banner.");
                }

                var oldBlobName = @event.SponsorConfig.SponsorImageBlobName;
                var oldImageUrl = @event.SponsorConfig.SponsorImageUrl;

                var upload = await _imageService.UploadImageAsync(
                    request.ImageData!, request.FileName, request.EventId, cancellationToken);
                if (!upload.IsSuccess)
                {
                    _logger.LogError(
                        "SetSponsorConfigImage: blob upload failed - {Errors}",
                        string.Join("; ", upload.Errors));
                    return Result<SetSponsorConfigImageResult>.Failure(upload.Errors);
                }

                var newConfigResult = @event.SponsorConfig.WithImage(upload.Value.Url, upload.Value.BlobName);
                if (!newConfigResult.IsSuccess)
                {
                    await _imageService.DeleteImageAsync(upload.Value.Url, cancellationToken);
                    _logger.LogWarning(
                        "SetSponsorConfigImage: WithImage failed - {Error}. Rolled back uploaded blob.",
                        newConfigResult.Error);
                    return Result<SetSponsorConfigImageResult>.Failure(newConfigResult.Errors);
                }

                var setResult = @event.SetSponsorConfiguration(newConfigResult.Value);
                if (!setResult.IsSuccess)
                {
                    await _imageService.DeleteImageAsync(upload.Value.Url, cancellationToken);
                    _logger.LogWarning(
                        "SetSponsorConfigImage: SetSponsorConfiguration failed - {Error}. Rolled back uploaded blob.",
                        setResult.Error);
                    return Result<SetSponsorConfigImageResult>.Failure(setResult.Errors);
                }

                await _unitOfWork.CommitAsync(cancellationToken);

                if (!string.IsNullOrEmpty(oldImageUrl))
                {
                    try
                    {
                        await _imageService.DeleteImageAsync(oldImageUrl, cancellationToken);
                        _logger.LogInformation(
                            "SetSponsorConfigImage: old blob deleted - OldBlob={OldBlob}",
                            oldBlobName);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex,
                            "SetSponsorConfigImage: failed to delete old blob - OldBlob={OldBlob}",
                            oldBlobName);
                    }
                }

                _logger.LogInformation(
                    "SetSponsorConfigImage SUCCESS: NewUrl={Url}, NewBlob={BlobName}",
                    upload.Value.Url, upload.Value.BlobName);

                return Result<SetSponsorConfigImageResult>.Success(
                    new SetSponsorConfigImageResult(upload.Value.Url, upload.Value.BlobName));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SetSponsorConfigImage FAILED");
                throw;
            }
        }
    }
}
