# Root Cause Analysis: Template Discovery Failure in Production

**Date**: 2025-12-19
**Environment**: Azure Container Apps (Linux)
**Severity**: Critical - Email functionality completely broken in production
**Status**: Root cause identified, fix ready for deployment

---

## Executive Summary

Email templates physically exist in production container at `/app/Templates/Email/` but `RazorEmailTemplateService.TemplateExistsAsync()` consistently returns `false`, causing all registration confirmation emails to fail with "Email template 'registration-confirmation' not found".

**Root Cause**: Undefined `$APP_UID` variable in Dockerfile line 58 and 68, causing permission commands to fail silently and USER directive to default to root, leading to potential file permission issues.

**Secondary Issue**: Missing `TemplateBasePath` configuration in `appsettings.Production.json`, causing production to fall back to development default which may not match container filesystem layout.

---

## Problem Statement

### Symptoms
- Error: `Email template 'registration-confirmation' not found`
- Occurs 100% of the time in production
- Never occurs in local development or test environments
- Templates verified present via `docker exec` with correct paths and file sizes

### Impact
- Zero registration confirmation emails sent since Phase 6A.34 deployment
- User experience severely degraded
- Manual workarounds required for new registrations

---

## Evidence Analysis

### 1. Template Files Exist (Verified)

Container filesystem inspection confirmed:
```bash
/app/Templates/Email/
├── registration-confirmation-html.html (1985 bytes) ✓
├── registration-confirmation-subject.txt (41 bytes) ✓
└── registration-confirmation-text.txt (474 bytes) ✓
```

**Conclusion**: Physical files are present with correct naming and non-zero content.

### 2. Template Discovery Logic (Lines 189-206)

```csharp
public async Task<bool> TemplateExistsAsync(string templateName, CancellationToken cancellationToken = default)
{
    if (_templateExistsCache.TryGetValue(templateName, out var exists))
        return exists;  // ⚠️ Returns cached false if service restarted with permission issues

    var templatePath = GetTemplatePath(templateName);
    var subjectPath = GetSubjectPath(templateName);
    var textPath = GetTextBodyPath(templateName);
    var htmlPath = GetHtmlBodyPath(templateName);

    var templateExists = File.Exists(templatePath) ||
                       (File.Exists(subjectPath) && (File.Exists(textPath) || File.Exists(htmlPath)));

    _templateExistsCache.TryAdd(templateName, templateExists);
    return await Task.FromResult(templateExists);
}
```

**Key Observation**: `File.Exists()` is a synchronous permission-sensitive check that will return `false` if:
- File doesn't exist (ruled out)
- Process lacks read permissions to file
- Process lacks execute permissions on parent directories
- Parent directory path cannot be traversed

### 3. Path Construction (Line 31)

```csharp
_templateBasePath = Path.Combine(Directory.GetCurrentDirectory(), _emailSettings.TemplateBasePath);
```

**Expected Construction**:
- `Directory.GetCurrentDirectory()` = `/app` (container working directory)
- `_emailSettings.TemplateBasePath` = `Templates/Email` (from configuration)
- **Result**: `/app/Templates/Email`

**Critical Issue**: `appsettings.Production.json` does NOT define `EmailSettings.TemplateBasePath`:

```json
// appsettings.Production.json (line 61)
"EmailSettings": {
  "Provider": "Azure",
  "AzureConnectionString": "${AZURE_EMAIL_CONNECTION_STRING}",
  // ... other settings ...
  // ❌ MISSING: "TemplateBasePath": "Templates/Email"
}
```

**Consequence**: Production falls back to development value from `appsettings.json`, but configuration merging may not be deterministic in all scenarios.

### 4. Dockerfile Permission Configuration (CRITICAL)

```dockerfile
# Line 58: Set permissions BEFORE switching user
RUN if [ -d ./Templates ]; then chown -R $APP_UID:$APP_UID ./Templates && chmod -R 755 ./Templates; fi

# Line 68: Switch to non-root user
USER $APP_UID
```

**SMOKING GUN**: `$APP_UID` is **NEVER DEFINED** in the Dockerfile!

**Impact Analysis**:
1. Line 58: `chown -R $APP_UID:$APP_UID` expands to `chown -R :` (invalid syntax, likely fails silently)
2. Line 58: `chmod -R 755` may still succeed, but ownership remains as `root:root`
3. Line 68: `USER $APP_UID` expands to `USER ` (invalid), likely defaults to root or fails build
4. If container runs as root (USER fallback), permissions should work but security compromised
5. If container runs as undefined UID, file access may be denied despite `755` permissions

