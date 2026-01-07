# Phase 6A.63: SMTP Configuration Fix for Staging Environment

**Date**: 2026-01-06
**Priority**: URGENT
**Environment**: Staging (Azure Container Apps)

---

## Quick Fix: Configure SMTP Settings

### Option 1: Azure CLI (Recommended)

```bash
# Replace with your actual SMTP credentials
az containerapp update \
  --name lankaconnect-api-staging \
  --resource-group lankaconnect-rg \
  --set-env-vars \
    "SmtpSettings__Host=smtp.sendgrid.net" \
    "SmtpSettings__Port=587" \
    "SmtpSettings__EnableSsl=true" \
    "SmtpSettings__Username=apikey" \
    "SmtpSettings__Password=YOUR_SENDGRID_API_KEY_HERE" \
    "SmtpSettings__FromEmail=noreply@lankaconnect.com" \
    "SmtpSettings__FromName=LankaConnect"

# Restart the container app to apply changes
az containerapp restart \
  --name lankaconnect-api-staging \
  --resource-group lankaconnect-rg
```

### Option 2: Azure Portal (Manual)

1. Go to: **Azure Portal → Container Apps → lankaconnect-api-staging**
2. Navigate to: **Settings → Environment variables**
3. Add the following environment variables:

| Name | Value | Type |
|------|-------|------|
| SmtpSettings__Host | smtp.sendgrid.net | Plain text |
| SmtpSettings__Port | 587 | Plain text |
| SmtpSettings__EnableSsl | true | Plain text |
| SmtpSettings__Username | apikey | Plain text |
| SmtpSettings__Password | YOUR_SENDGRID_API_KEY | Secret |
| SmtpSettings__FromEmail | noreply@lankaconnect.com | Plain text |
| SmtpSettings__FromName | LankaConnect | Plain text |

4. Click **Save** and **Restart** the container

---

## SMTP Provider Options

### Option A: SendGrid (Recommended for Azure)

**Pros**:
- Free tier: 100 emails/day
- Good Azure integration
- Reliable delivery rates
- Comprehensive analytics

**Setup**:
1. Create SendGrid account: https://signup.sendgrid.com/
2. Navigate to: **Settings → API Keys**
3. Create new API key with "Full Access" or "Mail Send" permission
4. Copy API key (starts with `SG.`)
5. Use configuration:
   ```
   Host: smtp.sendgrid.net
   Port: 587
   EnableSsl: true
   Username: apikey
   Password: <YOUR_API_KEY>
   ```

### Option B: Mailgun

**Pros**:
- Free tier: 5,000 emails/month (first 3 months)
- Simple API
- Good documentation

**Setup**:
1. Create account: https://signup.mailgun.com/
2. Go to: **Sending → Domain settings → SMTP credentials**
3. Use configuration:
   ```
   Host: smtp.mailgun.org
   Port: 587
   EnableSsl: true
   Username: postmaster@YOUR_DOMAIN.mailgun.org
   Password: <YOUR_SMTP_PASSWORD>
   ```

### Option C: SMTP2GO

**Pros**:
- Free tier: 1,000 emails/month
- Simple setup
- Good for testing

**Setup**:
1. Create account: https://www.smtp2go.com/
2. Go to: **Settings → Users**
3. Create SMTP user
4. Use configuration:
   ```
   Host: mail.smtp2go.com
   Port: 2525 or 587
   EnableSsl: true
   Username: <YOUR_USERNAME>
   Password: <YOUR_PASSWORD>
   ```

### Option D: Azure Communication Services Email

**Pros**:
- Native Azure service
- Pay-as-you-go pricing
- High deliverability

**Setup** (requires code changes):
1. Create Azure Communication Services resource
2. Add Email domain
3. Install NuGet: `Azure.Communication.Email`
4. Update `EmailService.cs` to support ACS
5. Use ACS SDK instead of SMTP

---

## Verification Steps

### 1. Check Current Configuration

```bash
# View current environment variables
az containerapp show \
  --name lankaconnect-api-staging \
  --resource-group lankaconnect-rg \
  --query "properties.configuration.secrets"
```

### 2. Test Email Sending

**Method A: Cancel a test event** (existing functionality)
1. Create a test event
2. Register for the event
3. Cancel the event
4. Check email inbox

**Method B: Add temporary test endpoint** (recommended for troubleshooting)

