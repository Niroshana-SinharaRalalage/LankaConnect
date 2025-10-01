using AutoMapper;
using LankaConnect.Application.Businesses.Common;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Business;
using LankaConnect.Domain.Common;

namespace LankaConnect.Application.Businesses.Queries.GetBusiness;

public class GetBusinessQueryHandlerSimple : IQueryHandler<GetBusinessQuery, BusinessDto?>
{
    private readonly IBusinessRepository _businessRepository;

    public GetBusinessQueryHandlerSimple(IBusinessRepository businessRepository)
    {
        _businessRepository = businessRepository;
    }

    public async Task<Result<BusinessDto?>> Handle(GetBusinessQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var business = await _businessRepository.GetByIdAsync(request.Id, cancellationToken);
            if (business == null)
                return Result<BusinessDto?>.Success(null);

            // Simple manual mapping for now
            var businessDto = new BusinessDto
            {
                Id = business.Id,
                Name = business.Profile.Name,
                Description = business.Profile.Description,
                ContactPhone = business.ContactInfo.PhoneNumber?.Value ?? string.Empty,
                ContactEmail = business.ContactInfo.Email?.Value ?? string.Empty,
                Website = business.ContactInfo.Website ?? string.Empty,
                Address = business.Location.Address.Street,
                City = business.Location.Address.City,
                Province = business.Location.Address.State,
                PostalCode = business.Location.Address.ZipCode,
                Latitude = business.Location.Coordinates?.Latitude != null ? (double)business.Location.Coordinates.Latitude : 0,
                Longitude = business.Location.Coordinates?.Longitude != null ? (double)business.Location.Coordinates.Longitude : 0,
                Category = business.Category,
                Status = business.Status,
                Rating = business.Rating,
                ReviewCount = business.ReviewCount,
                IsVerified = business.IsVerified,
                VerifiedAt = business.VerifiedAt ?? DateTime.MinValue,
                OwnerId = business.OwnerId,
                CreatedAt = business.CreatedAt,
                UpdatedAt = business.UpdatedAt ?? business.CreatedAt
            };

            return Result<BusinessDto?>.Success(businessDto);
        }
        catch (Exception ex)
        {
            return Result<BusinessDto?>.Failure($"Error retrieving business: {ex.Message}");
        }
    }
}