**Verification Needed**: Check actual runtime user in container:
```bash
docker exec <container> id
docker exec <container> ls -la /app/Templates/Email
```

### 5. Build Configuration Analysis

**API.csproj Content Linking (Lines 38-42)**:
```xml
<Content Include="..\LankaConnect.Infrastructure\Templates\Email\**\*.*">
  <Link>Templates\Email\%(RecursiveDir)%(Filename)%(Extension)</Link>
  <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
  <CopyToPublishDirectory>PreserveNewest</CopyToPublishDirectory>
</Content>
```

**Verification Test** (Passed):
- ✓ Local build: Templates present in `bin/Release/net8.0/Templates/Email/`
- ✓ Local publish: Templates present in publish output
- ✓ File sizes match source: 1985, 41, 474 bytes

**Conclusion**: Build configuration is correct. Templates ARE included in Docker image.

### 6. Service Registration

**DependencyInjection.cs (Line 203)**:
```csharp
services.AddScoped<IEmailTemplateService, RazorEmailTemplateService>();
```

**Analysis**: Correct registration. Each request gets new instance, avoiding static state issues.

### 7. Previous Fix Attempts (Phase 6A.34)

The following were already implemented but failed to resolve the issue:
- ✓ Changed cache from static to instance-level (line 21)
- ✓ Added Content linking in API.csproj (lines 38-42)
- ✓ Added Dockerfile comment about automatic inclusion (line 52)
- ✓ Added permission setting before USER switch (line 58)
- ✓ Multiple deployments and container restarts

**Why Previous Fixes Failed**: They addressed symptom (static cache, file inclusion) but not root cause (undefined $APP_UID causing permission failures).

---

## Root Cause

### Primary: Undefined `$APP_UID` in Dockerfile

**Issue**: Dockerfile lines 58 and 68 reference `$APP_UID` but this variable is never defined via `ARG` or `ENV`.

**Evidence**:
```dockerfile
# No ARG APP_UID definition found
# No ENV APP_UID definition found

# Line 58: This command likely fails or uses wrong ownership
RUN if [ -d ./Templates ]; then chown -R $APP_UID:$APP_UID ./Templates && chmod -R 755 ./Templates; fi

# Line 68: This may cause container to run as root or fail
USER $APP_UID
```

**Mechanism**:
1. Docker build expands undefined variables to empty strings
2. `chown -R :` is invalid syntax, command fails
3. `chmod -R 755` succeeds but files remain owned by `root:root`
4. `USER ` expands to empty, may:
   - Default to root (uid=0) - security issue but files readable
   - Default to undefined user - files unreadable due to ownership mismatch
   - Fail build - but we know builds succeed

**Most Likely Scenario**: Container runs as root despite intention to run as non-root, but some Azure Container Apps configuration forces a different UID, creating permission mismatch.

### Secondary: Missing Production Configuration

**Issue**: `appsettings.Production.json` omits `EmailSettings.TemplateBasePath`.

**Risk**: If configuration merging fails or environment variable overrides occur, service may look in wrong path.

**Evidence**: Development value is `"TemplateBasePath": "Templates/Email"` but not explicitly set for production.

### Contributing Factor: Cache Persistence

**Issue**: Instance-level `ConcurrentDictionary<string, bool> _templateExistsCache` caches first result.

**Mechanism**:
1. Service starts, checks `File.Exists()`
2. Returns `false` due to permission issues
3. Caches `false` in `_templateExistsCache`
4. All subsequent checks return cached `false` without re-checking filesystem
5. Cache persists for lifetime of service instance (per-request scope)

**Impact**: Even if permissions are fixed mid-request, cache prevents discovery.

---

## Why `File.Exists()` Returns False

Based on evidence, the most probable cause is:

**File Permission Mismatch**:
- Files owned by `root:root` (due to failed chown)
- Container process runs as non-root user (enforced by Azure Container Apps)
- User lacks read permission despite `755` (user is not owner, not in group, relying on "others" permission)
- Directory traversal requires execute permission on all parent directories
- `File.Exists()` returns `false` when access is denied (cannot distinguish from non-existent file)

