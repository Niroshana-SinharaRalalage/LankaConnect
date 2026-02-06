using MediatR;
using LankaConnect.Domain.Common;

namespace LankaConnect.Application.Communications.Commands.UnsubscribeFromNewsletter;

public record UnsubscribeFromNewsletterCommand(string UnsubscribeToken) : IRequest<Result<bool>>;
