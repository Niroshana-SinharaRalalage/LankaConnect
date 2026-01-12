using LankaConnect.Application.Common.Interfaces;

namespace LankaConnect.Application.Communications.Commands.PublishNewsletter;

/// <summary>
/// Command to publish a newsletter (Draft → Active)
/// Phase 6A.74: Newsletter publishing
/// </summary>
public record PublishNewsletterCommand(Guid Id) : ICommand<bool>;
