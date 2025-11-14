# Remove all state-level metro area entries from MetroAreaSeeder.cs

$filePath = "src\LankaConnect.Infrastructure\Data\Seeders\MetroAreaSeeder.cs"

# Read the entire file
$content = Get-Content $filePath -Raw

# Count original lines
$originalLines = ($content -split "`n").Count

# Pattern to match MetroArea.Create blocks with isStateLevelArea: true
# This uses .NET regex with single-line mode where . matches newlines
$pattern = '(?s)MetroArea\.Create\([^(]*?isStateLevelArea:\s*true[^)]*?\),\s*[\r\n]+'

# Remove all matches
$newContent = [regex]::Replace($content, $pattern, '')

# Count new lines
$newLines = ($newContent -split "`n").Count

# Write back
Set-Content $filePath -Value $newContent -NoNewline

Write-Host "Successfully removed state-level metro area entries"
Write-Host "  Original lines: $originalLines"
Write-Host "  New lines: $newLines"
Write-Host "  Removed: $($originalLines - $newLines) lines"
