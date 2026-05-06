-- Phase 8 S8.4 — data-fixup audit + cleanup script (staging-only).
--
-- Purpose: identify and (manually) clean up any in-flight broken rows from
-- the pre-S8 era when the seat-binding pipeline silently dropped seat
-- assignments. Two classes of broken rows the original S8 RCA flagged:
--
--   A) Confirmed AssignedSeating registrations whose AttendeeDetails.SeatId
--      is null (the buyer paid but never got a seat). Pre-S8 ALL paid
--      AssignedSeating registrations had this shape.
--
--   B) seat_reservations rows whose owning registration is in {Cancelled,
--      Abandoned, Refunded} — orphaned by missing release-on-cancel handler.
--      Should not exist post-S8.3.
--
-- Each query below is READ-ONLY; the operator decides whether to act on the
-- results. Refund + comp the affected buyers per architect Q3.

-- =====================================================================
-- AUDIT 1: Confirmed AssignedSeating registrations with null seat ids on
-- attendees. These represent the user-visible bug class S8 was built to fix.
-- =====================================================================
SELECT
    r."Id"               AS registration_id,
    r."EventId"          AS event_id,
    r."Status",
    r."PaymentStatus",
    r."CreatedAt"        AS reg_created,
    e.title              AS event_title,
    jsonb_array_length(r.attendees) AS attendee_count,
    (
        SELECT count(*)
        FROM jsonb_array_elements(r.attendees) AS a
        WHERE (a->>'SeatId') IS NULL
    )                    AS attendees_without_seat
FROM events.registrations r
JOIN events.events e ON e."Id" = r."EventId"
WHERE r."Status" = 'Confirmed'
  AND r."PaymentStatus" = 4              -- 4 = Completed
  AND e.seating_mode = 'AssignedSeating'
  AND (
        SELECT count(*)
        FROM jsonb_array_elements(r.attendees) AS a
        WHERE (a->>'SeatId') IS NULL
  ) > 0
ORDER BY r."CreatedAt" DESC;

-- =====================================================================
-- AUDIT 2: orphaned seat_reservations rows (registration left "owns the
-- seats" lifecycle but row was never deleted).
-- =====================================================================
SELECT
    sr."Id"               AS reservation_id,
    sr.seat_id,
    sr.registration_id,
    sr.event_id,
    r."Status"            AS registration_status,
    r."PaymentStatus"     AS registration_payment_status,
    sr.created_at         AS reservation_created
FROM events.seat_reservations sr
JOIN events.registrations r ON r."Id" = sr.registration_id
WHERE r."Status" IN ('Cancelled', 'Abandoned', 'Refunded')
ORDER BY sr.created_at DESC;

-- =====================================================================
-- AUDIT 3: stale active seat_holds beyond expiry (cleanup background
-- service should keep this list at zero; non-zero count means cleanup is
-- not running or has bug).
-- =====================================================================
SELECT
    "Id"               AS hold_id,
    seat_id,
    user_id,
    session_id,
    held_at,
    expires_at,
    EXTRACT(epoch FROM (now() - expires_at)) / 60 AS minutes_past_expiry
FROM events.seat_holds
WHERE status = 'Active'
  AND expires_at < now() - interval '5 minutes'
ORDER BY expires_at;

-- =====================================================================
-- CLEANUP HINTS — do NOT run blindly. Triage AUDIT results first.
--
-- Class B (orphaned reservations after S8.3 ships):
--   DELETE FROM events.seat_reservations
--    WHERE registration_id IN (
--        SELECT "Id" FROM events.registrations
--         WHERE "Status" IN ('Cancelled', 'Abandoned', 'Refunded')
--    );
--
-- Class A (Confirmed-but-unseated): these need refund + comp at the
-- application layer (Stripe API call + notification email). DO NOT
-- attempt to back-fill SeatId values on attendees — there's no safe
-- way to assign a seat to a buyer who already paid days/weeks ago.
-- The architect's Q3 decision was to issue a refund + comp.
-- =====================================================================
