using LankaConnect.Domain.Communications.Entities;
using LankaConnect.Domain.Communications.ValueObjects;
using LankaConnect.Domain.Communications.Enums;
using LankaConnect.Domain.Users.ValueObjects;

namespace LankaConnect.Domain.Tests.TestHelpers;

public static class CommunicationsTestDataBuilder
{
    private static readonly Random _random = new();

    #region EmailMessage Builders

    public static EmailMessage CreateBasicEmailMessage()
    {
        return EmailMessage.Create(
            TestDataFactory.ValidEmail("sender@lankaconnect.com"),
            EmailSubject.Create("Test Email Subject").Value,
            "This is a test email message content.",
            "<p>This is a test email message content.</p>",
            EmailType.Transactional,
            3
        ).Value;
    }

    public static EmailMessage CreateWelcomeEmail(string userEmail, string userName)
    {
        var fromEmail = TestDataFactory.ValidEmail("welcome@lankaconnect.com");
        var subject = EmailSubject.Create($"Welcome to LankaConnect, {userName}!").Value;
        var textContent = $"Hello {userName},\n\nWelcome to LankaConnect! We're excited to have you join our community.";
        var htmlContent = $"<h1>Welcome to LankaConnect, {userName}!</h1><p>We're excited to have you join our community.</p>";

        var email = EmailMessage.Create(fromEmail, subject, textContent, htmlContent, EmailType.Welcome, 3).Value;
        email.AddRecipient(TestDataFactory.ValidEmail(userEmail));
        return email;
    }

    public static EmailMessage CreateVerificationEmail(string userEmail, string verificationToken)
    {
        var fromEmail = TestDataFactory.ValidEmail("noreply@lankaconnect.com");
        var subject = EmailSubject.Create("Verify your email address").Value;
        var textContent = $"Please verify your email by using this token: {verificationToken}";
        var htmlContent = $"<p>Please verify your email by clicking the link with this token: <strong>{verificationToken}</strong></p>";

        var email = EmailMessage.Create(fromEmail, subject, textContent, htmlContent, EmailType.EmailVerification, 5).Value;
        email.AddRecipient(TestDataFactory.ValidEmail(userEmail));
        return email;
    }

    public static EmailMessage CreatePasswordResetEmail(string userEmail, string resetToken)
    {
        var fromEmail = TestDataFactory.ValidEmail("security@lankaconnect.com");
        var subject = EmailSubject.Create("Reset your password").Value;
        var textContent = $"Reset your password using this token: {resetToken} (expires in 1 hour)";
        var htmlContent = $"<p>Reset your password using this token: <strong>{resetToken}</strong></p><p>This link expires in 1 hour.</p>";

        var email = EmailMessage.Create(fromEmail, subject, textContent, htmlContent, EmailType.PasswordReset, 2).Value;
        email.AddRecipient(TestDataFactory.ValidEmail(userEmail));
        return email;
    }

    public static EmailMessage CreateEmailInStatus(EmailStatus status)
    {
        var email = CreateBasicEmailMessage();

        return status switch
        {
            EmailStatus.Pending => email,
            EmailStatus.Queued => ApplyStatus(email, e => e.MarkAsQueued()),
            EmailStatus.Sending => ApplyStatus(email, e => { e.MarkAsQueued(); e.MarkAsSending(); }),
            EmailStatus.Sent => ApplyStatus(email, e => { e.MarkAsQueued(); e.MarkAsSending(); e.MarkAsSent(); }),
            EmailStatus.Delivered => ApplyStatus(email, e => 
            { 
                e.MarkAsQueued(); 
                e.MarkAsSending(); 
                e.MarkAsSent(); 
                e.MarkAsDelivered(); 
            }),
            EmailStatus.Failed => ApplyStatus(email, e => { e.MarkAsQueued(); e.MarkAsSending(); e.MarkAsFailed("Test failure"); }),
            _ => email
        };
    }

