using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Business;
using LankaConnect.Domain.Common;
using MediatR;
using Microsoft.Extensions.Logging;

namespace LankaConnect.Application.Businesses.Commands.ReorderBusinessImages;

/// <summary>
/// Handler for reordering business images
/// </summary>
public sealed class ReorderBusinessImagesCommandHandler : IRequestHandler<ReorderBusinessImagesCommand, Result>
{
    private readonly IBusinessRepository _businessRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ReorderBusinessImagesCommandHandler> _logger;

    public ReorderBusinessImagesCommandHandler(
        IBusinessRepository businessRepository,
        IUnitOfWork unitOfWork,
        ILogger<ReorderBusinessImagesCommandHandler> logger)
    {
        _businessRepository = businessRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result> Handle(ReorderBusinessImagesCommand request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Reordering images for business {BusinessId}", request.BusinessId);

            // Get business entity
            var business = await _businessRepository.GetByIdAsync(request.BusinessId, cancellationToken);
            if (business == null)
            {
                _logger.LogWarning("Business not found: {BusinessId}", request.BusinessId);
                return Result.Failure("Business not found");
            }

            // Reorder images in the business domain model
            var reorderResult = business.ReorderImages(request.ImageIds);
            if (!reorderResult.IsSuccess)
            {
                _logger.LogError("Failed to reorder images for business {BusinessId}: {Errors}",
                    request.BusinessId, string.Join(", ", reorderResult.Errors));
                return reorderResult;
            }

            // Update business in repository
            _businessRepository.Update(business);
            await _unitOfWork.CommitAsync(cancellationToken);

            _logger.LogInformation("Successfully reordered images for business {BusinessId}", request.BusinessId);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during image reordering for business {BusinessId}",
                request.BusinessId);
            return Result.Failure("An unexpected error occurred during image reordering");
        }
    }
}