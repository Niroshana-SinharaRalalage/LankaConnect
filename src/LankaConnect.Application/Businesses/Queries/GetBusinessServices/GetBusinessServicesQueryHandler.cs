using AutoMapper;
using LankaConnect.Application.Businesses.Common;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Business;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Common.Exceptions;

namespace LankaConnect.Application.Businesses.Queries.GetBusinessServices;

public class GetBusinessServicesQueryHandler : IQueryHandler<GetBusinessServicesQuery, List<ServiceDto>>
{
    private readonly IBusinessRepository _businessRepository;
    private readonly IMapper _mapper;

    public GetBusinessServicesQueryHandler(IBusinessRepository businessRepository, IMapper mapper)
    {
        _businessRepository = businessRepository;
        _mapper = mapper;
    }

    public async Task<Result<List<ServiceDto>>> Handle(GetBusinessServicesQuery request, CancellationToken cancellationToken)
    {
        var business = await _businessRepository.GetByIdAsync(request.BusinessId, cancellationToken);
        if (business == null)
            throw new BusinessNotFoundException(request.BusinessId);

        var result = _mapper.Map<List<ServiceDto>>(business.Services);
        return Result<List<ServiceDto>>.Success(result);
    }
}