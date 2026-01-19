# Architectural Recommendations: Email Template System

**Date**: 2026-01-19
**Author**: SPARC Architecture Agent
**Context**: Phase 6A.51 Missing Email Templates RCA
**Status**: Approved for Implementation

---

## Executive Summary

This document provides architectural recommendations for improving the email template system based on the root cause analysis of missing signup commitment email templates. The recommendations focus on preventing similar issues through better tooling, processes, and architectural patterns.

---

## Table of Contents

1. [Migration Architecture](#migration-architecture)
2. [Email Template Lifecycle Management](#email-template-lifecycle-management)
3. [CI/CD Pipeline Improvements](#cicd-pipeline-improvements)
4. [Testing Strategy](#testing-strategy)
5. [Monitoring and Observability](#monitoring-and-observability)
6. [Developer Experience](#developer-experience)
7. [Implementation Roadmap](#implementation-roadmap)

---

## 1. Migration Architecture

### 1.1 Single Migration Directory

**Problem**: Multiple migration directories cause confusion and deployment issues.

**Current State**:
```
src/LankaConnect.Infrastructure/
├── Migrations/                    # WRONG - Contains Phase6A51
└── Data/
    └── Migrations/                # CORRECT - Main migration path
```

**Recommended State**:
```
src/LankaConnect.Infrastructure/
└── Data/
    └── Migrations/                # ONLY migration directory
```

**Action Items**:
1. Move all migrations from `Infrastructure/Migrations` to `Infrastructure/Data/Migrations`
2. Delete `Infrastructure/Migrations` directory
3. Update EF Core configuration to explicitly set migration path
4. Add build validation to prevent migrations in wrong location

**Implementation**:
```csharp
// Program.cs or DbContext configuration
services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(
        connectionString,
        npgsqlOptions => npgsqlOptions
            .MigrationsAssembly("LankaConnect.Infrastructure")
            .MigrationsHistoryTable("__EFMigrationsHistory", "public")
    )
);
```

### 1.2 Idempotent Migration Pattern

**Problem**: Non-idempotent migrations fail if run multiple times.

**Current Pattern (BAD)**:
```sql
INSERT INTO communications.email_templates (...)
VALUES (...);  -- Fails if template already exists
```

**Recommended Pattern (GOOD)**:
```sql
INSERT INTO communications.email_templates (...)
SELECT ...
WHERE NOT EXISTS (
    SELECT 1 FROM communications.email_templates
    WHERE name = 'template-name'
);
```

**Template for Data Seeding Migrations**:
```csharp
public partial class SeedEmailTemplate_{TemplateName} : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Idempotent insert with existence check
        migrationBuilder.Sql(@"
            INSERT INTO communications.email_templates
            (
                ""Id"",
                ""name"",
                ""description"",
                ""subject_template"",
                ""text_template"",
                ""html_template"",
                ""type"",
                ""category"",
                ""is_active"",
                ""created_at""
            )
            SELECT
                gen_random_uuid(),
                '{template-name}',
                '{description}',
                '{subject}',
                '{text_body}',
                '{html_body}',
                '{type}',
                '{category}',
                true,
                NOW()
            WHERE NOT EXISTS (
                SELECT 1 FROM communications.email_templates
                WHERE name = '{template-name}'
            );
        ");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Safe deletion
        migrationBuilder.Sql(@"
            DELETE FROM communications.email_templates
            WHERE name = '{template-name}';
        ");
    }
}
```

### 1.3 Migration Naming Convention

**Recommended Format**:
```
{Timestamp}_Phase{PhaseNumber}_{Feature}_{Action}.cs

Examples:
20260119_Phase6A51_SignupCommitmentEmails_AddTemplates.cs
20260119_Phase6A54Fix_EmailTemplates_AddMissing.cs
20260120_Phase6A74_Newsletter_UpdateTemplate.cs
```

**Benefits**:
- Clear phase association for tracking
- Descriptive feature name
- Action verb indicates operation type
- Timestamp ensures ordering

### 1.4 Designer File Validation

**Problem**: Missing Designer files cause migrations to be silently skipped.

**Solution**: Build-time validation script

**Implementation**:
```xml
<!-- LankaConnect.Infrastructure.csproj -->
<Target Name="ValidateMigrationDesignerFiles" BeforeTargets="Build">
  <ItemGroup>
    <MigrationFiles Include="Data\Migrations\*_*.cs" Exclude="Data\Migrations\*.Designer.cs;Data\Migrations\*ModelSnapshot.cs" />
  </ItemGroup>

  <Error
    Condition="!Exists('%(MigrationFiles.RootDir)%(MigrationFiles.Directory)%(MigrationFiles.Filename).Designer.cs')"
    Text="Missing Designer file for migration: %(MigrationFiles.Filename)" />
</Target>
```

---

## 2. Email Template Lifecycle Management

### 2.1 Template Registry Pattern

**Problem**: No central registry of all email templates.

**Solution**: Template registry with compile-time validation.

**Implementation**:
```csharp
// src/LankaConnect.Application/Common/Email/EmailTemplateRegistry.cs
public static class EmailTemplateRegistry
{
    // Compile-time constants for template names
    public const string SignupCommitmentConfirmation = "signup-commitment-confirmation";
    public const string SignupCommitmentUpdated = "signup-commitment-updated";
    public const string SignupCommitmentCancelled = "signup-commitment-cancelled";
    public const string MemberEmailVerification = "member-email-verification";
    public const string RegistrationCancellation = "registration-cancellation";
    public const string OrganizerCustomMessage = "organizer-custom-message";

    // Template metadata
    public static readonly Dictionary<string, EmailTemplateMetadata> Templates = new()
    {
        [SignupCommitmentConfirmation] = new()
        {
            Name = SignupCommitmentConfirmation,
            Description = "Confirmation email when user commits to bringing an item",
            Category = EmailCategory.Notification,
            Type = EmailType.SignupCommitmentConfirmation,
            RequiredVariables = new[] { "UserName", "EventTitle", "ItemDescription", "Quantity", "EventDateTime", "EventLocation", "PickupInstructions" },
            Phase = "6A.54",
            MigrationTimestamp = "20260119154406"
        },
        [SignupCommitmentUpdated] = new()
        {
            Name = SignupCommitmentUpdated,
            Description = "Confirmation email when user updates commitment quantity",
            Category = EmailCategory.Notification,
            Type = EmailType.SignupCommitmentUpdate,
            RequiredVariables = new[] { "UserName", "EventTitle", "ItemDescription", "OldQuantity", "NewQuantity", "EventDate", "EventLocation" },
            Phase = "6A.51",
            MigrationTimestamp = "20260118235411"
        },
        // ... other templates
    };

    // Validation method
    public static void ValidateTemplateVariables(string templateName, Dictionary<string, object> variables)
    {
        if (!Templates.TryGetValue(templateName, out var metadata))
            throw new InvalidOperationException($"Unknown template: {templateName}");

        var missing = metadata.RequiredVariables
            .Where(v => !variables.ContainsKey(v))
            .ToArray();

        if (missing.Any())
            throw new ArgumentException(
                $"Template '{templateName}' missing required variables: {string.Join(", ", missing)}");
    }
}

public class EmailTemplateMetadata
{
    public string Name { get; init; }
    public string Description { get; init; }
    public EmailCategory Category { get; init; }
    public EmailType Type { get; init; }
    public string[] RequiredVariables { get; init; }
    public string Phase { get; init; }
    public string MigrationTimestamp { get; init; }
}
```

**Usage in Event Handlers**:
```csharp
// CommitmentUpdatedEventHandler.cs
var templateData = new Dictionary<string, object>
{
    { "UserName", user.FirstName },
    { "EventTitle", @event.Title },
    { "ItemDescription", domainEvent.ItemDescription },
    { "OldQuantity", domainEvent.OldQuantity },
    { "NewQuantity", domainEvent.NewQuantity },
    { "EventDate", @event.StartDate.ToString("f") },
    { "EventLocation", @event.Location?.ToString() ?? "Location TBD" }
};

// Validate before sending
EmailTemplateRegistry.ValidateTemplateVariables(
    EmailTemplateRegistry.SignupCommitmentUpdated,
    templateData);

var result = await _emailService.SendTemplatedEmailAsync(
    EmailTemplateRegistry.SignupCommitmentUpdated,  // Use constant
    user.Email.Value,
    templateData,
    cancellationToken);
```

### 2.2 Template Version Control

**Problem**: Template updates are hard to track and rollback.

**Solution**: Template versioning with migration history.

**Schema Enhancement**:
```sql
ALTER TABLE communications.email_templates
ADD COLUMN version INT DEFAULT 1,
ADD COLUMN updated_at TIMESTAMP,
ADD COLUMN updated_by VARCHAR(255);

-- Create template history table
CREATE TABLE communications.email_template_history (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    template_id UUID NOT NULL REFERENCES communications.email_templates(id),
    version INT NOT NULL,
    name VARCHAR(255) NOT NULL,
    subject_template TEXT NOT NULL,
    text_template TEXT NOT NULL,
    html_template TEXT NOT NULL,
    updated_at TIMESTAMP NOT NULL DEFAULT NOW(),
    updated_by VARCHAR(255),
    migration_applied VARCHAR(255),
    UNIQUE (template_id, version)
);
```

**Migration Pattern for Updates**:
```csharp
protected override void Up(MigrationBuilder migrationBuilder)
{
    // Archive current version to history
    migrationBuilder.Sql(@"
        INSERT INTO communications.email_template_history
        (template_id, version, name, subject_template, text_template, html_template, updated_at, migration_applied)
        SELECT id, version, name, subject_template, text_template, html_template, NOW(), '20260119_TemplateUpdate'
        FROM communications.email_templates
        WHERE name = 'signup-commitment-updated';
    ");

    // Update template and increment version
    migrationBuilder.Sql(@"
        UPDATE communications.email_templates
        SET
            subject_template = '{new subject}',
            html_template = '{new html}',
            version = version + 1,
            updated_at = NOW(),
            updated_by = 'Migration_20260119'
        WHERE name = 'signup-commitment-updated';
    ");
}
```

### 2.3 Template Preview and Testing

**Solution**: Template preview API endpoint for testing.

**Implementation**:
```csharp
// src/LankaConnect.API/Controllers/EmailTemplateController.cs
[ApiController]
[Route("api/admin/email-templates")]
[Authorize(Roles = "Admin")]
public class EmailTemplateController : ControllerBase
{
    private readonly IEmailTemplateService _templateService;

    [HttpPost("{templateName}/preview")]
    public async Task<IActionResult> PreviewTemplate(
        string templateName,
        [FromBody] Dictionary<string, object> variables)
    {
        try
        {
            // Validate template exists
            EmailTemplateRegistry.ValidateTemplateVariables(templateName, variables);

            // Render preview
            var preview = await _templateService.RenderTemplateAsync(
                templateName,
                variables);

            return Ok(new
            {
                subject = preview.Subject,
                htmlBody = preview.HtmlBody,
                textBody = preview.TextBody
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet]
    public async Task<IActionResult> ListTemplates()
    {
        var templates = await _templateService.GetAllTemplatesAsync();
        return Ok(templates.Select(t => new
        {
            name = t.Name,
            description = t.Description,
            requiredVariables = EmailTemplateRegistry.Templates[t.Name].RequiredVariables,
            version = t.Version,
            isActive = t.IsActive
        }));
    }
}
```

---

## 3. CI/CD Pipeline Improvements

### 3.1 Migration Validation Pipeline

**GitHub Actions Workflow**:
```yaml
# .github/workflows/migration-validation.yml
name: Migration Validation

on:
  pull_request:
    paths:
      - 'src/LankaConnect.Infrastructure/Data/Migrations/**'

jobs:
  validate-migrations:
    runs-on: ubuntu-latest

    steps:
      - uses: actions/checkout@v3

      - name: Setup .NET
        uses: actions/setup-dotnet@v3
        with:
          dotnet-version: '8.0.x'

      - name: Check for migrations in wrong directory
        run: |
          if [ -d "src/LankaConnect.Infrastructure/Migrations" ]; then
            echo "ERROR: Migrations found in incorrect directory"
            echo "Please move to src/LankaConnect.Infrastructure/Data/Migrations"
            exit 1
          fi

      - name: Validate Designer files exist
        run: |
          cd src/LankaConnect.Infrastructure/Data/Migrations
          for migration in *_*.cs; do
            # Skip Designer files and ModelSnapshot
            if [[ "$migration" == *.Designer.cs ]] || [[ "$migration" == *ModelSnapshot.cs ]]; then
              continue
            fi

            designer="${migration%.cs}.Designer.cs"
            if [ ! -f "$designer" ]; then
              echo "ERROR: Missing Designer file for $migration"
              exit 1
            fi
          done

      - name: Check for idempotent pattern in email template migrations
        run: |
          cd src/LankaConnect.Infrastructure/Data/Migrations
          for migration in *EmailTemplate*.cs; do
            if grep -q "INSERT INTO communications.email_templates" "$migration"; then
              if ! grep -q "WHERE NOT EXISTS" "$migration"; then
                echo "WARNING: Migration $migration may not be idempotent"
                echo "Email template migrations should use WHERE NOT EXISTS pattern"
              fi
            fi
          done

      - name: Restore dependencies
        run: dotnet restore

      - name: Build
        run: dotnet build --no-restore --configuration Release

      - name: Run migration dry-run
        run: |
          dotnet ef migrations script \
            --project src/LankaConnect.Infrastructure \
            --startup-project src/LankaConnect.API \
            --context AppDbContext \
            --idempotent \
            --output migrations.sql

      - name: Upload migration script
        uses: actions/upload-artifact@v3
        with:
          name: migration-script
          path: migrations.sql
```

### 3.2 Email Template Validation

**Add to CI pipeline**:
```yaml
      - name: Validate email template references
        run: |
          # Extract template names from registry
          REGISTRY_TEMPLATES=$(grep -oP 'public const string \w+ = "\K[^"]+' \
            src/LankaConnect.Application/Common/Email/EmailTemplateRegistry.cs)

          # Check each template has corresponding migration
          for template in $REGISTRY_TEMPLATES; do
            if ! grep -r "name = '$template'" \
              src/LankaConnect.Infrastructure/Data/Migrations/*.cs; then
              echo "ERROR: Template '$template' not found in any migration"
              exit 1
            fi
          done
```

### 3.3 Database State Verification

**Post-deployment validation**:
```yaml
# .github/workflows/post-deploy-validation.yml
name: Post-Deploy Validation

on:
  deployment_status:
    types: [success]

jobs:
  validate-database:
    runs-on: ubuntu-latest
    if: github.event.deployment_status.state == 'success'

    steps:
      - uses: actions/checkout@v3

      - name: Verify email templates exist
        env:
          DATABASE_URL: ${{ secrets.STAGING_DATABASE_URL }}
        run: |
          # Get list of required templates from registry
          TEMPLATES=$(grep -oP 'public const string \w+ = "\K[^"]+' \
            src/LankaConnect.Application/Common/Email/EmailTemplateRegistry.cs)

          # Check each template exists in database
          for template in $TEMPLATES; do
            COUNT=$(psql $DATABASE_URL -t -c \
              "SELECT COUNT(*) FROM communications.email_templates WHERE name = '$template' AND is_active = true;")

            if [ "$COUNT" -eq "0" ]; then
              echo "ERROR: Template '$template' not found in staging database"
              exit 1
            fi
          done

          echo "All email templates validated successfully"
```

---

## 4. Testing Strategy

### 4.1 Integration Tests for Email Templates

**Test Suite**:
```csharp
// tests/LankaConnect.IntegrationTests/Email/EmailTemplateTests.cs
public class EmailTemplateTests : IntegrationTestBase
{
    [Fact]
    public async Task AllRegisteredTemplates_ShouldExistInDatabase()
    {
        // Arrange
        var templateNames = EmailTemplateRegistry.Templates.Keys;

        // Act & Assert
        foreach (var templateName in templateNames)
        {
            var template = await DbContext.EmailTemplates
                .FirstOrDefaultAsync(t => t.Name == templateName);

            Assert.NotNull(template);
            Assert.True(template.IsActive, $"Template '{templateName}' is not active");
            Assert.NotEmpty(template.SubjectTemplate);
            Assert.NotEmpty(template.HtmlTemplate);
            Assert.NotEmpty(template.TextTemplate);
        }
    }

    [Theory]
    [InlineData(EmailTemplateRegistry.SignupCommitmentUpdated)]
    [InlineData(EmailTemplateRegistry.SignupCommitmentCancelled)]
    [InlineData(EmailTemplateRegistry.SignupCommitmentConfirmation)]
    public async Task SignupCommitmentTemplates_ShouldRenderWithSampleData(string templateName)
    {
        // Arrange
        var metadata = EmailTemplateRegistry.Templates[templateName];
        var sampleData = GenerateSampleDataForTemplate(metadata);

        // Act
        var result = await EmailService.SendTemplatedEmailAsync(
            templateName,
            "test@example.com",
            sampleData,
            CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess, $"Failed to send {templateName}: {result.Error}");
    }

    [Fact]
    public async Task TemplateRegistry_ShouldMatchDatabaseTemplates()
    {
        // Arrange
        var dbTemplates = await DbContext.EmailTemplates
            .Where(t => t.IsActive)
            .Select(t => t.Name)
            .ToListAsync();

        var registryTemplates = EmailTemplateRegistry.Templates.Keys.ToList();

        // Act
        var missingInRegistry = dbTemplates.Except(registryTemplates).ToList();
        var missingInDb = registryTemplates.Except(dbTemplates).ToList();

        // Assert
        Assert.Empty(missingInRegistry);
        Assert.Empty(missingInDb);
    }

    private Dictionary<string, object> GenerateSampleDataForTemplate(EmailTemplateMetadata metadata)
    {
        var data = new Dictionary<string, object>();

        foreach (var variable in metadata.RequiredVariables)
        {
            data[variable] = variable switch
            {
                "UserName" => "Test User",
                "EventTitle" => "Sample Event",
                "ItemDescription" => "Test Item",
                "Quantity" or "OldQuantity" or "NewQuantity" => 5,
                "EventDate" or "EventDateTime" => DateTime.UtcNow.ToString("f"),
                "EventLocation" => "Test Location",
                "PickupInstructions" => "Sample pickup instructions",
                _ => $"Sample {variable}"
            };
        }

        return data;
    }
}
```

### 4.2 Unit Tests for Event Handlers

```csharp
// tests/LankaConnect.UnitTests/Events/CommitmentUpdatedEventHandlerTests.cs
public class CommitmentUpdatedEventHandlerTests
{
    private readonly Mock<IEmailService> _emailServiceMock;
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly Mock<IEventRepository> _eventRepositoryMock;
    private readonly CommitmentUpdatedEventHandler _handler;

    public CommitmentUpdatedEventHandlerTests()
    {
        _emailServiceMock = new Mock<IEmailService>();
        _userRepositoryMock = new Mock<IUserRepository>();
        _eventRepositoryMock = new Mock<IEventRepository>();

        _handler = new CommitmentUpdatedEventHandler(
            _emailServiceMock.Object,
            _userRepositoryMock.Object,
            _eventRepositoryMock.Object,
            Mock.Of<ILogger<CommitmentUpdatedEventHandler>>());
    }

    [Fact]
    public async Task Handle_ShouldSendEmail_WithCorrectTemplateAndVariables()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var signUpItemId = Guid.NewGuid();
        var domainEvent = new CommitmentUpdatedEvent(
            signUpItemId,
            userId,
            oldQuantity: 5,
            newQuantity: 10,
            itemDescription: "Water Bottles",
            DateTime.UtcNow);

        var user = CreateTestUser(userId);
        var @event = CreateTestEvent();

        _userRepositoryMock
            .Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _eventRepositoryMock
            .Setup(x => x.GetEventBySignUpItemIdAsync(signUpItemId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(@event);

        _emailServiceMock
            .Setup(x => x.SendTemplatedEmailAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<Dictionary<string, object>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        // Act
        await _handler.Handle(
            new DomainEventNotification<CommitmentUpdatedEvent>(domainEvent),
            CancellationToken.None);

        // Assert
        _emailServiceMock.Verify(
            x => x.SendTemplatedEmailAsync(
                EmailTemplateRegistry.SignupCommitmentUpdated,
                user.Email.Value,
                It.Is<Dictionary<string, object>>(d =>
                    d["UserName"].ToString() == user.FirstName &&
                    d["EventTitle"].ToString() == @event.Title &&
                    d["ItemDescription"].ToString() == "Water Bottles" &&
                    (int)d["OldQuantity"] == 5 &&
                    (int)d["NewQuantity"] == 10),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
```

---

## 5. Monitoring and Observability

### 5.1 Email Template Metrics

**Add telemetry to email service**:
```csharp
// src/LankaConnect.Infrastructure/Services/EmailService.cs
public class EmailService : IEmailService
{
    private readonly ILogger<EmailService> _logger;
    private readonly IMetrics _metrics;

    public async Task<Result> SendTemplatedEmailAsync(
        string templateName,
        string toEmail,
        Dictionary<string, object> templateData,
        CancellationToken cancellationToken)
    {
        using var activity = Activity.Current?.Source.StartActivity("EmailService.SendTemplatedEmail");
        activity?.SetTag("template.name", templateName);
        activity?.SetTag("recipient.email", toEmail);

        var stopwatch = Stopwatch.StartNew();

        try
        {
            // Get template
            var template = await GetTemplateAsync(templateName, cancellationToken);

            if (template == null)
            {
                _logger.LogError(
                    "Email template not found: {TemplateName}",
                    templateName);

                _metrics.Increment(
                    "email.template.missing",
                    tags: new[] { $"template:{templateName}" });

                return Result.Failure($"Email template '{templateName}' not found");
            }

            // Validate variables
            EmailTemplateRegistry.ValidateTemplateVariables(templateName, templateData);

            // Render and send
            var result = await RenderAndSendAsync(template, toEmail, templateData, cancellationToken);

            stopwatch.Stop();

            if (result.IsSuccess)
            {
                _metrics.Increment(
                    "email.sent.success",
                    tags: new[] { $"template:{templateName}" });

                _metrics.Histogram(
                    "email.send.duration",
                    stopwatch.ElapsedMilliseconds,
                    tags: new[] { $"template:{templateName}" });
            }
            else
            {
                _metrics.Increment(
                    "email.sent.failure",
                    tags: new[] { $"template:{templateName}", $"error:{result.Error}" });
            }

            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            _logger.LogError(ex,
                "Error sending templated email: {TemplateName}",
                templateName);

            _metrics.Increment(
                "email.sent.error",
                tags: new[] { $"template:{templateName}", $"exception:{ex.GetType().Name}" });

            return Result.Failure(ex.Message);
        }
    }
}
```

### 5.2 Dashboard Metrics

**Key metrics to track**:
- Email send success rate by template
- Missing template errors
- Template render time
- Email delivery rate
- Template usage frequency
- Variable validation failures

**Prometheus Query Examples**:
```promql
# Email send success rate by template
rate(email_sent_success_total[5m]) / rate(email_sent_total[5m])

# Missing template errors
sum(rate(email_template_missing_total[5m])) by (template)

# Template render time p95
histogram_quantile(0.95, rate(email_send_duration_bucket[5m]))
```

### 5.3 Alerting Rules

**Critical Alerts**:
```yaml
# prometheus/alerts/email-templates.yml
groups:
  - name: email_templates
    interval: 1m
    rules:
      - alert: EmailTemplateMissing
        expr: rate(email_template_missing_total[5m]) > 0
        for: 1m
        labels:
          severity: critical
        annotations:
          summary: "Email template not found: {{ $labels.template }}"
          description: "Template {{ $labels.template }} is missing from database"

      - alert: EmailSendFailureRate
        expr: |
          rate(email_sent_failure_total[5m])
          / rate(email_sent_total[5m]) > 0.1
        for: 5m
        labels:
          severity: warning
        annotations:
          summary: "High email send failure rate"
          description: "Email send failure rate is {{ $value | humanizePercentage }}"

      - alert: EmailTemplateVariableValidationFailure
        expr: rate(email_template_variable_validation_error_total[5m]) > 0
        for: 1m
        labels:
          severity: warning
        annotations:
          summary: "Email template variable validation failing"
          description: "Template {{ $labels.template }} has missing variables"
```

---

## 6. Developer Experience

### 6.1 Email Template CLI Tool

**Create developer tool for template management**:
```bash
# scripts/email-template-cli.sh
#!/bin/bash

case "$1" in
  create)
    dotnet run --project tools/EmailTemplateCLI -- create \
      --name "$2" \
      --description "$3" \
      --phase "$4"
    ;;

  preview)
    dotnet run --project tools/EmailTemplateCLI -- preview \
      --template "$2" \
      --variables-file "$3"
    ;;

  validate)
    dotnet run --project tools/EmailTemplateCLI -- validate
    ;;

  list)
    dotnet run --project tools/EmailTemplateCLI -- list
    ;;

  *)
    echo "Usage: email-template-cli.sh {create|preview|validate|list}"
    exit 1
    ;;
esac
```

### 6.2 Migration Generator Extension

**Custom migration generator with validation**:
```csharp
// tools/MigrationGenerator/EmailTemplateMigrationGenerator.cs
public class EmailTemplateMigrationGenerator
{
    public void GenerateMigration(EmailTemplateDefinition definition)
    {
        // Validate template definition
        ValidateDefinition(definition);

        // Generate migration file
        var migrationContent = GenerateMigrationContent(definition);

        // Use idempotent pattern
        var migrationPath = Path.Combine(
            "src",
            "LankaConnect.Infrastructure",
            "Data",
            "Migrations",
            $"{DateTime.UtcNow:yyyyMMddHHmmss}_Phase{definition.Phase}_{definition.Name}_AddTemplate.cs");

        File.WriteAllText(migrationPath, migrationContent);

        // Update registry
        UpdateRegistry(definition);

        Console.WriteLine($"Migration created: {migrationPath}");
        Console.WriteLine("Next steps:");
        Console.WriteLine("1. Review migration file");
        Console.WriteLine("2. Run: dotnet ef migrations add");
        Console.WriteLine("3. Test locally");
        Console.WriteLine("4. Commit and push");
    }

    private string GenerateMigrationContent(EmailTemplateDefinition definition)
    {
        return $@"
using Microsoft.EntityFrameworkCore.Migrations;

public partial class Phase{definition.Phase}_{definition.Name}_AddTemplate : Migration
{{
    protected override void Up(MigrationBuilder migrationBuilder)
    {{
        // {definition.Description}
        migrationBuilder.Sql(@""
            INSERT INTO communications.email_templates
            (
                ""Id"",
                ""name"",
                ""description"",
                ""subject_template"",
                ""text_template"",
                ""html_template"",
                ""type"",
                ""category"",
                ""is_active"",
                ""created_at""
            )
            SELECT
                gen_random_uuid(),
                '{definition.TemplateName}',
                '{definition.Description}',
                '{definition.Subject}',
                '{definition.TextBody}',
                '{definition.HtmlBody}',
                '{definition.Type}',
                '{definition.Category}',
                true,
                NOW()
            WHERE NOT EXISTS (
                SELECT 1 FROM communications.email_templates
                WHERE name = '{definition.TemplateName}'
            );
        "");
    }}

    protected override void Down(MigrationBuilder migrationBuilder)
    {{
        migrationBuilder.Sql(@""
            DELETE FROM communications.email_templates
            WHERE name = '{definition.TemplateName}';
        "");
    }}
}}";
    }
}
```

### 6.3 Documentation Generation

**Auto-generate template documentation**:
```csharp
// tools/DocumentationGenerator/EmailTemplateDocGenerator.cs
public class EmailTemplateDocGenerator
{
    public void GenerateDocumentation()
    {
        var markdown = new StringBuilder();

        markdown.AppendLine("# Email Templates");
        markdown.AppendLine();
        markdown.AppendLine("Auto-generated from EmailTemplateRegistry");
        markdown.AppendLine();

        foreach (var (name, metadata) in EmailTemplateRegistry.Templates)
        {
            markdown.AppendLine($"## {name}");
            markdown.AppendLine();
            markdown.AppendLine($"**Description**: {metadata.Description}");
            markdown.AppendLine();
            markdown.AppendLine($"**Phase**: {metadata.Phase}");
            markdown.AppendLine();
            markdown.AppendLine($"**Migration**: {metadata.MigrationTimestamp}");
            markdown.AppendLine();
            markdown.AppendLine("**Required Variables**:");

            foreach (var variable in metadata.RequiredVariables)
            {
                markdown.AppendLine($"- `{variable}`");
            }

            markdown.AppendLine();
        }

        File.WriteAllText("docs/EMAIL_TEMPLATES.md", markdown.ToString());
    }
}
```

---

## 7. Implementation Roadmap

### Phase 1: Immediate Fixes (Week 1)

**Priority**: Critical
**Duration**: 1 week

- [x] Create RCA document
- [ ] Create Phase6A54Fix_Part2 migration
- [ ] Test migration locally
- [ ] Deploy to staging
- [ ] Verify emails send successfully
- [ ] Update PROGRESS_TRACKER.md

### Phase 2: Foundation (Week 2-3)

**Priority**: High
**Duration**: 2 weeks

- [ ] Consolidate migration directories
- [ ] Create EmailTemplateRegistry
- [ ] Add Designer file validation to build
- [ ] Create email template integration tests
- [ ] Add CI/CD migration validation pipeline

### Phase 3: Monitoring (Week 4)

**Priority**: Medium
**Duration**: 1 week

- [ ] Add telemetry to EmailService
- [ ] Create Prometheus metrics
- [ ] Set up Grafana dashboards
- [ ] Configure alerting rules
- [ ] Add template usage tracking

### Phase 4: Developer Tools (Week 5-6)

**Priority**: Low
**Duration**: 2 weeks

- [ ] Create email template CLI tool
- [ ] Build migration generator
- [ ] Add template preview API
- [ ] Generate documentation automation
- [ ] Create developer guide

### Phase 5: Advanced Features (Week 7-8)

**Priority**: Nice to Have
**Duration**: 2 weeks

- [ ] Implement template versioning
- [ ] Add template history tracking
- [ ] Create template A/B testing framework
- [ ] Build template analytics dashboard
- [ ] Add template localization support

---

## Conclusion

These architectural recommendations address the root causes of the Phase 6A.51 email template issues and provide a robust foundation for email template management going forward. The implementation roadmap ensures critical fixes are deployed immediately while building systematic improvements over time.

**Key Takeaways**:
1. Single migration directory eliminates confusion
2. Idempotent migrations prevent deployment failures
3. Template registry provides compile-time safety
4. Automated validation catches issues early
5. Monitoring and alerting provide visibility
6. Developer tools improve productivity

By following these recommendations, we can prevent similar issues in the future and build a more robust, maintainable email system.

---

**Approved for Implementation**: 2026-01-19
**Review Date**: 2026-03-19 (2 months)