using LankaConnect.Application.Common.Interfaces;

namespace LankaConnect.Application.Events.Commands.SubmitEventForApproval;

public record SubmitEventForApprovalCommand(Guid EventId) : ICommand;
