namespace LankaConnect.Domain.Common.Enums;

/// <summary>
/// Synchronization priority levels for cross-region data synchronization
/// Used in disaster recovery and backup operations
/// </summary>
public enum SynchronizationPriority
{
    /// <summary>
    /// Low priority - background synchronization
    /// </summary>
    Low = 1,

    /// <summary>
    /// Medium priority - standard synchronization
    /// </summary>
    Medium = 2,

    /// <summary>
    /// High priority - expedited synchronization
    /// </summary>
    High = 3,

    /// <summary>
    /// Critical priority - immediate synchronization required
    /// </summary>
    Critical = 4,

    /// <summary>
    /// Emergency priority - maximum priority for disaster recovery
    /// </summary>
    Emergency = 5,

    /// <summary>
    /// Cultural event priority - highest priority for cultural events
    /// </summary>
    CulturalEvent = 6
}