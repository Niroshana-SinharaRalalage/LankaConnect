using AutoMapper;
using LankaConnect.Application.Communications.Common;
using LankaConnect.Domain.Communications.Entities;
using LankaConnect.Domain.Communications.Enums;

namespace LankaConnect.Application.Common.Mappings;

/// <summary>
/// AutoMapper profile for email-related mappings
/// </summary>
public class EmailMappingProfile : Profile
{
    public EmailMappingProfile()
    {
        // Email Status mappings - Updated to match EmailMessage entity
        CreateMap<EmailMessage, EmailStatusDto>()
            .ForMember(dest => dest.EmailId, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.ToEmail, opt => opt.MapFrom(src => src.ToEmails.FirstOrDefault() ?? string.Empty))
            .ForMember(dest => dest.Subject, opt => opt.MapFrom(src => src.Subject.Value))
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => (Communications.Common.EmailStatus)src.Status))
            .ForMember(dest => dest.Type, opt => opt.MapFrom(src => (Communications.Common.EmailType)src.Type))
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => src.CreatedAt))
            .ForMember(dest => dest.SentAt, opt => opt.MapFrom(src => src.SentAt))
            .ForMember(dest => dest.DeliveredAt, opt => opt.MapFrom(src => src.DeliveredAt))
            .ForMember(dest => dest.FailedAt, opt => opt.MapFrom(src => src.FailedAt))
            .ForMember(dest => dest.FailureReason, opt => opt.MapFrom(src => src.ErrorMessage))
            .ForMember(dest => dest.RetryCount, opt => opt.MapFrom(src => src.RetryCount))
            .ForMember(dest => dest.NextRetryAt, opt => opt.MapFrom(src => src.NextRetryAt));

        // Email Template mappings - Updated to match domain entity
        CreateMap<EmailTemplate, EmailTemplateDto>()
            .ForMember(dest => dest.DisplayName, opt => opt.MapFrom(src => src.Name))
            .ForMember(dest => dest.Subject, opt => opt.MapFrom(src => src.SubjectTemplate.Value))
            .ForMember(dest => dest.HtmlTemplate, opt => opt.MapFrom(src => src.HtmlTemplate ?? string.Empty))
            .ForMember(dest => dest.PlainTextTemplate, opt => opt.MapFrom(src => src.TextTemplate))
            .ForMember(dest => dest.Category, opt => opt.MapFrom(src => MapEmailTemplateCategory(src.Type)))
            .ForMember(dest => dest.RequiredParameters, opt => opt.MapFrom(src => ExtractParameters(src.TextTemplate, true)))
            .ForMember(dest => dest.OptionalParameters, opt => opt.MapFrom(src => ExtractParameters(src.TextTemplate, false)))
            .ForMember(dest => dest.LastModified, opt => opt.MapFrom(src => src.UpdatedAt ?? src.CreatedAt))
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => src.CreatedAt));

        // User Email Preferences mappings - Updated to match domain entity
        CreateMap<UserEmailPreferences, UserEmailPreferencesDto>()
            .ForMember(dest => dest.Email, opt => opt.Ignore()) // Will be set from User entity
            .ForMember(dest => dest.ReceiveWelcomeEmails, opt => opt.MapFrom(src => src.AllowTransactional))
            .ForMember(dest => dest.ReceiveBusinessNotifications, opt => opt.MapFrom(src => src.AllowNotifications))
            .ForMember(dest => dest.ReceiveMarketingEmails, opt => opt.MapFrom(src => src.AllowMarketing))
            .ForMember(dest => dest.ReceiveSystemAlerts, opt => opt.MapFrom(src => src.AllowTransactional))
            .ForMember(dest => dest.ReceivePasswordAlerts, opt => opt.MapFrom(src => src.AllowTransactional))
            .ForMember(dest => dest.NotificationFrequency, opt => opt.MapFrom(src => Communications.Common.EmailFrequency.Immediate))
            .ForMember(dest => dest.LastUpdated, opt => opt.MapFrom(src => src.UpdatedAt ?? DateTime.UtcNow));

        CreateMap<UserEmailPreferencesDto, UserEmailPreferences>()
            .ForMember(dest => dest.UserId, opt => opt.MapFrom(src => src.UserId))
            .ForMember(dest => dest.AllowMarketing, opt => opt.MapFrom(src => src.ReceiveMarketingEmails))
            .ForMember(dest => dest.AllowNotifications, opt => opt.MapFrom(src => src.ReceiveBusinessNotifications))
            .ForMember(dest => dest.AllowNewsletters, opt => opt.MapFrom(src => src.ReceiveMarketingEmails))
            .ForMember(dest => dest.AllowTransactional, opt => opt.MapFrom(src => src.ReceivePasswordAlerts || src.ReceiveSystemAlerts))
            .ForMember(dest => dest.PreferredLanguage, opt => opt.MapFrom(src => "en-US"));

        // Email Verification mappings
        CreateMap<Domain.Users.User, EmailVerificationDto>()
            .ForMember(dest => dest.UserId, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.Email.Value))
            .ForMember(dest => dest.IsEmailVerified, opt => opt.MapFrom(src => src.IsEmailVerified))
            .ForMember(dest => dest.VerificationTokenExpiresAt, opt => opt.MapFrom(src => src.EmailVerificationTokenExpiresAt))
            .ForMember(dest => dest.LastVerificationSentAt, opt => opt.MapFrom(src => 
                src.EmailVerificationTokenExpiresAt.HasValue ? src.EmailVerificationTokenExpiresAt.Value.AddHours(-24) : (DateTime?)null))
            .ForMember(dest => dest.VerificationAttempts, opt => opt.MapFrom(src => 
                src.IsEmailVerified ? 1 : (src.EmailVerificationToken != null ? 1 : 0)));

        // Password Reset mappings
        CreateMap<Domain.Users.User, PasswordResetDto>()
            .ForMember(dest => dest.UserId, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.Email.Value))
            .ForMember(dest => dest.HasActiveResetToken, opt => opt.MapFrom(src => 
                !string.IsNullOrEmpty(src.PasswordResetToken) && 
                src.PasswordResetTokenExpiresAt.HasValue && 
                src.PasswordResetTokenExpiresAt.Value > DateTime.UtcNow))
            .ForMember(dest => dest.ResetTokenExpiresAt, opt => opt.MapFrom(src => src.PasswordResetTokenExpiresAt))
            .ForMember(dest => dest.LastResetRequestAt, opt => opt.MapFrom(src => 
                src.PasswordResetTokenExpiresAt.HasValue ? src.PasswordResetTokenExpiresAt.Value.AddHours(-1) : (DateTime?)null))
            .ForMember(dest => dest.ResetAttempts, opt => opt.MapFrom(src => 
                !string.IsNullOrEmpty(src.PasswordResetToken) ? 1 : 0));
    }

    private static EmailTemplateCategory MapEmailTemplateCategory(LankaConnect.Domain.Communications.Enums.EmailType emailType)
    {
        return emailType switch
        {
            LankaConnect.Domain.Communications.Enums.EmailType.EmailVerification or LankaConnect.Domain.Communications.Enums.EmailType.PasswordReset => EmailTemplateCategory.Authentication,
            LankaConnect.Domain.Communications.Enums.EmailType.BusinessNotification => EmailTemplateCategory.Business,
            LankaConnect.Domain.Communications.Enums.EmailType.Marketing or LankaConnect.Domain.Communications.Enums.EmailType.Newsletter => EmailTemplateCategory.Marketing,
            LankaConnect.Domain.Communications.Enums.EmailType.Welcome => EmailTemplateCategory.Notification,
            _ => EmailTemplateCategory.System
        };
    }

    private static List<string> ExtractParameters(string template, bool required)
    {
        // Simple parameter extraction - in real implementation, this would parse template variables
        // For now, return common parameters based on template type
        var commonRequired = new List<string> { "recipientName", "recipientEmail" };
        var commonOptional = new List<string> { "companyName", "supportUrl", "unsubscribeUrl" };
        
        return required ? commonRequired : commonOptional;
    }
}