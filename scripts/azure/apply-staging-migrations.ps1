#!/usr/bin/env pwsh
# Apply EF Core migrations to Azure Staging PostgreSQL database

param(
    [string]$KeyVaultName = "lankaconnect-staging-kv"
)

$ErrorActionPreference = "Stop"

Write-Host "Azure Staging Database Migration" -ForegroundColor Cyan
Write-Host "=================================" -ForegroundColor Cyan
Write-Host ""

# Check Azure CLI
Write-Host "Checking Azure CLI..." -ForegroundColor Yellow
try {
    $azVersion = az --version 2>&1 | Select-Object -First 1
    Write-Host "OK: Azure CLI installed" -ForegroundColor Green
} catch {
    Write-Host "ERROR: Azure CLI not found" -ForegroundColor Red
    exit 1
}

# Check Azure login
Write-Host "Checking Azure login..." -ForegroundColor Yellow
$account = az account show 2>$null | ConvertFrom-Json
if (-not $account) {
    Write-Host "ERROR: Not logged in to Azure" -ForegroundColor Red
    Write-Host "Please run: az login" -ForegroundColor Yellow
    exit 1
}
Write-Host "OK: Logged in as $($account.user.name)" -ForegroundColor Green

# Check EF Core tools
Write-Host "Checking EF Core tools..." -ForegroundColor Yellow
$efVersion = dotnet ef --version 2>$null
if (-not $efVersion) {
    Write-Host "Installing EF Core tools..." -ForegroundColor Yellow
    dotnet tool install --global dotnet-ef
}
Write-Host "OK: EF Core tools ready" -ForegroundColor Green

Write-Host ""
Write-Host "Retrieving Database Connection String..." -ForegroundColor Yellow

try {
    $connectionString = az keyvault secret show `
        --vault-name $KeyVaultName `
        --name DATABASE-CONNECTION-STRING `
        --query value `
        -o tsv 2>&1

    if ($LASTEXITCODE -ne 0) {
        Write-Host "ERROR: Failed to retrieve connection string" -ForegroundColor Red
        Write-Host $connectionString -ForegroundColor Red
        exit 1
    }

    if ([string]::IsNullOrWhiteSpace($connectionString)) {
        Write-Host "ERROR: Connection string is empty" -ForegroundColor Red
        exit 1
    }

    $maskedConnectionString = $connectionString -replace "(Password=)[^;]+", '$1***'
    Write-Host "OK: Connection string retrieved" -ForegroundColor Green
    Write-Host "   $maskedConnectionString" -ForegroundColor Gray

} catch {
    Write-Host "ERROR: $_" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "Applying Migrations..." -ForegroundColor Yellow

$apiProjectPath = Join-Path $PSScriptRoot "..\..\src\LankaConnect.API"
if (-not (Test-Path $apiProjectPath)) {
    Write-Host "ERROR: API project not found" -ForegroundColor Red
    exit 1
}

Push-Location $apiProjectPath

try {
    $env:ConnectionStrings__DefaultConnection = $connectionString

    $migrateOutput = dotnet ef database update `
        --project ..\LankaConnect.Infrastructure\LankaConnect.Infrastructure.csproj `
        --startup-project LankaConnect.API.csproj `
        --context AppDbContext `
        --verbose 2>&1

    if ($LASTEXITCODE -ne 0) {
        Write-Host "ERROR: Migration failed" -ForegroundColor Red
        Write-Host $migrateOutput -ForegroundColor Red
        exit 1
    }

    Write-Host "OK: Migrations applied successfully" -ForegroundColor Green

} finally {
    Pop-Location
}

Write-Host ""
Write-Host "Migration Complete!" -ForegroundColor Green
Write-Host ""
Write-Host "Next: Deploy API to seed the database" -ForegroundColor Yellow
Write-Host "   git push origin develop" -ForegroundColor Gray
Write-Host ""
