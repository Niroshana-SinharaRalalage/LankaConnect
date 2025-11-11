using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using LankaConnect.API.Extensions;

namespace LankaConnect.API.Controllers;

/// <summary>
/// Dashboard endpoints for authenticated users
/// Phase 6A.3: Provides community stats and dashboard data
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class DashboardController : BaseController<DashboardController>
{
    public DashboardController(IMediator mediator, ILogger<DashboardController> logger) : base(mediator, logger)
    {
    }

    /// <summary>
    /// Get dashboard statistics for authenticated users
    /// Phase 6A.3: Returns community activity stats
    /// </summary>
    /// <returns>Dashboard statistics including user counts, posts, and events</returns>
    [HttpGet("stats")]
    [Authorize]
    [ProducesResponseType(typeof(DashboardStatsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public Task<IActionResult> GetDashboardStats()
    {
        Logger.LogInformation("Getting dashboard stats for user: {UserId}", User.TryGetUserId());

        // Phase 6A.3: Return mock stats for MVP
        // TODO Phase 6B: Implement real queries with GetDashboardStatsQuery
        var stats = new DashboardStatsDto
        {
            ActiveUsers = 12500,
            RecentPosts = 450,
            UpcomingEvents = 2200,
            UserRole = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value ?? "GeneralUser"
        };

        return Task.FromResult<IActionResult>(Ok(stats));
    }

    /// <summary>
    /// Get personalized feed items for authenticated user
    /// Phase 6A.3: Future endpoint for personalized activity feed
    /// </summary>
    [HttpGet("feed")]
    [Authorize]
    [ProducesResponseType(typeof(IReadOnlyList<FeedItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public Task<IActionResult> GetPersonalizedFeed([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        Logger.LogInformation("Getting personalized feed for user: {UserId}, page: {Page}", User.TryGetUserId(), page);

        // Phase 6A.3: Return empty list for MVP
        // TODO Phase 6B: Implement real feed with user preferences and metro area filtering
        var feed = new List<FeedItemDto>();

        return Task.FromResult<IActionResult>(Ok(feed));
    }
}

/// <summary>
/// Dashboard statistics DTO
/// </summary>
public record DashboardStatsDto
{
    public int ActiveUsers { get; init; }
    public int RecentPosts { get; init; }
    public int UpcomingEvents { get; init; }
    public string UserRole { get; init; } = string.Empty;
}

/// <summary>
/// Feed item DTO for dashboard activity feed
/// </summary>
public record FeedItemDto
{
    public Guid Id { get; init; }
    public string Type { get; init; } = string.Empty; // "event", "post", "business"
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string AuthorName { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public int Likes { get; init; }
    public int Comments { get; init; }
}
