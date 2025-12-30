$connectionString = 'Host=lankaconnect-staging-db.postgres.database.azure.com;Database=LankaConnectDB;Username=adminuser;Password=1qaz!QAZ;SslMode=Require'

# Find and load Npgsql
$npgsqlPath = (Get-ChildItem -Path $env:USERPROFILE\.nuget\packages\npgsql -Recurse -Filter 'Npgsql.dll' -ErrorAction SilentlyContinue | Where-Object { $_.FullName -match 'net8.0' } | Select-Object -First 1).FullName

if (-not $npgsqlPath) {
    $npgsqlPath = (Get-ChildItem -Path 'C:\Work\LankaConnect' -Recurse -Filter 'Npgsql.dll' -ErrorAction SilentlyContinue | Where-Object { $_.FullName -match 'net8.0' } | Select-Object -First 1).FullName
}

if ($npgsqlPath) {
    Add-Type -Path $npgsqlPath
    Write-Host "Loaded Npgsql from: $npgsqlPath"
} else {
    Write-Host "ERROR: Npgsql.dll not found!"
    exit 1
}

Write-Host ""
Write-Host "Connecting to database..."
$conn = New-Object Npgsql.NpgsqlConnection($connectionString)
$conn.Open()
Write-Host "✅ Database connected successfully!"
Write-Host ""

# Check 1: Migration
Write-Host "=== CHECK 1: Email Template Migration Status ==="
$cmd = $conn.CreateCommand()
$cmd.CommandText = "SELECT `"MigrationId`", `"ProductVersion`" FROM `"__EFMigrationsHistory`" WHERE `"MigrationId`" LIKE '%20251229231742%' ORDER BY `"MigrationId`" DESC"
$reader = $cmd.ExecuteReader()
if ($reader.Read()) {
    Write-Host "✅ Migration Applied: $($reader.GetString(0))"
    Write-Host "   Product Version: $($reader.GetString(1))"
} else {
    Write-Host "❌ Migration NOT found in database!"
}
$reader.Close()
Write-Host ""

# Check 2: Email Template
Write-Host "=== CHECK 2: Email Template Content ==="
$cmd = $conn.CreateCommand()
$cmd.CommandText = "SELECT `"Id`", `"TemplateType`", `"Subject`", LENGTH(`"HtmlBody`") as HtmlBodyLength, LEFT(`"HtmlBody`", 400) as HtmlBodyPreview, `"UpdatedAt`" FROM `"EmailTemplates`" WHERE `"TemplateType`" = 'EmailVerification' ORDER BY `"UpdatedAt`" DESC LIMIT 1"
$reader = $cmd.ExecuteReader()
if ($reader.Read()) {
    Write-Host "Template ID: $($reader.GetGuid(0))"
    Write-Host "Type: $($reader.GetString(1))"
    Write-Host "Subject: $($reader.GetString(2))"
    Write-Host "HTML Body Length: $($reader.GetInt32(3)) chars"
    Write-Host "Last Updated: $($reader.GetDateTime(5).ToString('yyyy-MM-dd HH:mm:ss'))"

    $preview = $reader.GetString(4)
    if ($preview -match '✦') {
        Write-Host "❌ WARNING: Template contains decorative stars (✦)"
    } else {
        Write-Host "✅ Template does NOT contain decorative stars"
    }

    if ($preview -match 'logo\.png' -or $preview -match '<img') {
        Write-Host "⚠️  Template may contain logo image"
    } else {
        Write-Host "✅ Template does NOT contain logo"
    }

    Write-Host ""
    Write-Host "Preview (first 400 chars):"
    Write-Host $preview
} else {
    Write-Host "❌ No email template found!"
}
$reader.Close()
Write-Host ""

# Check 3: Test User
Write-Host "=== CHECK 3: Test User Verification Status ==="
$cmd = $conn.CreateCommand()
$cmd.CommandText = "SELECT `"Email`", `"IsEmailVerified`", `"EmailVerificationToken`" IS NOT NULL as HasToken, `"EmailVerificationTokenExpiry`", `"CreatedAt`" FROM `"Users`" WHERE `"Email`" = 'test.phase6a53fix@gmail.com'"
$reader = $cmd.ExecuteReader()
if ($reader.Read()) {
    Write-Host "✅ User Found: $($reader.GetString(0))"
    Write-Host "   Email Verified: $($reader.GetBoolean(1))"
    Write-Host "   Has Verification Token: $($reader.GetBoolean(2))"
    if (-not $reader.IsDBNull(3)) {
        Write-Host "   Token Expiry: $($reader.GetDateTime(3).ToString('yyyy-MM-dd HH:mm:ss'))"
    }
    Write-Host "   Created At: $($reader.GetDateTime(4).ToString('yyyy-MM-dd HH:mm:ss'))"
} else {
    Write-Host "❌ Test user NOT found in database!"
}
$reader.Close()
Write-Host ""

# Check 4: Last 5 migrations
Write-Host "=== CHECK 4: Last 5 Migrations ==="
$cmd = $conn.CreateCommand()
$cmd.CommandText = "SELECT `"MigrationId`", `"ProductVersion`" FROM `"__EFMigrationsHistory`" ORDER BY `"MigrationId`" DESC LIMIT 5"
$reader = $cmd.ExecuteReader()
while ($reader.Read()) {
    Write-Host "  - $($reader.GetString(0)) ($($reader.GetString(1)))"
}
$reader.Close()

$conn.Close()
Write-Host ""
Write-Host "✅ Verification complete!"
