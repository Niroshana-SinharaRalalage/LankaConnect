using System.Diagnostics;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Common.Exceptions;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace LankaConnect.Application.Businesses.Commands.DeleteBusiness;

public class DeleteBusinessCommandHandler : ICommandHandler<DeleteBusinessCommand>
{
    private readonly IBusinessRepository _businessRepository;
    private readonly LankaConnect.Domain.Common.IUnitOfWork _unitOfWork;
    private readonly ILogger<DeleteBusinessCommandHandler> _logger;

    public DeleteBusinessCommandHandler(
        IBusinessRepository businessRepository,
        LankaConnect.Domain.Common.IUnitOfWork unitOfWork,
        ILogger<DeleteBusinessCommandHandler> logger)
    {
        _businessRepository = businessRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result> Handle(DeleteBusinessCommand request, CancellationToken cancellationToken)
    {
        using (LogContext.PushProperty("Operation", "DeleteBusiness"))
        using (LogContext.PushProperty("EntityType", "Business"))
        using (LogContext.PushProperty("BusinessId", request.Id))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(
                "DeleteBusiness START: BusinessId={BusinessId}",
                request.Id);

            try
            {
                if (request.Id == Guid.Empty)
                {
                    stopwatch.Stop();

                    _logger.LogWarning(
                        "DeleteBusiness FAILED: Invalid BusinessId - BusinessId={BusinessId}, Duration={ElapsedMs}ms",
                        request.Id, stopwatch.ElapsedMilliseconds);

                    return Result.Failure("Business ID cannot be empty");
                }

                var business = await _businessRepository.GetByIdAsync(request.Id, cancellationToken);

                if (business == null)
                {
                    stopwatch.Stop();

                    _logger.LogWarning(
                        "DeleteBusiness FAILED: Business not found - BusinessId={BusinessId}, Duration={ElapsedMs}ms",
                        request.Id, stopwatch.ElapsedMilliseconds);

                    throw new BusinessNotFoundException(request.Id);
                }

                _logger.LogInformation(
                    "DeleteBusiness: Deleting business - BusinessId={BusinessId}, Name={Name}",
                    request.Id, business.Profile.Name);

                await _businessRepository.DeleteAsync(request.Id, cancellationToken);
                await _unitOfWork.CommitAsync(cancellationToken);

                stopwatch.Stop();

                _logger.LogInformation(
                    "DeleteBusiness COMPLETE: BusinessId={BusinessId}, Duration={ElapsedMs}ms",
                    request.Id, stopwatch.ElapsedMilliseconds);

                return Result.Success();
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _logger.LogError(ex,
                    "DeleteBusiness FAILED: Exception occurred - BusinessId={BusinessId}, Duration={ElapsedMs}ms, Error={ErrorMessage}",
                    request.Id, stopwatch.ElapsedMilliseconds, ex.Message);

                throw;
            }
        }
    }
}