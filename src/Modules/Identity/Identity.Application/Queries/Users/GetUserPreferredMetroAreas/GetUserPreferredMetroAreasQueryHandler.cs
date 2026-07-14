using System.Diagnostics;
using AutoMapper;
using LankaConnect.BuildingBlocks.Application.Common.Interfaces;
using LankaConnect.SharedKernel.Geo.MetroAreas.Common;
using LankaConnect.BuildingBlocks.Domain;
using LankaConnect.Modules.Identity.Domain.Entities;
using LankaConnect.Modules.Identity.Domain.Repositories;
using LankaConnect.Modules.Identity.Domain.DomainEvents;
using LankaConnect.Modules.Identity.Domain.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Serilog.Context;
using LankaConnect.Modules.Identity.Infrastructure.Data; // Wave 6.5.f mirror (2026-07-09 Day 4): IdentityDbContext
using LankaConnect.Products.LankaEvents.Infrastructure.Data; // Sprint-Day 7 hotfix: LankaEventsDbContext for MetroArea reads
namespace LankaConnect.Modules.Identity.Application.Queries.Users.GetUserPreferredMetroAreas;

/// <summary>
/// Handler for GetUserPreferredMetroAreasQuery
/// Returns full metro area details for user's preferred metros
/// Phase 5A: User Preferred Metro Areas
/// </summary>
public class GetUserPreferredMetroAreasQueryHandler : IQueryHandler<GetUserPreferredMetroAreasQuery, IReadOnlyList<MetroAreaDto>>
{
    private readonly IUserRepository _userRepository;
    private readonly IdentityDbContext _dbContext;
    // Sprint-Day 7 (2026-07-14) hotfix: cross-module MetroArea reads route through
    // LankaEventsDbContext. IdentityDbContext Ignores<MetroArea> per Blueprint §7.8
    // so shadow-nav LoadAsync throws at runtime — use scalar-list hydration instead.
    private readonly LankaEventsDbContext _eventsContext;
    private readonly IMapper _mapper;
    private readonly ILogger<GetUserPreferredMetroAreasQueryHandler> _logger;

    public GetUserPreferredMetroAreasQueryHandler(
        IUserRepository userRepository,
        IdentityDbContext dbContext,
        LankaEventsDbContext eventsContext,
        IMapper mapper,
        ILogger<GetUserPreferredMetroAreasQueryHandler> logger)
    {
        _userRepository = userRepository;
        _dbContext = dbContext;
        _eventsContext = eventsContext;
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

                // Sprint-Day 7 (2026-07-14) hotfix: replaced shadow-nav LoadAsync (which throws
                // under IdentityDbContext because MetroArea is Ignored per Blueprint §7.8) with
                // scalar-list hydration. Read User.PreferredMetroAreaIds (populated from the
                // junction table via UserRepository.GetByIdAsync's guarded shadow-nav load, when
                // AppDbContext model exposes it), then fetch full MetroArea entities via
                // LankaEventsDbContext (the owning context).
                var preferredIds = user.PreferredMetroAreaIds?.ToList() ?? new List<Guid>();
                var currentMetroAreas = preferredIds.Any()
                    ? await _eventsContext.MetroAreas
                        .Where(m => preferredIds.Contains(m.Id))
                        .ToListAsync(cancellationToken)
                    : new List<LankaConnect.Products.LankaEvents.Domain.MetroArea>();

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
