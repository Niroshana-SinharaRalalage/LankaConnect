using LankaConnect.Application.Common.Interfaces;
namespace LankaConnect.Modules.Communications.Application.Commands.ReactivateNewsletter;

/// <summary>
/// Phase 6A.74 Hotfix: Command to reactivate an inactive newsletter
/// Extends newsletter visibility by 1 week (Inactive → Active)
/// </summary>
public record ReactivateNewsletterCommand(Guid Id) : ICommand<bool>;
