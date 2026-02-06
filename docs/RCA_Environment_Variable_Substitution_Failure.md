# Root Cause Analysis: Environment Variable Substitution Failure in Azure Container Apps

**Date:** 2026-01-01
**Issue:** Connection string error showing literal `${AZURE_EMAIL_CONNECTION_STRING}` instead of env var substitution
**Impact:** ALL deployments failing since 2025-12-31 16:13:20 (10 consecutive failures)
**Status:** CRITICAL - Production deployment blocked

---

## Executive Summary

Environment variable substitution in `appsettings.Production.json` has stopped working in Azure Container Apps. The application is treating the placeholder `${AZURE_EMAIL_CONNECTION_STRING}` as a literal string instead of performing environment variable substitution.

**Key Finding:** .NET configuration system does NOT automatically perform `${VAR}` substitution. This worked previously only if:
1. Azure Container Apps was performing pre-processing on the JSON, OR
2. Custom configuration code was handling variable expansion, OR
3. A different mechanism (e.g., Key Vault references) was being used

---

## Problem Analysis

### 1. What Stopped Working?

**Last successful deployment:** 2025-12-31 16:13:20 (commit 6cbad8c2 or earlier)
**First failure:** Shortly after, likely around Phase 6A.53 work

**Error Message:**
```
Connection string doesn't have value for keyword '${AZURE_EMAIL_CONNECTION_STRING}'
```

This indicates the Azure Communication Services SDK is receiving the literal placeholder string instead of the actual connection string value.

### 2. Configuration Structure Analysis

#### Current Production Configuration (appsettings.Production.json):
```json
{
  "EmailSettings": {
    "Provider": "Azure",
    "AzureConnectionString": "${AZURE_EMAIL_CONNECTION_STRING}",
    "AzureSenderAddress": "${AZURE_EMAIL_SENDER_ADDRESS}",
    // ... other settings
  }
}
```

#### How Configuration is Loaded (Program.cs lines 19-25):
```csharp
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(new ConfigurationBuilder()
        .SetBasePath(Directory.GetCurrentDirectory())
        .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
        .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development"}.json", optional: true, reloadOnChange: true)
        .AddEnvironmentVariables()
        .Build())
```

#### How Email Service Receives Configuration (AzureEmailService.cs lines 30-54):
```csharp
public AzureEmailService(
    ILogger<AzureEmailService> logger,
    IEmailMessageRepository emailMessageRepository,
    IEmailTemplateRepository emailTemplateRepository,
    IOptions<EmailSettings> emailSettings)  // ← Receives configuration via Options pattern
{
    _emailSettings = emailSettings.Value;

    // This check will PASS even with "${AZURE_EMAIL_CONNECTION_STRING}" literal string
    if (_emailSettings.Provider.Equals("Azure", StringComparison.OrdinalIgnoreCase) &&
        !string.IsNullOrEmpty(_emailSettings.AzureConnectionString))
    {
        _azureEmailClient = new EmailClient(_emailSettings.AzureConnectionString);
        // ↑ Azure SDK receives "${AZURE_EMAIL_CONNECTION_STRING}" literally
    }
}
```

#### How Configuration is Bound (DependencyInjection.cs line 211):
```csharp
services.Configure<EmailSettings>(configuration.GetSection(EmailSettings.SectionName));
```

---

## Root Cause: Missing Environment Variable Expansion

### Why `${VAR}` Syntax Doesn't Work

**FACT:** .NET's `IConfiguration` system does NOT natively support `${VARIABLE_NAME}` placeholder expansion in JSON files.

The configuration stack works as follows:
1. **AddJsonFile()** - Reads JSON literally (no substitution)
2. **AddEnvironmentVariables()** - Adds env vars as separate config keys
3. **GetSection()** - Returns the literal JSON value `"${AZURE_EMAIL_CONNECTION_STRING}"`

**What Actually Works in .NET:**
- **Hierarchical override**: Env var `EmailSettings__AzureConnectionString` overrides JSON value
- **Direct access**: `configuration["AZURE_EMAIL_CONNECTION_STRING"]` reads env var directly
- **NOT supported**: `${VARIABLE_NAME}` placeholder expansion in JSON

### Previous Working State - Possible Explanations

The fact this worked before means ONE of the following was true:

