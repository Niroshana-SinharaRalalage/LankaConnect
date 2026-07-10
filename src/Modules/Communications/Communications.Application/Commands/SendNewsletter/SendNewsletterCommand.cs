using LankaConnect.BuildingBlocks.Application.Common.Interfaces;
namespace LankaConnect.Modules.Communications.Application.Commands.SendNewsletter;

/// <summary>
/// Phase 6A.74: Command to queue newsletter sending via Hangfire background job
/// </summary>
public record SendNewsletterCommand(Guid Id) : ICommand<bool>;
