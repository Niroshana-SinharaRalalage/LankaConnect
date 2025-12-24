# Simple Azure Communication Services Email Test
# Run this to verify Azure Email connectivity without full application

param(
    [string]$RecipientEmail = "test@example.com"
)

Write-Host "Azure Communication Services Email Test" -ForegroundColor Cyan
Write-Host "=======================================" -ForegroundColor Cyan
Write-Host ""

# Configuration from appsettings.json
$connectionString = "endpoint=https://lankaconnect-communication.unitedstates.communication.azure.com/;accesskey=5XTkOE10iioKugbZBPPrQRq2NRkscM5l7SIgi7IBIdhDhQIp2IYhJQQJ99BLACULyCpl1gBuAAAAAZCSEsEs"
$senderAddress = "DoNotReply@7689582e-73cc-4552-b2ff-8afd9d1a6814.azurecomm.net"

Write-Host "Configuration:" -ForegroundColor Yellow
Write-Host "  Endpoint: https://lankaconnect-communication.unitedstates.communication.azure.com/" -ForegroundColor Gray
Write-Host "  Sender: $senderAddress" -ForegroundColor Gray
Write-Host "  Recipient: $RecipientEmail" -ForegroundColor Gray
Write-Host ""

# Create C# script to test
$csharpScript = @"
#r "nuget: Azure.Communication.Email, 1.0.1"

using Azure;
using Azure.Communication.Email;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

var connectionString = @"$connectionString";
var senderAddress = @"$senderAddress";
var recipientEmail = @"$RecipientEmail";

Console.WriteLine("Creating EmailClient...");
var emailClient = new EmailClient(connectionString);

Console.WriteLine("Building test email message...");
var message = new EmailMessage(
    senderAddress,
    new EmailRecipients(new List<EmailAddress> { new EmailAddress(recipientEmail) }),
    new EmailContent("Azure Email Service Test")
    {
        PlainText = "This is a test email from LankaConnect to verify Azure Communication Services connectivity.",
        Html = "<html><body><p>This is a test email from LankaConnect to verify Azure Communication Services connectivity.</p><p>Test Time: " + DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss UTC") + "</p></body></html>"
    }
);

Console.WriteLine("Attempting to send email via Azure SDK...");

try
{
    var operation = await emailClient.SendAsync(WaitUntil.Completed, message);

    Console.WriteLine("");
    Console.WriteLine("=== SUCCESS ===");
    Console.WriteLine("Status: {0}", operation.Value.Status);
    Console.WriteLine("Operation ID: {0}", operation.Id);
    Console.WriteLine("Message ID: {0}", operation.Value.MessageId);
    Console.WriteLine("");
    Console.WriteLine("Azure Communication Services is working correctly!");
    Environment.Exit(0);
}
catch (RequestFailedException ex)
{
    Console.WriteLine("");
    Console.WriteLine("=== AZURE REQUEST FAILED ===");
    Console.WriteLine("Error Code: {0}", ex.ErrorCode);
    Console.WriteLine("HTTP Status: {0}", ex.Status);
    Console.WriteLine("Message: {0}", ex.Message);
    Console.WriteLine("");
    Console.WriteLine("Full Error:");
    Console.WriteLine(ex.ToString());
    Console.WriteLine("");

    Console.WriteLine("DIAGNOSIS:");
    if (ex.Status == 401)
    {
        Console.WriteLine("  > Access key is INVALID or EXPIRED");
        Console.WriteLine("  > Action: Regenerate access key in Azure Portal");
    }
    else if (ex.Status == 403)
    {
        Console.WriteLine("  > Sender address is NOT VERIFIED or FORBIDDEN");
        Console.WriteLine("  > Action: Verify domain in Azure Portal");
    }
    else if (ex.Status == 429)
    {
        Console.WriteLine("  > RATE LIMIT exceeded or QUOTA reached");
        Console.WriteLine("  > Action: Check quota limits in Azure Portal");
    }
    else if (ex.Status >= 500)
    {
        Console.WriteLine("  > Azure service is DOWN or experiencing issues");
        Console.WriteLine("  > Action: Check Azure Status page");
    }
    else
    {
        Console.WriteLine("  > Unknown error - check error details above");
    }

    Environment.Exit(1);
}
catch (Exception ex)
{
    Console.WriteLine("");
    Console.WriteLine("=== GENERAL ERROR ===");
    Console.WriteLine("Type: {0}", ex.GetType().Name);
    Console.WriteLine("Message: {0}", ex.Message);
    Console.WriteLine("");
    Console.WriteLine("Full Error:");
    Console.WriteLine(ex.ToString());
    Environment.Exit(2);
}
"@

# Save to temporary file
$tempFile = "$env:TEMP\azure-email-test.csx"
$csharpScript | Out-File -FilePath $tempFile -Encoding UTF8

Write-Host "Running Azure Email Test..." -ForegroundColor Yellow
Write-Host ""

# Run the C# script using dotnet-script
try {
    # Check if dotnet-script is installed
    $dotnetScriptCheck = Get-Command dotnet-script -ErrorAction SilentlyContinue

    if (-not $dotnetScriptCheck) {
        Write-Host "ERROR: dotnet-script not found" -ForegroundColor Red
        Write-Host ""
        Write-Host "Install with:" -ForegroundColor Yellow
        Write-Host "  dotnet tool install -g dotnet-script" -ForegroundColor Gray
        Write-Host ""
        Write-Host "Alternative: Run the C# code manually" -ForegroundColor Yellow
        Write-Host "  Script saved to: $tempFile" -ForegroundColor Gray
        exit 3
    }

    # Run the test
    & dotnet-script $tempFile

    $exitCode = $LASTEXITCODE
    Write-Host ""

    if ($exitCode -eq 0) {
        Write-Host "RESULT: Azure Email Service is WORKING" -ForegroundColor Green
    }
    else {
        Write-Host "RESULT: Azure Email Service has ISSUES (Exit Code: $exitCode)" -ForegroundColor Red
    }

    exit $exitCode
}
catch {
    Write-Host "ERROR: Failed to run test" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red
    exit 4
}
finally {
    # Clean up temp file
    if (Test-Path $tempFile) {
        Remove-Item $tempFile -Force
    }
}
