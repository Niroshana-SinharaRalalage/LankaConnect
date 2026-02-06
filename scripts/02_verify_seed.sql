-- Phase 6A.47: Verification queries for seed data

-- Total row count (should be 402)
SELECT COUNT(*) as total_rows FROM reference_data.reference_values;

-- Distinct enum types (should be 41)
SELECT COUNT(DISTINCT enum_type) as distinct_enum_types FROM reference_data.reference_values;

-- Breakdown by enum type
SELECT enum_type, COUNT(*) as count
FROM reference_data.reference_values
GROUP BY enum_type
ORDER BY enum_type;

-- Check for duplicates (should return 0 rows)
SELECT enum_type, code, COUNT(*)
FROM reference_data.reference_values
GROUP BY enum_type, code
HAVING COUNT(*) > 1;
