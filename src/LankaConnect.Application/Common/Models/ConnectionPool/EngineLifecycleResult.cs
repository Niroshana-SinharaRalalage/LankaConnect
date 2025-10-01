namespace LankaConnect.Application.Common.Models.ConnectionPool;

/// <summary>
/// Result of an engine lifecycle operation
/// </summary>
public class EngineLifecycleResult
{
    /// <summary>
    /// The operation that was performed
    /// </summary>
    public EngineLifecycleOperation Operation { get; set; } = new();

    /// <summary>
    /// Whether the operation was successful
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// Error message if operation failed
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Timestamp when operation completed
    /// </summary>
    public DateTime CompletedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Duration of the operation
    /// </summary>
    public TimeSpan Duration { get; set; }
}