namespace LankaConnect.Application.Common.Models.ConnectionPool;

/// <summary>
/// Represents an engine lifecycle operation (start, stop, restart, etc.)
/// </summary>
public class EngineLifecycleOperation
{
    /// <summary>
    /// Unique identifier for the operation
    /// </summary>
    public Guid OperationId { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Type of lifecycle operation
    /// </summary>
    public string OperationType { get; set; } = string.Empty;

    /// <summary>
    /// Timestamp when operation was requested
    /// </summary>
    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Parameters specific to this operation
    /// </summary>
    public Dictionary<string, object>? Parameters { get; set; }
}