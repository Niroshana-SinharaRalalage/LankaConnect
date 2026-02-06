# Phase 6A.35: Template Discovery Fix - Implementation Checklist

**Issue**: Email templates exist but `File.Exists()` returns false in production
**Root Cause**: Undefined `$APP_UID` in Dockerfile causing permission failures
**Status**: Ready for implementation
**Priority**: Critical (P0)

---

## Quick Summary

**Problem**: `$APP_UID` used in Dockerfile lines 58 and 68 but never defined, causing:
- Permission commands to fail silently
- Container possibly running as root or with wrong UID
- Template files unreadable by application process

**Fix**: Define `APP_UID=1654`, create user explicitly, fix permissions

**Time to Deploy**: 30 minutes (build + deploy + verify)

---

## Files to Change

### 1. Dockerfile Fix (PRIMARY)

**File**: `src/LankaConnect.API/Dockerfile`

**Location**: After line 44 (after `FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final`)

**Add these lines**:
```dockerfile
# Phase 6A.35: Define non-root user for security
ARG APP_UID=1654
RUN groupadd -g $APP_UID appgroup && \
    useradd -m -u $APP_UID -g appgroup appuser
```

**Then modify line 58 (currently has conditional)**:
```dockerfile
# OLD (line 58):
RUN if [ -d ./Templates ]; then chown -R $APP_UID:$APP_UID ./Templates && chmod -R 755 ./Templates; fi

# NEW (line 58):
RUN chown -R $APP_UID:$APP_UID /app/Templates && \
    chmod -R 755 /app/Templates
```

**Then modify line 68**:
```dockerfile
# OLD (line 68):
USER $APP_UID

# NEW (line 68):
USER $APP_UID
# (No change needed, but now APP_UID is defined)
```

**Full section should look like**:
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

# Phase 6A.35: Switch to non-root user
USER $APP_UID

# Set ASP.NET Core environment
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:5000

# Entry point
ENTRYPOINT ["dotnet", "LankaConnect.API.dll"]
```

---

### 2. Production Configuration Fix (SECONDARY)

**File**: `src/LankaConnect.API/appsettings.Production.json`

**Location**: Inside `EmailSettings` section (around line 61)

**Add**:
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

**Add these 3 lines**:
- `"TemplateBasePath": "Templates/Email",`
- `"CacheTemplates": true,`
- `"TemplateCacheExpiryInMinutes": 60`

---

### 3. Diagnostic Logging Fix (DEFENSIVE)

**File**: `src/LankaConnect.Infrastructure/Email/Services/RazorEmailTemplateService.cs`

**Location**: In constructor, after line 31 (`_templateBasePath = ...`)

**Add**:
```csharp
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

---

## Build and Test Steps

### Local Docker Test
```bash
# 1. Build image
cd c:/Work/LankaConnect
docker build -t lankaconnect-api:phase-6a35-test -f src/LankaConnect.API/Dockerfile .

# 2. Run container
docker run --rm -d --name test-templates -p 5000:5000 \
  -e DATABASE_CONNECTION_STRING="Host=host.docker.internal;Port=5432;Database=LankaConnectDB;Username=lankaconnect;Password=dev_password_123" \
  -e AZURE_EMAIL_CONNECTION_STRING="endpoint=https://..." \
  -e JWT_SECRET_KEY="test-key-minimum-256-bits-long" \
  -e JWT_ISSUER="LankaConnect.API" \
  -e JWT_AUDIENCE="LankaConnect.Client" \
  lankaconnect-api:phase-6a35-test

# 3. Run diagnostics
bash scripts/diagnose-template-discovery.sh test-templates

# 4. Check logs
docker logs test-templates | grep -i template

# Expected output:
# [INF] Email template service initialized. Base path: /app/Templates/Email, Directory exists: True, Current user: appuser
# [INF] Found 9 template files in /app/Templates/Email: registration-confirmation-subject.txt, ...

# 5. Verify user and permissions
docker exec test-templates id
# Expected: uid=1654(appuser) gid=1654(appgroup)

docker exec test-templates ls -la /app/Templates/Email/
# Expected: Files owned by appuser:appgroup with 755 permissions

# 6. Test file access
docker exec test-templates cat /app/Templates/Email/registration-confirmation-subject.txt
# Expected: Registration Confirmed for {{EventTitle}}

# 7. Cleanup
docker stop test-templates
```

---

## Deployment Steps

### Step 1: Commit Changes
```bash
git add src/LankaConnect.API/Dockerfile
git add src/LankaConnect.API/appsettings.Production.json
git add src/LankaConnect.Infrastructure/Email/Services/RazorEmailTemplateService.cs
git add scripts/diagnose-template-discovery.sh
git add docs/ROOT_CAUSE_ANALYSIS_TEMPLATE_DISCOVERY_FAILURE.md
git add docs/PHASE_6A35_TEMPLATE_DISCOVERY_FIX.md

git commit -m "fix(phase-6a35): Define APP_UID in Dockerfile to fix template permission issues

CRITICAL FIX: Email templates physically exist but File.Exists() returns false
Root cause: APP_UID variable used but never defined in Dockerfile
Impact: Zero registration emails sent since Phase 6A.34

Changes:
- Define APP_UID=1654 and create user/group explicitly
- Fix permission commands to use absolute paths
- Add TemplateBasePath to production configuration
- Add diagnostic logging for template discovery

Verification:
- Container runs as uid=1654 (non-root)
- Templates owned by appuser:appgroup with 755 permissions
- Startup logs show template file discovery
- Registration emails sent successfully

Phase: 6A.35
Priority: P0 (Critical)
Related: Phase 6A.34 (initial template linking)

🤖 Generated with Claude Code
Co-Authored-By: Claude Sonnet 4.5 <noreply@anthropic.com>"
```

