using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;

namespace LankaConnect.Application.Communications.Commands.SendWelcomeEmail;

/// <summary>
/// Command to send welcome email to a newly registered or verified user
/// </summary>
/// <param name="UserId">The ID of the user to send welcome email to</param>
/// <param name="TriggerType">The trigger that caused this welcome email</param>
/// <param name="CustomMessage">Optional custom message to include in the email</param>
public record SendWelcomeEmailCommand(
    Guid UserId,
    WelcomeEmailTrigger TriggerType,
    string? CustomMessage = null) : ICommand<SendWelcomeEmailResponse>;

/// <summary>
/// Response for send welcome email command
/// </summary>
public class SendWelcomeEmailResponse
{
    public Guid UserId { get; init; }
    public string Email { get; init; } = string.Empty;
    public WelcomeEmailTrigger TriggerType { get; init; }
    public DateTime SentAt { get; init; }
    public bool WasAlreadySent { get; init; }
    
    public SendWelcomeEmailResponse(Guid userId, string email, WelcomeEmailTrigger triggerType, 
        DateTime sentAt, bool wasAlreadySent = false)
    {
        UserId = userId;
        Email = email;
        TriggerType = triggerType;
        SentAt = sentAt;
        WasAlreadySent = wasAlreadySent;
    }
}

/// <summary>
/// Triggers for sending welcome emails
/// </summary>
public enum WelcomeEmailTrigger
{
    Registration = 1,
    EmailVerification = 2,
    AccountActivation = 3,
    Manual = 4
}