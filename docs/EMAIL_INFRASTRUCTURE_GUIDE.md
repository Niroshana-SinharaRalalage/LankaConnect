# Email Infrastructure Guide

## Overview

The LankaConnect email infrastructure provides a robust, production-ready email service that integrates seamlessly with MailHog for local development and can be easily configured for production email services like SendGrid, AWS SES, or SMTP providers.

## Features

- **MailHog Integration**: Local email testing with MailHog SMTP server
- **Template Support**: Razor-based email templates with variable substitution
- **Multiple Formats**: Support for both HTML and plain text emails
- **Development Mode**: Save emails to files for debugging
- **Production Ready**: Easy configuration for production email services
- **Logging**: Comprehensive logging for debugging and monitoring
- **Error Handling**: Graceful error handling and retry mechanisms

## Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                    Email Infrastructure                     │
├─────────────────────────────────────────────────────────────┤
│  SimpleEmailService                                         │
│  ├── SMTP Client (MailHog/Production)                      │
│  ├── Template Service                                       │
│  ├── File Output (Development)                             │
│  └── Logging & Error Handling                              │
├─────────────────────────────────────────────────────────────┤
│  Configuration                                              │
│  ├── EmailSettings                                          │
│  ├── Template Settings                                      │
│  └── Environment-specific Settings                         │
└─────────────────────────────────────────────────────────────┘
```

## Quick Start

### 1. Install MailHog (for local development)

```bash
# Using Docker
docker run -p 1025:1025 -p 8025:8025 mailhog/mailhog

# Or download binary from https://github.com/mailhog/MailHog/releases
```

### 2. Configuration

The email service is configured through `appsettings.json`:

```json
{
  "EmailSettings": {
    "SmtpServer": "localhost",
    "SmtpPort": 1025,
    "SenderEmail": "noreply@lankaconnect.local",
    "SenderName": "LankaConnect",
    "Username": "",
    "Password": "",
    "EnableSsl": false,
    "TimeoutInSeconds": 30,
    "MaxRetryAttempts": 3,
    "RetryDelayInMinutes": 5,
    "BatchSize": 10,
    "ProcessingIntervalInSeconds": 30,
    "TemplateBasePath": "Templates/Email",
    "CacheTemplates": true,
    "TemplateCacheExpiryInMinutes": 60,
    "IsDevelopment": true,
    "SaveEmailsToFile": true,
    "EmailSaveDirectory": "EmailOutput"
  }
}
```

### 3. Dependency Injection

The email services are automatically registered in `DependencyInjection.cs`:

```csharp
// Add Email Services
services.Configure<EmailSettings>(configuration.GetSection(EmailSettings.SectionName));
services.AddScoped<ISimpleEmailService, SimpleEmailService>();
services.AddScoped<IEmailTemplateService, RazorEmailTemplateService>();
```

### 4. Basic Usage

```csharp
public class SomeController : ControllerBase
{
    private readonly ISimpleEmailService _emailService;

    public SomeController(ISimpleEmailService emailService)
    {
        _emailService = emailService;
    }

    public async Task<IActionResult> SendWelcomeEmail()
    {
        var result = await _emailService.SendEmailAsync(
            to: "user@example.com",
            subject: "Welcome to LankaConnect!",
            textBody: "Welcome to our platform...",
            htmlBody: "<h1>Welcome to our platform!</h1><p>...</p>"
        );

        return result ? Ok() : BadRequest("Failed to send email");
    }
}
```

## Email Templates

### Template Structure

Templates are stored in the `Templates/Email/` directory with the following naming convention:

- `{templateName}-subject.txt` - Email subject template
- `{templateName}-text.txt` - Plain text body template  
- `{templateName}-html.html` - HTML body template

### Example Templates

**welcome-subject.txt**:
```
Welcome to LankaConnect, {{Name}}!
```

**welcome-text.txt**:
```
Hello {{Name}},

Welcome to LankaConnect! We're excited to have you join our community.

Your account: {{Email}}
Date joined: {{Date}}

Visit us at: {{SiteUrl}}

Best regards,
The LankaConnect Team
```

**welcome-html.html**:
```html
<!DOCTYPE html>
<html>
<body>
    <h1>Welcome to LankaConnect, {{Name}}!</h1>
    <p>We're excited to have you join our community.</p>
    <div>
        <strong>Account:</strong> {{Email}}<br>
        <strong>Date joined:</strong> {{Date}}
    </div>
    <a href="{{SiteUrl}}">Visit LankaConnect</a>
</body>
</html>
```

### Using Templates

```csharp
var templateData = new Dictionary<string, object>
{
    ["Name"] = "John Doe",
    ["Email"] = "john@example.com", 
    ["Date"] = DateTime.Now.ToString("yyyy-MM-dd"),
    ["SiteUrl"] = "https://lankaconnect.lk"
};

