-- Phase 6A.47: Drop check constraint that's blocking seed data
-- This constraint only allowed the original 3 enum types, but we need 41 types

ALTER TABLE reference_data.reference_values
DROP CONSTRAINT IF EXISTS ck_reference_values_enum_type;

-- Verify constraint is dropped
SELECT COUNT(*) as constraints_remaining
FROM information_schema.constraint_column_usage
WHERE table_schema = 'reference_data'
  AND table_name = 'reference_values'
  AND constraint_name = 'ck_reference_values_enum_type';
