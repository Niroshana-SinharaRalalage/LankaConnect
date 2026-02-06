-- Phase 6A.X: Temporary fix for testing resend confirmation feature
--
-- PROBLEM: Existing test registrations have Status=Confirmed but PaymentStatus=Pending
--          This is historical data from before Phase 6A.81 fixes were deployed
--
-- SOLUTION: Manually update PaymentStatus to Completed for testing
--           This allows testing the resend confirmation feature
--
-- NOTE: This is ONLY for testing. New registrations will have correct state
--       via the fixed webhook handler (PaymentsController.HandleCheckoutSessionCompletedAsync)

-- Check current state before update
SELECT
    r.id AS registration_id,
    r.event_id,
    r.status AS registration_status,
    r.payment_status,
    r.stripe_payment_intent_id,
    r.created_at,
    e.title AS event_title
FROM events.registrations r
JOIN events.events e ON r.event_id = e.id
WHERE r.event_id = 'd543629f-a5ba-4475-b124-3d0fc5200f2f'  -- Christmas Dinner Dance 2025
  AND r.status = 1  -- RegistrationStatus.Confirmed
  AND r.payment_status = 0  -- PaymentStatus.Pending (incorrect state)
ORDER BY r.created_at DESC;

-- Expected result: 8 registrations with Status=Confirmed + PaymentStatus=Pending

-- ========================================
-- UPDATE: Fix PaymentStatus for testing
-- ========================================

-- Update ONE registration for initial testing
UPDATE events.registrations
SET
    payment_status = 2,  -- PaymentStatus.Completed
    updated_at = CURRENT_TIMESTAMP
WHERE id = '18422a29-61f7-4575-87d2-72ac0b1581d1'  -- First test registration
  AND status = 1  -- RegistrationStatus.Confirmed
  AND payment_status = 0  -- PaymentStatus.Pending
  AND event_id = 'd543629f-a5ba-4475-b124-3d0fc5200f2f';

-- Verify update
SELECT
    id AS registration_id,
    status AS registration_status,
    payment_status,
    stripe_payment_intent_id,
    updated_at
FROM events.registrations
WHERE id = '18422a29-61f7-4575-87d2-72ac0b1581d1';

-- Expected result: PaymentStatus should now be 2 (Completed)

-- ========================================
-- OPTIONAL: Update ALL test registrations
-- ========================================
-- Uncomment to fix all 8 registrations at once

/*
UPDATE events.registrations
SET
    payment_status = 2,  -- PaymentStatus.Completed
    updated_at = CURRENT_TIMESTAMP
WHERE event_id = 'd543629f-a5ba-4475-b124-3d0fc5200f2f'  -- Christmas Dinner Dance 2025
  AND status = 1  -- RegistrationStatus.Confirmed
  AND payment_status = 0;  -- PaymentStatus.Pending

-- Verify all updates
SELECT
    id AS registration_id,
    status AS registration_status,
    payment_status,
    stripe_payment_intent_id,
    created_at
FROM events.registrations
WHERE event_id = 'd543629f-a5ba-4475-b124-3d0fc5200f2f'
ORDER BY created_at DESC;
*/

-- ========================================
-- ENUM REFERENCE
-- ========================================
-- RegistrationStatus:
--   0 = Pending (deprecated)
--   1 = Confirmed
--   2 = Waitlisted
--   3 = Cancelled
--   4 = CheckedIn
--   5 = Attended (formerly Completed)
--   6 = Refunded
--   7 = Preliminary (Phase 6A.81)
--   8 = Abandoned (Phase 6A.81)
--
-- PaymentStatus:
--   0 = Pending
--   1 = Failed
--   2 = Completed
--   3 = Refunded
--   4 = NotRequired (free events)