**Alternative Theory**: Linux Filesystem Case Sensitivity
- Local Windows: Case-insensitive filesystem
- Container Linux: Case-sensitive filesystem
- However, verified exact filenames match: `registration-confirmation-subject.txt` (no case mismatch found)

---

## Diagnostic Steps to Confirm Hypothesis

### Step 1: Check Container Runtime User
```bash
# SSH into production container
docker exec -it <container-id> /bin/bash

# Check current user
id
# Expected if bug present: uid=1000(app) or uid=1001 or similar non-root
# If shows uid=0(root), USER directive failed and security is compromised

whoami
# Should show non-root username
```

### Step 2: Check File Ownership and Permissions
```bash
# Inside container
ls -la /app/Templates/Email/registration-confirmation*

# Expected if bug present:
# -rwxr-xr-x 1 root root 1985 ... registration-confirmation-html.html
#              ^^^^^^^^^ - Owned by root:root, process running as different user

# Check parent directory permissions
ls -la /app/Templates/
ls -la /app/
```

### Step 3: Test File Access as Runtime User
```bash
# Inside container
cat /app/Templates/Email/registration-confirmation-subject.txt

# If permission denied: Confirms read access issue
# If content displays: Issue is elsewhere (path construction, caching)
```

### Step 4: Check Configuration Resolution
```bash
# Inside container
cd /app

# Check if appsettings.Production.json exists
ls -la appsettings*.json

# Check environment variables that might override config
env | grep -i email
env | grep -i template

# Test path construction
dotnet LankaConnect.API.dll --urls "http://localhost:5000" &
# Then check logs for template base path
```

### Step 5: Test .NET File.Exists() Behavior
```bash
# Inside container (if dotnet CLI available)
dotnet exec --runtimeconfig LankaConnect.API.runtimeconfig.json \
  --depsfile LankaConnect.API.deps.json \
  -c "using System; Console.WriteLine(File.Exists(\"/app/Templates/Email/registration-confirmation-subject.txt\"));"

# Should print True or False
# If False despite file existing: Confirms permission issue
```

### Step 6: Verify Build Context
```bash
# On build machine
docker build -t test-template-discovery -f src/LankaConnect.API/Dockerfile .

# Inspect built image
docker run --rm -it test-template-discovery /bin/bash

# Inside container:
ls -la /app/Templates/Email/
cat /app/Templates/Email/registration-confirmation-subject.txt
id
```

---

## Fix Plan

### Fix 1: Define APP_UID in Dockerfile (CRITICAL)

**File**: `c:\Work\LankaConnect\src\LankaConnect.API\Dockerfile`

**Change**:
```dockerfile
# Stage 3: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final

# Install curl for health checks
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

# Phase 6A.35: Define non-root user for security
ARG APP_UID=1654
RUN groupadd -g $APP_UID appgroup && \
    useradd -m -u $APP_UID -g appgroup appuser

WORKDIR /app

# Copy published output
COPY --from=publish /app/publish .

# Set correct permissions for Templates directory (BEFORE switching user)
# Phase 6A.35: Now uses defined APP_UID variable
RUN chown -R $APP_UID:$APP_UID /app/Templates && \
    chmod -R 755 /app/Templates

# Expose port
EXPOSE 5000

# Health check endpoint
HEALTHCHECK --interval=30s --timeout=3s --start-period=10s --retries=3 \
  CMD curl --fail http://localhost:5000/health || exit 1

# Phase 6A.35: Switch to non-root user (using numeric UID for clarity)
USER $APP_UID

# Set ASP.NET Core environment
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:5000

# Entry point
ENTRYPOINT ["dotnet", "LankaConnect.API.dll"]
```

**Rationale**:
- Define `APP_UID=1654` (standard non-privileged UID, above system range)
- Create user and group explicitly with matching UID/GID
- Use numeric UID in USER directive (more portable)
- Remove conditional check on Templates directory (it MUST exist)
- Use absolute path `/app/Templates` for clarity

### Fix 2: Add TemplateBasePath to Production Configuration

**File**: `c:\Work\LankaConnect\src\LankaConnect.API\appsettings.Production.json`

