using LankaConnect.Domain.Common;
using MediatR;
namespace LankaConnect.BuildingBlocks.Application.Common;

/// <summary>
/// Wrapper to bridge IDomainEvent to MediatR's INotification
/// This allows domain events to be published through MediatR without adding MediatR dependency to Domain layer
/// </summary>
public class DomainEventNotification<TDomainEvent> : INotification where TDomainEvent : IDomainEvent
{
    public TDomainEvent DomainEvent { get; }

    public DomainEventNotification(TDomainEvent domainEvent)
    {
        DomainEvent = domainEvent;
    }
}
