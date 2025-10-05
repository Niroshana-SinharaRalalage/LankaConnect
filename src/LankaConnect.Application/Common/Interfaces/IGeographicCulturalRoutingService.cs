using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using LankaConnect.Domain.Common.Enums;

namespace LankaConnect.Application.Common.Interfaces;

/// <summary>
/// Service interface for geographic and cultural routing intelligence
/// Provides cultural-aware routing decisions based on geographic diaspora communities
/// </summary>
public interface IGeographicCulturalRoutingService
{
    /// <summary>
    /// Routes requests based on cultural affinity and geographic proximity
    /// </summary>
    Task<object> RouteBasedOnCulturalAffinityAsync(
        object request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Calculates cultural routing scores for diaspora communities
    /// </summary>
    Task<object> CalculateCulturalRoutingScoreAsync(
        GeographicRegion region,
        CancellationToken cancellationToken = default);
}
