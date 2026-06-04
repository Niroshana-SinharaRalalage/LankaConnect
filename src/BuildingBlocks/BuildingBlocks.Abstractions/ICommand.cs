using MediatR;

namespace LankaConnect.BuildingBlocks.Application.Abstractions;

/// <summary>
/// Marker for a state-changing message (a CQRS command). Commands run inside
/// a database transaction (<see cref="Behaviors.TransactionBehavior{TRequest,TResponse}"/>),
/// emit integration events through the outbox
/// (<see cref="Behaviors.OutboxBehavior{TRequest,TResponse}"/>), and are
/// audit-logged (<see cref="Behaviors.AuditBehavior{TRequest,TResponse}"/>).
/// </summary>
/// <typeparam name="TResponse">
/// Handler response type — typically a <c>Result</c> or value-returning
/// <c>Result&lt;T&gt;</c> from BuildingBlocks.Domain.
/// </typeparam>
public interface ICommand<out TResponse> : IRequest<TResponse>
{
}
