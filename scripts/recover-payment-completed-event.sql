-- ============================================================================
-- RECOVERY SCRIPT: Manual PaymentCompletedEvent Trigger
-- ============================================================================
--
-- PURPOSE: Recover from failed webhook processing due to IPublisher NULL bug
--
-- SCENARIO:
--   - Webhook evt_1SfSktLvfbr023L1qB78D1CR processed by old revision
--   - Payment status updated successfully
--   - Domain event raised but NOT dispatched (IPublisher was NULL)
--   - No email sent, no ticket generated
--   - Idempotency flag prevents reprocessing
--
-- SOLUTION:
--   This script provides the data needed for a manual domain event trigger
--   via a temporary API endpoint or administrative tool.
--
-- SAFETY:
--   - Read-only verification queries
--   - Does NOT modify any data
--   - Safe to run multiple times
--
-- ============================================================================

USE LankaConnect;
GO

-- Step 1: Verify Registration State
-- ============================================================================
PRINT '=== STEP 1: Verify Registration State ==='
PRINT ''

SELECT
    r.Id AS RegistrationId,
    r.EventId,
    r.UserId,
    r.Status AS RegistrationStatus,
    r.PaymentStatus,
    r.StripeCheckoutSessionId,
    r.StripePaymentIntentId,
    r.Quantity,
    r.CreatedAt,
    r.UpdatedAt,
    r.Contact_Email AS ContactEmail,
    r.Contact_Name AS ContactName,
    r.Contact_PhoneNumber AS ContactPhone,
    r.TotalPrice_Amount AS TotalAmount,
    r.TotalPrice_Currency AS Currency
FROM
    Registrations r
WHERE
    r.Id = '219dd972-2309-4898-a972-4e0b6a7224fb';

PRINT ''
PRINT '--- Expected Values ---'
PRINT 'Status: Confirmed (enum value 1)'
PRINT 'PaymentStatus: Completed (enum value 2)'
PRINT 'StripePaymentIntentId: Should be populated'
PRINT ''

-- Step 2: Verify Event Details
-- ============================================================================
PRINT '=== STEP 2: Verify Event Details ==='
PRINT ''

SELECT
    e.Id AS EventId,
    e.Title_Value AS EventTitle,
    e.StartDate,
    e.EndDate,
    e.Location_Address_Street,
    e.Location_Address_City,
    e.Location_Address_State,
    e.Location_Address_ZipCode,
    e.IsPaidEvent,
    e.BasePrice_Amount AS EventBasePrice,
    e.BasePrice_Currency AS EventCurrency
FROM
    Events e
WHERE
    e.Id = (SELECT EventId FROM Registrations WHERE Id = '219dd972-2309-4898-a972-4e0b6a7224fb');

PRINT ''

-- Step 3: Verify Webhook Event Record
-- ============================================================================
PRINT '=== STEP 3: Verify Webhook Event Record ==='
PRINT ''

SELECT
    EventId,
    EventType,
    Processed,
    ProcessedAt,
    ErrorMessage,
    AttemptCount,
    CreatedAt
FROM
    stripe_webhook_events
WHERE
    EventId = 'evt_1SfSktLvfbr023L1qB78D1CR';

PRINT ''
PRINT '--- Expected Values ---'
PRINT 'Processed: 1 (true)'
PRINT 'ProcessedAt: Should be around 2025-12-16 21:59 UTC (old revision processing time)'
PRINT 'ErrorMessage: Should be NULL (old revision returned HTTP 200)'
PRINT ''

-- Step 4: Check if Ticket Already Exists (Should NOT exist)
-- ============================================================================
PRINT '=== STEP 4: Check if Ticket Already Exists ==='
PRINT ''

SELECT
    t.Id AS TicketId,
    t.RegistrationId,
    t.EventId,
    t.TicketCode,
    t.Status,
    t.QRCodeData,
    t.GeneratedAt,
    t.ExpiresAt
FROM
    Tickets t
WHERE
    t.RegistrationId = '219dd972-2309-4898-a972-4e0b6a7224fb';

IF @@ROWCOUNT = 0
BEGIN
    PRINT ''
    PRINT '✓ CONFIRMED: No ticket exists (as expected)'
    PRINT ''
END
ELSE
BEGIN
    PRINT ''
    PRINT '⚠ WARNING: Ticket already exists! Manual recovery may not be needed.'
    PRINT ''
END

-- Step 5: Get Attendee Details
-- ============================================================================
PRINT '=== STEP 5: Get Attendee Details ==='
PRINT ''

SELECT
    ad.RegistrationId,
    ad.Name,
    ad.Age,
    ad.Id
FROM
    AttendeeDetails ad
WHERE
    ad.RegistrationId = '219dd972-2309-4898-a972-4e0b6a7224fb'
ORDER BY
    ad.Id;

PRINT ''

-- Step 6: Generate PaymentCompletedEvent Parameters
-- ============================================================================
PRINT '=== STEP 6: PaymentCompletedEvent Parameters for Manual Trigger ==='
PRINT ''

SELECT
    r.EventId,
    r.Id AS RegistrationId,
    r.UserId,
    COALESCE(r.Contact_Email, u.Email) AS ContactEmail,
    r.StripePaymentIntentId AS PaymentIntentId,
    r.TotalPrice_Amount AS AmountPaid,
    r.Quantity AS AttendeeCount,
    r.UpdatedAt AS PaymentCompletedAt
FROM
    Registrations r
    LEFT JOIN Users u ON r.UserId = u.Id
WHERE
    r.Id = '219dd972-2309-4898-a972-4e0b6a7224fb';

PRINT ''
PRINT '============================================================================'
PRINT 'NEXT STEPS:'
PRINT '============================================================================'
PRINT ''
PRINT '1. Create a temporary admin endpoint (e.g., POST /api/admin/trigger-payment-event)'
PRINT '   that accepts RegistrationId and manually publishes PaymentCompletedEvent'
PRINT ''
PRINT '2. Use the parameters from Step 6 above to construct the event:'
PRINT '   new PaymentCompletedEvent('
PRINT '       EventId: <from query>,'
PRINT '       RegistrationId: 219dd972-2309-4898-a972-4e0b6a7224fb,'
PRINT '       UserId: <from query>,'
PRINT '       ContactEmail: <from query>,'
PRINT '       PaymentIntentId: <from query>,'
PRINT '       AmountPaid: <from query>,'
PRINT '       AttendeeCount: <from query>,'
PRINT '       PaymentCompletedAt: <from query>'
PRINT '   )'
PRINT ''
PRINT '3. Publish the event via IPublisher (MediatR):'
PRINT '   await _publisher.Publish(new DomainEventNotification<PaymentCompletedEvent>(event));'
PRINT ''
PRINT '4. Verify email sent and ticket generated in logs'
PRINT ''
PRINT '5. DELETE the temporary endpoint after recovery'
PRINT ''
PRINT '============================================================================'

GO
