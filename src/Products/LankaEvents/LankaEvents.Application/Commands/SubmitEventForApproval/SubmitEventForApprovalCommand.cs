using LankaConnect.BuildingBlocks.Application.Common.Interfaces;
namespace LankaConnect.Products.LankaEvents.Application.Commands.SubmitEventForApproval;

public record SubmitEventForApprovalCommand(Guid EventId) : ICommand;
