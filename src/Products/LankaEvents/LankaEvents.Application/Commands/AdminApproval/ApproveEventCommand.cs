using LankaConnect.BuildingBlocks.Application.Common.Interfaces;
namespace LankaConnect.Products.LankaEvents.Application.Commands.AdminApproval;

public record ApproveEventCommand(Guid EventId, Guid ApprovedByAdminId) : ICommand;
