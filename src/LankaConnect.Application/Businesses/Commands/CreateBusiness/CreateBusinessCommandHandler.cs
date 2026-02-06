using System.Diagnostics;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Business;
using LankaConnect.Domain.Business.ValueObjects;
using LankaConnect.Domain.Common;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace LankaConnect.Application.Businesses.Commands.CreateBusiness;

public class CreateBusinessCommandHandler : ICommandHandler<CreateBusinessCommand, Guid>
{
    private readonly IBusinessRepository _businessRepository;
    private readonly LankaConnect.Domain.Common.IUnitOfWork _unitOfWork;
    private readonly ILogger<CreateBusinessCommandHandler> _logger;

    public CreateBusinessCommandHandler(
        IBusinessRepository businessRepository,
        LankaConnect.Domain.Common.IUnitOfWork unitOfWork,
        ILogger<CreateBusinessCommandHandler> logger)
    {
        _businessRepository = businessRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<Guid>> Handle(CreateBusinessCommand request, CancellationToken cancellationToken)
    {
        using (LogContext.PushProperty("Operation", "CreateBusiness"))
        using (LogContext.PushProperty("EntityType", "Business"))
        using (LogContext.PushProperty("OwnerId", request.OwnerId))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(
                "CreateBusiness START: Name={Name}, Category={Category}, OwnerId={OwnerId}, City={City}",
                request.Name, request.Category, request.OwnerId, request.City);

            try
            {
                if (request.OwnerId == Guid.Empty)
                {
                    stopwatch.Stop();

                    _logger.LogWarning(
                        "CreateBusiness FAILED: Invalid OwnerId - OwnerId={OwnerId}, Duration={ElapsedMs}ms",
                        request.OwnerId, stopwatch.ElapsedMilliseconds);

                    return Result<Guid>.Failure("Owner ID cannot be empty");
                }

                if (string.IsNullOrWhiteSpace(request.Name))
                {
                    stopwatch.Stop();

                    _logger.LogWarning(
                        "CreateBusiness FAILED: Invalid business name - OwnerId={OwnerId}, Duration={ElapsedMs}ms",
                        request.OwnerId, stopwatch.ElapsedMilliseconds);

                    return Result<Guid>.Failure("Business name is required");
                }

                // Create value objects
                var profile = BusinessProfile.Create(
                    request.Name,
                    request.Description,
                    request.Website,
                    null, // SocialMediaLinks
                    new List<string> { "General" }, // Default services
                    new List<string> { "General" } // Default specializations
                ).Value;

                var location = BusinessLocation.Create(
                    request.Address, request.City, request.Province, request.PostalCode, "Sri Lanka",
                    (decimal)request.Latitude, (decimal)request.Longitude).Value;

                var contactInfo = ContactInformation.Create(
                    request.ContactPhone, request.ContactEmail, request.Website).Value;

                var hours = BusinessHours.CreateAlwaysClosed(); // Create default hours

                _logger.LogInformation(
                    "CreateBusiness: Value objects created - Name={Name}, Location={City}, {Province}",
                    request.Name, request.City, request.Province);

                // Create business
                var businessResult = Business.Create(
                    profile,
                    location,
                    contactInfo,
                    hours,
                    request.Category,
                    request.OwnerId);

                if (!businessResult.IsSuccess)
                {
                    stopwatch.Stop();

                    _logger.LogWarning(
                        "CreateBusiness FAILED: Business creation failed - Name={Name}, OwnerId={OwnerId}, Error={Error}, Duration={ElapsedMs}ms",
                        request.Name, request.OwnerId, businessResult.Error, stopwatch.ElapsedMilliseconds);

                    throw new InvalidOperationException(businessResult.Error);
                }

                var business = businessResult.Value;

                using (LogContext.PushProperty("BusinessId", business.Id))
                {
                    // Add to repository
                    await _businessRepository.AddAsync(business, cancellationToken);
                    await _unitOfWork.CommitAsync(cancellationToken);

                    stopwatch.Stop();

                    _logger.LogInformation(
                        "CreateBusiness COMPLETE: BusinessId={BusinessId}, Name={Name}, Category={Category}, OwnerId={OwnerId}, City={City}, Duration={ElapsedMs}ms",
                        business.Id, request.Name, request.Category, request.OwnerId, request.City, stopwatch.ElapsedMilliseconds);

                    return Result<Guid>.Success(business.Id);
                }
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _logger.LogError(ex,
                    "CreateBusiness FAILED: Exception occurred - Name={Name}, OwnerId={OwnerId}, Duration={ElapsedMs}ms, Error={ErrorMessage}",
                    request.Name, request.OwnerId, stopwatch.ElapsedMilliseconds, ex.Message);

                throw;
            }
        }
    }
}