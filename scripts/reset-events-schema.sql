-- Reset Events Schema for Staging Database
-- This script drops and recreates the events schema to fix broken migration state
-- Run this against the staging database to resolve migration errors

-- Drop events schema and all objects in it
DROP SCHEMA IF EXISTS events CASCADE;

-- Delete migration history entries for Events-related migrations
DELETE FROM "__EFMigrationsHistory"
WHERE "MigrationId" LIKE '%Event%'
   OR "MigrationId" LIKE '%Registration%';

-- Note: After running this script, redeploy the application
-- The migrations will run from scratch and create the schema correctly
