# LankaConnect Email Configuration Guide

**Date**: 2025-12-10
**Author**: Development Team
**Status**: Active Configuration

---

## Table of Contents

1. [Overview](#overview)
2. [Current Architecture](#current-architecture)
3. [Azure Communication Services Setup](#azure-communication-services-setup)
4. [Configuration Reference](#configuration-reference)
5. [Provider Switching Guide](#provider-switching-guide)
6. [Cost Analysis](#cost-analysis)
7. [Troubleshooting](#troubleshooting)

---

## Overview

LankaConnect uses a flexible email system that supports multiple email providers through configuration changes. The current implementation uses **Azure Communication Services** with an Azure-managed domain.

### Key Features

- **Provider Agnostic**: Switch between providers (Azure, SendGrid, Gmail, SES) via configuration
- **SDK-based Sending**: Uses Azure.Communication.Email SDK for reliable delivery
- **SMTP Fallback**: Supports standard SMTP for alternative providers
- **Template Support**: Razor-based email templates
- **Queue Processing**: Background job processing for bulk emails

---

## Current Architecture

### Email Services Structure

```
src/LankaConnect.Infrastructure/Email/
├── Configuration/
│   └── EmailSettings.cs          # Unified email configuration
├── Interfaces/
│   └── ISimpleEmailService.cs    # Simple email interface
├── Models/
│   └── SimpleEmailMessage.cs     # Email message model
└── Services/
    ├── EmailService.cs           # Full-featured email service (IEmailService)
    ├── SimpleEmailService.cs     # Simple SMTP service
    ├── AzureEmailService.cs      # Azure SDK-based service
    ├── EmailQueueProcessor.cs    # Background queue processor
    └── RazorEmailTemplateService.cs  # Template rendering
```

### Service Registration

```csharp
// In DependencyInjection.cs
services.Configure<EmailSettings>(configuration.GetSection("EmailSettings"));
services.AddScoped<IEmailService, AzureEmailService>();  // Production
services.AddScoped<ISimpleEmailService, SimpleEmailService>(); // SMTP fallback
```

---

## Azure Communication Services Setup

### Prerequisites

1. Azure subscription
2. Resource group (e.g., `lankaconnect-staging`)

### Step 1: Create Communication Services Resource

```bash
# Via Azure CLI
az communication create \
  --name "lankaconnect-communication" \
  --resource-group "lankaconnect-staging" \
  --location "global" \
  --data-location "unitedstates"
```

Or via Azure Portal:
1. Go to Azure Portal → Create Resource
2. Search "Communication Services"
3. Click Create and fill in details

### Step 2: Create Email Service Resource

```bash
az communication email create \
  --name "lankaconnect-email" \
  --resource-group "lankaconnect-staging" \
  --location "global" \
  --data-location "unitedstates"
```

### Step 3: Add Azure Managed Domain

```bash
az communication email domain create \
  --domain-name "AzureManagedDomain" \
  --email-service-name "lankaconnect-email" \
  --resource-group "lankaconnect-staging" \
  --domain-management "AzureManaged"
```

This creates a domain like: `xxxxxxxx-xxxx-xxxx.azurecomm.net`

### Step 4: Connect Domain to Communication Services

```bash
MSYS_NO_PATHCONV=1 az communication update \
  --name "lankaconnect-communication" \
  --resource-group "lankaconnect-staging" \
  --linked-domains "/subscriptions/{subscription-id}/resourceGroups/lankaconnect-staging/providers/Microsoft.Communication/emailServices/lankaconnect-email/domains/AzureManagedDomain"
```

### Step 5: Get Connection String

```bash
az communication list-key \
  --name "lankaconnect-communication" \
  --resource-group "lankaconnect-staging" \
  -o json
```

Save the `primaryConnectionString` for configuration.

### Current Configuration

| Setting | Value |
|---------|-------|
| Communication Service | `lankaconnect-communication` |
| Email Service | `lankaconnect-email` |
| Domain | `7689582e-73cc-4552-b2ff-8afd9d1a6814.azurecomm.net` |
| Sender Email | `DoNotReply@7689582e-73cc-4552-b2ff-8afd9d1a6814.azurecomm.net` |
| Resource Group | `lankaconnect-staging` |

---

## Configuration Reference

### EmailSettings.cs Properties

```csharp
public class EmailSettings
{
    // Provider Selection
    public string Provider { get; set; } = "Azure"; // Azure, SMTP, SendGrid

    // Azure Communication Services
    public string AzureConnectionString { get; set; } = string.Empty;
    public string AzureSenderAddress { get; set; } = string.Empty;

    // SMTP Settings (for non-Azure providers)
    public string SmtpServer { get; set; } = string.Empty;
    public int SmtpPort { get; set; } = 587;
    public string SenderEmail { get; set; } = string.Empty;
    public string SenderName { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public bool EnableSsl { get; set; } = true;

    // Development Settings
    public bool IsDevelopment { get; set; } = false;
    public bool SaveEmailsToFile { get; set; } = false;
    public string EmailSaveDirectory { get; set; } = "EmailOutput";
}
```

### appsettings.json (Development)

```json
{
  "EmailSettings": {
    "Provider": "Azure",
    "AzureConnectionString": "endpoint=https://lankaconnect-communication.unitedstates.communication.azure.com/;accesskey=YOUR_KEY",
    "AzureSenderAddress": "DoNotReply@7689582e-73cc-4552-b2ff-8afd9d1a6814.azurecomm.net",
    "SenderName": "LankaConnect",
    "IsDevelopment": true,
    "SaveEmailsToFile": false
  }
}
```

### appsettings.Staging.json

```json
{
  "EmailSettings": {
    "Provider": "Azure",
    "AzureConnectionString": "${AZURE_EMAIL_CONNECTION_STRING}",
    "AzureSenderAddress": "${AZURE_EMAIL_SENDER_ADDRESS}",
    "SenderName": "LankaConnect"
  }
}
```

### appsettings.Production.json

```json
{
  "EmailSettings": {
    "Provider": "Azure",
    "AzureConnectionString": "${AZURE_EMAIL_CONNECTION_STRING}",
    "AzureSenderAddress": "${AZURE_EMAIL_SENDER_ADDRESS}",
    "SenderName": "LankaConnect"
  }
}
```

---

## Provider Switching Guide

### Switch to SendGrid

```json
{
  "EmailSettings": {
    "Provider": "SMTP",
    "SmtpServer": "smtp.sendgrid.net",
    "SmtpPort": 587,
    "SenderEmail": "noreply@yourdomain.com",
    "SenderName": "LankaConnect",
    "Username": "apikey",
    "Password": "SG.your-sendgrid-api-key",
    "EnableSsl": true
  }
}
```

### Switch to Gmail

```json
{
  "EmailSettings": {
    "Provider": "SMTP",
    "SmtpServer": "smtp.gmail.com",
    "SmtpPort": 587,
    "SenderEmail": "your-email@gmail.com",
    "SenderName": "LankaConnect",
    "Username": "your-email@gmail.com",
    "Password": "your-app-password",
    "EnableSsl": true
  }
}
```

**Note**: Gmail requires an App Password (not regular password). Enable 2FA and generate at: https://myaccount.google.com/apppasswords

### Switch to Amazon SES

```json
{
  "EmailSettings": {
    "Provider": "SMTP",
    "SmtpServer": "email-smtp.us-east-1.amazonaws.com",
    "SmtpPort": 587,
    "SenderEmail": "noreply@yourdomain.com",
    "SenderName": "LankaConnect",
    "Username": "AKIAIOSFODNN7EXAMPLE",
    "Password": "wJalrXUtnFEMI/K7MDENG/bPxRfiCYEXAMPLEKEY",
    "EnableSsl": true
  }
}
```

### Switch to Outlook/Office 365

```json
{
  "EmailSettings": {
    "Provider": "SMTP",
    "SmtpServer": "smtp-mail.outlook.com",
    "SmtpPort": 587,
    "SenderEmail": "your-email@outlook.com",
    "SenderName": "LankaConnect",
    "Username": "your-email@outlook.com",
    "Password": "your-password",
    "EnableSsl": true
  }
}
```

---

## Cost Analysis

### Monthly Cost Comparison (100,000 emails/month)

| Provider | Cost | Notes |
|----------|------|-------|
| **Amazon SES** | ~$10 | Cheapest, requires domain verification |
| **Azure Communication Services** | ~$25 | Azure-managed domain included |
| **SendGrid (Free)** | $0 (100/day) | Limited to 3,000/month |
| **SendGrid (Essentials)** | $19.95 | 50,000 emails/month |
| **SendGrid (Pro)** | $89.95 | 100,000 emails/month |
| **Mailgun** | $35 | 50,000 emails included |
| **Postmark** | $50 | 50,000 emails/month |

### Azure Communication Services Pricing

| Tier | Price |
|------|-------|
| First 100,000 emails | $0.00025 per email ($25/100K) |
| 100,001 - 1,000,000 | $0.0002 per email |
| 1,000,001+ | $0.00015 per email |

---

## Troubleshooting

### Common Issues

#### 1. "Connection refused" error

- Check firewall allows outbound port 587/25
- Verify SMTP server address is correct
- For Azure, ensure connection string is valid

#### 2. "Authentication failed"

- Verify username/password are correct
- For Gmail: Use App Password, not regular password
- For SendGrid: Username must be exactly "apikey"
- For Azure: Check connection string format

#### 3. Emails not received

- Check spam/junk folder
- Verify sender email domain is verified
- Check email service logs in Azure Portal
- Test with Azure CLI: `az communication email send ...`

#### 4. "Sender not authorized"

- Ensure sender email matches verified domain
- For Azure managed domain, use the provided `DoNotReply@xxx.azurecomm.net` address

### Test Email via CLI

```bash
# Test Azure Communication Services
az communication email send \
  --sender "DoNotReply@7689582e-73cc-4552-b2ff-8afd9d1a6814.azurecomm.net" \
  --subject "Test Email" \
  --to "recipient@example.com" \
  --text "Test message" \
  --connection-string "your-connection-string"
```

### View Email Logs

```bash
# Check recent email status
az communication email status \
  --name "lankaconnect-communication" \
  --resource-group "lankaconnect-staging"
```

---

## Future Improvements

### Custom Domain Setup (Planned)

When ready to use a custom domain (e.g., `noreply@lankaconnect.com`):

1. Add custom domain in Email Service
2. Configure DNS records (SPF, DKIM, DMARC)
3. Verify domain ownership
4. Update `AzureSenderAddress` in configuration

### SMS Integration (Planned)

Azure Communication Services also supports SMS:

```csharp
// Future implementation
var smsClient = new SmsClient(connectionString);
await smsClient.SendAsync(from: "+1234567890", to: "+0987654321", message: "Hello!");
```

### Push Notifications (Planned)

For mobile app notifications, use Azure Notification Hubs (separate service).

---

## References

- [Azure Communication Services Documentation](https://learn.microsoft.com/en-us/azure/communication-services/)
- [Azure Email SMTP Overview](https://learn.microsoft.com/en-us/azure/communication-services/concepts/email/email-smtp-overview)
- [SendGrid SMTP Integration](https://docs.sendgrid.com/for-developers/sending-email/integrating-with-the-smtp-api)
- [Gmail SMTP Settings](https://support.google.com/mail/answer/7126229)
