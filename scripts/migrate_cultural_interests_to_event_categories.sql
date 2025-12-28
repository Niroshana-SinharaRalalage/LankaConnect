-- Phase 6A.47: Migrate Cultural Interests to EventCategory codes
-- Maps old hardcoded cultural interest codes to EventCategory codes from reference_values

-- Mapping strategy:
-- Old cultural interest codes → EventCategory codes
-- Most cultural interests map to "Cultural" category
-- Some specific ones map to other categories

BEGIN;

-- Step 1: Create mapping table for reference
CREATE TEMP TABLE cultural_interest_mapping (
    old_code VARCHAR(50),
    new_code VARCHAR(50),
    category_name VARCHAR(100)
);

INSERT INTO cultural_interest_mapping (old_code, new_code, category_name) VALUES
-- Religious cultural interests → Religious category
('BUDDHIST_FEST', 'Religious', 'Buddhist Festivals & Traditions'),
('HINDU_FEST', 'Religious', 'Hindu Festivals & Traditions'),
('ISLAMIC_FEST', 'Religious', 'Islamic Festivals & Traditions'),
('CHRISTIAN_FEST', 'Religious', 'Christian Festivals & Traditions'),
('VESAK', 'Religious', 'Vesak & Poson Celebrations'),
('TEMPLE_ARCH', 'Religious', 'Temple Architecture & Heritage Sites'),

-- Cultural traditions → Cultural category
('SL_CUISINE', 'Cultural', 'Sri Lankan Cuisine'),
('TRAD_DANCE', 'Cultural', 'Traditional Dance'),
('SINHALA_MUSIC', 'Cultural', 'Sinhala Music & Arts'),
('TAMIL_MUSIC', 'Cultural', 'Tamil Music & Arts'),
('SINHALA_NY', 'Cultural', 'Sinhala & Tamil New Year'),
('TEA_CULTURE', 'Cultural', 'Ceylon Tea Culture'),
('TRAD_ARTS', 'Cultural', 'Traditional Arts & Crafts'),
('SL_WEDDINGS', 'Cultural', 'Sri Lankan Wedding Traditions'),
('SL_LITERATURE', 'Cultural', 'Sinhala/Tamil Literature & Poetry'),
('TRAD_GAMES', 'Cultural', 'Traditional Games'),
('SL_FASHION', 'Cultural', 'Traditional Dress & Fashion'),

-- Educational/wellness → Educational category
('AYURVEDA', 'Educational', 'Ayurvedic Medicine & Wellness'),

-- Entertainment → Entertainment category
('CRICKET', 'Entertainment', 'Cricket & Sports'),

-- Community/networking → Community category
('DIASPORA_NET', 'Community', 'Diaspora Community & Networking');

-- Step 2: Update user_cultural_interests table
-- Replace old codes with new EventCategory codes
UPDATE users.user_cultural_interests uci
SET interest_code = cim.new_code
FROM cultural_interest_mapping cim
WHERE uci.interest_code = cim.old_code;

-- Step 3: Verify migration
-- Show count of users affected per category
SELECT
    cim.new_code AS event_category,
    COUNT(DISTINCT uci."UserId") AS user_count,
    COUNT(*) AS total_selections
FROM users.user_cultural_interests uci
JOIN cultural_interest_mapping cim ON uci.interest_code = cim.new_code
GROUP BY cim.new_code
ORDER BY user_count DESC;

-- Step 4: Check for unmapped codes (should be empty)
SELECT DISTINCT interest_code
FROM users.user_cultural_interests
WHERE interest_code NOT IN (SELECT code FROM reference_data.reference_values WHERE enum_type = 'EventCategory')
  AND interest_code NOT IN (SELECT old_code FROM cultural_interest_mapping);

COMMIT;

-- Summary:
-- This migration preserves user data by mapping old cultural interest codes
-- to EventCategory codes. Users will now see EventCategory options instead
-- of the 20 hardcoded cultural interests.