var result = await _emailService.SendTemplatedEmailAsync(
    "john@example.com",
    "welcome",
    templateData
);
```

## Testing

### Email Controller

The project includes an `EmailController` for testing email functionality:

```http
POST /api/email/test-connection
POST /api/email/send-test
POST /api/email/send-template
```

### Standalone Email Tester

Run the email tester console application:

```bash
cd EmailTester
dotnet run
```

This will test:
1. SMTP connection
2. Simple text email
3. HTML email  
4. Template email

### MailHog Web Interface

View sent emails in the MailHog web interface:
- URL: http://localhost:8025
- Shows all emails sent through the SMTP server
- Supports email search and filtering

## Production Configuration

### SendGrid Example

```json
{
  "EmailSettings": {
    "SmtpServer": "smtp.sendgrid.net",
    "SmtpPort": 587,
    "SenderEmail": "noreply@yourdomain.com",
    "SenderName": "Your Application",
    "Username": "apikey",
    "Password": "your-sendgrid-api-key",
    "EnableSsl": true,
    "IsDevelopment": false,
    "SaveEmailsToFile": false
  }
}
```

### AWS SES Example

```json
{
  "EmailSettings": {
    "SmtpServer": "email-smtp.us-west-2.amazonaws.com",
    "SmtpPort": 587,
    "SenderEmail": "noreply@yourdomain.com", 
    "SenderName": "Your Application",
    "Username": "your-ses-username",
    "Password": "your-ses-password",
    "EnableSsl": true,
    "IsDevelopment": false,
    "SaveEmailsToFile": false
  }
}
```

## Monitoring and Logging

The email service includes comprehensive logging:

```csharp
// Connection attempts
_logger.LogInformation("Testing SMTP connection to {Server}:{Port}", server, port);

// Email sending
_logger.LogInformation("Sending email to {Recipient} with subject {Subject}", to, subject);
_logger.LogInformation("Email sent successfully to {Recipient}", to);

// Errors
_logger.LogError(ex, "Failed to send email to {Recipient}", to);

// Template processing
_logger.LogInformation("Template {TemplateName} rendered successfully", templateName);
```

## Error Handling

The service handles various error scenarios:

- **SMTP Connection Failures**: Logged with detailed error information
- **Template Not Found**: Graceful failure with error logging
- **Invalid Email Addresses**: Validation before sending
- **Template Rendering Errors**: Specific template error messages
- **File System Errors**: Non-blocking for development file saving

## File Structure

```
src/
├── LankaConnect.Infrastructure/
│   └── Email/
│       ├── Configuration/
│       │   └── EmailSettings.cs
│       ├── Interfaces/
│       │   └── ISimpleEmailService.cs  
│       ├── Models/
│       │   └── SimpleEmailMessage.cs
│       └── Services/
│           ├── SimpleEmailService.cs
│           └── RazorEmailTemplateService.cs
├── LankaConnect.API/
│   └── Controllers/
│       └── EmailController.cs
└── Templates/
    └── Email/
        ├── welcome-subject.txt
        ├── welcome-text.txt
        ├── welcome-html.html
        ├── password-reset-subject.txt
        ├── password-reset-text.txt
        └── password-reset-html.html
```

## Best Practices

1. **Always test locally** with MailHog before deploying
2. **Use templates** for consistent email formatting
3. **Include both HTML and text** versions of emails
4. **Log email operations** for debugging and monitoring
5. **Validate email addresses** before sending
6. **Handle failures gracefully** with proper error messages
7. **Use environment-specific configuration** for different stages
8. **Monitor email delivery** in production

## Troubleshooting

### MailHog Not Receiving Emails

1. Check MailHog is running: `docker ps`
2. Verify port 1025 is not in use by other services
3. Check SMTP server configuration in appsettings.json
4. Review application logs for connection errors

### Templates Not Loading

1. Verify template files exist in `Templates/Email/` directory
2. Check file naming convention (`templatename-subject.txt`, etc.)
3. Ensure template files are copied to output directory
4. Review template service logs for errors

### Production Email Issues

1. Verify SMTP credentials and server settings
2. Check firewall rules for SMTP port access
3. Monitor email service provider logs/dashboard
4. Review application logs for detailed error messages

## Future Enhancements

- **Background Queue Processing**: For high-volume email sending
- **Email Analytics**: Open/click tracking and reporting
- **Advanced Templates**: More sophisticated template engine
- **Email Scheduling**: Delayed email sending
- **Attachment Support**: File attachment handling
- **Bulk Email Operations**: Efficient mass email sending