# LankaConnect Environment Verification Script
# This script verifies the complete development environment setup

param(
    [switch]$SkipDocker,
    [switch]$Verbose,
    [string]$Configuration = "Debug"
)

$ErrorActionPreference = "Stop"
$VerbosePreference = if ($Verbose) { "Continue" } else { "SilentlyContinue" }

Write-Host "🚀 Starting LankaConnect Environment Verification" -ForegroundColor Green
Write-Host "Configuration: $Configuration" -ForegroundColor Cyan
Write-Host "===============================================" -ForegroundColor Green

$testResults = @{
    DotNetBuild = $false
    UnitTests = $false
    IntegrationTests = $false
    DockerServices = $false
    DatabaseConnection = $false
    LoggingConfiguration = $false
}

# Function to log test results
function Write-TestResult {
    param($TestName, $Success, $Message = "")
    
    $status = if ($Success) { "✅ PASS" } else { "❌ FAIL" }
    $color = if ($Success) { "Green" } else { "Red" }
    
    Write-Host "$status $TestName" -ForegroundColor $color
    if ($Message -and -not $Success) {
        Write-Host "   └─ $Message" -ForegroundColor Yellow
    }
}

# Test 1: .NET Build
Write-Host "📦 Testing .NET Build..." -ForegroundColor Cyan
try {
    $buildOutput = dotnet build --configuration $Configuration --verbosity minimal 2>&1
    if ($LASTEXITCODE -eq 0) {
        $testResults.DotNetBuild = $true
        Write-TestResult "DotNet Build" $true
    } else {
        Write-TestResult "DotNet Build" $false $buildOutput[-1]
    }
} catch {
    Write-TestResult "DotNet Build" $false $_.Exception.Message
}

# Test 2: Unit Tests
Write-Host "🧪 Running Unit Tests..." -ForegroundColor Cyan
try {
    $testOutput = dotnet test --configuration $Configuration --no-build --verbosity minimal --filter "Category!=Integration" 2>&1
    if ($LASTEXITCODE -eq 0) {
        $testResults.UnitTests = $true
        Write-TestResult "Unit Tests" $true
        
        # Extract test counts
        $testLine = $testOutput | Where-Object { $_ -match "Passed!" }
        if ($testLine) {
            Write-Host "   └─ $testLine" -ForegroundColor Green
        }
    } else {
        Write-TestResult "Unit Tests" $false ($testOutput | Select-Object -Last 3 | Out-String)
    }
} catch {
    Write-TestResult "Unit Tests" $false $_.Exception.Message
}

# Test 3: Docker Services
if (-not $SkipDocker) {
    Write-Host "🐳 Testing Docker Services..." -ForegroundColor Cyan
    try {
        # Check if Docker is running
        docker info > $null 2>&1
        if ($LASTEXITCODE -ne 0) {
            Write-TestResult "Docker Services" $false "Docker is not running"
        } else {
            # Start services if not running
            Write-Verbose "Starting Docker services..."
            docker-compose up -d --wait > $null 2>&1
            
            # Test individual services
            $services = @{
                "PostgreSQL" = @{ Host = "localhost"; Port = 5432 }
                "Redis" = @{ Host = "localhost"; Port = 6379 }
                "MailHog" = @{ Host = "localhost"; Port = 8025 }
                "Seq" = @{ Host = "localhost"; Port = 8080 }
                "Azurite" = @{ Host = "localhost"; Port = 10000 }
            }
            
            $allServicesUp = $true
            foreach ($service in $services.Keys) {
                $config = $services[$service]
                try {
                    $connection = New-Object System.Net.Sockets.TcpClient
                    $connection.ReceiveTimeout = 3000
                    $connection.SendTimeout = 3000
                    $connection.Connect($config.Host, $config.Port)
                    $connection.Close()
                    Write-Verbose "$service is accessible on port $($config.Port)"
                } catch {
                    Write-TestResult "$service Service" $false "Port $($config.Port) not accessible"
                    $allServicesUp = $false
                }
            }
            
            $testResults.DockerServices = $allServicesUp
            Write-TestResult "Docker Services" $allServicesUp
        }
    } catch {
        Write-TestResult "Docker Services" $false $_.Exception.Message
    }
} else {
    Write-Host "⏭️  Skipping Docker Services tests" -ForegroundColor Yellow
}

