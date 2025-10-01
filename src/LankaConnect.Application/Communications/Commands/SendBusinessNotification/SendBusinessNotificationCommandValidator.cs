using FluentValidation;

namespace LankaConnect.Application.Communications.Commands.SendBusinessNotification;

/// <summary>
/// Validator for SendBusinessNotificationCommand
/// </summary>
public class SendBusinessNotificationCommandValidator : AbstractValidator<SendBusinessNotificationCommand>
{
    public SendBusinessNotificationCommandValidator()
    {
        RuleFor(x => x.BusinessId)
            .NotEmpty()
            .WithMessage("Business ID is required");

        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("User ID is required");

        RuleFor(x => x.NotificationType)
            .IsInEnum()
            .WithMessage("Invalid notification type");

        RuleFor(x => x.Subject)
            .NotEmpty()
            .WithMessage("Subject is required")
            .MaximumLength(200)
            .WithMessage("Subject must not exceed 200 characters");

        RuleFor(x => x.Data)
            .Must(HaveValidDataKeys)
            .WithMessage("Data contains invalid keys")
            .When(x => x.Data != null && x.Data.Count > 0);
    }

    private static bool HaveValidDataKeys(Dictionary<string, object>? data)
    {
        if (data == null) return true;

        // Check for reserved parameter names that shouldn't be overridden
        var reservedKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "UserName", "FirstName", "UserEmail", "BusinessName", "BusinessId", 
            "CompanyName", "NotificationType", "Subject", "NotificationDate"
        };

        return !data.Keys.Any(key => reservedKeys.Contains(key));
    }
}