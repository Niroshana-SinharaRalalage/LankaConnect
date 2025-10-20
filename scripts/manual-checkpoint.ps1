# Manual Checkpoint Script
# Quick checkpoint for specific stage completions

param(
    [Parameter(Mandatory=$true)]
    [string]$StageName
)

$logFile = "C:\Work\LankaConnect\docs\validation-monitoring-log.md"

function Get-BuildMetrics {
    $buildOutput = dotnet build 2>&1 | Out-String

    $totalErrors = ($buildOutput | Select-String -Pattern "error CS" -AllMatches).Matches.Count

    $errorTypes = @{}
    $matches = $buildOutput | Select-String -Pattern "error (CS\d+)" -AllMatches
    foreach ($match in $matches.Matches) {
        $errorCode = $match.Groups[1].Value
        if ($errorTypes.ContainsKey($errorCode)) {
            $errorTypes[$errorCode]++
        } else {
            $errorTypes[$errorCode] = 1
        }
    }

    return @{
        Total = $totalErrors
        Types = $errorTypes
        Timestamp = Get-Date
    }
}

Write-Host "Running manual checkpoint for: $StageName"
$metrics = Get-BuildMetrics

$cs0535 = if ($metrics.Types.ContainsKey("CS0535")) { $metrics.Types["CS0535"] } else { 0 }
$cs0738 = if ($metrics.Types.ContainsKey("CS0738")) { $metrics.Types["CS0738"] } else { 0 }
$cs0246 = if ($metrics.Types.ContainsKey("CS0246")) { $metrics.Types["CS0246"] } else { 0 }
$cs0234 = if ($metrics.Types.ContainsKey("CS0234")) { $metrics.Types["CS0234"] } else { 0 }

$timestamp = $metrics.Timestamp.ToString("HH:mm:ss")
$row = "| $timestamp | $($metrics.Total) | $cs0535 | $cs0738 | $cs0246 | $cs0234 | - | $StageName |"

Add-Content -Path $logFile -Value $row

Write-Host "Checkpoint recorded:"
Write-Host "  Total Errors: $($metrics.Total)"
Write-Host "  CS0535 (Missing implementation): $cs0535"
Write-Host "  CS0738 (Return type mismatch): $cs0738"
Write-Host "  CS0246 (Missing type): $cs0246"
Write-Host "  CS0234 (Missing namespace member): $cs0234"

# Store in coordination memory
$memoryKey = "swarm/stages34/$($StageName.Replace(' ', '-').ToLower())"
npx claude-flow@alpha hooks post-edit --file "manual-checkpoint" --memory-key $memoryKey 2>&1 | Out-Null

Write-Host "`nCheckpoint saved to: $logFile"
