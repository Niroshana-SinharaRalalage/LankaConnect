using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace LankaConnect.Application.Common.Interfaces;

/// <summary>
/// Service interface for cultural business directory operations
/// Manages culturally-aware business listings and directory services
/// </summary>
public interface ICulturalBusinessDirectoryService
{
    /// <summary>
    /// Searches for businesses with cultural affinity matching
    /// </summary>
    Task<object> SearchBusinessesWithCulturalAffinityAsync(
        object searchCriteria,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves business profile with cultural intelligence
    /// </summary>
    Task<object> GetBusinessProfileWithCulturalContextAsync(
        Guid businessId,
        CancellationToken cancellationToken = default);
}
