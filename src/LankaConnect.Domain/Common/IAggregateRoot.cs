namespace LankaConnect.Domain.Common;

/// <summary>
/// Interface for aggregate roots in domain-driven design
/// </summary>
public interface IAggregateRoot
{
    /// <summary>
    /// Gets the version for optimistic concurrency control
    /// </summary>
    byte[] Version { get; }
    
    /// <summary>
    /// Updates the aggregate version for concurrency control
    /// </summary>
    void SetVersion(byte[] version);
    
    /// <summary>
    /// Validates the current state of the aggregate
    /// </summary>
    ValidationResult ValidateState();
    
    /// <summary>
    /// Checks if the aggregate is in a valid state for business operations
    /// </summary>
    bool IsValid();
}