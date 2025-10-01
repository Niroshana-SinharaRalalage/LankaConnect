using MediatR;
using LankaConnect.Domain.Common;

namespace LankaConnect.Application.Common.Interfaces;

public interface IQueryHandler<TQuery, TResponse> : IRequestHandler<TQuery, Result<TResponse>>
    where TQuery : IQuery<TResponse>
{
}