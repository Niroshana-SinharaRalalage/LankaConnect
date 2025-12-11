-- Reset Events Schema for Staging Database
-- This script drops and recreates the events schema to fix broken migration state
-- Run this against the staging database to resolve migration errors
-- DESTRUCTIVE - NO UNDO - Only run on staging/dev environments

-- Drop events schema and all objects in it
DROP SCHEMA IF EXISTS events CASCADE;

-- Delete migration history entries for Events-related migrations
-- Specifically delete the migrations that were failing
DELETE FROM "__EFMigrationsHistory"
WHERE "MigrationId" IN (
    '20251102061243_AddEventLocationWithPostGIS',
    '20251102144315_AddEventCategoryAndTicketPrice',
    '20251103040053_AddEventImages'
);

-- Note: After running this script, restart the container app
-- The application will detect missing migrations and apply them cleanly
-- Expected result: Events table created with all columns including Status
