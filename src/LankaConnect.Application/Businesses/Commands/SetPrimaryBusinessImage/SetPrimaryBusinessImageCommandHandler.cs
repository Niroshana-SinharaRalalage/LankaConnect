using System.Diagnostics;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Business;
using LankaConnect.Domain.Common;
using MediatR;
using Microsoft.Extensions.Logging;
using Serilog.Context;

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
        using (LogContext.PushProperty("Operation", "SetPrimaryBusinessImage"))
        using (LogContext.PushProperty("EntityType", "Business"))
        using (LogContext.PushProperty("BusinessId", request.BusinessId))
        using (LogContext.PushProperty("ImageId", request.ImageId))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(
                "SetPrimaryBusinessImage START: BusinessId={BusinessId}, ImageId={ImageId}",
                request.BusinessId, request.ImageId);

            try
            {
                if (request.BusinessId == Guid.Empty)
                {
                    stopwatch.Stop();

                    _logger.LogWarning(
                        "SetPrimaryBusinessImage FAILED: Invalid BusinessId - BusinessId={BusinessId}, ImageId={ImageId}, Duration={ElapsedMs}ms",
                        request.BusinessId, request.ImageId, stopwatch.ElapsedMilliseconds);

                    return Result.Failure("Business ID cannot be empty");
                }

                if (string.IsNullOrWhiteSpace(request.ImageId))
                {
                    stopwatch.Stop();

                    _logger.LogWarning(
                        "SetPrimaryBusinessImage FAILED: Invalid ImageId - BusinessId={BusinessId}, ImageId={ImageId}, Duration={ElapsedMs}ms",
                        request.BusinessId, request.ImageId, stopwatch.ElapsedMilliseconds);

                    return Result.Failure("Image ID cannot be empty");
                }

                // Get business entity
                var business = await _businessRepository.GetByIdAsync(request.BusinessId, cancellationToken);

                if (business == null)
                {
                    stopwatch.Stop();

                    _logger.LogWarning(
                        "SetPrimaryBusinessImage FAILED: Business not found - BusinessId={BusinessId}, ImageId={ImageId}, Duration={ElapsedMs}ms",
                        request.BusinessId, request.ImageId, stopwatch.ElapsedMilliseconds);

                    return Result.Failure("Business not found");
                }

                _logger.LogInformation(
                    "SetPrimaryBusinessImage: Setting primary image - BusinessId={BusinessId}, ImageId={ImageId}, CurrentImageCount={ImageCount}",
                    request.BusinessId, request.ImageId, business.Images.Count);

                // Set primary image in the business domain model
                var setPrimaryResult = business.SetPrimaryImage(request.ImageId);

                if (!setPrimaryResult.IsSuccess)
                {
                    stopwatch.Stop();

                    _logger.LogWarning(
                        "SetPrimaryBusinessImage FAILED: Operation failed - BusinessId={BusinessId}, ImageId={ImageId}, Error={Error}, Duration={ElapsedMs}ms",
                        request.BusinessId, request.ImageId, string.Join(", ", setPrimaryResult.Errors), stopwatch.ElapsedMilliseconds);

                    return setPrimaryResult;
                }

                // Update business in repository
                _businessRepository.Update(business);
                await _unitOfWork.CommitAsync(cancellationToken);

                stopwatch.Stop();

                _logger.LogInformation(
                    "SetPrimaryBusinessImage COMPLETE: BusinessId={BusinessId}, ImageId={ImageId}, Duration={ElapsedMs}ms",
                    request.BusinessId, request.ImageId, stopwatch.ElapsedMilliseconds);

                return Result.Success();
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _logger.LogError(ex,
                    "SetPrimaryBusinessImage FAILED: Exception occurred - BusinessId={BusinessId}, ImageId={ImageId}, Duration={ElapsedMs}ms, Error={ErrorMessage}",
                    request.BusinessId, request.ImageId, stopwatch.ElapsedMilliseconds, ex.Message);

                return Result.Failure("An unexpected error occurred while setting primary image");
            }
        }
    }
}