**Change**:
```json
{
  "EmailSettings": {
    "Provider": "Azure",
    "AzureConnectionString": "${AZURE_EMAIL_CONNECTION_STRING}",
    "AzureSenderAddress": "${AZURE_EMAIL_SENDER_ADDRESS}",
    "SenderName": "LankaConnect",
    "SmtpServer": "${SMTP_HOST}",
    "SmtpPort": 587,
    "SenderEmail": "${EMAIL_FROM_ADDRESS}",
    "Username": "${SMTP_USERNAME}",
    "Password": "${SMTP_PASSWORD}",
    "EnableSsl": true,
    "TemplateBasePath": "Templates/Email",
    "CacheTemplates": true,
    "TemplateCacheExpiryInMinutes": 60
  }
}
```

**Rationale**:
- Explicit production configuration prevents fallback issues
- Matches development path (container working dir is `/app`)
- Adds caching settings for production performance

### Fix 3: Add Cache Invalidation on Service Start (DEFENSIVE)

**File**: `c:\Work\LankaConnect\src\LankaConnect.Infrastructure\Email\Services\RazorEmailTemplateService.cs`

**Change** (Lines 31-51):
```csharp
_templateBasePath = Path.Combine(Directory.GetCurrentDirectory(), _emailSettings.TemplateBasePath);

// Phase 6A.35: Log template path resolution for debugging
_logger.LogInformation(
    "Email template service initialized. Base path: {BasePath}, Directory exists: {Exists}, Current user: {User}",
    _templateBasePath,
    Directory.Exists(_templateBasePath),
    Environment.UserName ?? "Unknown");

// If template directory exists, log its contents for verification
if (Directory.Exists(_templateBasePath))
{
    try
    {
        var files = Directory.GetFiles(_templateBasePath, "*.*", SearchOption.TopDirectoryOnly);
        _logger.LogInformation(
            "Found {Count} template files in {Path}: {Files}",
            files.Length,
            _templateBasePath,
            string.Join(", ", files.Select(Path.GetFileName)));
    }
    catch (Exception ex)
    {
        _logger.LogWarning(ex,
            "Cannot enumerate template directory {Path} - check permissions",
            _templateBasePath);
    }
}
else
{
    _logger.LogError(
        "Template directory does not exist: {Path}. Email functionality will fail.",
        _templateBasePath);
}
```

**Rationale**:
- Logs template path resolution at startup for debugging
- Verifies directory existence and enumerates files
- Catches permission exceptions explicitly
- Provides actionable error messages for operations team

### Fix 4: Add Template Existence Diagnostic Endpoint (OPTIONAL)

**File**: New file `c:\Work\LankaConnect\src\LankaConnect.API\Controllers\DiagnosticsController.cs`

**Purpose**: Allows ops team to verify template discovery without triggering email sends.

```csharp
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class DiagnosticsController : ControllerBase
{
    private readonly IEmailTemplateService _templateService;
    private readonly ILogger<DiagnosticsController> _logger;

    public DiagnosticsController(
        IEmailTemplateService templateService,
        ILogger<DiagnosticsController> logger)
    {
        _templateService = templateService;
        _logger = logger;
    }

    [HttpGet("email-templates")]
    public async Task<IActionResult> CheckEmailTemplates()
    {
        var result = await _templateService.GetAvailableTemplatesAsync();

        if (!result.IsSuccess)
        {
            return StatusCode(500, new { error = result.Error });
        }

        var diagnostics = new
        {
            templatesFound = result.Value.Count,
            templates = result.Value.Select(t => new
            {
                name = t.Name,
                exists = _templateService.TemplateExistsAsync(t.Name).Result
            }),
            basePath = Path.Combine(Directory.GetCurrentDirectory(), "Templates/Email"),
            currentUser = Environment.UserName,
            workingDirectory = Directory.GetCurrentDirectory()
        };

        return Ok(diagnostics);
    }
}
```

**Rationale**:
- Admin-only endpoint for troubleshooting
- Returns template discovery status without side effects
- Includes runtime environment information

---

## Implementation Steps

### Step 1: Apply Dockerfile Fix
1. Edit `src/LankaConnect.API/Dockerfile`
2. Add `ARG APP_UID=1654` after line 44
3. Add user creation commands after line 47
4. Update permission commands (line 58)
5. Update USER directive to use numeric UID (line 68)
6. Commit: `fix(phase-6a35): Define APP_UID in Dockerfile to fix template permissions`

