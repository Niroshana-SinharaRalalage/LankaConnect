using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using LankaConnect.Application.Dashboard.Queries.GetCommunityStats;

namespace LankaConnect.API.Controllers;

/// <summary>
/// Phase 6A.69: Public endpoints that don't require authentication
/// Used for landing page hero statistics and other public data
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class PublicController : BaseController<PublicController>
{
    public PublicController(IMediator mediator, ILogger<PublicController> logger)
        : base(mediator, logger)
    {
    }

    /// <summary>
    /// Get community statistics for landing page hero section
    /// Public endpoint - no authentication required
    /// Cached for 5 minutes to reduce database load
    /// </summary>
    /// <returns>Total counts of active users, published events, and active businesses</returns>
    [HttpGet("stats")]
    [AllowAnonymous]
    [ResponseCache(Duration = 300, Location = ResponseCacheLocation.Any)] // 5 minutes cache
    [ProducesResponseType(typeof(CommunityStatsDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCommunityStats()
    {
        Logger.LogInformation("Fetching community stats for public landing page");

        var query = new GetCommunityStatsQuery();
        var result = await Mediator.Send(query);

        Logger.LogInformation(
            "Community stats retrieved: {Users} users, {Events} events, {Businesses} businesses",
            result.Value.TotalUsers,
            result.Value.TotalEvents,
            result.Value.TotalBusinesses
        );

        return HandleResult(result);
    }
}
