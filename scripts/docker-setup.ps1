# LankaConnect Docker Development Environment Setup Script
# PowerShell script to set up and manage Docker development environment

param(
    [Parameter(Mandatory=$false)]
    [ValidateSet("up", "down", "restart", "logs", "status", "clean")]
    [string]$Action = "up",
    
    [Parameter(Mandatory=$false)]
    [string]$Service = "",
    
    [Parameter(Mandatory=$false)]
    [switch]$Build,
    
    [Parameter(Mandatory=$false)]
    [switch]$Detached = $true
)

$ErrorActionPreference = "Stop"

Write-Host "LankaConnect Docker Environment Manager" -ForegroundColor Cyan
Write-Host "=======================================" -ForegroundColor Cyan

# Check if Docker is running
function Test-DockerRunning {
    try {
        docker info > $null 2>&1
        return $true
    }
    catch {
        return $false
    }
}

# Check if docker-compose.yml exists
function Test-ComposeFile {
    return Test-Path "docker-compose.yml"
}

# Display service status
function Show-ServiceStatus {
    Write-Host "`nService Status:" -ForegroundColor Yellow
    docker-compose ps
    
    Write-Host "`nService URLs:" -ForegroundColor Yellow
    Write-Host "PostgreSQL: localhost:5432" -ForegroundColor Green
    Write-Host "Redis: localhost:6379" -ForegroundColor Green
    Write-Host "MailHog UI: http://localhost:8025" -ForegroundColor Green
    Write-Host "Azurite Blob: http://localhost:10000" -ForegroundColor Green
    Write-Host "Seq Logs: http://localhost:8080" -ForegroundColor Green
    Write-Host "pgAdmin: http://localhost:8081" -ForegroundColor Green
    Write-Host "Redis Commander: http://localhost:8082" -ForegroundColor Green
}

# Main execution
if (-not (Test-DockerRunning)) {
    Write-Error "Docker is not running. Please start Docker Desktop and try again."
    exit 1
}

if (-not (Test-ComposeFile)) {
    Write-Error "docker-compose.yml not found. Please run this script from the project root directory."
    exit 1
}

switch ($Action) {
    "up" {
        Write-Host "Starting LankaConnect development environment..." -ForegroundColor Green
        
        $composeArgs = @("up")
        
        if ($Detached) {
            $composeArgs += "-d"
        }
        
        if ($Build) {
            $composeArgs += "--build"
        }
        
        if ($Service) {
            $composeArgs += $Service
        }
        
        docker-compose @composeArgs
        
        if ($?) {
            Start-Sleep -Seconds 5
            Show-ServiceStatus
            
            Write-Host "`nEnvironment setup complete!" -ForegroundColor Green
            Write-Host "Copy .env.docker to .env.local and modify as needed." -ForegroundColor Yellow
        }
    }
    
    "down" {
        Write-Host "Stopping LankaConnect development environment..." -ForegroundColor Yellow
        docker-compose down
    }
    
    "restart" {
        Write-Host "Restarting LankaConnect development environment..." -ForegroundColor Yellow
        docker-compose restart $Service
        Start-Sleep -Seconds 3
        Show-ServiceStatus
    }
    
    "logs" {
        if ($Service) {
            docker-compose logs -f $Service
        } else {
            docker-compose logs -f
        }
    }
    
    "status" {
        Show-ServiceStatus
    }
    
    "clean" {
        Write-Host "Cleaning up Docker environment..." -ForegroundColor Red
        Write-Host "This will remove containers, volumes, and networks." -ForegroundColor Red
        
        $confirmation = Read-Host "Are you sure? (y/N)"
        if ($confirmation -eq 'y' -or $confirmation -eq 'Y') {
            docker-compose down -v --remove-orphans
            docker system prune -f
            Write-Host "Cleanup complete." -ForegroundColor Green
        } else {
            Write-Host "Cleanup cancelled." -ForegroundColor Yellow
        }
    }
}

Write-Host "`nScript execution completed." -ForegroundColor Cyan