Add to `EventsController.cs`:
```csharp
[HttpPost("test-email")]
[Authorize]
public async Task<IActionResult> TestEmailNotification([FromQuery] string email)
{
    var result = await _emailService.SendTemplatedEmailAsync(
        "event-cancelled-notification",
        email,
        new Dictionary<string, object>
        {
            ["EventTitle"] = "Test Event - Email System Check",
            ["EventStartDate"] = "January 15, 2026",
            ["EventStartTime"] = "10:00 AM",
            ["EventLocation"] = "Test Location, Test City, TS",
            ["CancellationReason"] = "This is a test email to verify SMTP configuration is working correctly."
        });

    if (result.IsSuccess)
        return Ok(new { message = "Test email sent successfully", recipient = email });
    else
        return BadRequest(new { message = "Email send failed", errors = result.Errors });
}
```

**Test with**:
```bash
curl -X POST "https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/api/events/test-email?email=your-email@example.com" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"
```

### 3. Check Logs

**Azure Portal**:
1. Go to: **Container Apps → lankaconnect-api-staging → Monitoring → Log stream**
2. Filter by: `Phase 6A.63` OR `SMTP` OR `EventCancelledEventHandler`

**Expected SUCCESS logs**:
```
[Phase 6A.63] Sending cancellation emails to 5 unique recipients for Event {EventId}
[Phase 6A.63] Event cancellation emails completed for event {EventId}. Success: 5, Failed: 0
```

**Expected FAILURE logs** (if still not working):
```
[ERROR] SMTP send failed for email to user@example.com
[Phase 6A.63] Failed to send event cancellation email to user@example.com: SMTP send failed: ...
[Phase 6A.63] Event cancellation emails completed. Success: 0, Failed: 5
```

---

## Common Issues & Troubleshooting

### Issue 1: "SMTP server requires a secure connection"
**Cause**: Wrong port or EnableSsl setting
**Fix**:
- Try port 587 with EnableSsl=true
- Or port 465 with EnableSsl=true
- Or port 25 with EnableSsl=false (NOT recommended)

### Issue 2: "Authentication failed"
**Cause**: Wrong username/password
**Fix**:
- Verify credentials with your SMTP provider
- For SendGrid: username must be "apikey" (literal string)
- Check if API key has mail send permission

### Issue 3: "Connection timeout"
**Cause**: Azure Container Apps firewall or wrong host
**Fix**:
- Verify SMTP host is correct
- Check if outbound connections are allowed
- Try different SMTP provider

### Issue 4: "Email template not found"
**Cause**: Database migration not applied
**Fix**:
```sql
-- Check if template exists
SELECT * FROM communications.email_templates
WHERE name = 'event-cancelled-notification';

-- If missing, run migration manually
-- Phase6A63Fix4_FixCategoryValue migration should have created it
```

### Issue 5: "Emails sent but not received"
**Cause**: Spam filtering or wrong FromEmail
**Fix**:
- Check spam/junk folders
- Verify FromEmail domain is configured with SPF/DKIM
- Use verified sender domain with SendGrid

---

## Production Checklist

Before deploying to production:

- [ ] SMTP credentials are stored as **secrets** (not plain text)
- [ ] FromEmail domain has SPF and DKIM records configured
- [ ] Email sending rate limits configured (prevent abuse)
- [ ] Application Insights configured for email delivery metrics
- [ ] Email retry logic implemented (exponential backoff)
- [ ] Dead letter queue for failed emails
- [ ] Admin dashboard to view email delivery status
- [ ] Unsubscribe functionality implemented
- [ ] Email templates use production branding
- [ ] Test emails sent to multiple providers (Gmail, Outlook, Yahoo)
- [ ] Monitoring alerts configured for high email failure rates

---

## Next Steps (After SMTP Fix)

1. **Monitor Email Delivery**:
   - Track success/failure rates in Application Insights
   - Set up alerts for email failures > 10%

2. **Implement Email Queue**:
   - Use Hangfire for email retry logic
   - Prevent email sending from blocking HTTP requests
   - Add exponential backoff for transient failures

3. **Add Email Delivery Tracking**:
   - Store email send status in database
   - Track opens/clicks (if needed)
   - Provide delivery status to users

4. **Enhance Email Templates**:
   - Add HTML styling
   - Add inline images (event logo)
   - Add unsubscribe footer
   - Add social media links

5. **Production-Ready Email Service**:
   - Switch to Azure Communication Services or SendGrid API
   - Implement email queueing with Hangfire
   - Add comprehensive logging and metrics
   - Implement retry logic with exponential backoff

---

## Support

**If emails still don't send after SMTP configuration**:

1. Check Application Insights logs for detailed error messages
2. Verify database has correct email template (run SQL query above)
3. Test with temporary test endpoint to isolate issue
4. Contact SMTP provider support for connection issues
5. Consider switching to different SMTP provider

**For urgent issues**:
- Check `docs/PHASE_6A63_EMAIL_NOTIFICATION_ROOT_CAUSE_ANALYSIS.md` for complete flow analysis
- All code paths are correct - issue is purely configuration
- SMTP connection is the only failure point
