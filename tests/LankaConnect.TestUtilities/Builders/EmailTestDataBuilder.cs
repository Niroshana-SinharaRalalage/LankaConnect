using LankaConnect.Domain.Communications.Entities;
using LankaConnect.Domain.Communications.ValueObjects;
using LankaConnect.Domain.Communications.Enums;
using LankaConnect.Application.Common.Interfaces;
using UserEmail = LankaConnect.Domain.Users.ValueObjects.Email;

namespace LankaConnect.TestUtilities.Builders;

/// <summary>
/// Test wrapper for email address to match integration test expectations
/// </summary>
public class TestEmailAddress
{
    public string Address { get; }
    
    public TestEmailAddress(string address)
    {
        Address = address;
    }
}

/// <summary>
/// Centralized test data builder for email-related domain objects.
/// Follows Clean Architecture by providing domain-specific test data creation.
/// </summary>
public static class EmailTestDataBuilder
{
    private static readonly Random _random = new();

    // Email builders
    public static UserEmail CreateValidEmail(string? address = null)
    {
        var emailAddress = address ?? GenerateRandomEmail();
        return UserEmail.Create(emailAddress).Value;
    }

    public static TestEmailAddress CreateValidEmailAddress(string? address = null)
    {
        var emailAddress = address ?? GenerateRandomEmail();
        return new TestEmailAddress(emailAddress);
    }

    public static UserEmail CreateTestEmail() => UserEmail.Create("test@example.com").Value;

    public static UserEmail CreateSenderEmail() => UserEmail.Create("noreply@lankaconnect.com").Value;

    public static List<UserEmail> CreateMultipleEmails(int count = 5)
    {
        return Enumerable.Range(0, count)
            .Select(_ => CreateValidEmail())
            .ToList();
    }

    // Email message builders (using actual domain model)
    public static EmailMessage CreateBasicEmailMessage()
    {
        var fromEmail = CreateSenderEmail();
        var subject = EmailSubject.Create("Test Subject").Value;
        var textContent = "Test message body";
        var htmlContent = "<p>Test message body</p>";
        
        return EmailMessage.Create(fromEmail, subject, textContent, htmlContent, EmailType.Transactional, 3).Value;
    }

    public static EmailMessage CreateCustomEmailMessage(UserEmail recipientEmail, string subject, string textContent)
    {
        var fromEmail = CreateSenderEmail();
        var subjectObj = EmailSubject.Create(subject).Value;
        var htmlContent = $"<p>{textContent}</p>";
        
        var email = EmailMessage.Create(fromEmail, subjectObj, textContent, htmlContent, EmailType.Transactional, 3).Value;
        email.AddRecipient(recipientEmail);
        return email;
    }

    // Email message state builders
    public static EmailMessage CreateSentEmail(string? messageId = null)
    {
        var email = CreateBasicEmailMessage();
        email.MarkAsSent(messageId ?? $"msg-{Guid.NewGuid():N}");
        return email;
    }

    public static EmailMessage CreateDeliveredEmail()
    {
        var email = CreateSentEmail();
        email.MarkAsDelivered();
        return email;
    }

    public static EmailMessage CreateFailedEmail(string? errorMessage = null, DateTime? nextRetryAt = null)
    {
        var email = CreateBasicEmailMessage();
        email.MarkAsFailed(
            errorMessage ?? "SMTP connection failed",
            nextRetryAt ?? DateTime.UtcNow.AddMinutes(5)
        );
        return email;
    }

    public static EmailMessage CreateFailedEmailWithMultipleAttempts(int attemptCount = 3)
    {
        var email = CreateBasicEmailMessage();
        
        for (int i = 0; i < attemptCount; i++)
        {
            email.MarkAsFailed($"Attempt {i + 1} failed", DateTime.UtcNow.AddMinutes((i + 1) * 5));
        }
        
        return email;
    }

    public static EmailMessage CreateOpenedEmail()
    {
        var email = CreateDeliveredEmail();
        email.MarkAsOpened();
        return email;
    }

    public static EmailMessage CreateClickedEmail()
    {
        var email = CreateOpenedEmail();
        email.MarkAsClicked();
        return email;
    }

    public static EmailMessage CreateRetryableFailedEmail()
    {
        var email = CreateBasicEmailMessage();
        var nextRetryAt = DateTime.UtcNow.AddMinutes(-1); // Past time, ready for retry
        email.MarkAsFailed("Temporary SMTP failure", nextRetryAt);
        return email;
    }

