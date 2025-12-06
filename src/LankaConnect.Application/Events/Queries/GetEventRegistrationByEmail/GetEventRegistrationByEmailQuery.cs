using LankaConnect.Application.Common.Interfaces;

namespace LankaConnect.Application.Events.Queries.GetEventRegistrationByEmail;

/// <summary>
/// Query to check if an email has registered for a specific event
/// Phase 6A.15: Enhanced sign-up list UX with email validation
/// </summary>
public record GetEventRegistrationByEmailQuery(
    Guid EventId,
    string Email
) : IQuery<bool>;
