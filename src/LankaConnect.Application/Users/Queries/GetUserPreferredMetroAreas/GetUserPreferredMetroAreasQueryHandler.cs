using AutoMapper;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.MetroAreas.Common;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace LankaConnect.Application.Users.Queries.GetUserPreferredMetroAreas;

/// <summary>
/// Handler for GetUserPreferredMetroAreasQuery
/// Returns full metro area details for user's preferred metros
/// Phase 5A: User Preferred Metro Areas
/// </summary>
public class GetUserPreferredMetroAreasQueryHandler : IQueryHandler<GetUserPreferredMetroAreasQuery, IReadOnlyList<MetroAreaDto>>
{
    private readonly IUserRepository _userRepository;
    private readonly IApplicationDbContext _dbContext;
    private readonly IMapper _mapper;

    public GetUserPreferredMetroAreasQueryHandler(
        IUserRepository userRepository,
        IApplicationDbContext dbContext,
        IMapper mapper)
    {
        _userRepository = userRepository;
        _dbContext = dbContext;
        _mapper = mapper;
    }

    public async Task<Result<IReadOnlyList<MetroAreaDto>>> Handle(
        GetUserPreferredMetroAreasQuery request,
        CancellationToken cancellationToken)
    {
        // Get user
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (user == null)
        {
            return Result<IReadOnlyList<MetroAreaDto>>.Failure("User not found");
        }

        // If user has no preferred metro areas, return empty list
        if (!user.PreferredMetroAreaIds.Any())
        {
            return Result<IReadOnlyList<MetroAreaDto>>.Success(new List<MetroAreaDto>());
        }

        // Get metro area details for user's preferred metros
        var metroAreas = await _dbContext.MetroAreas
            .Where(m => user.PreferredMetroAreaIds.Contains(m.Id))
            .OrderBy(m => m.State)
            .ThenBy(m => m.Name)
            .ToListAsync(cancellationToken);

        var dtos = metroAreas.Select(m => _mapper.Map<MetroAreaDto>(m)).ToList();

        return Result<IReadOnlyList<MetroAreaDto>>.Success(dtos);
    }
}