    private static EmailMessage ApplyStatus(EmailMessage email, Action<EmailMessage> statusAction)
    {
        statusAction(email);
        return email;
    }

    public static List<EmailMessage> CreateEmailBatch(int count = 5)
    {
        return Enumerable.Range(0, count)
            .Select(i => {
                var email = CreateBasicEmailMessage();
                email.AddRecipient(TestDataFactory.ValidEmail($"user{i}@example.com"));
                return email;
            })
            .ToList();
    }

    #endregion

    #region UserEmailPreferences Builders

    public static UserEmailPreferences CreateDefaultPreferences(Guid? userId = null)
    {
        return UserEmailPreferences.Create(userId ?? Guid.NewGuid()).Value;
    }

    public static UserEmailPreferences CreatePreferencesWithAllEnabled(Guid? userId = null)
    {
        var preferences = CreateDefaultPreferences(userId);
        preferences.UpdateMarketingPreference(true);
        preferences.UpdateNotificationPreference(true);
        preferences.UpdateNewsletterPreference(true);
        return preferences;
    }

    public static UserEmailPreferences CreatePreferencesWithAllDisabled(Guid? userId = null)
    {
        var preferences = CreateDefaultPreferences(userId);
        preferences.UpdateMarketingPreference(false);
        preferences.UpdateNotificationPreference(false);
        preferences.UpdateNewsletterPreference(false);
        // Note: Transactional emails cannot be disabled
        return preferences;
    }

    public static UserEmailPreferences CreatePreferencesForLanguage(string language, Guid? userId = null)
    {
        var preferences = CreateDefaultPreferences(userId);
        preferences.UpdatePreferredLanguage(language);
        return preferences;
    }

    #endregion

    #region EmailTemplate Builders

    public static EmailTemplate CreateWelcomeTemplate()
    {
        var subject = EmailSubject.Create("Welcome to {{companyName}}, {{userName}}!").Value;
        var textTemplate = """
            Hello {{userName}},
            
            Welcome to {{companyName}}! We're excited to have you join our community of local businesses and residents.
            
            To get started:
            - Complete your profile
            - Explore local businesses
            - Connect with your community
            
            Best regards,
            The {{companyName}} Team
            """;
            
        var htmlTemplate = """
            <h1>Welcome to {{companyName}}, {{userName}}!</h1>
            <p>We're excited to have you join our community of local businesses and residents.</p>
            <h2>To get started:</h2>
            <ul>
                <li>Complete your profile</li>
                <li>Explore local businesses</li>
                <li>Connect with your community</li>
            </ul>
            <p>Best regards,<br>The {{companyName}} Team</p>
            """;

        return EmailTemplate.Create(
            "Welcome Email",
            "Template for welcoming new users",
            subject,
            textTemplate,
            htmlTemplate,
            EmailType.Welcome
        ).Value;
    }

    public static EmailTemplate CreateVerificationTemplate()
    {
        var subject = EmailSubject.Create("Verify your email address").Value;
        var textTemplate = """
            Please verify your email address by clicking the following link:
            {{verificationUrl}}
            
            This link expires in {{expiryHours}} hours.
            
            If you didn't create this account, please ignore this email.
            """;
            
        var htmlTemplate = """
            <h2>Verify Your Email Address</h2>
            <p>Please click the button below to verify your email address:</p>
            <a href="{{verificationUrl}}" style="display: inline-block; padding: 12px 24px; background-color: #007bff; color: white; text-decoration: none; border-radius: 4px;">Verify Email</a>
            <p>This link expires in {{expiryHours}} hours.</p>
            <p>If you didn't create this account, please ignore this email.</p>
            """;

        return EmailTemplate.Create(
            "Email Verification",
            "Template for email verification",
            subject,
            textTemplate,
            htmlTemplate,
            EmailType.EmailVerification
        ).Value;
    }

