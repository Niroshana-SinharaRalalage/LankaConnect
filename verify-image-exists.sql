-- Check if image exists for that event
SELECT ei.Id as ImageId, ei.ImageUrl, ei.IsPrimary, e.Id as EventId
FROM EventImages ei
JOIN Events e ON ei.EventId = e.Id
WHERE e.Id = '0458806b-8672-4ad5-a7cb-f5346f1b282a'
  AND ei.Id = '3f55cb35-2bb9-4748-8e0c-f843fc5d5723';

-- Also show all images for that event
SELECT ei.Id as ImageId, ei.ImageUrl, ei.IsPrimary
FROM EventImages ei
WHERE ei.EventId = '0458806b-8672-4ad5-a7cb-f5346f1b282a';
