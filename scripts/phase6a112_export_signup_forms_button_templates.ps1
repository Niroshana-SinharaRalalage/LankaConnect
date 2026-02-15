# Phase 6A.112/113: Export 14 email templates for "View Signup Forms" button addition
# These templates will be modified to add the {{#HasSignupForms}} conditional button

$stagingConnectionString = "Host=lankaconnect-staging-db.postgres.database.azure.com;Database=LankaConnectDB;Username=adminuser;Password=1qaz!QAZ;SslMode=Require"

# 14 template names that need "View Signup Forms" button
# Derived from EmailParams classes with HasSignupForms property
$templateNames = @(
    'template-free-event-registration-confirmation',              # FreeEventRegistrationEmailParams
    'template-paid-event-registration-confirmation-with-ticket',  # TicketConfirmationEmailParams
    'template-event-reminder',                                    # EventReminderEmailParams
    'template-event-registration-cancellation',                   # RegistrationCancellationEmailParams
    'template-attendees-added-confirmation',                      # AttendeesAddedEmailParams
    'template-event-cancellation-notifications',                  # EventCancellationEmailParams
    'template-new-event-publication',                             # EventPublishedEmailParams
    'template-newsletter-notification',                           # NewsletterEmailParams
    'template-signup-list-commitment-confirmation',               # SignupCommitmentEmailParams (3 templates)
    'template-signup-list-commitment-update',
    'template-signup-list-commitment-cancellation',
    'template-refund-requested',                                  # RefundEmailParams (2 templates)
    'template-refund-completed',
    'template-event-details-publication'                          # EventDetailsEmailParams
)

Write-Host "Phase 6A.112/113: Exporting 14 templates for 'View Signup Forms' button addition" -ForegroundColor Cyan
Write-Host "Templates to export:" -ForegroundColor Yellow
$templateNames | ForEach-Object { Write-Host "  - $_" }
Write-Host ""

# Build SQL query
$templateList = ($templateNames | ForEach-Object { "'$_'" }) -join ', '

$sql = @"
SELECT
    name,
    subject,
    html_body,
    text_body,
    description,
    is_active,
    created_at,
    updated_at
FROM communications.email_templates
WHERE name IN ($templateList)
ORDER BY name;
"@

Write-Host "Connecting to staging database..." -ForegroundColor Cyan

# Execute query using psql
$env:PGPASSWORD = "1qaz!QAZ"
$output = & psql -h lankaconnect-staging-db.postgres.database.azure.com `
                -U adminuser `
                -d LankaConnectDB `
                -c $sql `
                --csv

# Save to file
$outputFile = "c:\Work\LankaConnect\scripts\phase6a112_signup_forms_button_templates.csv"
$output | Out-File -FilePath $outputFile -Encoding UTF8

Write-Host "✅ Exported 14 templates to: $outputFile" -ForegroundColor Green
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Yellow
Write-Host "1. Review exported templates"
Write-Host "2. Identify where to add {{#HasSignupForms}} button"
Write-Host "3. Create migration with template updates"

# Also export as JSON for easier processing
$jsonSql = @"
SELECT json_agg(t)
FROM (
    SELECT
        name,
        subject,
        html_body,
        text_body,
        description
    FROM communications.email_templates
    WHERE name IN ($templateList)
    ORDER BY name
) t;
"@

$jsonOutput = & psql -h lankaconnect-staging-db.postgres.database.azure.com `
                    -U adminuser `
                    -d LankaConnectDB `
                    -c $jsonSql `
                    -t `
                    -A

$jsonFile = "c:\Work\LankaConnect\scripts\phase6a112_signup_forms_button_templates.json"
$jsonOutput | Out-File -FilePath $jsonFile -Encoding UTF8

Write-Host "✅ Exported JSON to: $jsonFile" -ForegroundColor Green

# Clean up password environment variable
Remove-Item env:PGPASSWORD