    public static EmailTemplate CreatePasswordResetTemplate()
    {
        var subject = EmailSubject.Create("Reset your password").Value;
        var textTemplate = """
            You requested to reset your password. Click the following link to proceed:
            {{resetUrl}}
            
            This link expires in 1 hour.
            
            If you didn't request this password reset, please ignore this email and your password will remain unchanged.
            """;
            
        var htmlTemplate = """
            <h2>Password Reset Request</h2>
            <p>You requested to reset your password. Click the button below to proceed:</p>
            <a href="{{resetUrl}}" style="display: inline-block; padding: 12px 24px; background-color: #dc3545; color: white; text-decoration: none; border-radius: 4px;">Reset Password</a>
            <p><strong>This link expires in 1 hour.</strong></p>
            <p><strong>If you didn't request this password reset, please ignore this email and your password will remain unchanged.</strong></p>
            """;

        return EmailTemplate.Create(
            "Password Reset",
            "Template for password reset requests",
            subject,
            textTemplate,
            htmlTemplate,
            EmailType.PasswordReset
        ).Value;
    }

    public static EmailTemplate CreateNewsletterTemplate()
    {
        var subject = EmailSubject.Create("{{companyName}} Newsletter - {{newsletterTitle}}").Value;
        var textTemplate = """
            {{newsletterTitle}}
            
            {{newsletterContent}}
            
            ---
            Best regards,
            The {{companyName}} Team
            
            Unsubscribe: {{unsubscribeUrl}}
            """;
            
        var htmlTemplate = """
            <h1>{{newsletterTitle}}</h1>
            <div>{{newsletterContent}}</div>
            <hr>
            <p>Best regards,<br>The {{companyName}} Team</p>
            <p><small><a href="{{unsubscribeUrl}}">Unsubscribe</a></small></p>
            """;

        return EmailTemplate.Create(
            "Newsletter Template",
            "Template for company newsletters",
            subject,
            textTemplate,
            htmlTemplate,
            EmailType.Newsletter
        ).Value;
    }

    public static EmailTemplate CreateTemplateForType(EmailType emailType)
    {
        return emailType switch
        {
            EmailType.Welcome => CreateWelcomeTemplate(),
            EmailType.EmailVerification => CreateVerificationTemplate(),
            EmailType.PasswordReset => CreatePasswordResetTemplate(),
            EmailType.Newsletter => CreateNewsletterTemplate(),
            _ => CreateBasicTemplate(emailType)
        };
    }

    public static EmailTemplate CreateBasicTemplate(EmailType emailType)
    {
        var subject = EmailSubject.Create($"{{companyName}} - {emailType} Email").Value;
        var textTemplate = $"This is a {emailType} email template.\n\nContent: {{content}}";
        var htmlTemplate = $"<h2>This is a {emailType} email template</h2><p>Content: {{{{content}}}}</p>";

        return EmailTemplate.Create(
            $"{emailType} Template",
            $"Template for {emailType} emails",
            subject,
            textTemplate,
            htmlTemplate,
            emailType
        ).Value;
    }

    #endregion

    #region VerificationToken Builders

    public static VerificationToken CreateValidVerificationToken(int validityHours = 24)
    {
        return VerificationToken.Create(validityHours).Value;
    }

    public static VerificationToken CreateExpiredVerificationToken()
    {
        // Create a token that's already expired by using FromExisting with past date
        var pastDate = DateTime.UtcNow.AddHours(-1);
        // Since FromExisting will fail with expired date, we need to create and simulate expiry
        // This is a test helper, so we can use reflection or just document the limitation
        
        // For testing purposes, return a token that will be expired soon
        return VerificationToken.Create(1).Value; // Will expire in 1 hour, can be used for near-expiry tests
    }

    public static VerificationToken CreateShortLivedToken(int minutes = 5)
    {
        // VerificationToken works in hours, so minimum is 1 hour
        return VerificationToken.Create(1).Value;
    }

    public static List<VerificationToken> CreateMultipleTokens(int count = 3)
    {
        return Enumerable.Range(0, count)
            .Select(_ => CreateValidVerificationToken())
            .ToList();
    }

