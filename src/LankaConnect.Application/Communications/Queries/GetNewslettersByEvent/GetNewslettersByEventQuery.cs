using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Communications.Common;

namespace LankaConnect.Application.Communications.Queries.GetNewslettersByEvent;

/// <summary>
/// Phase 6A.74 Part 3D: Query to get newsletters linked to an event
/// </summary>
public record GetNewslettersByEventQuery(Guid EventId) : IQuery<IReadOnlyList<NewsletterDto>>;
