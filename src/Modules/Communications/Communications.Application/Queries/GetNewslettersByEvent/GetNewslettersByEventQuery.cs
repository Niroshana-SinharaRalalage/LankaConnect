using LankaConnect.BuildingBlocks.Application.Common.Interfaces;
using LankaConnect.Modules.Communications.Application.Common;
namespace LankaConnect.Modules.Communications.Application.Queries.GetNewslettersByEvent;

/// <summary>
/// Phase 6A.74 Part 3D: Query to get newsletters linked to an event
/// </summary>
public record GetNewslettersByEventQuery(Guid EventId) : IQuery<IReadOnlyList<NewsletterDto>>;
