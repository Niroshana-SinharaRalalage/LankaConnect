# Phase 6A.49: Comprehensive Email Issue Diagnostics
# Based on system-architect RCA

$ErrorActionPreference = "Stop"

Write-Host "=== PHASE 6A.49 EMAIL DIAGNOSTICS ===" -ForegroundColor Cyan
Write-Host "Event ID: 9e3722f5-c255-4dcc-b167-afef56bc5592" -ForegroundColor Gray
Write-Host ""

# Database connection (Azure staging)
$env:PGPASSWORD = "YourPassword123!"
$dbHost = "lankaconnect-staging-db.postgres.database.azure.com"
$dbName = "LankaConnectDB"
$dbUser = "adminuser"

# Test 1: Check if registrations exist for this event
Write-Host "Test 1: Checking registrations in database..." -ForegroundColor Yellow
$query1 = @"
SELECT
    r."Id",
    r."UserId",
    r."EventId",
    r."Status",
    r."PaymentStatus",
    r."CreatedAt",
    r."UpdatedAt",
    (r."Attendees"::jsonb)::text as attendees_json
FROM events."Registrations" r
WHERE r."EventId" = '9e3722f5-c255-4dcc-b167-afef56bc5592'
ORDER BY r."CreatedAt" DESC
LIMIT 5;
"@

try {
    $result1 = psql -h $dbHost -U $dbUser -d $dbName -c $query1 -t -A -F "|"
    if ($result1) {
        Write-Host "✓ Found registrations:" -ForegroundColor Green
        Write-Host $result1
    } else {
        Write-Host "✗ No registrations found!" -ForegroundColor Red
    }
} catch {
    Write-Host "✗ Database query failed: $_" -ForegroundColor Red
}

Write-Host ""

# Test 2: Check email template structure
Write-Host "Test 2: Checking ticket-confirmation template structure..." -ForegroundColor Yellow
$query2 = @"
SELECT
  name,
  is_active,
  LENGTH(html_template) as html_length,
  LENGTH(subject_template) as subject_length,
  html_template LIKE '%{{#%' as has_conditionals,
  html_template LIKE '%{{#HasTicket}}%' as has_ticket_conditional,
  html_template LIKE '%{{/HasTicket}}%' as has_closing_ticket,
  (LENGTH(html_template) - LENGTH(REPLACE(html_template, '{{#', ''))) / 3 as opening_tags,
  (LENGTH(html_template) - LENGTH(REPLACE(html_template, '{{/', ''))) / 3 as closing_tags,
  updated_at
FROM communications.email_templates
WHERE name = 'ticket-confirmation';
"@

try {
    $result2 = psql -h $dbHost -U $dbUser -d $dbName -c $query2 -t -A -F "|"
    Write-Host $result2

    # Parse result to check if tags are balanced
    $fields = $result2 -split "\|"
    if ($fields.Count -ge 9) {
        $openTags = [int]$fields[7]
        $closeTags = [int]$fields[8]

        if ($openTags -eq $closeTags) {
            Write-Host "✓ Template tags balanced ($openTags opening, $closeTags closing)" -ForegroundColor Green
        } else {
            Write-Host "✗ UNBALANCED TAGS! ($openTags opening, $closeTags closing)" -ForegroundColor Red
        }
    }
} catch {
    Write-Host "✗ Template query failed: $_" -ForegroundColor Red
}

Write-Host ""

# Test 3: Check if there are any email send attempts
Write-Host "Test 3: Checking email send history..." -ForegroundColor Yellow
$query3 = @"
SELECT
    "Id",
    "To",
    "Subject",
    "Status",
    "CreatedAt",
    "ErrorMessage"
FROM communications."EmailMessages"
WHERE "To" LIKE '%niroshhh@gmail.com%'
ORDER BY "CreatedAt" DESC
LIMIT 5;
"@

try {
    $result3 = psql -h $dbHost -U $dbUser -d $dbName -c $query3 -t -A -F "|"
    if ($result3) {
        Write-Host "✓ Email send attempts found:" -ForegroundColor Green
        Write-Host $result3
    } else {
        Write-Host "⚠ No email send attempts found (suggests event not being raised)" -ForegroundColor Yellow
    }
} catch {
    Write-Host "Note: EmailMessages table might not exist" -ForegroundColor Gray
}

Write-Host ""
Write-Host "=== DIAGNOSIS COMPLETE ===" -ForegroundColor Cyan
Write-Host ""
Write-Host "Next Steps:" -ForegroundColor Yellow
Write-Host "1. If registrations exist but no emails -> Event handler not executing"
Write-Host "2. If template tags unbalanced -> Template corruption issue"
Write-Host "3. If no email attempts -> PaymentCompletedEvent not being raised"
