using LankaConnect.Application.Businesses.Common;
using LankaConnect.Application.Common.Interfaces;

namespace LankaConnect.Application.Businesses.Queries.GetBusiness;

public record GetBusinessQuery(Guid Id) : IQuery<BusinessDto?>;