#### Theory 1: Azure Container Apps JSON Preprocessing (MOST LIKELY)
Some Azure deployment configurations support environment variable expansion in JSON config files BEFORE the application starts. This may have been:
- An Azure Container Apps feature that was removed/changed
- A deployment manifest setting that got modified
- An ARM template feature that's no longer active

**Evidence for this theory:**
- No custom code in our codebase performs `${VAR}` expansion
- Configuration loading is standard .NET (no preprocessing)
- Multiple docs reference `${VAR}` syntax in Azure environments

#### Theory 2: Configuration Was Different
Previously, the production config may have used:
- Direct environment variable keys (no placeholders)
- Azure Key Vault references (`@Microsoft.KeyVault(...)` syntax)
- Different configuration binding mechanism

**Evidence against this theory:**
- Current `appsettings.Production.json` shows `${VAR}` syntax
- No git history changes to email configuration structure (no commits found)

#### Theory 3: Custom Configuration Provider
There may have been custom configuration code that:
- Performed post-processing on configuration values
- Expanded `${VAR}` placeholders after binding
- This code was removed or stopped working

**Evidence against this theory:**
- No such code found in current codebase
- Program.cs configuration loading is standard

---

## Why This Breaks Deployment

### Failure Chain

1. **Azure Container App starts** with env vars set correctly:
   - `AZURE_EMAIL_CONNECTION_STRING=endpoint=https://...;accesskey=...`

2. **Configuration loads** from `appsettings.Production.json`:
   - `EmailSettings:AzureConnectionString = "${AZURE_EMAIL_CONNECTION_STRING}"` (literal)

3. **Options pattern binds** via `Configure<EmailSettings>()`:
   - `_emailSettings.AzureConnectionString = "${AZURE_EMAIL_CONNECTION_STRING}"` (literal)

4. **AzureEmailService initializes**:
   - String is not empty ✅ (passes null check)
   - `new EmailClient("${AZURE_EMAIL_CONNECTION_STRING}")` ❌ (invalid connection string)

5. **First email send attempt**:
   - Azure SDK parses connection string
   - Fails with: `"Connection string doesn't have value for keyword '${AZURE_EMAIL_CONNECTION_STRING}'"`

### Why Health Checks Pass

