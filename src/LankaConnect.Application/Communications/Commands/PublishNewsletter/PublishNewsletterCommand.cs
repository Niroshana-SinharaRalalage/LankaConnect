using LankaConnect.Application.Common.Interfaces;

namespace LankaConnect.Application.Communications.Commands.PublishNewsletter;

/// <summary>
/// Phase 6A.74: Command to publish a newsletter (Draft → Active)
/// </summary>
public record PublishNewsletterCommand(Guid Id) : ICommand<bool>;
