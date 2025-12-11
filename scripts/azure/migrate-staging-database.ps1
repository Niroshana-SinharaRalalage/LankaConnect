#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Apply EF Core migrations and seed data to Azure Staging PostgreSQL database

.DESCRIPTION
    This script:
    1. Retrieves the database connection string from Azure Key Vault
    2. Applies EF Core migrations to the staging database
    3. Seeds the database with initial data (25 events)

.PARAMETER ResourceGroup
    Azure Resource Group name (default: lankaconnect-staging)

.PARAMETER KeyVaultName
    Azure Key Vault name (default: lankaconnect-staging-kv)

.PARAMETER SkipSeeding
    Skip database seeding (only apply migrations)

.EXAMPLE
    .\migrate-staging-database.ps1

.EXAMPLE
    .\migrate-staging-database.ps1 -SkipSeeding

.EXAMPLE
    .\migrate-staging-database.ps1 -ResourceGroup my-rg -KeyVaultName my-kv
#>

param(
    [string]$ResourceGroup = "lankaconnect-staging",
    [string]$KeyVaultName = "lankaconnect-staging-kv",
    [switch]$SkipSeeding
)

$ErrorActionPreference = "Stop"

Write-Host "════════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "  Azure Staging Database Migration & Seeding" -ForegroundColor Cyan
Write-Host "════════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""

# Check if Azure CLI is installed
Write-Host "✓ Checking Azure CLI..." -ForegroundColor Yellow
$azVersion = az --version 2>$null
if (-not $azVersion) {
    Write-Host "❌ Azure CLI not found. Please install from: https://aka.ms/InstallAzureCLIDirect" -ForegroundColor Red
    exit 1
}
Write-Host "✓ Azure CLI installed" -ForegroundColor Green

# Check if logged in to Azure
Write-Host "✓ Checking Azure login..." -ForegroundColor Yellow
$account = az account show 2>$null | ConvertFrom-Json
if (-not $account) {
    Write-Host "❌ Not logged in to Azure. Please run: az login" -ForegroundColor Red
    exit 1
}
Write-Host "✓ Logged in as: $($account.user.name)" -ForegroundColor Green
Write-Host "  Subscription: $($account.name)" -ForegroundColor Gray

# Check if EF Core tools are installed
Write-Host "✓ Checking EF Core tools..." -ForegroundColor Yellow
$efVersion = dotnet ef --version 2>$null
if (-not $efVersion) {
    Write-Host "⚠ EF Core tools not found. Installing..." -ForegroundColor Yellow
    dotnet tool install --global dotnet-ef
    $efVersion = dotnet ef --version
}
Write-Host "✓ EF Core tools: $efVersion" -ForegroundColor Green

Write-Host ""
Write-Host "════════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "  Step 1: Retrieve Database Connection String" -ForegroundColor Cyan
Write-Host "════════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""

Write-Host "Retrieving connection string from Key Vault: $KeyVaultName" -ForegroundColor Yellow

