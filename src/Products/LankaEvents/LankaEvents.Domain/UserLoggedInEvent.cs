using LankaConnect.Domain.Common;
namespace LankaConnect.Products.LankaEvents.Domain;

public record UserLoggedInEvent(Guid UserId, string Email, DateTime LoginTime) : DomainEvent;