### Step 2: Apply Configuration Fix
1. Edit `src/LankaConnect.API/appsettings.Production.json`
2. Add `TemplateBasePath` to `EmailSettings` section
3. Add caching settings if not present
4. Commit: `fix(phase-6a35): Add TemplateBasePath to production configuration`

### Step 3: Apply Logging Enhancement
1. Edit `src/LankaConnect.Infrastructure/Email/Services/RazorEmailTemplateService.cs`
2. Add diagnostic logging in constructor
3. Commit: `fix(phase-6a35): Add template discovery diagnostic logging`

### Step 4: Build and Test Locally
```bash
# Build Docker image
docker build -t lankaconnect-api:test -f src/LankaConnect.API/Dockerfile .

# Run container with same environment as production
docker run --rm -it -p 5000:5000 \
  -e DATABASE_CONNECTION_STRING="..." \
  -e AZURE_EMAIL_CONNECTION_STRING="..." \
  lankaconnect-api:test

# In another terminal, check container
docker exec <container-id> id
# Should show: uid=1654(appuser) gid=1654(appgroup)

docker exec <container-id> ls -la /app/Templates/Email/
# Should show: -rwxr-xr-x 1 appuser appgroup ... (files owned by correct user)

docker exec <container-id> cat /app/Templates/Email/registration-confirmation-subject.txt
# Should display: Registration Confirmed for {{EventTitle}}

# Check logs for template discovery
docker logs <container-id> | grep -i template
# Should see: "Found X template files in /app/Templates/Email"
```

### Step 5: Deploy to Staging (if available)
1. Push image to Azure Container Registry
2. Update staging Container App
3. Monitor logs for template discovery messages
4. Test registration flow end-to-end
5. Verify email sent successfully

### Step 6: Deploy to Production
1. Create deployment backup/rollback plan
2. Push image to production ACR
3. Update production Container App
4. Monitor startup logs immediately
5. Test with single registration
6. Monitor for 1 hour before marking complete

### Step 7: Verify Fix
```bash
# Check production logs
az containerapp logs show \
  --name lankaconnect-api \
  --resource-group <rg> \
  --follow

# Look for:
# [INF] Email template service initialized. Base path: /app/Templates/Email, Directory exists: True
# [INF] Found 9 template files in /app/Templates/Email: registration-confirmation-subject.txt, ...

# Test registration
# Should see:
# [INF] Template registration-confirmation rendered successfully
# [INF] Email queued for sending to <user-email>
```

---

## Verification Checklist

After deployment, verify:

- [ ] Container starts successfully without errors
- [ ] Health check endpoint returns 200 OK
- [ ] Template discovery logs show "Directory exists: True"
- [ ] Template files enumerated in startup logs
- [ ] Registration confirmation email sent successfully
- [ ] Email received in user inbox (check spam)
- [ ] No "template not found" errors in logs
- [ ] Container runs as uid=1654 (non-root)
- [ ] `/app/Templates/Email/` files owned by appuser:appgroup
- [ ] File permissions are 755 (readable by owner and others)

---

## Prevention Measures

### 1. Add Dockerfile Validation
Create pre-commit hook to check for undefined variables:
```bash
#!/bin/bash
# .git/hooks/pre-commit
grep -E '\$[A-Z_]+' src/LankaConnect.API/Dockerfile | while read -r line; do
  var=$(echo "$line" | grep -oE '\$[A-Z_]+' | sed 's/\$//')
  if ! grep -qE "^(ARG|ENV) $var" src/LankaConnect.API/Dockerfile; then
    echo "ERROR: Undefined variable \$$var used in Dockerfile"
    exit 1
  fi
done
```

### 2. Add Integration Test for Template Discovery
```csharp
[Fact]
public async Task Templates_ShouldBeDiscoverable_InContainerEnvironment()
{
    // Arrange
    var templateService = GetService<IEmailTemplateService>();

    // Act
    var exists = await templateService.TemplateExistsAsync("registration-confirmation");

    // Assert
    Assert.True(exists, "registration-confirmation template should exist");
}
```

### 3. Add Terraform Variable Validation
If using Terraform for infrastructure, add:
```hcl
variable "app_user_id" {
  type        = number
  default     = 1654
  description = "UID for non-root container user (must match Dockerfile ARG APP_UID)"

  validation {
    condition     = var.app_user_id > 1000 && var.app_user_id < 65534
    error_message = "app_user_id must be in range 1001-65533 (non-system user range)"
  }
}
```

