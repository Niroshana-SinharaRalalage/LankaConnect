using LankaConnect.Application.Businesses.Common;
using LankaConnect.Application.Common.Interfaces;

namespace LankaConnect.Application.Businesses.Queries.GetBusinessServices;

public record GetBusinessServicesQuery(Guid BusinessId) : IQuery<List<ServiceDto>>;