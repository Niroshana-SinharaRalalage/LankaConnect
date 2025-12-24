using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Events.Common;

namespace LankaConnect.Application.Events.Queries.GetRegistrationById;

/// <summary>
/// Phase 6A.44: Query to get registration details by registration ID
/// Used for anonymous users on payment success page to display attendee count and total amount
/// </summary>
public record GetRegistrationByIdQuery(
    Guid RegistrationId
) : IQuery<RegistrationDetailsDto?>;
