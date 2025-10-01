using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LankaConnect.Application.Common.Models.MultiLanguage;
using LankaConnect.Application.Common.Models.Routing;

namespace LankaConnect.Application.Common.Interfaces;

/// <summary>
/// Multi-Language Routing Service Interface
/// Handles comprehensive routing optimization for South Asian diaspora language preferences
/// Performance targets: <100ms standard, <50ms during cultural events
/// Decomposed from IMultiLanguageAffinityRoutingEngine following DDD principles
/// Supports high-concurrency scenarios with 5x traffic during Vesak, Diwali, Eid
/// </summary>
public interface IMultiLanguageRoutingService
{
    /// <summary>
    /// Execute comprehensive multi-language routing with performance optimization
    /// Performance target: <100ms standard, <50ms during cultural events
    /// </summary>
    /// <param name="routingRequest">Multi-language routing request with preferences</param>
    /// <returns>Optimized routing response with performance metrics</returns>
    Task<LankaConnect.Application.Common.Models.Routing.MultiLanguageRoutingResponse> ExecuteMultiLanguageRoutingAsync(LankaConnect.Application.Common.Models.Routing.MultiLanguageRoutingRequest routingRequest);

    /// <summary>
    /// Optimize routing for high-concurrency scenarios (cultural event traffic spikes)
    /// Handles 5x traffic increase during major cultural celebrations
    /// </summary>
    /// <param name="concurrentRequests">Batch of routing requests for optimization</param>
    /// <returns>Batch routing results with performance optimization</returns>
    Task<LankaConnect.Application.Common.Models.Routing.BatchMultiLanguageRoutingResponse> ExecuteBatchMultiLanguageRoutingAsync(List<LankaConnect.Application.Common.Models.Routing.MultiLanguageRoutingRequest> concurrentRequests);

    /// <summary>
    /// Generate intelligent routing fallback strategies for service continuity
    /// Ensures 99.99% uptime during database partition failures
    /// </summary>
    /// <param name="primaryRoutingFailure">Primary routing failure context</param>
    /// <param name="userProfile">User profile for personalized fallback</param>
    /// <returns>Intelligent fallback routing strategy</returns>
    Task<LankaConnect.Application.Common.Models.Routing.RoutingFallbackStrategy> GenerateIntelligentRoutingFallbackAsync(
        LankaConnect.Application.Common.Models.Routing.RoutingFailureContext primaryRoutingFailure,
        LankaConnect.Application.Common.Models.Routing.MultiLanguageUserProfile userProfile);
}