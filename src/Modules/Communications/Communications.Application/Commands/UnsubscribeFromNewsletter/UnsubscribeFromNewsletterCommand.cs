using MediatR;
using LankaConnect.Domain.Common;
namespace LankaConnect.Modules.Communications.Application.Commands.UnsubscribeFromNewsletter;

public record UnsubscribeFromNewsletterCommand(string UnsubscribeToken) : IRequest<Result<bool>>;
