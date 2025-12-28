-- Script to clear all Event Interests (Cultural Interests) for all users
-- Run this to remove legacy hardcoded interests before users select new EventCategory-based interests
-- Phase 6A: Event Interests Unlimited + Database-driven

-- Clear all user cultural interests
DELETE FROM identity.user_cultural_interests;

-- Verify the data has been cleared
SELECT COUNT(*) as remaining_interests FROM identity.user_cultural_interests;

-- Expected result: 0 rows
