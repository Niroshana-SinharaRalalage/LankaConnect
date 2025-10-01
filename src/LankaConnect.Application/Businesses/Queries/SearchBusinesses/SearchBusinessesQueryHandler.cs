using AutoMapper;
using LankaConnect.Application.Businesses.Common;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Common.Models;
using LankaConnect.Domain.Business;
using LankaConnect.Domain.Business.Specifications;
using LankaConnect.Domain.Common;

namespace LankaConnect.Application.Businesses.Queries.SearchBusinesses;

public class SearchBusinessesQueryHandler : IQueryHandler<SearchBusinessesQuery, PaginatedList<BusinessDto>>
{
    private readonly IBusinessRepository _businessRepository;
    private readonly IMapper _mapper;

    public SearchBusinessesQueryHandler(IBusinessRepository businessRepository, IMapper mapper)
    {
        _businessRepository = businessRepository;
        _mapper = mapper;
    }

    public async Task<Result<PaginatedList<BusinessDto>>> Handle(SearchBusinessesQuery request, CancellationToken cancellationToken)
    {
        var specification = new BusinessSearchSpecification(
            request.SearchTerm,
            request.Category,
            request.City,
            request.Province,
            request.Latitude,
            request.Longitude,
            request.RadiusKm,
            request.MinRating,
            request.IsVerified);

        var businesses = await _businessRepository.GetPagedAsync(
            request.PageNumber,
            request.PageSize,
            null, // We'll need to modify this to handle specifications later
            cancellationToken);

        var businessDtos = _mapper.Map<List<BusinessDto>>(businesses.Items);

        var result = new PaginatedList<BusinessDto>(
            businessDtos,
            businesses.TotalCount,
            request.PageNumber,
            request.PageSize);
        
        return Result<PaginatedList<BusinessDto>>.Success(result);
    }
}