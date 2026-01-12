using LankaConnect.Application.Common.Interfaces;

namespace LankaConnect.Application.Communications.Commands.DeleteNewsletter;

/// <summary>
/// Phase 6A.74: Command to delete a newsletter (Draft only)
/// </summary>
public record DeleteNewsletterCommand(Guid Id) : ICommand<bool>;