The application may pass health checks because:
- Email service instantiation succeeds (constructor doesn't validate connection string)
- Health check doesn't test email sending
- Database and Redis checks pass independently

---

## Architecture Issues Identified

### 1. **Configuration Binding Without Validation**

**Problem:** `EmailSettings` binds successfully with invalid values.

**Current Code (DependencyInjection.cs:211):**
```csharp
services.Configure<EmailSettings>(configuration.GetSection(EmailSettings.SectionName));
```

**Issue:** No validation that `AzureConnectionString` contains an actual connection string vs. placeholder.

### 2. **Late-Bound Failure**

**Problem:** Azure client initialization happens in constructor but connection string validity isn't checked until first use.

**Current Code (AzureEmailService.cs:45):**
```csharp
_azureEmailClient = new EmailClient(_emailSettings.AzureConnectionString);
```

**Issue:** `EmailClient` constructor doesn't validate connection string format. Failure occurs on first send attempt, potentially hours after deployment.

### 3. **No Startup Validation**

**Problem:** Application starts successfully even with broken email configuration.

**Missing:** Startup health check for email service that attempts actual Azure connection validation.

---

## Systematic Solution Approaches

### Option 1: Use .NET Standard Environment Variable Override (RECOMMENDED)

**Solution:** Remove `${VAR}` placeholders, rely on hierarchical configuration override.

**Changes Required:**

**appsettings.Production.json:**
```json
{
  "EmailSettings": {
    "Provider": "Azure",
    "AzureConnectionString": "",  // ← Empty/default, will be overridden
    "AzureSenderAddress": "",
    // ... other settings
  }
}
```

**Azure Container Apps Environment Variables:**
```bash
EmailSettings__AzureConnectionString=endpoint=https://...;accesskey=...
EmailSettings__AzureSenderAddress=DoNotReply@...
```

**Why This Works:**
- .NET configuration hierarchy: Env vars override JSON values
- Key format: `Section__SubSection__Property` maps to `Section:SubSection:Property`
- Native .NET feature, no custom code needed

**Pros:**
- Uses standard .NET configuration patterns
- No dependency on Azure-specific preprocessing
- Clear separation: defaults in JSON, secrets in env vars

**Cons:**
- Env var names are longer (require `EmailSettings__` prefix)
- Need to update all env var names in Azure Container Apps config

---

### Option 2: Implement Custom Variable Expansion

**Solution:** Add custom configuration source that expands `${VAR}` placeholders.

**Implementation:**
```csharp
// Add custom configuration source
public class EnvironmentVariableExpansionConfigurationSource : IConfigurationSource
{
    public IConfigurationProvider Build(IConfigurationBuilder builder)
    {
        return new EnvironmentVariableExpansionConfigurationProvider();
    }
}

public class EnvironmentVariableExpansionConfigurationProvider : ConfigurationProvider
{
    public override void Load()
    {
        // Post-process all configuration values
        foreach (var key in Data.Keys.ToList())
        {
            var value = Data[key];
            if (!string.IsNullOrEmpty(value) && value.Contains("${"))
            {
                Data[key] = ExpandEnvironmentVariables(value);
            }
        }
    }

    private string ExpandEnvironmentVariables(string input)
    {
        var pattern = @"\$\{([^}]+)\}";
        return System.Text.RegularExpressions.Regex.Replace(input, pattern, match =>
        {
            var varName = match.Groups[1].Value;
            return Environment.GetEnvironmentVariable(varName) ?? match.Value;
        });
    }
}

// Program.cs
builder.Configuration.Sources.Insert(
    builder.Configuration.Sources.Count,
    new EnvironmentVariableExpansionConfigurationSource());
```

**Pros:**
- Keeps current `${VAR}` syntax in JSON files
- Self-documenting (clear what env vars are needed)
- Single place to manage expansion logic

**Cons:**
- Custom code to maintain
- Runs on every configuration reload
- May have edge cases with complex variable expansion

---

### Option 3: Add Startup Validation (COMPLEMENT TO OTHER OPTIONS)

**Solution:** Validate email configuration on application startup.

**Implementation:**
```csharp
// Program.cs - After services configuration
using (var scope = app.Services.CreateScope())
{
    var emailSettings = scope.ServiceProvider
        .GetRequiredService<IOptions<EmailSettings>>().Value;

    // Validate configuration
    if (emailSettings.Provider.Equals("Azure", StringComparison.OrdinalIgnoreCase))
    {
        if (string.IsNullOrEmpty(emailSettings.AzureConnectionString) ||
            emailSettings.AzureConnectionString.StartsWith("${"))
        {
            throw new InvalidOperationException(
                "AZURE_EMAIL_CONNECTION_STRING environment variable is not set or " +
                "placeholder expansion failed. Cannot start application.");
        }

        if (string.IsNullOrEmpty(emailSettings.AzureSenderAddress) ||
            emailSettings.AzureSenderAddress.StartsWith("${"))
        {
            throw new InvalidOperationException(
                "AZURE_EMAIL_SENDER_ADDRESS environment variable is not set or " +
                "placeholder expansion failed. Cannot start application.");
        }

        // Attempt to create Azure client to validate connection string format
        try
        {
            var testClient = new Azure.Communication.Email.EmailClient(
                emailSettings.AzureConnectionString);
            logger.LogInformation(
                "Email service configuration validated successfully. Sender: {Sender}",
                emailSettings.AzureSenderAddress);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Invalid Azure email connection string: {ex.Message}", ex);
        }
    }
}
```

**Pros:**
- Fail fast - errors caught at startup, not first email send
- Clear error messages for configuration issues
- Prevents deployment with broken configuration

**Cons:**
- Adds startup time (minimal)
- Requires service scope creation before app.Run()

---

## Recommended Action Plan

### Immediate Fix (Deploy Today)

**Step 1:** Update Azure Container Apps environment variables

```bash
# Remove old env vars
az containerapp env delete-env-var \
  -n lankaconnect-api-prod \
  -g LankaConnect \
  --env-var-names AZURE_EMAIL_CONNECTION_STRING AZURE_EMAIL_SENDER_ADDRESS

# Add hierarchical env vars
az containerapp env set-env-vars \
  -n lankaconnect-api-prod \
  -g LankaConnect \
  --env-vars \
    "EmailSettings__AzureConnectionString=endpoint=https://lankaconnect-communication.unitedstates.communication.azure.com/;accesskey=..." \
    "EmailSettings__AzureSenderAddress=DoNotReply@7689582e-73cc-4552-b2ff-8afd9d1a6814.azurecomm.net"
```

**Step 2:** Update `appsettings.Production.json` (remove placeholders)

```json
{
  "EmailSettings": {
    "Provider": "Azure",
    "AzureConnectionString": "",
    "AzureSenderAddress": "",
    "SenderName": "LankaConnect",
    "TemplateBasePath": "Templates/Email"
  }
}
```

**Step 3:** Deploy and verify

```bash
git add src/LankaConnect.API/appsettings.Production.json
git commit -m "fix: Replace ${VAR} placeholders with hierarchical env var override pattern"
git push origin develop
```

### Medium-Term Improvement (Next Sprint)

**Add startup validation:**
1. Implement Option 3 configuration validation
2. Add health check that tests email service connectivity
3. Add integration test that validates production-like configuration

### Long-Term Architecture Review

1. **Document configuration patterns** in ARCHITECTURE.md:
   - How environment variables work in .NET
   - Hierarchical configuration override rules
   - Why `${VAR}` syntax is NOT natively supported

2. **Add configuration validation layer:**
   - Strongly-typed configuration classes with validation attributes
   - Startup validation for all external service connections
   - Clear error messages for configuration issues

3. **Infrastructure as Code:**
   - Document all required environment variables in ARM templates
   - Automate environment variable provisioning
   - Add CI/CD validation step for configuration completeness

---

## Questions Answered

### 1. What could cause environment variable substitution to suddenly stop working in Azure Container Apps?

**Answer:** Environment variable substitution using `${VAR}` syntax was NEVER a native .NET feature. It worked before due to:
- Azure Container Apps preprocessing JSON files before app startup (likely), OR
- Custom configuration code that was removed/broken (no evidence found), OR
- Different configuration approach that got changed

The .NET configuration system uses **hierarchical override** via `Section__Property` env var naming, not `${VAR}` placeholder expansion.

### 2. The connection string error shows `'${AZURE_EMAIL_CONNECTION_STRING}'` - why would the $ and braces be treated as literals?

**Answer:** Because `AddJsonFile()` in .NET's `IConfiguration` reads JSON values literally. There is NO built-in mechanism to expand `${VAR}` placeholders. The JSON deserializer treats the entire string `"${AZURE_EMAIL_CONNECTION_STRING}"` as a literal string value.

### 3. This was working before Dec 31 - what type of code change would break env var substitution?

**Answer:** No code change in the LankaConnect codebase broke this. Possible external factors:
- **Azure Container Apps platform change** (most likely)
- **Deployment manifest modification** that disabled JSON preprocessing
- **ARM template update** that changed how env vars are injected
- **Azure region migration** to environment without preprocessing support

No recent commits touched email configuration or Program.cs configuration loading (verified via git log).

### 4. Where in the code should I look for configuration issues?

**Answer:** Key locations:
1. **Program.cs lines 19-25** - Configuration builder (standard, no issues)
2. **DependencyInjection.cs line 211** - EmailSettings binding (no validation)
3. **AzureEmailService.cs lines 42-45** - Client initialization (late-bound failure)
4. **appsettings.Production.json lines 61-73** - Contains `${VAR}` placeholders (ROOT CAUSE)

**No code changes needed** - just configuration approach (Option 1 recommended).

---

## Verification Checklist

After implementing Option 1 (hierarchical env vars):

- [ ] Updated `appsettings.Production.json` (remove `${VAR}` placeholders)
- [ ] Updated Azure Container Apps env vars (add `EmailSettings__` prefix)
- [ ] Deployed to staging environment
- [ ] Tested email sending in staging
- [ ] Reviewed application logs for configuration warnings
- [ ] Deployed to production
- [ ] Verified email sending in production
- [ ] Documented configuration pattern in team wiki

---

## References

- [ASP.NET Core Configuration](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/configuration/)
- [Options Pattern in ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/configuration/options)
- [Azure Container Apps Environment Variables](https://learn.microsoft.com/en-us/azure/container-apps/environment-variables)
- [Hierarchical Configuration Keys](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/configuration/#hierarchical-configuration-data)

---

**Document Version:** 1.0
**Created By:** System Architect
**Review Status:** Ready for Implementation
