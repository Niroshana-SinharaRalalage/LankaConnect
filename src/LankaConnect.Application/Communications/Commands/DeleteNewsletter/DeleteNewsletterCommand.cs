using LankaConnect.Application.Common.Interfaces;

namespace LankaConnect.Application.Communications.Commands.DeleteNewsletter;

/// <summary>
/// Command to delete a draft newsletter
/// Phase 6A.74: Newsletter deletion (Draft only)
/// </summary>
public record DeleteNewsletterCommand(Guid Id) : ICommand<bool>;
