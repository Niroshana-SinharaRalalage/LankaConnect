# PowerShell script to run diagnostic query against Azure PostgreSQL
# Requires: Azure CLI or PostgreSQL client

$serverName = "lankaconnect-staging-db.postgres.database.azure.com"
$database = "LankaConnectDB"
$username = "adminuser"
$password = "1qaz!QAZ"

$query = Get-Content ".\scripts\diagnose-rice-tay.sql" -Raw

# Try using psql if available
if (Get-Command psql -ErrorAction SilentlyContinue) {
    $env:PGPASSWORD = $password
    psql -h $serverName -U $username -d $database -c $query
    Remove-Item Env:\PGPASSWORD
} else {
    Write-Host "ERROR: psql not found. Please install PostgreSQL client tools or use Azure Data Studio." -ForegroundColor Red
    Write-Host ""
    Write-Host "Manual Steps:" -ForegroundColor Yellow
    Write-Host "1. Open Azure Data Studio or pgAdmin"
    Write-Host "2. Connect to: $serverName"
    Write-Host "3. Database: $database"
    Write-Host "4. Username: $username"
    Write-Host "5. Run the query from: .\scripts\diagnose-rice-tay.sql"
}
