using System.Diagnostics;
using AutoMapper;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.MetroAreas.Common;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Serilog.Context;

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
    private readonly ILogger<GetUserPreferredMetroAreasQueryHandler> _logger;

    public GetUserPreferredMetroAreasQueryHandler(
        IUserRepository userRepository,
        IApplicationDbContext dbContext,
        IMapper mapper,
        ILogger<GetUserPreferredMetroAreasQueryHandler> logger)
    {
        _userRepository = userRepository;
        _dbContext = dbContext;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<Result<IReadOnlyList<MetroAreaDto>>> Handle(
        GetUserPreferredMetroAreasQuery request,
        CancellationToken cancellationToken)
    {
        using (LogContext.PushProperty("Operation", "GetUserPreferredMetroAreas"))
        using (LogContext.PushProperty("EntityType", "MetroArea"))
        using (LogContext.PushProperty("UserId", request.UserId))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(
                "GetUserPreferredMetroAreas START: UserId={UserId}",
                request.UserId);

            try
            {
                // Validate request
                if (request.UserId == Guid.Empty)
                {
                    stopwatch.Stop();

                    _logger.LogWarning(
                        "GetUserPreferredMetroAreas FAILED: Invalid UserId - UserId={UserId}, Duration={ElapsedMs}ms",
                        request.UserId, stopwatch.ElapsedMilliseconds);

                    return Result<IReadOnlyList<MetroAreaDto>>.Failure("User ID is required");
                }

                // Get user
                var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
                if (user == null)
                {
                    stopwatch.Stop();

                    _logger.LogWarning(
                        "GetUserPreferredMetroAreas FAILED: User not found - UserId={UserId}, Duration={ElapsedMs}ms",
                        request.UserId, stopwatch.ElapsedMilliseconds);

                    return Result<IReadOnlyList<MetroAreaDto>>.Failure("User not found");
                }

                // Phase 6A.9 FIX: Access shadow navigation directly using EF.Property<>
                // The domain's _preferredMetroAreaIds collection is not synchronized on load
                // Infrastructure layer manages shadow navigation per ADR-009
                var efDbContext = _dbContext as Microsoft.EntityFrameworkCore.DbContext
                    ?? throw new InvalidOperationException("DbContext must be EF Core DbContext");

                // Load shadow navigation if not already tracked
                var metroAreasCollection = efDbContext.Entry(user).Collection("_preferredMetroAreaEntities");
                await metroAreasCollection.LoadAsync(cancellationToken);

                var currentMetroAreas = metroAreasCollection.CurrentValue as ICollection<Domain.Events.MetroArea>
                    ?? new List<Domain.Events.MetroArea>();

                // If user has no preferred metro areas, return empty list
                if (!currentMetroAreas.Any())
                {
                    stopwatch.Stop();

                    _logger.LogInformation(
                        "GetUserPreferredMetroAreas COMPLETE: UserId={UserId}, MetroAreaCount=0, Duration={ElapsedMs}ms",
                        request.UserId, stopwatch.ElapsedMilliseconds);

                    return Result<IReadOnlyList<MetroAreaDto>>.Success(new List<MetroAreaDto>());
                }

                // Map loaded entities to DTOs (already have full data)
                var dtos = currentMetroAreas
                    .OrderBy(m => m.State)
                    .ThenBy(m => m.Name)
                    .Select(m => _mapper.Map<MetroAreaDto>(m))
                    .ToList();

                stopwatch.Stop();

                _logger.LogInformation(
                    "GetUserPreferredMetroAreas COMPLETE: UserId={UserId}, MetroAreaCount={MetroAreaCount}, Duration={ElapsedMs}ms",
                    request.UserId, dtos.Count, stopwatch.ElapsedMilliseconds);

                return Result<IReadOnlyList<MetroAreaDto>>.Success(dtos);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _logger.LogError(ex,
                    "GetUserPreferredMetroAreas FAILED: Exception occurred - UserId={UserId}, Duration={ElapsedMs}ms, Error={ErrorMessage}",
                    request.UserId, stopwatch.ElapsedMilliseconds, ex.Message);

                throw;
            }
        }
    }
}