    // Bulk email builders
    public static List<EmailMessage> CreateEmailBatch(int count = 10)
    {
        return Enumerable.Range(0, count)
            .Select(i => CreateCustomEmailMessage(
                CreateValidEmail($"test{i}@example.com"),
                $"Batch Email {i + 1}",
                $"This is batch email number {i + 1}"
            ))
            .ToList();
    }

    public static List<EmailMessage> CreateMixedStatusEmails()
    {
        return new List<EmailMessage>
        {
            CreateBasicEmailMessage(),      // Pending
            CreateSentEmail(),              // Sent
            CreateDeliveredEmail(),         // Delivered
            CreateFailedEmail(),            // Failed
            CreateOpenedEmail(),            // Opened
            CreateClickedEmail()            // Clicked
        };
    }

    public static List<EmailMessage> CreatePendingEmails(int count = 5)
    {
        return Enumerable.Range(0, count)
            .Select(_ => CreateBasicEmailMessage())
            .ToList();
    }

    public static List<EmailMessage> CreateFailedEmails(int count = 3)
    {
        return Enumerable.Range(0, count)
            .Select(_ => CreateFailedEmail())
            .ToList();
    }

    // Email verification scenarios
    public static EmailMessage CreateEmailVerificationMessage(string email, string verificationToken)
    {
        var templateData = new Dictionary<string, object>
        {
            ["email"] = email,
            ["verificationToken"] = verificationToken,
            ["verificationUrl"] = $"https://lankaconnect.com/verify?token={verificationToken}"
        };

        var fromEmail = CreateSenderEmail();
        var subject = EmailSubject.Create("Verify Your Email Address").Value;
        var textContent = "Please verify your email address by clicking the link below";
        
        var message = EmailMessage.Create(fromEmail, subject, textContent, null, EmailType.EmailVerification, 3).Value;
        message.AddRecipient(UserEmail.Create(email).Value);
        return message;
    }

    public static EmailMessage CreatePasswordResetMessage(string email, string resetToken)
    {
        var templateData = new Dictionary<string, object>
        {
            ["email"] = email,
            ["resetToken"] = resetToken,
            ["resetUrl"] = $"https://lankaconnect.com/reset-password?token={resetToken}",
            ["expiryTime"] = DateTime.UtcNow.AddHours(1).ToString("yyyy-MM-dd HH:mm:ss UTC")
        };

        var fromEmail = CreateSenderEmail();
        var subject = EmailSubject.Create("Reset Your Password").Value;
        var textContent = "You requested to reset your password. Click the link below to proceed.";
        
        var message = EmailMessage.Create(fromEmail, subject, textContent, null, EmailType.PasswordReset, 3).Value;
        message.AddRecipient(UserEmail.Create(email).Value);
        return message;
    }

    public static EmailMessage CreateWelcomeMessage(string email, string userName)
    {
        var templateData = new Dictionary<string, object>
        {
            ["userName"] = userName,
            ["email"] = email,
            ["loginUrl"] = "https://lankaconnect.com/login",
            ["supportEmail"] = "support@lankaconnect.com"
        };

        var fromEmail = CreateSenderEmail();
        var subject = EmailSubject.Create("Welcome to LankaConnect!").Value;
        var textContent = $"Welcome {userName}! Thank you for joining LankaConnect.";
        
        var message = EmailMessage.Create(fromEmail, subject, textContent, null, EmailType.Welcome, 3).Value;
        message.AddRecipient(UserEmail.Create(email).Value);
        return message;
    }

    // Template data builders
    public static Dictionary<string, object> CreateUserRegistrationTemplateData(
        string userName, 
        string email, 
        string verificationToken)
    {
        return new Dictionary<string, object>
        {
            ["userName"] = userName,
            ["email"] = email,
            ["verificationToken"] = verificationToken,
            ["verificationUrl"] = $"https://lankaconnect.com/verify?token={verificationToken}",
            ["companyName"] = "LankaConnect",
            ["supportEmail"] = "support@lankaconnect.com",
            ["currentYear"] = DateTime.UtcNow.Year
        };
    }

