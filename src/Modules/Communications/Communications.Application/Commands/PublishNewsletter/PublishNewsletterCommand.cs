using LankaConnect.BuildingBlocks.Application.Common.Interfaces;
namespace LankaConnect.Modules.Communications.Application.Commands.PublishNewsletter;

/// <summary>
/// Phase 6A.74: Command to publish a newsletter (Draft → Active)
/// </summary>
public record PublishNewsletterCommand(Guid Id) : ICommand<bool>;
