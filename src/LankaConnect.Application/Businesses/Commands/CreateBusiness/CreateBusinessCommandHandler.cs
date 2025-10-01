using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Business;
using LankaConnect.Domain.Business.ValueObjects;
using LankaConnect.Domain.Common;

namespace LankaConnect.Application.Businesses.Commands.CreateBusiness;

public class CreateBusinessCommandHandler : ICommandHandler<CreateBusinessCommand, Guid>
{
    private readonly IBusinessRepository _businessRepository;
    private readonly LankaConnect.Domain.Common.IUnitOfWork _unitOfWork;

    public CreateBusinessCommandHandler(IBusinessRepository businessRepository, LankaConnect.Domain.Common.IUnitOfWork unitOfWork)
    {
        _businessRepository = businessRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<Guid>> Handle(CreateBusinessCommand request, CancellationToken cancellationToken)
    {
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

        // Create business
        var businessResult = Business.Create(
            profile,
            location,
            contactInfo,
            hours,
            request.Category,
            request.OwnerId);

        if (!businessResult.IsSuccess)
            throw new InvalidOperationException(businessResult.Error);

        var business = businessResult.Value;

        // Add to repository
        await _businessRepository.AddAsync(business, cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);

        return Result<Guid>.Success(business.Id);
    }
}