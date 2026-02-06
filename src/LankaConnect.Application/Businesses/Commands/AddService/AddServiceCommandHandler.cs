using System.Diagnostics;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Business;
using LankaConnect.Domain.Business.ValueObjects;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Common.Exceptions;
using DomainUnitOfWork = LankaConnect.Domain.Common.IUnitOfWork;
using LankaConnect.Domain.Shared.ValueObjects;
using LankaConnect.Domain.Shared.Enums;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace LankaConnect.Application.Businesses.Commands.AddService;

public class AddServiceCommandHandler : ICommandHandler<AddServiceCommand, Guid>
{
    private readonly IBusinessRepository _businessRepository;
    private readonly DomainUnitOfWork _unitOfWork;
    private readonly ILogger<AddServiceCommandHandler> _logger;

    public AddServiceCommandHandler(
        IBusinessRepository businessRepository,
        DomainUnitOfWork unitOfWork,
        ILogger<AddServiceCommandHandler> logger)
    {
        _businessRepository = businessRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<Guid>> Handle(AddServiceCommand request, CancellationToken cancellationToken)
    {
        using (LogContext.PushProperty("Operation", "AddService"))
        using (LogContext.PushProperty("EntityType", "Business"))
        using (LogContext.PushProperty("BusinessId", request.BusinessId))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(
                "AddService START: BusinessId={BusinessId}, ServiceName={ServiceName}, Price={Price}, IsAvailable={IsAvailable}",
                request.BusinessId, request.Name, request.Price, request.IsAvailable);

            try
            {
                if (request.BusinessId == Guid.Empty)
                {
                    stopwatch.Stop();

                    _logger.LogWarning(
                        "AddService FAILED: Invalid BusinessId - BusinessId={BusinessId}, Duration={ElapsedMs}ms",
                        request.BusinessId, stopwatch.ElapsedMilliseconds);

                    return Result<Guid>.Failure("Business ID cannot be empty");
                }

                if (string.IsNullOrWhiteSpace(request.Name))
                {
                    stopwatch.Stop();

                    _logger.LogWarning(
                        "AddService FAILED: Invalid service name - BusinessId={BusinessId}, Duration={ElapsedMs}ms",
                        request.BusinessId, stopwatch.ElapsedMilliseconds);

                    return Result<Guid>.Failure("Service name is required");
                }

                if (request.Price < 0)
                {
                    stopwatch.Stop();

                    _logger.LogWarning(
                        "AddService FAILED: Invalid price - BusinessId={BusinessId}, Price={Price}, Duration={ElapsedMs}ms",
                        request.BusinessId, request.Price, stopwatch.ElapsedMilliseconds);

                    return Result<Guid>.Failure("Service price cannot be negative");
                }

                var business = await _businessRepository.GetByIdAsync(request.BusinessId, cancellationToken);

                if (business == null)
                {
                    stopwatch.Stop();

                    _logger.LogWarning(
                        "AddService FAILED: Business not found - BusinessId={BusinessId}, Duration={ElapsedMs}ms",
                        request.BusinessId, stopwatch.ElapsedMilliseconds);

                    throw new BusinessNotFoundException(request.BusinessId);
                }

                // Create service with USD pricing for US businesses
                var serviceResult = Service.Create(
                    request.Name,
                    request.Description,
                    Money.Create(request.Price, Currency.USD).Value,
                    request.Duration,
                    request.BusinessId);

                if (!serviceResult.IsSuccess)
                {
                    stopwatch.Stop();

                    _logger.LogWarning(
                        "AddService FAILED: Service creation failed - BusinessId={BusinessId}, Error={Error}, Duration={ElapsedMs}ms",
                        request.BusinessId, serviceResult.Error, stopwatch.ElapsedMilliseconds);

                    throw new InvalidOperationException(serviceResult.Error);
                }

                var service = serviceResult.Value;

                using (LogContext.PushProperty("ServiceId", service.Id))
                {
                    if (!request.IsAvailable)
                    {
                        service.Deactivate();

                        _logger.LogInformation(
                            "AddService: Service deactivated - BusinessId={BusinessId}, ServiceId={ServiceId}",
                            request.BusinessId, service.Id);
                    }

                    // Add to business
                    var addResult = business.AddService(service);
                    if (!addResult.IsSuccess)
                    {
                        stopwatch.Stop();

                        _logger.LogWarning(
                            "AddService FAILED: Failed to add service to business - BusinessId={BusinessId}, ServiceId={ServiceId}, Error={Error}, Duration={ElapsedMs}ms",
                            request.BusinessId, service.Id, addResult.Error, stopwatch.ElapsedMilliseconds);

                        throw new InvalidOperationException(addResult.Error);
                    }

                    await _unitOfWork.CommitAsync(cancellationToken);

                    stopwatch.Stop();

                    _logger.LogInformation(
                        "AddService COMPLETE: BusinessId={BusinessId}, ServiceId={ServiceId}, ServiceName={ServiceName}, Price={Price}, IsAvailable={IsAvailable}, Duration={ElapsedMs}ms",
                        request.BusinessId, service.Id, request.Name, request.Price, request.IsAvailable, stopwatch.ElapsedMilliseconds);

                    return Result<Guid>.Success(service.Id);
                }
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _logger.LogError(ex,
                    "AddService FAILED: Exception occurred - BusinessId={BusinessId}, Duration={ElapsedMs}ms, Error={ErrorMessage}",
                    request.BusinessId, stopwatch.ElapsedMilliseconds, ex.Message);

                throw;
            }
        }
    }
}