# Quick Start: Webhook Idempotency Recovery

## 30-Second Overview

**Problem**: User paid but didn't receive email/ticket due to old revision bug.
**Solution**: Call admin endpoint to manually trigger email/ticket generation.
**Registration**: `219dd972-2309-4898-a972-4e0b6a7224fb`

---

## Step-by-Step Execution

### 1. Verify Current State (2 minutes)

Run SQL verification script:

```bash
sqlcmd -S <your-server> -d LankaConnect -i scripts/recover-payment-completed-event.sql
```

**Expected**:
- ✅ PaymentStatus: Completed
- ✅ RegistrationStatus: Confirmed
- ❌ Ticket: NOT found (this is what we're fixing)

---

### 2. Call Recovery Endpoint (1 minute)

#### Using curl:

```bash
curl -X POST https://lankaconnect.azurecontainerapps.io/api/admin/recovery/trigger-payment-event \
  -H "Authorization: Bearer <YOUR_JWT_TOKEN>" \
  -H "Content-Type: application/json" \
  -d '{"registrationId":"219dd972-2309-4898-a972-4e0b6a7224fb"}'
```

#### Using PowerShell:

```powershell
$headers = @{
    "Authorization" = "Bearer <YOUR_JWT_TOKEN>"
    "Content-Type" = "application/json"
}

$body = @{
    registrationId = "219dd972-2309-4898-a972-4e0b6a7224fb"
} | ConvertTo-Json

Invoke-RestMethod `
    -Uri "https://lankaconnect.azurecontainerapps.io/api/admin/recovery/trigger-payment-event" `
    -Method Post `
    -Headers $headers `
    -Body $body
```

#### Using Swagger UI:

1. Go to `https://lankaconnect.azurecontainerapps.io/swagger`
2. Authenticate (click "Authorize" button)
3. Find `POST /api/admin/recovery/trigger-payment-event`
4. Click "Try it out"
5. Enter:
   ```json
   {
     "registrationId": "219dd972-2309-4898-a972-4e0b6a7224fb"
   }
   ```
6. Click "Execute"

---

### 3. Verify Success (5 minutes)

#### Check API Response:

```json
{
  "message": "PaymentCompletedEvent published successfully",
  "registrationId": "219dd972-2309-4898-a972-4e0b6a7224fb",
  "eventId": "<event-guid>",
  "contactEmail": "<user-email>",
  "amountPaid": 250.00,
  "attendeeCount": 1
}
```

#### Check Database:

```sql
-- Should return 1 ticket
SELECT * FROM Tickets
WHERE RegistrationId = '219dd972-2309-4898-a972-4e0b6a7224fb';
```

#### Check Logs:

Look for in Azure Container App logs:
```
[ADMIN RECOVERY] Successfully published PaymentCompletedEvent
[Phase 6A.24] ✅ PaymentCompletedEventHandler INVOKED
Ticket generated successfully
Payment confirmation email sent successfully
```

#### Ask User:

- Did they receive email?
- Does email have PDF attachment?
- Can they open the ticket PDF?

---

## Troubleshooting

### Error: "Registration not found"
- Check registration ID is correct
- Run SQL verification script

### Error: "Payment not completed"
- Registration has incorrect payment status
- DO NOT PROCEED - investigate payment issue first

### Error: "No contact email found"
- Database missing contact information
- Check `Registrations.Contact_Email` field

### Success Response but No Email Sent
- Check application logs for email service errors
- Verify Azure Communication Services is configured
- Check email templates exist

### Duplicate Tickets Created
- Safe to ignore - only latest ticket is valid
- Older tickets will be marked as Cancelled
- User can use any ticket (QR code validation checks registration)

---

## After Recovery

### Remove Temporary Controller

**IMPORTANT**: Once recovery is complete and verified, DELETE the temporary controller:

```bash
# Remove the file
rm src/LankaConnect.API/Controllers/AdminRecoveryController.cs

# Rebuild and redeploy
dotnet build src/LankaConnect.API/LankaConnect.API.csproj
dotnet publish -c Release

# Deploy to Azure (your deployment process)
```

### Document Incident

Add to post-mortem:
- Root cause: IPublisher NULL in old revision
- Impact: 1 user missing email/ticket
- Resolution: Manual domain event trigger
- Prevention: Add DI container health checks

---

## Safety Notes

✅ **SAFE OPERATIONS**:
- Can be called multiple times without harm
- No modification to payment or registration state
- Only triggers email/ticket generation
- Comprehensive logging for audit

⚠️ **CAUTION**:
- User may receive duplicate emails if called multiple times
- Only call once per registration

❌ **NEVER DO**:
- Modify payment status directly in database
- Delete webhook event records
- Create new payment for same registration

---

## Quick Reference Links

- **Full Documentation**: `docs/architecture/WEBHOOK_IDEMPOTENCY_RECOVERY_PLAN.md`
- **SQL Verification**: `scripts/recover-payment-completed-event.sql`
- **Controller Code**: `src/LankaConnect.API/Controllers/AdminRecoveryController.cs`

---

**Questions?** Check full documentation or contact development team.
