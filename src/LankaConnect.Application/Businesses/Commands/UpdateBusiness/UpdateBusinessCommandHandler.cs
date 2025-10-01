using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Business.ValueObjects;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Common.Exceptions;

namespace LankaConnect.Application.Businesses.Commands.UpdateBusiness;

public class UpdateBusinessCommandHandler : ICommandHandler<UpdateBusinessCommand>
{
    private readonly IBusinessRepository _businessRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateBusinessCommandHandler(IBusinessRepository businessRepository, IUnitOfWork unitOfWork)
    {
        _businessRepository = businessRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(UpdateBusinessCommand request, CancellationToken cancellationToken)
    {
        var business = await _businessRepository.GetByIdAsync(request.Id, cancellationToken);
        if (business == null)
            throw new BusinessNotFoundException(request.Id);

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

        // Update location
        var location = BusinessLocation.Create(
            request.Address, request.City, request.Province, request.PostalCode, "Sri Lanka",
            (decimal)request.Latitude, (decimal)request.Longitude).Value;
        business.UpdateLocation(location);

        // Update contact info
        var contactInfo = ContactInformation.Create(
            request.ContactPhone, request.ContactEmail, request.Website).Value;
        business.UpdateContactInfo(contactInfo);

        await _unitOfWork.CommitAsync(cancellationToken);
        return Result.Success();
    }
}