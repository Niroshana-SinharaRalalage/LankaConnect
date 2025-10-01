# TDD Enforcement Checklist - MANDATORY for every development task
param(
    [Parameter(Mandatory=$true)]
    [ValidateSet("pre-work", "during-work", "post-work")]
    [string]$Phase,
    
    [Parameter(Mandatory=$false)]
    [string]$ComponentPath = "",
    
    [Parameter(Mandatory=$false)]
    [string]$TestFile = "",
    
    [Parameter(Mandatory=$false)]
    [string]$TaskDescription = ""
)

function Write-PhaseHeader {
    param([string]$Title)
    Write-Host "`n===============================================" -ForegroundColor Yellow
    Write-Host " $Title" -ForegroundColor Yellow
    Write-Host "===============================================`n" -ForegroundColor Yellow
}

function Test-CompilationSuccess {
    param([string]$Project)
    Write-Host "TESTING COMPILATION: $Project" -ForegroundColor Cyan
    $result = & dotnet build $Project --verbosity quiet 2>&1
    $errorCount = ($result | Select-String "error CS" | Measure-Object).Count
    
    if ($errorCount -eq 0) {
        Write-Host "COMPILATION SUCCESS: 0 errors" -ForegroundColor Green
        return $true
    } else {
        Write-Host "COMPILATION FAILED: $errorCount errors" -ForegroundColor Red
        $result | Select-String "error CS" | Select-Object -First 5 | ForEach-Object {
            Write-Host "   $_" -ForegroundColor Red
        }
        return $false
    }
}

switch ($Phase) {
    "pre-work" {
        Write-PhaseHeader "PRE-WORK TDD CHECKLIST"
        
        Write-Host "TASK: $TaskDescription" -ForegroundColor White
        Write-Host "TARGET: $ComponentPath" -ForegroundColor White
        Write-Host "TEST FILE: $TestFile" -ForegroundColor White
        
        Write-Host "`nBASELINE COMPILATION CHECK"
        if (-not (Test-CompilationSuccess $ComponentPath)) {
            Write-Host "CANNOT START: Fix existing compilation errors first" -ForegroundColor Red
            exit 1
        }
        
        if (-not $TestFile) {
            Write-Host "CANNOT START: Must specify test file for TDD" -ForegroundColor Red
            exit 1
        }
        
        Write-Host "`nPRE-WORK CHECKLIST PASSED - Ready for TDD" -ForegroundColor Green
    }
    
    "during-work" {
        Write-PhaseHeader "DURING-WORK TDD VALIDATION"
        
        Write-Host "INCREMENTAL COMPILATION CHECK"
        if (-not (Test-CompilationSuccess $ComponentPath)) {
            Write-Host "TDD VIOLATION: Code doesn't compile - fix immediately" -ForegroundColor Red
            exit 1
        }
        
        Write-Host "`nDURING-WORK VALIDATION PASSED" -ForegroundColor Green
    }
    
    "post-work" {
        Write-PhaseHeader "POST-WORK TDD COMPLETION"
        
        Write-Host "FINAL COMPILATION CHECK"
        if (-not (Test-CompilationSuccess $ComponentPath)) {
            Write-Host "WORK INCOMPLETE: Final compilation must succeed" -ForegroundColor Red
            exit 1
        }
        
        Write-Host "`nPOST-WORK VALIDATION PASSED - Work Complete" -ForegroundColor Green
        Write-Host "READY TO MOVE TO NEXT COMPONENT" -ForegroundColor Green
    }
    
    default {
        Write-Host "Invalid phase specified" -ForegroundColor Red
        exit 1
    }
}