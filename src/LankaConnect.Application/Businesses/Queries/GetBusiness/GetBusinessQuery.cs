using LankaConnect.Application.Businesses.Common;
using LankaConnect.BuildingBlocks.Application.Common.Interfaces;

namespace LankaConnect.Application.Businesses.Queries.GetBusiness;

public record GetBusinessQuery(Guid Id) : IQuery<BusinessDto?>;