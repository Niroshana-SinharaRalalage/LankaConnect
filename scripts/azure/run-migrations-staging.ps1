# Run EF Core migrations on Azure staging database
# This script applies pending migrations to the staging database

$ErrorActionPreference = "Stop"

Write-Host "================================================================"
Write-Host "Running EF Core Migrations on Azure Staging Database"
Write-Host "================================================================"

# Configuration
$KEY_VAULT_NAME = "lankaconnect-staging-kv"
$CONNECTION_STRING_SECRET = "DATABASE-CONNECTION-STRING"

# Step 1: Get connection string from Azure Key Vault
Write-Host "`n[1/3] Retrieving connection string from Azure Key Vault..."
$connectionString = az keyvault secret show `
    --vault-name $KEY_VAULT_NAME `
    --name $CONNECTION_STRING_SECRET `
    --query value `
    -o tsv

if ($LASTEXITCODE -ne 0) {
    Write-Error "Failed to retrieve connection string from Key Vault"
    exit 1
}

Write-Host "[OK] Connection string retrieved successfully"

# Step 2: Navigate to Infrastructure project directory
Write-Host "`n[2/3] Navigating to Infrastructure project..."
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$infraDir = Join-Path $scriptDir "..\..\src\LankaConnect.Infrastructure"
Push-Location $infraDir

Write-Host "[OK] Current directory: $infraDir"

# Step 3: Run EF Core migrations
Write-Host "`n[3/3] Applying EF Core migrations to staging database..."
$env:ConnectionStrings__DefaultConnection = $connectionString

try {
    dotnet ef database update `
        --project LankaConnect.Infrastructure.csproj `
        --startup-project ..\LankaConnect.API\LankaConnect.API.csproj `
        --context AppDbContext `
        --verbose

    if ($LASTEXITCODE -ne 0) {
        Write-Error "Migration failed with exit code $LASTEXITCODE"
        Pop-Location
        exit 1
    }

    Write-Host "`n================================================================"
    Write-Host "[SUCCESS] Migrations applied successfully to staging database!"
    Write-Host "================================================================"
} finally {
    Pop-Location
    Remove-Item Env:ConnectionStrings__DefaultConnection -ErrorAction SilentlyContinue
}
