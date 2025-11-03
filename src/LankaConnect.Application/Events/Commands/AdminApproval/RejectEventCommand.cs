using LankaConnect.Application.Common.Interfaces;

namespace LankaConnect.Application.Events.Commands.AdminApproval;

public record RejectEventCommand(Guid EventId, Guid RejectedByAdminId, string Reason) : ICommand;
