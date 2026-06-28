using LankaConnect.Application.Common.Interfaces;

namespace LankaConnect.Products.LankaEvents.Application.Commands.AdminApproval;

public record RejectEventCommand(Guid EventId, Guid RejectedByAdminId, string Reason) : ICommand;
