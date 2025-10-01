# TDD Phase D: Incremental Type Extraction Script
# Zero Tolerance Compilation Error Elimination - Safe Type Extraction

param(
    [Parameter(Mandatory=$true)]
    [string]$SourceFile,

    [Parameter(Mandatory=$true)]
    [string]$TypeName,

    [string]$TargetNamespace = $null,
    [string]$TargetDirectory = $null,
    [switch]$DryRun = $false,
    [switch]$Force = $false,
    [switch]$Verbose = $false
)

# Validation and setup
if (-not (Test-Path $SourceFile)) {
    Write-Error "Source file not found: $SourceFile"
    exit 1
}

Write-Host "🔧 TDD Phase D: Incremental Type Extraction" -ForegroundColor Green
Write-Host "📁 Source File: $SourceFile" -ForegroundColor Cyan
Write-Host "🎯 Target Type: $TypeName" -ForegroundColor Cyan

# Function to create Git checkpoint
function New-GitCheckpoint {
    param([string]$Message)

    if ($Verbose) {
        Write-Host "  📝 Creating Git checkpoint..." -ForegroundColor Gray
    }

    $currentBranch = git branch --show-current
    $commitHash = git rev-parse HEAD

    git add -A
    git commit -m "TDD Phase D Checkpoint: $Message" -q

    return @{
        Branch = $currentBranch
        CommitHash = $commitHash
        CheckpointHash = (git rev-parse HEAD)
    }
}

# Function to rollback changes
function Invoke-GitRollback {
    param([string]$CommitHash)

    Write-Host "🔄 Rolling back changes..." -ForegroundColor Yellow
    git reset --hard $CommitHash
    Write-Host "✅ Rollback complete" -ForegroundColor Green
}

# Function to validate compilation
function Test-Compilation {
    Write-Host "🔧 Validating compilation..." -ForegroundColor Yellow

    $buildResult = dotnet build --no-restore 2>&1
    $exitCode = $LASTEXITCODE

    if ($exitCode -eq 0) {
        Write-Host "✅ Compilation successful" -ForegroundColor Green
        return $true
    } else {
        Write-Host "❌ Compilation failed" -ForegroundColor Red
        if ($Verbose) {
            $buildResult | Where-Object { $_ -match "error" } | ForEach-Object {
                Write-Host "  $($_)" -ForegroundColor Red
            }
        }
        return $false
    }
}

# Function to extract type definition
function Get-TypeDefinition {
    param(
        [string]$FilePath,
        [string]$TypeName
    )

    $content = Get-Content $FilePath -Raw
    $lines = Get-Content $FilePath

    # Find type definition with proper scope handling
    $typePattern = "(?ms)^(\s*(?:public|internal|private)?(?:\s+(?:abstract|sealed|static))?\s*(?:class|interface|enum|struct|record)\s+$TypeName\b.*?)(?=^\s*(?:public|internal|private)?\s*(?:class|interface|enum|struct|record|namespace|\}|\z))"

    $match = [regex]::Match($content, $typePattern)

    if (-not $match.Success) {
        Write-Error "Type '$TypeName' not found in file: $FilePath"
        return $null
    }

    $typeDefinition = $match.Groups[1].Value.Trim()
    $startLine = ($content.Substring(0, $match.Index) -split "`n").Count
    $endLine = $startLine + ($typeDefinition -split "`n").Count - 1

    # Extract using statements
    $usingStatements = $lines | Where-Object { $_ -match "^\s*using\s+" } | ForEach-Object { $_.Trim() }

    # Extract namespace
    $namespaceMatch = [regex]::Match($content, "^\s*namespace\s+([\w\.]+)", [System.Text.RegularExpressions.RegexOptions]::Multiline)
    $currentNamespace = if ($namespaceMatch.Success) { $namespaceMatch.Groups[1].Value } else { "Global" }

    return @{
        TypeDefinition = $typeDefinition
        UsingStatements = $usingStatements
        Namespace = $currentNamespace
        StartLine = $startLine
        EndLine = $endLine
        OriginalContent = $content
    }
}

# Function to determine target file path
function Get-TargetFilePath {
    param(
        [string]$TypeName,
        [string]$Namespace,
        [string]$SourceFile
    )

    if ($TargetDirectory) {
        $targetDir = $TargetDirectory
    } else {
        # Determine target directory based on namespace and type
        $sourceDir = Split-Path $SourceFile -Parent

        # For value objects and models, create appropriate subdirectories
        if ($TypeName -like "*ValueObject*" -or $TypeName -like "*Model*") {
            $targetDir = Join-Path $sourceDir "Models"
        } elseif ($TypeName -like "*Enum*" -or $TypeName -match "Type$|Status$|State$") {
            $targetDir = Join-Path $sourceDir "Enums"
        } elseif ($TypeName -like "*Result*" -or $TypeName -like "*Response*") {
            $targetDir = Join-Path $sourceDir "Results"
        } else {
            $targetDir = $sourceDir
        }
    }

    # Ensure directory exists
    if (-not (Test-Path $targetDir)) {
        New-Item -ItemType Directory -Path $targetDir -Force | Out-Null
        Write-Host "📁 Created directory: $targetDir" -ForegroundColor Green
    }

    return Join-Path $targetDir "$TypeName.cs"
}

# Function to create new file content
function New-TypeFileContent {
    param(
        [array]$UsingStatements,
        [string]$Namespace,
        [string]$TypeDefinition
    )

    $content = @()

    # Add using statements (deduplicated)
    $uniqueUsings = $UsingStatements | Sort-Object -Unique | Where-Object { $_ -and $_ -notmatch "^\s*$" }
    if ($uniqueUsings) {
        $content += $uniqueUsings
        $content += ""
    }

    # Add namespace declaration
    $content += "namespace $Namespace;"
    $content += ""

    # Add type definition
    $content += $TypeDefinition

    return $content -join "`n"
}

