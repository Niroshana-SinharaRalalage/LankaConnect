using LankaConnect.BuildingBlocks.Application.Common.Interfaces;
using LankaConnect.Products.LankaEvents.Application.Common;
namespace LankaConnect.Products.LankaEvents.Application.Queries.GetTicketTiers;

public record GetTicketTiersQuery(Guid EventId) : IQuery<IReadOnlyList<TicketTierDto>>;
