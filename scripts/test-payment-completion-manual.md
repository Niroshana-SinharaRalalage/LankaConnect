# Manual Testing for Phase 6A.24 Payment Completion Flow

## Issue Discovered

When you clicked "Resend event" in Stripe Dashboard, the webhook returned HTTP 200 but didn't send email/generate ticket because:

**Root Cause**: Idempotency Protection
- Stripe sends the SAME event ID when resending
- Our webhook checks `IsEventProcessedAsync(eventId)` (line 242 in PaymentsController.cs)
- If already processed, returns HTTP 200 OK immediately without processing (line 244-246)
- This is correct behavior to prevent duplicate charges/emails

## Testing Options

### Option 1: Create NEW Test Registration (RECOMMENDED)

This tests the complete end-to-end flow:

1. Go to staging site: http://localhost:3000/events
2. Find a paid event
3. Register with a NEW email address (important!)
4. Complete payment with Stripe test card: `4242 4242 4242 4242`
5. Stripe will send a NEW webhook event with unique event ID
6. This will trigger the complete flow:
   - ✅ Webhook received
   - ✅ Registration.CompletePayment() called
   - ✅ PaymentCompletedEvent raised
   - ✅ CommitAsync() dispatches to MediatR (NEW FIX)
   - ✅ PaymentCompletedEventHandler generates ticket + sends email (NEW FIX)

### Option 2: Test Existing Registration via API

For an existing registration that's stuck in "Completed" status but has no ticket:

#### Step 1: Find Registration ID

Check the database for registrations with status "Completed" but no ticket:

```sql
SELECT
    er.id as registration_id,
    er.status,
    er.payment_status,
    e.title as event_title,
    u.email as user_email,
    t.id as ticket_id
FROM event_registrations er
JOIN events e ON er.event_id = e.id
LEFT JOIN users u ON er.user_id = u.id
LEFT JOIN tickets t ON t.registration_id = er.id
WHERE er.status = 'Completed'
  AND er.payment_status = 'Paid'
  AND t.id IS NULL
ORDER BY er.updated_at DESC;
```

#### Step 2: Create Test API Endpoint (Temporary)

Add this to PaymentsController.cs for testing only:

```csharp
[HttpPost("test/complete-payment/{registrationId}")]
[AllowAnonymous]
public async Task<IActionResult> TestCompletePayment(Guid registrationId)
{
    try
    {
        // Find registration
        var registration = await _context.EventRegistrations
            .Include(r => r.Event)
            .FirstOrDefaultAsync(r => r.Id == registrationId);

        if (registration == null)
            return NotFound("Registration not found");

        // Manually trigger payment completion (even if already completed)
        registration.CompletePayment("test_payment_intent", 50.00m, "niroshhh@gmail.com");

        // Save and dispatch events
        await _context.CommitAsync();

        return Ok(new { message = "Payment completion event triggered" });
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error testing payment completion");
        return StatusCode(500, new { error = ex.Message });
    }
}
```

Then call: `POST https://lankaconnect-api-staging.../api/payments/test/complete-payment/{registrationId}`

### Option 3: Delete Webhook Event Record (Database Hack)

**WARNING**: This is a hack for testing only. Don't use in production.

Find the event ID in Stripe Dashboard (e.g., `evt_1S6RB8Lvfbr023L17G4L11DG`) and delete it from the database:

```sql
DELETE FROM stripe_webhook_events
WHERE event_id = 'evt_1S6RB8Lvfbr023L17G4L11DG';
```

Then "Resend event" from Stripe Dashboard. The webhook will process it as a new event.

## Recommendation

**Use Option 1** (new test registration) to verify the complete end-to-end flow is working with all fixes deployed.

## Expected Results After Fix

After testing with Option 1:
1. ✅ Payment succeeds in Stripe
2. ✅ Webhook receives HTTP 200
3. ✅ Registration status: "Completed"
4. ✅ Ticket generated with QR code
5. ✅ Email sent with PDF attachment
6. ✅ Email received at user's email address

## Verification Checklist

- [ ] Email received at correct address (niroshhh@gmail.com or niroshanaks@gmail.com)
- [ ] Email contains event details
- [ ] PDF attachment present
- [ ] QR code visible in PDF
- [ ] Ticket visible in user's "My Events" page
- [ ] QR code scannable (can test with phone camera)
