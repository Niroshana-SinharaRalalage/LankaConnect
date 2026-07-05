using LankaConnect.BuildingBlocks.Application.Common.Interfaces;

namespace LankaConnect.Application.Businesses.Commands.DeleteBusiness;

public record DeleteBusinessCommand(Guid Id) : ICommand;