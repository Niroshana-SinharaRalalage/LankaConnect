using LankaConnect.BuildingBlocks.Domain.Contracts;
using LankaConnect.BuildingBlocks.Domain;
namespace LankaConnect.Products.LankaEvents.Domain.DomainEvents;

public record SignUpListUpdatedEvent(
    Guid SignUpListId,
    string Category,
    string Description,
    bool HasMandatoryItems,
    bool HasPreferredItems,
    bool HasSuggestedItems,
    DateTime OccurredAt) : IDomainEvent;
