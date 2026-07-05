using LankaConnect.Domain.Common;
using MediatR;
namespace LankaConnect.Modules.Communications.Application.Commands.UnpublishNewsletter;

/// <summary>
/// Unpublish Newsletter Command (Active → Draft)
/// Phase 6A.74 Part 9A: Unpublish functionality
/// Reverts published newsletter to draft status
/// </summary>
public record UnpublishNewsletterCommand(Guid Id) : IRequest<Result>;
