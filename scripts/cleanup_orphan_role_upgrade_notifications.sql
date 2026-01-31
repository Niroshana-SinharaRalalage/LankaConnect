-- Phase 6A.X Issue #46: Data Cleanup Script for Orphan RoleUpgradeApproved Notifications
--
-- This script identifies and cleans up notifications where:
-- 1. A RoleUpgradeApproved notification exists
-- 2. But the user's current role is NOT EventOrganizer (meaning the upgrade didn't actually happen)
--
-- Run this script to identify orphan notifications before cleanup:
-- Step 1: Run the SELECT query to identify affected records
-- Step 2: Review the results
-- Step 3: Run the UPDATE/DELETE if cleanup is needed

-- ============================================================
-- STEP 1: IDENTIFY ORPHAN NOTIFICATIONS
-- ============================================================
-- Find all RoleUpgradeApproved notifications where the user is NOT an EventOrganizer
-- This indicates a potential data inconsistency (notification exists but role wasn't upgraded)

SELECT
    n."Id" AS "NotificationId",
    n."UserId",
    u."Email",
    u."FirstName",
    u."LastName",
    u."Role" AS "CurrentRole",
    n."Title",
    n."Message",
    n."IsRead",
    n."CreatedAt" AS "NotificationCreatedAt",
    n."ReadAt",
    u."PendingUpgradeRole",
    u."UpgradeRequestedAt"
FROM notifications.notifications n
INNER JOIN identity.users u ON n."UserId" = u."Id"
WHERE n."Type" = 1  -- RoleUpgradeApproved enum value
  AND u."Role" != 2  -- Not EventOrganizer (assuming enum value 2)
ORDER BY n."CreatedAt" DESC;

-- ============================================================
-- STEP 2: OPTION A - SOFT CLEANUP (Mark as Read)
-- ============================================================
-- Use this if you want to keep the notifications but hide them from the user
-- Uncomment to execute:

-- UPDATE notifications.notifications n
-- SET "IsRead" = true,
--     "ReadAt" = NOW(),
--     "UpdatedAt" = NOW()
-- FROM identity.users u
-- WHERE n."UserId" = u."Id"
--   AND n."Type" = 1  -- RoleUpgradeApproved
--   AND u."Role" != 2  -- Not EventOrganizer
--   AND n."IsRead" = false;

-- ============================================================
-- STEP 3: OPTION B - HARD CLEANUP (Delete Notifications)
-- ============================================================
-- Use this if you want to completely remove orphan notifications
-- Uncomment to execute:

-- DELETE FROM notifications.notifications n
-- USING identity.users u
-- WHERE n."UserId" = u."Id"
--   AND n."Type" = 1  -- RoleUpgradeApproved
--   AND u."Role" != 2;  -- Not EventOrganizer

-- ============================================================
-- ADDITIONAL: Find specific user's notifications (for investigation)
-- ============================================================
-- Replace '<user-email>' with the actual email address:

-- SELECT
--     n."Id",
--     n."UserId",
--     n."Title",
--     n."Message",
--     n."Type",
--     n."IsRead",
--     n."CreatedAt",
--     n."ReadAt"
-- FROM notifications.notifications n
-- INNER JOIN identity.users u ON n."UserId" = u."Id"
-- WHERE u."Email" = '<user-email>'
-- ORDER BY n."CreatedAt" DESC;

-- ============================================================
-- AUDIT: Check admin audit logs for role upgrades
-- ============================================================
-- This shows who approved role upgrades (available after Phase 6A.X Issue #46 fix)

-- SELECT
--     al."Id",
--     al."AdminUserId",
--     admin."Email" AS "AdminEmail",
--     al."Action",
--     al."TargetUserId",
--     target."Email" AS "TargetEmail",
--     al."Details",
--     al."CreatedAt"
-- FROM support.admin_audit_logs al
-- LEFT JOIN identity.users admin ON al."AdminUserId" = admin."Id"
-- LEFT JOIN identity.users target ON al."TargetUserId" = target."Id"
-- WHERE al."Action" = 'ROLE_UPGRADE_APPROVED'
-- ORDER BY al."CreatedAt" DESC;
