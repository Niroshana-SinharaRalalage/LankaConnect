using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Business;
using LankaConnect.Domain.Common;
using MediatR;
using Microsoft.Extensions.Logging;

namespace LankaConnect.Application.Businesses.Commands.SetPrimaryBusinessImage;

/// <summary>
/// Handler for setting a business image as primary
/// </summary>
public sealed class SetPrimaryBusinessImageCommandHandler : IRequestHandler<SetPrimaryBusinessImageCommand, Result>
{
    private readonly IBusinessRepository _businessRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<SetPrimaryBusinessImageCommandHandler> _logger;

    public SetPrimaryBusinessImageCommandHandler(
        IBusinessRepository businessRepository,
        IUnitOfWork unitOfWork,
        ILogger<SetPrimaryBusinessImageCommandHandler> logger)
    {
        _businessRepository = businessRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result> Handle(SetPrimaryBusinessImageCommand request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Setting primary image {ImageId} for business {BusinessId}", 
                request.ImageId, request.BusinessId);

            // Get business entity
            var business = await _businessRepository.GetByIdAsync(request.BusinessId, cancellationToken);
            if (business == null)
            {
                _logger.LogWarning("Business not found: {BusinessId}", request.BusinessId);
                return Result.Failure("Business not found");
            }

            // Set primary image in the business domain model
            var setPrimaryResult = business.SetPrimaryImage(request.ImageId);
            if (!setPrimaryResult.IsSuccess)
            {
                _logger.LogError("Failed to set primary image for business {BusinessId}: {Errors}",
                    request.BusinessId, string.Join(", ", setPrimaryResult.Errors));
                return setPrimaryResult;
            }

            // Update business in repository
            _businessRepository.Update(business);
            await _unitOfWork.CommitAsync(cancellationToken);

            _logger.LogInformation("Successfully set primary image {ImageId} for business {BusinessId}", 
                request.ImageId, request.BusinessId);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error setting primary image for business {BusinessId}, image {ImageId}",
                request.BusinessId, request.ImageId);
            return Result.Failure("An unexpected error occurred while setting primary image");
        }
    }
}