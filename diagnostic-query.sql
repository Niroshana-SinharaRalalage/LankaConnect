-- Diagnostic query to check SetPrimaryImage constraint issues
-- Run this on the staging database

-- 1. Check if the unique index exists
SELECT * FROM information_schema.statistics 
WHERE table_schema = 'events' 
  AND table_name = 'EventImages' 
  AND index_name = 'IX_EventImages_EventId_IsPrimary_True';

-- 2. Check for the specific event: how many images have IsPrimary = true?
SELECT 
    EventId,
    COUNT(*) as PrimaryCount,
    GROUP_CONCAT(Id) as ImageIds
FROM events.EventImages
WHERE EventId = '0458806b-8672-4ad5-a7cb-f5346f1b282a'
  AND IsPrimary = true
GROUP BY EventId;

-- 3. Show all images for this event
SELECT Id, EventId, DisplayOrder, IsPrimary, ImageUrl
FROM events.EventImages
WHERE EventId = '0458806b-8672-4ad5-a7cb-f5346f1b282a'
ORDER BY DisplayOrder;
