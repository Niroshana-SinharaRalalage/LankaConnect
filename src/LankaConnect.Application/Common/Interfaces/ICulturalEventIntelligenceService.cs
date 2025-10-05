using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace LankaConnect.Application.Common.Interfaces;

/// <summary>
/// Service interface for cultural event intelligence and analysis
/// Provides insights into cultural events, sacred occasions, and community celebrations
/// </summary>
public interface ICulturalEventIntelligenceService
{
    /// <summary>
    /// Analyzes cultural event patterns and provides intelligence
    /// </summary>
    Task<object> AnalyzeCulturalEventPatternsAsync(
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Predicts upcoming cultural events based on historical patterns
    /// </summary>
    Task<object> PredictUpcomingCulturalEventsAsync(
        int daysAhead,
        CancellationToken cancellationToken = default);
}
