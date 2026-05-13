using MediatR;

namespace LankaConnect.BuildingBlocks.Application.Abstractions;

/// <summary>
/// Marker for a read-only message (a CQRS query). Queries skip the transaction
/// and outbox behaviors — they're idempotent reads with no side effects.
/// They still receive logging + validation.
/// </summary>
public interface IQuery<out TResponse> : IRequest<TResponse>
{
}
