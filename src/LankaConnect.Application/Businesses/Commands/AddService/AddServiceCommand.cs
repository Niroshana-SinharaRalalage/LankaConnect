using LankaConnect.Application.Common.Interfaces;

namespace LankaConnect.Application.Businesses.Commands.AddService;

public record AddServiceCommand(
    Guid BusinessId,
    string Name,
    string Description,
    decimal Price,
    string Duration,
    bool IsAvailable
) : ICommand<Guid>;