using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events;
using LankaConnect.Domain.Events.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace LankaConnect.Application.Events.Commands.ClearSponsorConfigImage;

/// <summary>
/// Phase 6A.143 — clears the sponsor banner image from an event's SponsorConfiguration.
/// Idempotent: succeeds with no-op if there was no banner to clear.
/// </summary>
public record ClearSponsorConfigImageCommand : IRequest<Result>
{
    public Guid EventId { get; init; }
}

public class ClearSponsorConfigImageCommandHandler
    : IRequestHandler<ClearSponsorConfigImageCommand, Result>
{
    private readonly IEventRepository _eventRepository;
    private readonly IImageService _imageService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ClearSponsorConfigImageCommandHandler> _logger;

    public ClearSponsorConfigImageCommandHandler(
        IEventRepository eventRepository,
        IImageService imageService,
        IUnitOfWork unitOfWork,
        ILogger<ClearSponsorConfigImageCommandHandler> logger)
    {
        _eventRepository = eventRepository;
        _imageService = imageService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result> Handle(
        ClearSponsorConfigImageCommand request, CancellationToken cancellationToken)
    {
        using (LogContext.PushProperty("Operation", "ClearSponsorConfigImage"))
        using (LogContext.PushProperty("EventId", request.EventId))
        {
            _logger.LogInformation("ClearSponsorConfigImage START");

            try
            {
                var @event = await _eventRepository.GetByIdAsync(request.EventId, cancellationToken);
                if (@event is null)
                    return Result.NotFound($"Event {request.EventId} not found");

                if (@event.SponsorConfig is null)
                {
                    // No config to clear from — treat as no-op success.
                    _logger.LogInformation("ClearSponsorConfigImage: no sponsor config exists; nothing to clear");
                    return Result.Success();
                }

                var oldImageUrl = @event.SponsorConfig.SponsorImageUrl;
                if (string.IsNullOrEmpty(oldImageUrl))
                {
                    _logger.LogInformation("ClearSponsorConfigImage: no banner image set; nothing to clear");
                    return Result.Success();
                }

                var newConfig = @event.SponsorConfig.WithoutImage();
                var setResult = @event.SetSponsorConfiguration(newConfig);
                if (!setResult.IsSuccess)
                {
                    _logger.LogWarning(
                        "ClearSponsorConfigImage: SetSponsorConfiguration failed - {Error}",
                        setResult.Error);
                    return setResult;
                }

                await _unitOfWork.CommitAsync(cancellationToken);

                try
                {
                    await _imageService.DeleteImageAsync(oldImageUrl, cancellationToken);
                    _logger.LogInformation("ClearSponsorConfigImage: old blob deleted");
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex,
                        "ClearSponsorConfigImage: failed to delete old blob. " +
                        "Forensic gap acceptable; banner cleared on entity.");
                }

                _logger.LogInformation("ClearSponsorConfigImage SUCCESS");
                return Result.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ClearSponsorConfigImage FAILED");
                throw;
            }
        }
    }
}
