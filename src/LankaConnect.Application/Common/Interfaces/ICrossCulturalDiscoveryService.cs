using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace LankaConnect.Application.Common.Interfaces;

/// <summary>
/// Service interface for cross-cultural discovery and connection opportunities
/// Facilitates discovery of culturally relevant content and community connections
/// </summary>
public interface ICrossCulturalDiscoveryService
{
    /// <summary>
    /// Discovers cross-cultural connection opportunities
    /// </summary>
    Task<object> DiscoverCrossCulturalConnectionsAsync(
        object discoveryRequest,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Routes cross-cultural discovery requests to appropriate communities
    /// </summary>
    Task<object> RouteCrossCulturalDiscoveryAsync(
        object routingRequest,
        CancellationToken cancellationToken = default);
}
