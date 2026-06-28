using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;
using LankaConnect.Products.LankaEvents.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace LankaConnect.Products.LankaEvents.Application.Commands.ClearAddOnDefinitionImage;

/// <summary>
/// Phase 6A.143 — clears the display image from a single add-on definition.
/// Idempotent: succeeds with no-op if there was no image to clear.
/// Deletes the blob from storage best-effort after the entity is persisted.
/// </summary>
public record ClearAddOnDefinitionImageCommand : IRequest<Result>
{
    public Guid EventId { get; init; }
    public Guid DefinitionId { get; init; }
}

public class ClearAddOnDefinitionImageCommandHandler
    : IRequestHandler<ClearAddOnDefinitionImageCommand, Result>
{
    private readonly IAddOnDefinitionRepository _definitionRepository;
    private readonly IImageService _imageService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ClearAddOnDefinitionImageCommandHandler> _logger;

    public ClearAddOnDefinitionImageCommandHandler(
        IAddOnDefinitionRepository definitionRepository,
        IImageService imageService,
        IUnitOfWork unitOfWork,
        ILogger<ClearAddOnDefinitionImageCommandHandler> logger)
    {
        _definitionRepository = definitionRepository;
        _imageService = imageService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result> Handle(
        ClearAddOnDefinitionImageCommand request, CancellationToken cancellationToken)
    {
        using (LogContext.PushProperty("Operation", "ClearAddOnDefinitionImage"))
        using (LogContext.PushProperty("EventId", request.EventId))
        using (LogContext.PushProperty("DefinitionId", request.DefinitionId))
        {
            _logger.LogInformation("ClearAddOnDefinitionImage START");

            try
            {
                var definition = await _definitionRepository.GetByIdAsync(request.DefinitionId, cancellationToken);
                if (definition is null || definition.EventId != request.EventId)
                {
                    _logger.LogWarning("ClearAddOnDefinitionImage: definition not found or wrong event");
                    return Result.NotFound(
                        $"Add-on definition {request.DefinitionId} not found for event {request.EventId}");
                }

                var oldImageUrl = definition.ImageUrl;

                var clearResult = definition.ClearImage();
                if (!clearResult.IsSuccess)
                {
                    _logger.LogWarning(
                        "ClearAddOnDefinitionImage: domain ClearImage failed - {Error}",
                        clearResult.Error);
                    return clearResult;
                }

                await _unitOfWork.CommitAsync(cancellationToken);

                if (!string.IsNullOrEmpty(oldImageUrl))
                {
                    try
                    {
                        await _imageService.DeleteImageAsync(oldImageUrl, cancellationToken);
                        _logger.LogInformation("ClearAddOnDefinitionImage: old blob deleted");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex,
                            "ClearAddOnDefinitionImage: failed to delete old blob. " +
                            "Forensic gap acceptable; image cleared on entity.");
                    }
                }

                _logger.LogInformation("ClearAddOnDefinitionImage SUCCESS");
                return Result.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ClearAddOnDefinitionImage FAILED");
                throw;
            }
        }
    }
}
