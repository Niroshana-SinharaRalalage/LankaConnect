# Get database connection string from Azure Key Vault
$connString = az keyvault secret show --vault-name lankaconnect-staging-kv --name DATABASE-CONNECTION-STRING --query value -o tsv

# Parse connection string to extract components
if ($connString -match "Host=([^;]+);.*Database=([^;]+);.*Username=([^;]+);.*Password=([^;]+)") {
    $host = $Matches[1]
    $database = $Matches[2]
    $username = $Matches[3]
    $password = $Matches[4]

    Write-Host "Connecting to database: $database on $host"

    # Install npgsql if needed
    if (-not (Get-Module -ListAvailable -Name "Npgsql")) {
        Write-Host "Installing Npgsql module..."
        Install-Module -Name Npgsql -Force -Scope CurrentUser
    }

    # Query the database
    $query = "SELECT name, type, category, is_active, created_at FROM communications.email_templates WHERE name = 'event-cancelled-notification';"

    try {
        # Use Azure CLI to execute query
        $result = az postgres flexible-server execute `
            --name ($host -split '\.')[0] `
            --admin-user $username `
            --admin-password $password `
            --database-name $database `
            --querytext $query `
            --output json | ConvertFrom-Json

        if ($result) {
            Write-Host "`nTemplate found in database:" -ForegroundColor Green
            $result | Format-Table
        } else {
            Write-Host "`nTemplate NOT found in database!" -ForegroundColor Red
        }
    } catch {
        Write-Host "Error querying database: $($_.Exception.Message)" -ForegroundColor Red
    }
} else {
    Write-Host "Failed to parse connection string" -ForegroundColor Red
}