    public static Dictionary<string, object> CreateBusinessApprovalTemplateData(
        string businessName, 
        string ownerName)
    {
        return new Dictionary<string, object>
        {
            ["businessName"] = businessName,
            ["ownerName"] = ownerName,
            ["approvalDate"] = DateTime.UtcNow.ToString("MMMM dd, yyyy"),
            ["dashboardUrl"] = "https://lankaconnect.com/dashboard",
            ["supportEmail"] = "support@lankaconnect.com"
        };
    }

    // Utility methods
    private static string GenerateRandomEmail()
    {
        var domains = new[] { "example.com", "test.com", "demo.org", "sample.net" };
        var username = $"user{_random.Next(1000, 9999)}";
        var domain = domains[_random.Next(domains.Length)];
        return $"{username}@{domain}";
    }

    public static string GenerateSecureToken(int length = 32)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
        return new string(Enumerable.Repeat(chars, length)
            .Select(s => s[_random.Next(s.Length)])
            .ToArray());
    }

    public static string GenerateHtmlEmailBody(string title, string content)
    {
        return $"""
            <!DOCTYPE html>
            <html>
            <head>
                <meta charset="utf-8">
                <title>{title}</title>
            </head>
            <body style="font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;">
                <h1 style="color: #333;">{title}</h1>
                <div style="line-height: 1.6;">
                    {content}
                </div>
                <hr style="margin: 20px 0;">
                <p style="font-size: 12px; color: #666;">
                    This email was sent from LankaConnect. Please do not reply to this email.
                </p>
            </body>
            </html>
            """;
    }

    // Helper methods for integration test compatibility
    public static EmailMessageDto CreateEmailMessageDto(TestEmailAddress to, string subject, string body, string? htmlBody = null)
    {
        return new EmailMessageDto
        {
            ToEmail = to.Address,
            Subject = subject,
            PlainTextBody = body,
            HtmlBody = htmlBody ?? $"<p>{body}</p>",
            Priority = 2
        };
    }
}

/// <summary>
/// Extension methods for IEmailService to provide backward compatibility with integration tests
/// </summary>
public static class EmailServiceExtensions
{
    public static async Task<bool> SendEmailAsync(this IEmailService emailService, TestEmailAddress to, string subject, string body)
    {
        var emailDto = EmailTestDataBuilder.CreateEmailMessageDto(to, subject, body);
        var result = await emailService.SendEmailAsync(emailDto);
        return result.IsSuccess;
    }

    public static async Task<bool> SendEmailAsync(this IEmailService emailService, TestEmailAddress to, string subject, string textBody, string htmlBody)
    {
        var emailDto = EmailTestDataBuilder.CreateEmailMessageDto(to, subject, textBody, htmlBody);
        var result = await emailService.SendEmailAsync(emailDto);
        return result.IsSuccess;
    }

    public static async Task<Guid> QueueEmailAsync(this IEmailService emailService, TestEmailAddress to, string subject, string body, string? htmlBody = null, int priority = 2)
    {
        var emailDto = new EmailMessageDto
        {
            ToEmail = to.Address,
            Subject = subject,
            PlainTextBody = body,
            HtmlBody = htmlBody ?? $"<p>{body}</p>",
            Priority = priority
        };
        
        await emailService.SendEmailAsync(emailDto);
        // Return a dummy GUID for now - in real implementation this would be the actual queue ID
        return Guid.NewGuid();
    }

    public static async Task ProcessEmailQueueAsync(this IEmailService emailService, int batchSize = 10)
    {
        // This is a stub for integration tests
        // In real implementation, this would process queued emails
        await Task.CompletedTask;
    }

    public static Task<EmailMessage?> GetEmailStatusAsync(this IEmailService emailService, Guid emailId)
    {
        // This is a stub for integration tests
        // In real implementation, this would retrieve email status from the repository
        return Task.FromResult<EmailMessage?>(null);
    }

    public static async Task RetryFailedEmailsAsync(this IEmailService emailService)
    {
        // This is a stub for integration tests
        // In real implementation, this would retry failed emails
        await Task.CompletedTask;
    }

    public static async Task<bool> SendTemplatedEmailAsync(this IEmailService emailService, TestEmailAddress to, string templateName, Dictionary<string, object> templateData)
    {
        var result = await emailService.SendTemplatedEmailAsync(templateName, to.Address, templateData);
        return result.IsSuccess;
    }
}