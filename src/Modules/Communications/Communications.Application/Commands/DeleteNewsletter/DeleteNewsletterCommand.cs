using LankaConnect.BuildingBlocks.Application.Common.Interfaces;
namespace LankaConnect.Modules.Communications.Application.Commands.DeleteNewsletter;

/// <summary>
/// Phase 6A.74: Command to delete a newsletter (Draft only)
/// </summary>
public record DeleteNewsletterCommand(Guid Id) : ICommand<bool>;