# Test 4: Database Connection
Write-Host "🗄️  Testing Database Connection..." -ForegroundColor Cyan
try {
    # This requires the database to be running
    $connectionString = "Host=localhost;Port=5432;Database=LankaConnectDB;Username=lankaconnect;Password=dev_password_123;Timeout=10"
    
    Add-Type -AssemblyName "Npgsql, Version=8.0.0.0, Culture=neutral, PublicKeyToken=5d8b90d52f46fda7" -ErrorAction SilentlyContinue
    
    if ([System.Type]::GetType("Npgsql.NpgsqlConnection") -ne $null) {
        $connection = New-Object Npgsql.NpgsqlConnection($connectionString)
        $connection.Open()
        
        $command = New-Object Npgsql.NpgsqlCommand("SELECT version()", $connection)
        $version = $command.ExecuteScalar()
        
        $connection.Close()
        $connection.Dispose()
        
        $testResults.DatabaseConnection = $true
        Write-TestResult "Database Connection" $true
        Write-Verbose "PostgreSQL Version: $version"
    } else {
        # Fallback to dotnet test for database tests
        $dbTestOutput = dotnet test --configuration $Configuration --no-build --verbosity minimal --filter "Category=Database" 2>&1
        if ($LASTEXITCODE -eq 0) {
            $testResults.DatabaseConnection = $true
            Write-TestResult "Database Connection" $true
        } else {
            Write-TestResult "Database Connection" $false "Database tests failed"
        }
    }
} catch {
    Write-TestResult "Database Connection" $false $_.Exception.Message
}

# Test 5: Integration Tests
Write-Host "🔗 Running Integration Tests..." -ForegroundColor Cyan
try {
    $integrationOutput = dotnet test --configuration $Configuration --no-build --verbosity minimal --filter "Category=Integration" 2>&1
    if ($LASTEXITCODE -eq 0) {
        $testResults.IntegrationTests = $true
        Write-TestResult "Integration Tests" $true
        
        # Extract test counts
        $testLine = $integrationOutput | Where-Object { $_ -match "Passed!" }
        if ($testLine) {
            Write-Host "   └─ $testLine" -ForegroundColor Green
        }
    } else {
        # Integration tests might fail if services aren't running
        Write-TestResult "Integration Tests" $false "Some integration tests may require running services"
    }
} catch {
    Write-TestResult "Integration Tests" $false $_.Exception.Message
}

# Test 6: Logging Configuration
Write-Host "📝 Testing Logging Configuration..." -ForegroundColor Cyan
try {
    # Test that logging configuration files exist and are valid
    $appsettingsExists = Test-Path "src/LankaConnect.API/appsettings.json"
    $appsettingsDevExists = Test-Path "src/LankaConnect.API/appsettings.Development.json"
    
    if ($appsettingsExists -and $appsettingsDevExists) {
        # Try to parse JSON to validate structure
        $appsettings = Get-Content "src/LankaConnect.API/appsettings.json" | ConvertFrom-Json
        $serilogConfig = $appsettings.Serilog
        
        if ($serilogConfig -and $serilogConfig.WriteTo) {
            $testResults.LoggingConfiguration = $true
            Write-TestResult "Logging Configuration" $true
        } else {
            Write-TestResult "Logging Configuration" $false "Serilog configuration missing or invalid"
        }
    } else {
        Write-TestResult "Logging Configuration" $false "Configuration files missing"
    }
} catch {
    Write-TestResult "Logging Configuration" $false $_.Exception.Message
}

# Summary
Write-Host "`n🎯 Environment Verification Summary" -ForegroundColor Green
Write-Host "===================================" -ForegroundColor Green

$passedTests = ($testResults.Values | Where-Object { $_ -eq $true }).Count
$totalTests = $testResults.Count
$successRate = [math]::Round(($passedTests / $totalTests) * 100, 2)

Write-Host "Passed: $passedTests/$totalTests tests ($successRate%)" -ForegroundColor $(if ($passedTests -eq $totalTests) { "Green" } else { "Yellow" })

if ($passedTests -eq $totalTests) {
    Write-Host "🎉 All environment checks passed! Ready for development." -ForegroundColor Green
    exit 0
} else {
    Write-Host "⚠️  Some environment checks failed. Please review and fix issues above." -ForegroundColor Yellow
    
    # Show specific failed tests
    Write-Host "`nFailed Tests:" -ForegroundColor Red
    foreach ($test in $testResults.Keys) {
        if (-not $testResults[$test]) {
            Write-Host "  - $test" -ForegroundColor Red
        }
    }
    
    exit 1
}