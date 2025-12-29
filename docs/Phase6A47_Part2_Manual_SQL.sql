-- Phase 6A.47 Part 2: Manual Database Cleanup
-- Remove EventStatus and UserRole from reference_values table
-- These are code enums that should NOT be in reference data

BEGIN TRANSACTION;

-- Backup before deletion (audit)
SELECT 'EventStatus records before deletion:' as action, COUNT(*) as count
FROM reference_data.reference_values
WHERE enum_type = 'EventStatus';

SELECT 'UserRole records before deletion:' as action, COUNT(*) as count
FROM reference_data.reference_values
WHERE enum_type = 'UserRole';

-- Delete EventStatus records (8 total)
DELETE FROM reference_data.reference_values
WHERE enum_type = 'EventStatus';

-- Delete UserRole records (6 total)
DELETE FROM reference_data.reference_values
WHERE enum_type = 'UserRole';

-- Verify deletion
SELECT 'EventStatus records after deletion:' as action, COUNT(*) as count
FROM reference_data.reference_values
WHERE enum_type = 'EventStatus';

SELECT 'UserRole records after deletion:' as action, COUNT(*) as count
FROM reference_data.reference_values
WHERE enum_type = 'UserRole';

-- Verify only EventCategory remains in seed data
SELECT 'Remaining enum types:' as action, enum_type, COUNT(*) as count
FROM reference_data.reference_values
GROUP BY enum_type
ORDER BY enum_type;

COMMIT;
