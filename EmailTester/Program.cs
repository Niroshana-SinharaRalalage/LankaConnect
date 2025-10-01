using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using LankaConnect.Infrastructure.Email.Configuration;
using LankaConnect.Infrastructure.Email.Interfaces;
using LankaConnect.Infrastructure.Email.Services;

var builder = Host.CreateApplicationBuilder(args);

// Configure logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();

// Configure email settings
builder.Services.Configure<EmailSettings>(options =>
{
    options.SmtpServer = "localhost";
    options.SmtpPort = 1025;
    options.SenderEmail = "test@lankaconnect.local";
    options.SenderName = "LankaConnect Tester";
    options.Username = "";
    options.Password = "";
    options.EnableSsl = false;
    options.TimeoutInSeconds = 30;
    options.IsDevelopment = true;
    options.SaveEmailsToFile = true;
    options.EmailSaveDirectory = "EmailOutput";
    options.TemplateBasePath = "Templates";
    options.CacheTemplates = true;
});

// Register services
builder.Services.AddScoped<ISimpleEmailService, SimpleEmailService>();
builder.Services.AddScoped<IEmailTemplateService, RazorEmailTemplateService>();

var host = builder.Build();

// Test email service
using var scope = host.Services.CreateScope();
var emailService = scope.ServiceProvider.GetRequiredService<ISimpleEmailService>();
var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

logger.LogInformation("=== LankaConnect Email Service Tester ===");
logger.LogInformation("Testing email service configuration...");

// Test 1: Connection test
logger.LogInformation("1. Testing SMTP connection...");
var connectionResult = await emailService.TestConnectionAsync();
logger.LogInformation("Connection test result: {Result}", connectionResult ? "SUCCESS" : "FAILED");

if (connectionResult)
{
    // Test 2: Simple text email
    logger.LogInformation("2. Testing simple text email...");
    var textResult = await emailService.SendEmailAsync(
        "recipient@example.com",
        "LankaConnect Test - Simple Text Email",
        "This is a simple text email test from LankaConnect email service."
    );
    logger.LogInformation("Simple text email result: {Result}", textResult ? "SUCCESS" : "FAILED");

    // Test 3: HTML email
    logger.LogInformation("3. Testing HTML email...");
    var htmlResult = await emailService.SendEmailAsync(
        "recipient@example.com",
        "LankaConnect Test - HTML Email",
        "This is the plain text version of the email.",
        "<html><body><h2>HTML Email Test</h2><p>This is the <strong>HTML version</strong> of the email.</p><p>Time sent: " + DateTime.Now + "</p></body></html>",
        "Test Recipient"
    );
    logger.LogInformation("HTML email result: {Result}", htmlResult ? "SUCCESS" : "FAILED");

    // Test 4: Template email (if templates exist)
    logger.LogInformation("4. Testing templated email...");
    var templateData = new Dictionary<string, object>
    {
        ["Name"] = "John Doe",
        ["Email"] = "john.doe@example.com",
        ["Date"] = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
        ["SiteUrl"] = "https://lankaconnect.lk"
    };

    var templateResult = await emailService.SendTemplatedEmailAsync(
        "john.doe@example.com",
        "welcome",
        templateData
    );
    logger.LogInformation("Template email result: {Result}", templateResult ? "SUCCESS" : "FAILED");
}
else
{
    logger.LogError("SMTP connection failed. Please check your MailHog server is running on localhost:1025");
    logger.LogInformation("To start MailHog, run: docker run -p 1025:1025 -p 8025:8025 mailhog/mailhog");
}

logger.LogInformation("=== Email Service Test Complete ===");
logger.LogInformation("Check the EmailOutput directory for saved emails (if file saving is enabled)");
logger.LogInformation("Check MailHog web interface at http://localhost:8025 to see sent emails");

await host.StopAsync();