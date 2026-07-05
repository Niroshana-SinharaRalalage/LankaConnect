using MediatR;
using LankaConnect.BuildingBlocks.Domain;
namespace LankaConnect.Modules.Communications.Application.Commands.UnsubscribeFromNewsletter;

public record UnsubscribeFromNewsletterCommand(string UnsubscribeToken) : IRequest<Result<bool>>;
