# Transparent Progress Display System
param(
    [Parameter(Mandatory=$true)]
    [ValidateSet("start-task", "update-progress", "show-current-work", "complete-task")]
    [string]$Action,
    
    [Parameter(Mandatory=$false)]
    [string]$TaskId = "",
    
    [Parameter(Mandatory=$false)]
    [string]$CurrentFile = "",
    
    [Parameter(Mandatory=$false)]
    [string]$SpecificChange = "",
    
    [Parameter(Mandatory=$false)]
    [string]$TDDPhase = "",
    
    [Parameter(Mandatory=$false)]
    [string]$ErrorCount = ""
)

function Write-ProgressHeader {
    $timestamp = Get-Date -Format "HH:mm:ss"
    Write-Host "`nLIVE PROGRESS TRACKING - $timestamp" -ForegroundColor Blue
}

function Show-CurrentStatus {
    $workingDir = Get-Location
    Write-Host "`nCURRENT LOCATION: $workingDir" -ForegroundColor Yellow
    
    if (Test-Path "progress-state.json") {
        $state = Get-Content "progress-state.json" | ConvertFrom-Json
        
        Write-Host "ACTIVE TASK: $($state.TaskId)" -ForegroundColor Cyan
        Write-Host "CURRENT FILE: $($state.CurrentFile)" -ForegroundColor White
        Write-Host "SPECIFIC CHANGE: $($state.SpecificChange)" -ForegroundColor Green
        Write-Host "TDD PHASE: $($state.TDDPhase)" -ForegroundColor Magenta
        Write-Host "ERROR COUNT: $($state.ErrorCount)" -ForegroundColor Red
        Write-Host "STARTED: $($state.StartTime)" -ForegroundColor Gray
        Write-Host "PROGRESS: $($state.CompletionPercentage)%" -ForegroundColor Yellow
    } else {
        Write-Host "NO ACTIVE WORK TRACKED" -ForegroundColor Red
    }
}

function Update-ProgressState {
    param($Data)
    
    $state = @{
        TaskId = $Data.TaskId
        CurrentFile = $Data.CurrentFile  
        SpecificChange = $Data.SpecificChange
        TDDPhase = $Data.TDDPhase
        ErrorCount = $Data.ErrorCount
        StartTime = if (Test-Path "progress-state.json") { 
            (Get-Content "progress-state.json" | ConvertFrom-Json).StartTime 
        } else { 
            Get-Date -Format "yyyy-MM-dd HH:mm:ss" 
        }
        LastUpdate = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
        CompletionPercentage = $Data.CompletionPercentage
    }
    
    $state | ConvertTo-Json | Out-File "progress-state.json" -Encoding UTF8
}

switch ($Action) {
    "start-task" {
        Write-ProgressHeader
        Write-Host "STARTING NEW TASK: $TaskId" -ForegroundColor Green
        
        Update-ProgressState @{
            TaskId = $TaskId
            CurrentFile = ""
            SpecificChange = "Task initialization"
            TDDPhase = "SETUP"
            ErrorCount = "Unknown"
            CompletionPercentage = 0
        }
    }
    
    "update-progress" {
        Write-ProgressHeader
        Write-Host "PROGRESS UPDATE FOR: $TaskId" -ForegroundColor Yellow
        
        if ($CurrentFile) {
            Write-Host "  NOW WORKING ON: $CurrentFile" -ForegroundColor White
        }
        if ($SpecificChange) {  
            Write-Host "  MAKING CHANGE: $SpecificChange" -ForegroundColor Green
        }
        if ($TDDPhase) {
            $color = if ($TDDPhase -eq "RED") { "Red" } elseif ($TDDPhase -eq "GREEN") { "Green" } else { "Yellow" }
            Write-Host "  TDD PHASE: $TDDPhase" -ForegroundColor $color
        }
        if ($ErrorCount) {
            Write-Host "  ERROR COUNT: $ErrorCount" -ForegroundColor $(if ([int]$ErrorCount -eq 0) { "Green" } else { "Red" })
        }
        
        $completion = if ($ErrorCount -and [int]$ErrorCount -eq 0) { 90 } elseif ($TDDPhase -eq "GREEN") { 75 } elseif ($TDDPhase -eq "RED") { 25 } else { 10 }
        
        Update-ProgressState @{
            TaskId = $TaskId
            CurrentFile = $CurrentFile
            SpecificChange = $SpecificChange  
            TDDPhase = $TDDPhase
            ErrorCount = $ErrorCount
            CompletionPercentage = $completion
        }
        
        Show-CurrentStatus
    }
    
    "show-current-work" {
        Write-ProgressHeader
        Show-CurrentStatus
    }
    
    "complete-task" {
        Write-ProgressHeader  
        Write-Host "TASK COMPLETED: $TaskId" -ForegroundColor Green
        
        if (Test-Path "progress-state.json") {
            Remove-Item "progress-state.json"
        }
        
        Write-Host "READY FOR NEXT TASK" -ForegroundColor Green
    }
    
    default {
        Write-Host "Invalid action specified" -ForegroundColor Red
        exit 1
    }
}