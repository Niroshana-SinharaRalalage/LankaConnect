namespace LankaConnect.Domain.Common;

/// <summary>
/// Classifies a Result failure so the API layer can map to an appropriate HTTP status code
/// without parsing error-message strings. Slice 5 introduced non-Validation kinds for the
/// seating CRUD endpoints (authorization, optimistic concurrency, structural-edit guard).
/// Default for string-only Failure(...) remains Validation → 400 Bad Request.
/// </summary>
public enum ErrorKind
{
    /// <summary>Success sentinel — no failure.</summary>
    None = 0,

    /// <summary>Input/invariant validation failed. Maps to HTTP 400.</summary>
    Validation = 1,

    /// <summary>Caller is authenticated but not authorized for this resource. Maps to HTTP 403.</summary>
    Forbidden = 2,

    /// <summary>Target resource does not exist. Maps to HTTP 404.</summary>
    NotFound = 3,

    /// <summary>Optimistic-concurrency failure (stale RowVersion/ETag). Maps to HTTP 409.</summary>
    Conflict = 4,

    /// <summary>
    /// Structural change rejected because seats are currently held or confirmed-reserved.
    /// Emits the <c>layout.structural_edit_rejected</c> metric. Maps to HTTP 422.
    /// </summary>
    StructuralEditRejected = 5
}