### Step 2: Build and Push Image
```bash
# Build for production
docker build -t lankaconnect-api:6a35 -f src/LankaConnect.API/Dockerfile .

# Tag for Azure Container Registry
docker tag lankaconnect-api:6a35 <your-acr>.azurecr.io/lankaconnect-api:6a35
docker tag lankaconnect-api:6a35 <your-acr>.azurecr.io/lankaconnect-api:latest

# Push to ACR
az acr login --name <your-acr>
docker push <your-acr>.azurecr.io/lankaconnect-api:6a35
docker push <your-acr>.azurecr.io/lankaconnect-api:latest
```

### Step 3: Deploy to Container App
```bash
# Option 1: Azure Portal
# - Navigate to Container App
# - Update container image to :6a35 tag
# - Restart application

# Option 2: Azure CLI
az containerapp update \
  --name lankaconnect-api \
  --resource-group <your-rg> \
  --image <your-acr>.azurecr.io/lankaconnect-api:6a35

# Monitor deployment
az containerapp logs show \
  --name lankaconnect-api \
  --resource-group <your-rg> \
  --follow
```

### Step 4: Verify Deployment
```bash
# Watch logs for startup messages
az containerapp logs show \
  --name lankaconnect-api \
  --resource-group <your-rg> \
  --follow \
  --tail 100

# Look for these SUCCESS indicators:
# ✅ [INF] Email template service initialized. Base path: /app/Templates/Email, Directory exists: True
# ✅ [INF] Found 9 template files in /app/Templates/Email: ...
# ✅ [INF] LankaConnect API started successfully

# Look for these FAILURE indicators:
# ❌ [ERR] Template directory does not exist: /app/Templates/Email
# ❌ [WRN] Cannot enumerate template directory /app/Templates/Email - check permissions
# ❌ [ERR] Email template 'registration-confirmation' not found
```

### Step 5: Functional Test
```bash
# Test registration flow via API or web UI
# 1. Create new user account
# 2. Register for an event
# 3. Check logs for email sending

# Expected log messages:
# [INF] Template registration-confirmation rendered successfully
# [INF] Email queued for sending to user@example.com
# [INF] Email sent successfully via Azure Email Service

# Check user's inbox (including spam folder)
# Should receive "Registration Confirmed for [Event Title]" email
```

---

## Verification Checklist

After deployment, confirm:

- [ ] Container started without errors
- [ ] Health check returns 200 OK (`curl https://<app-url>/health`)
- [ ] Startup logs show "Email template service initialized"
- [ ] Startup logs show "Found X template files"
- [ ] Directory exists logged as "True"
- [ ] No "template not found" errors in logs
- [ ] Registration confirmation email received
- [ ] Email content renders correctly with user data

---

## Rollback Plan

If deployment fails:

### Option 1: Quick Rollback
```bash
# Revert to previous image tag
az containerapp update \
  --name lankaconnect-api \
  --resource-group <your-rg> \
  --image <your-acr>.azurecr.io/lankaconnect-api:6a34
```

### Option 2: Git Rollback
```bash
# Revert commit
git revert HEAD
git push origin develop

# Rebuild and deploy previous version
```

### Option 3: Configuration-Only Fix
If Dockerfile change causes build issues, can deploy config fix only:
1. Update `appsettings.Production.json` with `TemplateBasePath`
2. Redeploy without Dockerfile changes
3. This is partial fix but may improve situation

---

## Success Criteria

### Critical (Must Pass)
- ✅ Container starts successfully
- ✅ Template discovery logs show success
- ✅ Registration email sent and received
- ✅ No "template not found" errors

### Important (Should Pass)
- ✅ Container runs as non-root (uid=1654)
- ✅ Templates owned by appuser:appgroup
- ✅ Health check passes
- ✅ All email templates discoverable

### Nice to Have
- ✅ Diagnostic endpoint accessible
- ✅ Performance unchanged or improved
- ✅ Cache working correctly

---

## Timeline

- **Code changes**: 15 minutes
- **Local testing**: 15 minutes
- **Build and push**: 10 minutes
- **Deployment**: 5 minutes
- **Verification**: 15 minutes
- **Total**: 60 minutes (with buffer)

---

## Risk Assessment

**Risk Level**: Low-Medium

**Risks**:
1. User creation might fail if UID 1654 conflicts (unlikely in Azure Container Apps)
2. Permission changes might affect other files (mitigated by targeting /app/Templates only)
3. Configuration change might have syntax errors (mitigated by JSON validation)

**Mitigation**:
- Test locally before production deployment
- Have rollback plan ready
- Deploy during low-traffic window
- Monitor logs continuously for 30 minutes post-deployment

---

## Related Documentation

- Root Cause Analysis: `docs/ROOT_CAUSE_ANALYSIS_TEMPLATE_DISCOVERY_FAILURE.md`
- Diagnostic Script: `scripts/diagnose-template-discovery.sh`
- Phase 6A.34 (previous fix): Commit c575212
- Template Service: `src/LankaConnect.Infrastructure/Email/Services/RazorEmailTemplateService.cs`

---

**Phase**: 6A.35
**Status**: Ready for Implementation
**Assignee**: DevOps / Platform Team
**Estimated Time**: 1 hour (including testing)
**Priority**: P0 (Critical - blocking production email functionality)
