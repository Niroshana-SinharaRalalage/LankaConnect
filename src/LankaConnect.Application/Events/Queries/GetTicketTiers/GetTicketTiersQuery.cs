using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Events.Common;

namespace LankaConnect.Application.Events.Queries.GetTicketTiers;

public record GetTicketTiersQuery(Guid EventId) : IQuery<IReadOnlyList<TicketTierDto>>;
