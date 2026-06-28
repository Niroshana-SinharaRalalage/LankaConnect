using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Events.Common;

namespace LankaConnect.Products.LankaEvents.Application.Queries.GetTicketTiers;

public record GetTicketTiersQuery(Guid EventId) : IQuery<IReadOnlyList<TicketTierDto>>;
