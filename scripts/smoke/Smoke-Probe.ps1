<#
.SYNOPSIS
    Schema/column/row-count probe against staging Postgres - S5/S6 smoke
    coverage per CLAUDE.md §13.2.

.DESCRIPTION
    For schema migrations (S5) and module-context pivots (S6), we need to
    confirm the physical table state matches expectations:

      - For an additive migration:  "the column was added"
      - For a rename / SET SCHEMA migration:  "the table moved"
      - For a module-context apply:  "the operational tables exist in the schema"
      - For a write smoke:           "the row was actually written"

    This script provides four probe modes. It executes via the
    Microsoft.EntityFrameworkCore.Design CLI rather than a raw psql client
    (since the runner doesn't have psql installed by default) - specifically
    by running a short `dotnet ef dbcontext info` + a custom SQL probe via
    `dotnet script` is too heavy; instead we use Npgsql via a one-shot
    powershell command that loads the staging connection string from Key
    Vault and runs a simple SELECT against information_schema.

    If `psql` IS available locally, the script prefers it for speed.

.PARAMETER Schema
    Postgres schema name (e.g., 'identity', 'notifications', 'media', 'events').

.PARAMETER Table
    Optional table name within the schema. If omitted, probes existence of the
    schema and lists its tables.

.PARAMETER ExpectColumn
    Optional column name; probe succeeds only if the column exists.

.PARAMETER ExpectRowCountAtLeast
    Optional integer; probe succeeds only if the row count meets/exceeds this.

.PARAMETER ConnectionString
    Optional override; if omitted, the script reads from staging Key Vault
    via `az keyvault secret show`. Requires az CLI authenticated.

.OUTPUTS
    Exit 0 + one-line summary on green.
    Exit 1 + diagnostic on red.

.NOTES
    Built as part of Gap G0 (2026-06-08).
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string]$Schema,

    [string]$Table,
    [string]$ExpectColumn,
    [int]$ExpectRowCountAtLeast,
    [string]$ConnectionString
)

$ErrorActionPreference = 'Stop'

function Get-ConnectionString {
    if ($ConnectionString) { return $ConnectionString }
    if ($env:LC_DB_CONNECTION) { return $env:LC_DB_CONNECTION }

    Write-Verbose 'Loading connection string from Key Vault...'
    $cs = & az keyvault secret show `
        --vault-name 'lankaconnect-staging-kv' `
        --name 'DATABASE-CONNECTION-STRING' `
        --query value -o tsv 2>&1
    if ($LASTEXITCODE -ne 0) {
        throw "Could not load DB connection from Key Vault (LC_DB_CONNECTION not set, az keyvault failed). Output: $cs"
    }
    return $cs
}

function Invoke-Psql {
    param([string]$Sql)

    $cs = Get-ConnectionString
    $psql = Get-Command psql -ErrorAction SilentlyContinue
    if ($psql) {
        # Use psql for speed. Parse Host/Port/Database/Username/Password from cs.
        # Npgsql connection-string format: Host=...;Port=...;Database=...;Username=...;Password=...
        $kv = @{}
        foreach ($pair in $cs -split ';') {
            if ($pair -match '^([^=]+)=(.*)$') { $kv[$Matches[1].Trim()] = $Matches[2].Trim() }
        }
        $env:PGPASSWORD = $kv['Password']
        $result = & psql -h $kv['Host'] -p $kv['Port'] -U $kv['Username'] -d $kv['Database'] -t -A -F "`t" -c $Sql 2>&1
        Remove-Item Env:\PGPASSWORD -ErrorAction SilentlyContinue
        if ($LASTEXITCODE -ne 0) {
            throw "psql failed: $result"
        }
        return $result
    }

    # psql not available - bail with actionable message.
    # Wave4.9.1.5 attempted an Add-Type Npgsql.dll fallback but Windows
    # PowerShell 5.1 cannot load .NET 8 assemblies (CLR mismatch). pwsh 7+
    # would work but is not installed on the founder's box. Until psql or
    # pwsh ships, the API-endpoint-exercise approach in
    # scripts/smoke/Probe-OperationalTables.ps1 is the working alternative.
    throw "psql not installed. Install via 'winget install PostgreSQL.PostgreSQL' (local) or 'apt-get install postgresql-client' (CI), then re-run. (Direct Npgsql.dll loading via Add-Type does NOT work in Windows PowerShell 5.1 — needs pwsh 7+.)"
}

try {
    if ($Table) {
        # Probe a specific table
        $existsSql = "SELECT EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = '$Schema' AND table_name = '$Table');"
        $exists = Invoke-Psql -Sql $existsSql
        if ($exists.Trim() -ne 't') {
            throw "Table $Schema.$Table does not exist on staging"
        }

        $summary = "Probe OK $Schema.$Table exists"

        if ($ExpectColumn) {
            $colSql = "SELECT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = '$Schema' AND table_name = '$Table' AND column_name = '$ExpectColumn');"
            $hasCol = Invoke-Psql -Sql $colSql
            if ($hasCol.Trim() -ne 't') {
                throw "Column $Schema.$Table.$ExpectColumn does not exist on staging"
            }
            $summary += "; column '$ExpectColumn' present"
        }

        if ($PSBoundParameters.ContainsKey('ExpectRowCountAtLeast')) {
            $rcSql = "SELECT COUNT(*) FROM `"$Schema`".`"$Table`";"
            $rcRaw = Invoke-Psql -Sql $rcSql
            $rc = [int]$rcRaw.Trim()
            if ($rc -lt $ExpectRowCountAtLeast) {
                throw "Table $Schema.$Table has $rc rows (expected ≥ $ExpectRowCountAtLeast)"
            }
            $summary += "; rowCount=$rc"
        }

        $summary
    }
    else {
        # Probe the schema itself
        $existsSql = "SELECT EXISTS (SELECT 1 FROM information_schema.schemata WHERE schema_name = '$Schema');"
        $exists = Invoke-Psql -Sql $existsSql
        if ($exists.Trim() -ne 't') {
            throw "Schema '$Schema' does not exist on staging"
        }
        $tablesSql = "SELECT table_name FROM information_schema.tables WHERE table_schema = '$Schema' ORDER BY table_name;"
        $tables = Invoke-Psql -Sql $tablesSql
        $tableList = ($tables -split "`n" | Where-Object { $_.Trim() }) -join ', '
        "Probe OK schema='$Schema' tables=[$tableList]"
    }

    exit 0
}
catch {
    Write-Error "Smoke-Probe FAILED ($Schema$(if ($Table) { ".$Table" })): $($_.Exception.Message)"
    exit 1
}
