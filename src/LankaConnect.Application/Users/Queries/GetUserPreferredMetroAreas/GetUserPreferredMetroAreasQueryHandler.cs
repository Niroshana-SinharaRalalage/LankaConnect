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

        // Phase 6A.9 FIX: Access shadow navigation directly using EF.Property<>
        // The domain's _preferredMetroAreaIds collection is not synchronized on load
        // Infrastructure layer manages shadow navigation per ADR-009
        var dbContext = _dbContext as Microsoft.EntityFrameworkCore.DbContext
            ?? throw new InvalidOperationException("DbContext must be EF Core DbContext");

        // Load shadow navigation if not already tracked
        var metroAreasCollection = dbContext.Entry(user).Collection("_preferredMetroAreaEntities");
        await metroAreasCollection.LoadAsync(cancellationToken);

        var currentMetroAreas = metroAreasCollection.CurrentValue as ICollection<Domain.Events.MetroArea>
            ?? new List<Domain.Events.MetroArea>();

        // If user has no preferred metro areas, return empty list
        if (!currentMetroAreas.Any())
        {
            return Result<IReadOnlyList<MetroAreaDto>>.Success(new List<MetroAreaDto>());
        }

        // Map loaded entities to DTOs (already have full data)
        var dtos = currentMetroAreas
            .OrderBy(m => m.State)
            .ThenBy(m => m.Name)
            .Select(m => _mapper.Map<MetroAreaDto>(m))
            .ToList();

        return Result<IReadOnlyList<MetroAreaDto>>.Success(dtos);
    }
}
