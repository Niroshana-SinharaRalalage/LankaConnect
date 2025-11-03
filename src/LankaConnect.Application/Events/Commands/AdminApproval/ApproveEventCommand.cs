using LankaConnect.Application.Common.Interfaces;

namespace LankaConnect.Application.Events.Commands.AdminApproval;

public record ApproveEventCommand(Guid EventId, Guid ApprovedByAdminId) : ICommand;
