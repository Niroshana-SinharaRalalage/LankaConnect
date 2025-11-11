@echo off
REM Apply EF Core migrations to Azure Staging PostgreSQL database

echo ========================================
echo Azure Staging Database Migration
echo ========================================
echo.

echo [1/4] Checking Azure login...
az account show >nul 2>&1
if errorlevel 1 (
    echo ERROR: Not logged in to Azure
    echo Please run: az login
    exit /b 1
)
echo OK: Logged in to Azure
echo.

echo [2/4] Retrieving database connection string from Key Vault...
for /f "delims=" %%i in ('az keyvault secret show --vault-name lankaconnect-staging-kv --name DATABASE-CONNECTION-STRING --query value -o tsv 2^>^&1') do set CONN_STRING=%%i

if "%CONN_STRING%"=="" (
    echo ERROR: Failed to retrieve connection string
    exit /b 1
)

echo OK: Connection string retrieved
echo.

echo [3/4] Applying EF Core migrations...
cd /d "%~dp0..\..\src\LankaConnect.API"

set ConnectionStrings__DefaultConnection=%CONN_STRING%

dotnet ef database update ^
    --project ..\LankaConnect.Infrastructure\LankaConnect.Infrastructure.csproj ^
    --startup-project LankaConnect.API.csproj ^
    --context AppDbContext ^
    --verbose

if errorlevel 1 (
    echo ERROR: Migration failed
    exit /b 1
)

echo.
echo OK: Migrations applied successfully
echo.

echo [4/4] Summary
echo ========================================
echo Migration Complete!
echo.
echo The database schema has been updated.
echo The seeder will run automatically when you deploy to Azure.
echo.
echo Next Steps:
echo   1. Commit and push your changes
echo   2. GitHub Actions will deploy to Azure staging
echo   3. Seeder will run on first startup
echo.