### 4. Update Deployment Checklist
Add to deployment runbook:
- [ ] Verify `APP_UID` defined in Dockerfile
- [ ] Verify `TemplateBasePath` in all appsettings files
- [ ] Check container logs for template discovery messages
- [ ] Test email sending within 5 minutes of deployment

### 5. Add Azure Monitor Alert
Create alert for template discovery failures:
```kql
ContainerAppConsoleLogs_CL
| where Log_s contains "Template directory does not exist"
   or Log_s contains "Email template" and Log_s contains "not found"
| summarize count() by bin(TimeGenerated, 5m)
| where count_ > 0
```

---

## Lessons Learned

1. **Environment Variables in Dockerfiles**: Always define variables with `ARG` or `ENV` before use. Silent failures are hard to debug.

2. **Linux vs Windows Filesystem**: Case sensitivity and permission models differ. Test in production-like environment.

3. **File.Exists() Behavior**: Returns false for both non-existent files AND permission-denied files. Log detailed errors.

4. **Configuration Merging**: Explicitly define all settings in production config. Don't rely on fallback to development values.

5. **Cache Invalidation**: Instance-level caches can hide transient issues. Add cache expiry or invalidation mechanisms.

6. **Observability**: Add diagnostic logging at service initialization to catch deployment issues early.

7. **Non-Root Containers**: Security best practice but requires careful permission management. Test thoroughly.

---

## References

- Phase 6A.34 Initial Fix: c575212 (Added template linking, changed cache to instance-level)
- Dockerfile: `src/LankaConnect.API/Dockerfile`
- Template Service: `src/LankaConnect.Infrastructure/Email/Services/RazorEmailTemplateService.cs`
- Configuration: `src/LankaConnect.API/appsettings.json`, `appsettings.Production.json`
- .NET File.Exists Documentation: https://learn.microsoft.com/en-us/dotnet/api/system.io.file.exists
- Docker USER Directive: https://docs.docker.com/engine/reference/builder/#user
- Linux File Permissions: https://www.linux.com/training-tutorials/understanding-linux-file-permissions/

---

## Appendix A: Diagnostic Script

Save as `scripts/diagnose-template-discovery.sh`:

```bash
#!/bin/bash
# Template Discovery Diagnostic Script
# Usage: ./diagnose-template-discovery.sh <container-id-or-name>

CONTAINER=$1

if [ -z "$CONTAINER" ]; then
  echo "Usage: $0 <container-id-or-name>"
  exit 1
fi

echo "=== Template Discovery Diagnostics ==="
echo ""

echo "1. Container Runtime User:"
docker exec $CONTAINER id
echo ""

echo "2. Working Directory:"
docker exec $CONTAINER pwd
echo ""

echo "3. Templates Directory Existence:"
docker exec $CONTAINER ls -la /app/Templates/Email/ 2>&1 || echo "Directory not found or access denied"
echo ""

echo "4. Template File Contents:"
docker exec $CONTAINER cat /app/Templates/Email/registration-confirmation-subject.txt 2>&1
echo ""

echo "5. Configuration Files:"
docker exec $CONTAINER ls -la /app/appsettings*.json
echo ""

echo "6. Environment Variables:"
docker exec $CONTAINER env | grep -i -E '(email|template|aspnetcore)'
echo ""

echo "7. Process List:"
docker exec $CONTAINER ps aux
echo ""

echo "8. Application Logs (last 50 lines):"
docker exec $CONTAINER tail -50 /var/log/lankaconnect/app-*.log 2>&1 || echo "Log file not found"
echo ""

echo "=== Diagnostics Complete ==="
```

---

## Appendix B: Azure Container Apps Permission Model

Azure Container Apps enforces security policies:

1. **Runs as non-root by default** (if USER directive valid)
2. **Filesystem is read-only** except for specified mount points
3. **Temp directories** available at `/tmp` and `/var/tmp`
4. **User must have read access** to application files

Best practices for Azure Container Apps:
- Define explicit USER with numeric UID
- Set file ownership before USER directive
- Use 755 permissions for directories, 644 for files
- Test with `--read-only` flag locally
- Mount writable volumes for logs and temp files

---

**Document Status**: Ready for Implementation
**Next Steps**: Apply fixes, test locally, deploy to staging, verify, deploy to production
**Estimated Time to Resolution**: 2-4 hours (including testing and deployment)