try {
    $connectionString = az keyvault secret show `
        --vault-name $KeyVaultName `
        --name DATABASE-CONNECTION-STRING `
        --query value `
        -o tsv 2>&1

    if ($LASTEXITCODE -ne 0) {
        Write-Host "❌ Failed to retrieve connection string from Key Vault" -ForegroundColor Red
        Write-Host "Error: $connectionString" -ForegroundColor Red
        Write-Host ""
        Write-Host "Possible issues:" -ForegroundColor Yellow
        Write-Host "  1. Key Vault does not exist: $KeyVaultName" -ForegroundColor Gray
        Write-Host "  2. Secret 'DATABASE-CONNECTION-STRING' does not exist" -ForegroundColor Gray
        Write-Host "  3. You don't have permissions to access the Key Vault" -ForegroundColor Gray
        Write-Host ""
        Write-Host "To grant yourself access:" -ForegroundColor Yellow
        Write-Host "  az keyvault set-policy --name $KeyVaultName --upn <your-email> --secret-permissions get list" -ForegroundColor Gray
        exit 1
    }

    # Validate connection string is not empty
    if ([string]::IsNullOrWhiteSpace($connectionString)) {
        Write-Host "❌ Connection string is empty" -ForegroundColor Red
        exit 1
    }

    # Mask password in connection string for display
    $maskedConnectionString = $connectionString -replace "(Password=)[^;]+", '$1***'
    Write-Host "✓ Connection string retrieved" -ForegroundColor Green
    Write-Host "  $maskedConnectionString" -ForegroundColor Gray

} catch {
    Write-Host "❌ Failed to retrieve connection string: $_" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "════════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "  Step 2: Apply Database Migrations" -ForegroundColor Cyan
Write-Host "════════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""

# Navigate to API project directory
$apiProjectPath = Join-Path $PSScriptRoot "..\..\src\LankaConnect.API"
if (-not (Test-Path $apiProjectPath)) {
    Write-Host "❌ API project not found at: $apiProjectPath" -ForegroundColor Red
    exit 1
}

Push-Location $apiProjectPath

try {
    Write-Host "Applying EF Core migrations to Azure staging database..." -ForegroundColor Yellow
    Write-Host "  Project: LankaConnect.API" -ForegroundColor Gray
    Write-Host "  Startup Project: LankaConnect.API" -ForegroundColor Gray
    Write-Host "  Context: AppDbContext (Infrastructure)" -ForegroundColor Gray
    Write-Host ""

    # Set connection string as environment variable
    $env:ConnectionStrings__DefaultConnection = $connectionString

    # Apply migrations
    $migrateOutput = dotnet ef database update `
        --project ..\LankaConnect.Infrastructure\LankaConnect.Infrastructure.csproj `
        --startup-project LankaConnect.API.csproj `
        --context AppDbContext `
        --verbose 2>&1

    if ($LASTEXITCODE -ne 0) {
        Write-Host "❌ Migration failed" -ForegroundColor Red
        Write-Host $migrateOutput -ForegroundColor Red
        exit 1
    }

    Write-Host "✓ Migrations applied successfully" -ForegroundColor Green
    Write-Host ""

} catch {
    Write-Host "❌ Migration error: $_" -ForegroundColor Red
    exit 1
} finally {
    Pop-Location
}

if ($SkipSeeding) {
    Write-Host ""
    Write-Host "════════════════════════════════════════════════════════════════" -ForegroundColor Cyan
    Write-Host "  Migration Complete (Seeding Skipped)" -ForegroundColor Cyan
    Write-Host "════════════════════════════════════════════════════════════════" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "✅ Database schema updated successfully" -ForegroundColor Green
    Write-Host ""
    Write-Host "Seeding was skipped. To seed data manually:" -ForegroundColor Yellow
    Write-Host "  1. Deploy the API to Azure Container Apps" -ForegroundColor Gray
    Write-Host "  2. The seeder will run automatically on application startup" -ForegroundColor Gray
    exit 0
}

Write-Host "════════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "  Step 3: Seed Database with Initial Data" -ForegroundColor Cyan
Write-Host "════════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""

Write-Host "Creating temporary seeder application..." -ForegroundColor Yellow

# Create temporary directory for seeder
$tempDir = Join-Path $env:TEMP "LankaConnect-Seeder-$(Get-Date -Format 'yyyyMMddHHmmss')"
New-Item -ItemType Directory -Path $tempDir -Force | Out-Null

$seederScript = @'
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using LankaConnect.Infrastructure.Data;
using LankaConnect.Infrastructure.Data.Seeders;

var configuration = new ConfigurationBuilder()
    .AddEnvironmentVariables()
    .Build();

var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection");
if (string.IsNullOrEmpty(connectionString))
{
    Console.WriteLine("❌ Connection string not found in environment variables");
    return 1;
}

var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
optionsBuilder.UseNpgsql(connectionString, o => o.UseNetTopologySuite());

var loggerFactory = LoggerFactory.Create(builder =>
{
    builder.AddConsole();
    builder.SetMinimumLevel(LogLevel.Information);
});

using var context = new AppDbContext(optionsBuilder.Options);
var logger = loggerFactory.CreateLogger<Program>();

try
{
    Console.WriteLine("Seeding database with 25 events...");
    await DbInitializer.SeedAsync(context, logger);
    Console.WriteLine("✓ Database seeded successfully");
    return 0;
}
catch (Exception ex)
{
    Console.WriteLine($"❌ Seeding failed: {ex.Message}");
    return 1;
}
'@

$seederProjectFile = @'
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\src\LankaConnect.Infrastructure\LankaConnect.Infrastructure.csproj" />
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.EntityFrameworkCore.Design" Version="8.0.10" />
    <PackageReference Include="Microsoft.Extensions.Configuration.EnvironmentVariables" Version="8.0.0" />
  </ItemGroup>
</Project>
'@

# Write seeder files
Set-Content -Path (Join-Path $tempDir "Program.cs") -Value $seederScript
Set-Content -Path (Join-Path $tempDir "Seeder.csproj") -Value $seederProjectFile

try {
    Write-Host "Building seeder application..." -ForegroundColor Yellow
    Push-Location $tempDir

    $buildOutput = dotnet build --configuration Release 2>&1
    if ($LASTEXITCODE -ne 0) {
        Write-Host "❌ Seeder build failed" -ForegroundColor Red
        Write-Host $buildOutput -ForegroundColor Red
        exit 1
    }

    Write-Host "Running seeder..." -ForegroundColor Yellow
    $env:ConnectionStrings__DefaultConnection = $connectionString
    $seedOutput = dotnet run --configuration Release --no-build 2>&1

    if ($LASTEXITCODE -ne 0) {
        Write-Host "❌ Seeding failed" -ForegroundColor Red
        Write-Host $seedOutput -ForegroundColor Red
        exit 1
    }

    Write-Host "✓ Database seeded successfully" -ForegroundColor Green
    Write-Host "  25 events added to staging database" -ForegroundColor Gray

} catch {
    Write-Host "❌ Seeding error: $_" -ForegroundColor Red
    exit 1
} finally {
    Pop-Location
    # Cleanup temp directory
    Remove-Item -Recurse -Force $tempDir -ErrorAction SilentlyContinue
}

Write-Host ""
Write-Host "════════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "  Migration and Seeding Complete!" -ForegroundColor Green
Write-Host "════════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""
Write-Host "Summary:" -ForegroundColor Yellow
Write-Host "  ✓ Database schema updated" -ForegroundColor Green
Write-Host "  ✓ 25 events seeded" -ForegroundColor Green
Write-Host ""
Write-Host "Next Steps:" -ForegroundColor Yellow
Write-Host "  1. Deploy API to Azure Container Apps:" -ForegroundColor Gray
Write-Host "     git add ." -ForegroundColor Gray
Write-Host "     git commit -m 'feat: Add event seeder and database integration'" -ForegroundColor Gray
Write-Host "     git push origin develop" -ForegroundColor Gray
Write-Host ""
Write-Host "  2. GitHub Actions will automatically:" -ForegroundColor Gray
Write-Host "     - Build and test the application" -ForegroundColor Gray
Write-Host "     - Push Docker image to Azure Container Registry" -ForegroundColor Gray
Write-Host "     - Deploy to Azure Container Apps (Staging)" -ForegroundColor Gray
Write-Host ""
Write-Host "  3. Verify deployment:" -ForegroundColor Gray
Write-Host "     - Health: https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/health" -ForegroundColor Gray
Write-Host "     - Events: https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/api/events" -ForegroundColor Gray
Write-Host ""
Write-Host "  4. Update frontend .env.local to use staging API:" -ForegroundColor Gray
Write-Host "     NEXT_PUBLIC_API_URL=https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/api" -ForegroundColor Gray
Write-Host ""
