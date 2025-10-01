using AutoMapper;
using LankaConnect.Application.Businesses.Common;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Business;
using LankaConnect.Domain.Common;

namespace LankaConnect.Application.Businesses.Queries.GetBusiness;

public class GetBusinessQueryHandler : IQueryHandler<GetBusinessQuery, BusinessDto?>
{
    private readonly IBusinessRepository _businessRepository;
    private readonly IMapper _mapper;

    public GetBusinessQueryHandler(IBusinessRepository businessRepository, IMapper mapper)
    {
        _businessRepository = businessRepository;
        _mapper = mapper;
    }

    public async Task<Result<BusinessDto?>> Handle(GetBusinessQuery request, CancellationToken cancellationToken)
    {
        var business = await _businessRepository.GetByIdAsync(request.Id, cancellationToken);
        var result = business == null ? null : _mapper.Map<BusinessDto>(business);
        return Result<BusinessDto?>.Success(result);
    }
}