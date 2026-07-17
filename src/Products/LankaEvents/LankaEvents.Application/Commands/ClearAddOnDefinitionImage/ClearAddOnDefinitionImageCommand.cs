using LankaConnect.BuildingBlocks.Application.Common.Interfaces;
using LankaConnect.BuildingBlocks.Domain;
using LankaConnect.Products.LankaEvents.Domain.Repositories;
using LankaConnect.Products.LankaEvents.Infrastructure.Data; // Wave 8.5.g
using MediatR;
using Microsoft.EntityFrameworkCore; // Wave 8.5.g
using Microsoft.Extensions.Logging;
using Serilog.Context;
using LankaConnect.Modules.Media.Contracts.LegacyPromotions; // 4C.h Day 5: IImageService promoted
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
    private readonly LankaEventsDbContext _dbContext; // Wave 8.5.g direct-SaveChanges
    private readonly ILogger<ClearAddOnDefinitionImageCommandHandler> _logger;

    public ClearAddOnDefinitionImageCommandHandler(
        IAddOnDefinitionRepository definitionRepository,
        IImageService imageService,
        IUnitOfWork unitOfWork,
        LankaEventsDbContext dbContext,
        ILogger<ClearAddOnDefinitionImageCommandHandler> logger)
    {
        _definitionRepository = definitionRepository;
        _imageService = imageService;
        _unitOfWork = unitOfWork;
        _dbContext = dbContext;
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

                await _dbContext.SaveChangesAsync(cancellationToken); // Wave 8.5.g: direct-SaveChanges on LankaEventsDbContext (was IUnitOfWork = 0 changes on AppDbContext)

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
