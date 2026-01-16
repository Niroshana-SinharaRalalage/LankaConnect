-- Check if migration was applied
SELECT "MigrationId", "ProductVersion" 
FROM "__EFMigrationsHistory" 
WHERE "MigrationId" LIKE '%Phase6A61_AddEventDetailsTemplate%'
ORDER BY "MigrationId";