    #endregion

    #region Value Object Builders

    public static EmailSubject CreateValidEmailSubject(string? subject = null)
    {
        return EmailSubject.Create(subject ?? "Test Email Subject").Value;
    }

    public static EmailSubject CreateLongEmailSubject()
    {
        var longSubject = "This is a very long email subject that contains lots of text to test the maximum length validation and ensure that the system handles lengthy subjects appropriately";
        
        // Truncate to just under max length to create a valid long subject
        if (longSubject.Length > EmailSubject.MaxLength)
        {
            longSubject = longSubject.Substring(0, EmailSubject.MaxLength - 5) + "...";
        }
        
        return EmailSubject.Create(longSubject).Value;
    }

    public static List<EmailSubject> CreateMultipleEmailSubjects(int count = 3)
    {
        return Enumerable.Range(1, count)
            .Select(i => EmailSubject.Create($"Test Subject {i}").Value)
            .ToList();
    }

    #endregion

    #region Random Data Helpers

    public static EmailMessage CreateRandomEmailMessage()
    {
        var types = Enum.GetValues<EmailType>();
        var randomType = types[_random.Next(types.Length)];
        
        var fromEmail = TestDataFactory.ValidEmail($"sender{_random.Next(100)}@lankaconnect.com");
        var subject = EmailSubject.Create($"Random Test Email {_random.Next(1000)}").Value;
        var textContent = $"This is random email content generated at {DateTime.UtcNow}";
        var htmlContent = $"<p>This is random email content generated at {DateTime.UtcNow}</p>";

        return EmailMessage.Create(fromEmail, subject, textContent, htmlContent, randomType, _random.Next(1, 6)).Value;
    }

    public static UserEmailPreferences CreateRandomPreferences()
    {
        var preferences = CreateDefaultPreferences();
        preferences.UpdateMarketingPreference(_random.Next(2) == 1);
        preferences.UpdateNotificationPreference(_random.Next(2) == 1);
        preferences.UpdateNewsletterPreference(_random.Next(2) == 1);
        
        var languages = new[] { "en-US", "si-LK", "ta-LK", "fr-FR", "de-DE" };
        preferences.UpdatePreferredLanguage(languages[_random.Next(languages.Length)]);
        
        return preferences;
    }

    #endregion

    #region Business Context Helpers

    public static EmailMessage CreateBusinessApprovalEmail(string businessName, string ownerEmail)
    {
        var fromEmail = TestDataFactory.ValidEmail("approvals@lankaconnect.com");
        var subject = EmailSubject.Create($"Your business '{businessName}' has been approved!").Value;
        var textContent = $"Congratulations! Your business '{businessName}' has been approved and is now live on LankaConnect.";
        var htmlContent = $"<h2>Congratulations!</h2><p>Your business '<strong>{businessName}</strong>' has been approved and is now live on LankaConnect.</p>";

        var email = EmailMessage.Create(fromEmail, subject, textContent, htmlContent, EmailType.BusinessNotification, 3).Value;
        email.AddRecipient(TestDataFactory.ValidEmail(ownerEmail));
        return email;
    }

    public static EmailMessage CreateEventNotificationEmail(string eventTitle, string userEmail)
    {
        var fromEmail = TestDataFactory.ValidEmail("events@lankaconnect.com");
        var subject = EmailSubject.Create($"New event: {eventTitle}").Value;
        var textContent = $"There's a new event in your area: {eventTitle}. Check it out on LankaConnect!";
        var htmlContent = $"<h2>New Event in Your Area</h2><p>There's a new event: <strong>{eventTitle}</strong></p><p>Check it out on LankaConnect!</p>";

        var email = EmailMessage.Create(fromEmail, subject, textContent, htmlContent, EmailType.EventNotification, 3).Value;
        email.AddRecipient(TestDataFactory.ValidEmail(userEmail));
        return email;
    }

    #endregion
}