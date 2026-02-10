namespace LankaConnect.Domain.Common;

/// <summary>
/// Interface for aggregate roots in domain-driven design.
/// Concurrency control is handled at the infrastructure level (e.g., PostgreSQL xmin).
/// </summary>
public interface IAggregateRoot
{
    /// <summary>
    /// Validates the current state of the aggregate
    /// </summary>
    ValidationResult ValidateState();

    /// <summary>
    /// Checks if the aggregate is in a valid state for business operations
    /// </summary>
    bool IsValid();
}