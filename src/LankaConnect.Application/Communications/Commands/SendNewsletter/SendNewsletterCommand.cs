using LankaConnect.Application.Common.Interfaces;

namespace LankaConnect.Application.Communications.Commands.SendNewsletter;

/// <summary>
/// Command to send newsletter email
/// Phase 6A.74: Newsletter email sending (triggers background job)
/// </summary>
public record SendNewsletterCommand(Guid Id) : ICommand<bool>;
