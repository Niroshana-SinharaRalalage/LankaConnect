using LankaConnect.Application.Businesses.Common;
using LankaConnect.BuildingBlocks.Application.Common.Interfaces;

namespace LankaConnect.Application.Businesses.Queries.GetBusinessServices;

public record GetBusinessServicesQuery(Guid BusinessId) : IQuery<List<ServiceDto>>;