namespace LankaConnect.Modules.Forms.Contracts;

/// <summary>
/// Cross-module read-only projection of a Form (without question detail).
/// Returned by <see cref="IFormQueries.GetByOwnerAsync"/> and
/// <see cref="IFormQueries.GetByIdAsync"/>.
/// </summary>
/// <remarks>
/// Wave 5.3a (2026-06-11). Field surface tracks the Form aggregate's public
/// state as of Wave 5.2b. <see cref="OwnerEntityId"/> mirrors EventId for
/// Event-owned forms (the only owner kind today); when Wave 5.2d drops the
/// legacy <c>event_id</c> column, OwnerEntityId remains the canonical owner FK.
/// </remarks>
public sealed record FormSummaryDto(
    Guid Id,
    FormOwnerEntityTypeDto OwnerEntityType,
    Guid OwnerEntityId,
    string Title,
    string? Description,
    FormStatusDto Status,
    bool AllowMultipleResponses,
    DateTime? ResponseDeadline,
    int? MaxResponses,
    bool HasResponses,
    int QuestionCount,
    int ResponseCount,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    bool AllowAttendeesToViewResponses);
