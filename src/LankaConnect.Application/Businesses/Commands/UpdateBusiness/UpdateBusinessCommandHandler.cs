using System.Diagnostics;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Business.ValueObjects;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Common.Exceptions;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace LankaConnect.Application.Businesses.Commands.UpdateBusiness;

public class UpdateBusinessCommandHandler : ICommandHandler<UpdateBusinessCommand>
{
    private readonly IBusinessRepository _businessRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UpdateBusinessCommandHandler> _logger;

    public UpdateBusinessCommandHandler(
        IBusinessRepository businessRepository,
        IUnitOfWork unitOfWork,
        ILogger<UpdateBusinessCommandHandler> logger)
    {
        _businessRepository = businessRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result> Handle(UpdateBusinessCommand request, CancellationToken cancellationToken)
    {
        using (LogContext.PushProperty("Operation", "UpdateBusiness"))
        using (LogContext.PushProperty("EntityType", "Business"))
        using (LogContext.PushProperty("BusinessId", request.Id))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(
                "UpdateBusiness START: BusinessId={BusinessId}, Name={Name}, City={City}",
                request.Id, request.Name, request.City);

            try
            {
                if (request.Id == Guid.Empty)
                {
                    stopwatch.Stop();

                    _logger.LogWarning(
                        "UpdateBusiness FAILED: Invalid BusinessId - BusinessId={BusinessId}, Duration={ElapsedMs}ms",
                        request.Id, stopwatch.ElapsedMilliseconds);

                    return Result.Failure("Business ID cannot be empty");
                }

                if (string.IsNullOrWhiteSpace(request.Name))
                {
                    stopwatch.Stop();

                    _logger.LogWarning(
                        "UpdateBusiness FAILED: Invalid business name - BusinessId={BusinessId}, Duration={ElapsedMs}ms",
                        request.Id, stopwatch.ElapsedMilliseconds);

                    return Result.Failure("Business name is required");
                }

                var business = await _businessRepository.GetByIdAsync(request.Id, cancellationToken);

                if (business == null)
                {
                    stopwatch.Stop();

                    _logger.LogWarning(
                        "UpdateBusiness FAILED: Business not found - BusinessId={BusinessId}, Duration={ElapsedMs}ms",
                        request.Id, stopwatch.ElapsedMilliseconds);

                    throw new BusinessNotFoundException(request.Id);
                }

                // Update profile
                var profile = BusinessProfile.Create(
                    request.Name,
                    request.Description,
                    request.Website,
                    null, // SocialMediaLinks
                    new List<string> { "General" }, // Default services
                    new List<string> { "General" } // Default specializations
                ).Value;
                business.UpdateProfile(profile);

                _logger.LogInformation(
                    "UpdateBusiness: Profile updated - BusinessId={BusinessId}, Name={Name}",
                    request.Id, request.Name);

                // Update location
                var location = BusinessLocation.Create(
                    request.Address, request.City, request.Province, request.PostalCode, "Sri Lanka",
                    (decimal)request.Latitude, (decimal)request.Longitude).Value;
                business.UpdateLocation(location);

                _logger.LogInformation(
                    "UpdateBusiness: Location updated - BusinessId={BusinessId}, City={City}, Province={Province}",
                    request.Id, request.City, request.Province);

                // Update contact info
                var contactInfo = ContactInformation.Create(
                    request.ContactPhone, request.ContactEmail, request.Website).Value;
                business.UpdateContactInfo(contactInfo);

                _logger.LogInformation(
                    "UpdateBusiness: Contact info updated - BusinessId={BusinessId}, Email={Email}",
                    request.Id, request.ContactEmail);

                await _unitOfWork.CommitAsync(cancellationToken);

                stopwatch.Stop();

                _logger.LogInformation(
                    "UpdateBusiness COMPLETE: BusinessId={BusinessId}, Name={Name}, City={City}, Duration={ElapsedMs}ms",
                    request.Id, request.Name, request.City, stopwatch.ElapsedMilliseconds);

                return Result.Success();
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _logger.LogError(ex,
                    "UpdateBusiness FAILED: Exception occurred - BusinessId={BusinessId}, Duration={ElapsedMs}ms, Error={ErrorMessage}",
                    request.Id, stopwatch.ElapsedMilliseconds, ex.Message);

                throw;
            }
        }
    }
}