# Function to remove type from source file
function Remove-TypeFromSource {
    param(
        [string]$FilePath,
        [string]$TypeName,
        [int]$StartLine,
        [int]$EndLine
    )

    $lines = Get-Content $FilePath
    $newLines = @()

    for ($i = 0; $i -lt $lines.Count; $i++) {
        $lineNumber = $i + 1
        if ($lineNumber -lt $StartLine -or $lineNumber -gt $EndLine) {
            $newLines += $lines[$i]
        }
    }

    # Remove empty lines that might have been left behind
    $cleanedLines = @()
    $previousEmpty = $false

    foreach ($line in $newLines) {
        $isEmpty = [string]::IsNullOrWhiteSpace($line)

        if (-not ($isEmpty -and $previousEmpty)) {
            $cleanedLines += $line
        }

        $previousEmpty = $isEmpty
    }

    $cleanedLines | Set-Content $FilePath -Encoding UTF8
}

# Function to add using statement to consuming files
function Add-UsingStatement {
    param(
        [string]$FilePath,
        [string]$Namespace
    )

    $content = Get-Content $FilePath -Raw
    $usingStatement = "using $Namespace;"

    # Check if using statement already exists
    if ($content -match [regex]::Escape($usingStatement)) {
        return # Already exists
    }

    $lines = Get-Content $FilePath
    $newLines = @()
    $usingAdded = $false

    foreach ($line in $lines) {
        if (-not $usingAdded -and $line -match "^\s*using\s+" -and $line -gt $usingStatement) {
            $newLines += $usingStatement
            $usingAdded = $true
        }
        $newLines += $line
    }

    # If no using statements exist, add at the beginning
    if (-not $usingAdded) {
        $newLines = @($usingStatement, "") + $newLines
    }

    $newLines | Set-Content $FilePath -Encoding UTF8
}

# Main extraction process
try {
    # Step 1: Create checkpoint
    $checkpoint = New-GitCheckpoint "Before extracting $TypeName from $SourceFile"

    # Step 2: Validate initial compilation
    if (-not (Test-Compilation)) {
        Write-Error "Initial compilation failed. Fix errors before extraction."
        exit 1
    }

    # Step 3: Extract type definition
    Write-Host "🔍 Extracting type definition..." -ForegroundColor Yellow
    $typeInfo = Get-TypeDefinition $SourceFile $TypeName

    if (-not $typeInfo) {
        exit 1
    }

    # Step 4: Determine target file path
    $targetNamespace = if ($TargetNamespace) { $TargetNamespace } else { $typeInfo.Namespace }
    $targetFilePath = Get-TargetFilePath $TypeName $targetNamespace $SourceFile

    Write-Host "📁 Target File: $targetFilePath" -ForegroundColor Cyan
    Write-Host "🎯 Target Namespace: $targetNamespace" -ForegroundColor Cyan

    # Step 5: Check if target file already exists
    if ((Test-Path $targetFilePath) -and -not $Force) {
        Write-Error "Target file already exists: $targetFilePath. Use -Force to overwrite."
        exit 1
    }

    if ($DryRun) {
        Write-Host "🔍 DRY RUN - No changes will be made" -ForegroundColor Yellow
        Write-Host "  Would extract: $TypeName"
        Write-Host "  From: $SourceFile"
        Write-Host "  To: $targetFilePath"
        Write-Host "  Namespace: $targetNamespace"
        exit 0
    }

    # Step 6: Create new file
    Write-Host "📝 Creating new type file..." -ForegroundColor Yellow
    $newFileContent = New-TypeFileContent $typeInfo.UsingStatements $targetNamespace $typeInfo.TypeDefinition
    $newFileContent | Set-Content $targetFilePath -Encoding UTF8

    # Step 7: Remove type from source file
    Write-Host "🗑️  Removing type from source file..." -ForegroundColor Yellow
    Remove-TypeFromSource $SourceFile $TypeName $typeInfo.StartLine $typeInfo.EndLine

    # Step 8: Add using statement to source file
    Write-Host "🔗 Adding using statement to source file..." -ForegroundColor Yellow
    Add-UsingStatement $SourceFile $targetNamespace

    # Step 9: Validate compilation
    Write-Host "🔧 Validating post-extraction compilation..." -ForegroundColor Yellow
    if (Test-Compilation) {
        Write-Host "✅ Type extraction successful!" -ForegroundColor Green
        Write-Host "📁 Created: $targetFilePath" -ForegroundColor Green

        # Commit successful extraction
        git add -A
        git commit -m "TDD Phase D: Extract $TypeName to dedicated file

- Extracted $TypeName from $SourceFile
- Created dedicated file: $targetFilePath
- Added appropriate using statements
- Compilation validated successfully" -q

        Write-Host "✅ Changes committed to Git" -ForegroundColor Green

    } else {
        Write-Host "❌ Compilation failed after extraction. Rolling back..." -ForegroundColor Red
        Invoke-GitRollback $checkpoint.CommitHash
        exit 1
    }

} catch {
    Write-Host "❌ Error during extraction: $($_.Exception.Message)" -ForegroundColor Red
    if ($checkpoint) {
        Invoke-GitRollback $checkpoint.CommitHash
    }
    exit 1
}

Write-Host "`n🎯 TDD Phase D: Type extraction complete!" -ForegroundColor Green
Write-Host "📊 Run compilation validation and continue with next type" -ForegroundColor Cyan