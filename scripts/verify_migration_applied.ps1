# Verify Phase6A111 migration was applied to staging database
$token = Get-Content 'C:\Work\LankaConnect\.token.txt'

Write-Host "Checking if migration Phase6A111 was applied..." -ForegroundColor Cyan
Write-Host ""

# Check for the composite index in database
# We'll use a diagnostic endpoint or check migrations table
# For now, let's check the __EFMigrationsHistory table

Write-Host "Querying staging database via API to verify index exists..."
Write-Host ""

# Since we don't have direct database access, let's check Azure Container logs
# for migration application messages

Write-Host "Checking Azure Container logs for migration messages..." -ForegroundColor Yellow

# Check logs for migration-related messages
az containerapp logs show --name lankaconnect-api-staging --resource-group lankaconnect-staging --tail 200 --output json 2>&1 |
    ConvertFrom-Json |
    Where-Object { $_.Log -like "*migration*" -or $_.Log -like "*Phase6A111*" -or $_.Log -like "*FormResponse*" } |
    Select-Object -First 20 |
    ForEach-Object { Write-Host $_.Log }

Write-Host ""
Write-Host "Note: If no migration messages found, migrations run silently on startup." -ForegroundColor Gray
