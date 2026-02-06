# Root Cause Analysis: Azure Container Apps UI Deployment Failure

**Document Version**: 1.0
**Date**: 2026-01-06
**Status**: CRITICAL - Container activation failing
**Analyst**: System Architecture Designer

---

## Executive Summary

**ROOT CAUSE IDENTIFIED**: The Next.js standalone build is **NOT being created** during the CI/CD build process, causing the Docker container to fail at runtime when it attempts to execute `CMD ["node", "server.js"]` - a file that doesn't exist.

**Evidence**:
- Local `.next` directory contains only `dev/` folder (development mode)
- `.next/standalone/server.js` is MISSING
- Docker build succeeds (layers cached from previous attempts)
- Container activation fails immediately (no server.js to execute)

**Impact**: 0/3 replicas running, HTTP 504 Gateway Timeout, staging deployment blocked

---

## 1. Root Cause Analysis

### 1.1 The Critical Problem

**The Next.js standalone build is not being generated during CI/CD.**

#### Evidence Chain:

1. **Local Build State**:
   ```bash
   c:\Work\LankaConnect\web\.next\
   ├── dev/          # Development mode artifacts only
   └── (standalone/  # MISSING - this should exist after 'npm run build')
   ```

2. **Docker Build Success is Misleading**:
   - Docker build completes successfully
   - But it's using **cached layers** from previous builds
   - The `COPY --from=builder /app/.next/standalone ./` layer succeeds with empty/cached data
   - No error thrown because Docker doesn't validate file existence in COPY

3. **Runtime Failure**:
   - Container starts
   - Attempts to execute `CMD ["node", "server.js"]`
   - **File doesn't exist** → Immediate crash
   - Container Apps marks as "ActivationFailed"
   - Health probe never succeeds (no process running)

### 1.2 Why the Standalone Build is Missing

Analyzing the CI/CD workflow `deploy-ui-staging.yml`:

```yaml
# Line 38: Install dependencies
- name: Install dependencies
  run: npm ci

# Line 75-80: Build Next.js application
- name: Build Next.js application
  run: npm run build
  env:
    NEXT_PUBLIC_API_URL: /api/proxy
    NEXT_PUBLIC_ENV: staging
    NEXT_TELEMETRY_DISABLED: 1
```

**The build command should work correctly**, but there are several potential failure points:

#### Hypothesis 1: Build Failing Silently (Most Likely)
The `npm run build` step might be failing, but workflow continues because:
- No explicit error checking
- Build errors might be logged but not blocking deployment
- Previous successful builds cached in Docker layers mask the issue

#### Hypothesis 2: Build Not Creating Standalone Output
- `next.config.js` has `output: 'standalone'` configured correctly
- But build might be failing before this stage
- TypeScript errors or missing dependencies could abort early

#### Hypothesis 3: File System Timing Issue
- Build creates `.next/standalone` temporarily
- But it's cleaned up or not persisted before Docker build
- GitHub Actions workspace issues

### 1.3 Why Docker Build "Succeeds"

```dockerfile
# Stage 2: Builder
FROM node:20-alpine AS builder
WORKDIR /app
COPY --from=deps /app/node_modules ./node_modules
COPY . .
RUN npm run build  # <-- THIS IS FAILING OR NOT CREATING STANDALONE

# Stage 3: Runner
COPY --from=builder /app/.next/standalone ./  # <-- COPIES NOTHING, NO ERROR
```

**Docker's behavior**:
- `COPY` with missing source doesn't fail by default
- Docker uses layer caching aggressively
- If `.next/standalone` was created in a previous build, that cached layer is reused
- Build completes "successfully" with stale/empty data

---

## 2. Classification

**Primary Category**: CI/CD Build Configuration Issue
**Secondary Category**: Next.js Standalone Build Failure
**Tertiary Category**: Docker Layer Caching Masking Problem

**This is NOT**:
- Container Apps configuration issue (ingress, env vars are correct)
- Dockerfile structure issue (Dockerfile is properly configured)
- Runtime dependency issue (dependencies would fail during build, not activation)

---

## 3. Evidence-Based Analysis

### 3.1 What's Working

1. **Docker Build Process**:
   - Multi-stage build structure is correct
   - Dependencies install successfully (`npm ci`)
   - Image pushes to ACR successfully
   - Container Apps pulls image successfully

2. **Container Apps Configuration**:
   ```json
   {
     "env": [
       {"name": "BACKEND_API_URL", "value": "https://..."},
       {"name": "NEXT_PUBLIC_API_URL", "value": "/api/proxy"},
       {"name": "NODE_ENV", "value": "production"}
     ],
     "image": "lankaconnectstaging.azurecr.io/lankaconnect-ui:bc44fba8..."
   }
   ```
   - Environment variables properly quoted
   - Image reference correct
   - No path mangling issues

3. **Application Code**:
   - `/api/health` endpoint exists (`web/src/app/api/health/route.ts`)
   - `/api/proxy/[...path]` endpoint exists for backend proxying
   - `next.config.js` has `output: 'standalone'` configured

### 3.2 What's Broken

1. **Build Output**:
   - `.next/standalone/` directory missing
   - `.next/standalone/server.js` missing (required for Docker CMD)

2. **Container Activation**:
   ```json
   {
     "healthState": "Unhealthy",
     "runningState": "ActivationFailed",
     "replicas": 0
   }
   ```

3. **No Logs Available**:
   - Container crashes immediately on startup
   - No replicas running to query logs from
   - Cannot diagnose without local reproduction

### 3.3 Journey of Failed Fixes

| Attempt | Issue | Fix Applied | Result |
|---------|-------|------------|--------|
| 1 | ESLint not installed | Removed lint step | Build continued, new error |
| 2 | TypeScript errors in tests | `continue-on-error: true` | Build continued, new error |
| 3 | Env var path mangling | Quoted env vars in `az containerapp update` | Env vars correct, build still fails |
| 4 | `autoprefixer` missing | Changed `npm ci --only=production` to `npm ci` | Docker build succeeds, **container fails** |
| 5 (Current) | Container won't start | ??? | **BLOCKED - Need RCA** |

**Pattern**: Each fix addressed a symptom, not the root cause.

---

## 4. Systematic Diagnosis Plan

### 4.1 Immediate Diagnostics (Priority 1)

#### Test 1: Verify CI/CD Build Output
```bash
# In GitHub Actions workflow, add after build step:
- name: Verify standalone build
  run: |
    echo "Checking .next directory structure..."
    ls -la web/.next/
    echo "Checking for standalone directory..."
    ls -la web/.next/standalone/ || echo "ERROR: standalone directory missing"
    echo "Checking for server.js..."
    test -f web/.next/standalone/server.js && echo "server.js EXISTS" || echo "ERROR: server.js MISSING"
```

**Expected outcome**: This will reveal if `npm run build` is actually creating the standalone output.

#### Test 2: Check Build Logs for Errors
```yaml
- name: Build Next.js application
  run: npm run build
  env:
    NEXT_PUBLIC_API_URL: /api/proxy
    NEXT_PUBLIC_ENV: staging
    NEXT_TELEMETRY_DISABLED: 1
  continue-on-error: false  # ADD THIS - fail fast if build has errors
```

#### Test 3: Validate Docker Build Locally
```bash
# On local machine
cd c:\Work\LankaConnect\web

# Clear .next directory
rm -rf .next

# Run production build
npm run build

# Verify standalone output
ls -la .next/standalone/
ls -la .next/standalone/server.js
ls -la .next/static/

# Build Docker image locally
docker build -t lankaconnect-ui:test .

# Inspect image contents
docker run --rm lankaconnect-ui:test ls -la /app/
docker run --rm lankaconnect-ui:test ls -la /app/.next/

# Test container startup
docker run --rm -p 3000:3000 \
  -e BACKEND_API_URL=https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/api \
  -e NEXT_PUBLIC_API_URL=/api/proxy \
  -e NEXT_PUBLIC_ENV=staging \
  lankaconnect-ui:test

# Test health endpoint
curl http://localhost:3000/api/health
```

**Expected outcome**: This will prove if the issue is build-related or Docker-related.

### 4.2 Secondary Diagnostics (Priority 2)

#### Test 4: Docker Build Without Cache
```bash
# Force rebuild without cache to eliminate stale layers
docker build --no-cache -t lankaconnect-ui:test .
```

#### Test 5: Check TypeScript Compilation
```bash
cd web
npx tsc --noEmit
```

**Expected outcome**: TypeScript errors might be blocking the build silently.

#### Test 6: Check for Missing Dependencies
```bash
cd web
npm ci
npm ls autoprefixer postcss tailwindcss
```

### 4.3 Diagnostic Decision Tree

```
START: Container Activation Fails
│
├─ Test 1: Is .next/standalone/ created in CI/CD?
│  ├─ YES → Docker layer caching issue
│  │        → Fix: Add --no-cache to docker build
│  │
│  └─ NO → Build is failing
│           │
│           ├─ Test 2: Does build log show errors?
│           │  ├─ YES → TypeScript/Dependency errors
│           │  │        → Fix: Resolve build errors
│           │  │
│           │  └─ NO → Silent failure
│           │           │
│           │           ├─ Test 5: TypeScript errors?
│           │           │  └─ Fix: Resolve type errors
│           │           │
│           │           └─ Test 6: Missing dependencies?
│           │              └─ Fix: Install missing deps
│           │
│           └─ Test 3: Does local build work?
│              ├─ YES → CI/CD environment issue
│              │        → Fix: Align CI/CD env with local
│              │
│              └─ NO → Code/Config issue
│                       → Fix: Debug next.config.js
```

---

## 5. Durable Fix Strategy

### 5.1 Short-Term Fix (Deploy ASAP)

**Goal**: Get a working deployment to staging within 1 hour.

#### Step 1: Local Validation
```bash
# Clean build environment
cd c:\Work\LankaConnect\web
rm -rf .next node_modules
npm ci
npm run build

# Verify standalone output
test -f .next/standalone/server.js || exit 1
```

#### Step 2: Test Docker Image Locally
```bash
# Build without cache
docker build --no-cache -t lankaconnect-ui:test .

# Run locally
docker run --rm -p 3000:3000 \
  -e BACKEND_API_URL=https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/api \
  -e NEXT_PUBLIC_API_URL=/api/proxy \
  -e NEXT_PUBLIC_ENV=staging \
  -e NODE_ENV=production \
  lankaconnect-ui:test

# Verify in another terminal
curl http://localhost:3000/api/health
curl http://localhost:3000/
```

#### Step 3: Fix CI/CD Workflow
```yaml
# Add verification and fail-fast
- name: Build Next.js application
  run: npm run build
  env:
    NEXT_PUBLIC_API_URL: /api/proxy
    NEXT_PUBLIC_ENV: staging
    NEXT_TELEMETRY_DISABLED: 1

- name: Verify standalone build output
  run: |
    if [ ! -f ".next/standalone/server.js" ]; then
      echo "ERROR: Standalone build failed - server.js not found"
      echo "Contents of .next directory:"
      ls -la .next/
      exit 1
    fi
    echo "✅ Standalone build verified - server.js exists"

- name: Build and push Docker image
  uses: docker/build-push-action@v5
  with:
    context: ./web
    file: ./web/Dockerfile
    push: true
    no-cache: true  # ADD THIS - force fresh build
    tags: |
      ${{ env.AZURE_CONTAINER_REGISTRY }}.azurecr.io/${{ env.IMAGE_NAME }}:${{ github.sha }}
```

### 5.2 Medium-Term Fix (Robustness)

#### Fix 1: Add Build Health Checks
```yaml
- name: Build Next.js application
  run: |
    npm run build

    # Comprehensive verification
    echo "Verifying Next.js standalone build..."

    # Check server.js
    if [ ! -f ".next/standalone/server.js" ]; then
      echo "❌ ERROR: server.js not found"
      exit 1
    fi

    # Check static files
    if [ ! -d ".next/static" ]; then
      echo "❌ ERROR: static directory not found"
      exit 1
    fi

    # Check public files (if any)
    if [ -d "public" ] && [ ! "$(ls -A public)" ]; then
      echo "⚠️  WARNING: public directory is empty"
    fi

    echo "✅ Build verification complete"
  env:
    NEXT_PUBLIC_API_URL: /api/proxy
    NEXT_PUBLIC_ENV: staging
    NEXT_TELEMETRY_DISABLED: 1
```

#### Fix 2: Improve Dockerfile Validation
```dockerfile
# Stage 2: Builder
FROM node:20-alpine AS builder
WORKDIR /app
COPY --from=deps /app/node_modules ./node_modules
COPY . .
RUN npm run build

# ADD: Verify build output before proceeding
RUN if [ ! -f ".next/standalone/server.js" ]; then \
      echo "ERROR: Standalone build failed - server.js not created"; \
      exit 1; \
    fi

# Stage 3: Runner
...
```

#### Fix 3: Remove Type Check `continue-on-error`
```yaml
- name: Run type checking
  run: npx tsc --noEmit
  # REMOVE: continue-on-error: true
  # Type errors should block deployment
```

### 5.3 Long-Term Fix (Prevention)

#### Fix 1: Pre-Deployment Testing
Create `web/scripts/validate-build.sh`:
```bash
#!/bin/bash
set -e

echo "🔍 Validating Next.js build..."

# Required files
required_files=(
  ".next/standalone/server.js"
  ".next/standalone/package.json"
)

# Required directories
required_dirs=(
  ".next/static"
)

# Check files
for file in "${required_files[@]}"; do
  if [ ! -f "$file" ]; then
    echo "❌ ERROR: Required file missing: $file"
    exit 1
  fi
  echo "✅ Found: $file"
done

# Check directories
for dir in "${required_dirs[@]}"; do
  if [ ! -d "$dir" ]; then
    echo "❌ ERROR: Required directory missing: $dir"
    exit 1
  fi
  echo "✅ Found: $dir"
done

echo "✅ Build validation passed"
```

Use in CI/CD:
```yaml
- name: Validate build
  run: chmod +x scripts/validate-build.sh && ./scripts/validate-build.sh
  working-directory: ./web
```

#### Fix 2: Docker Build Validation
Create `web/scripts/validate-docker.sh`:
```bash
#!/bin/bash
set -e

IMAGE_NAME="lankaconnect-ui:validation"

echo "🔍 Building Docker image..."
docker build -t "$IMAGE_NAME" .

echo "🔍 Validating image contents..."
docker run --rm "$IMAGE_NAME" ls -la /app/server.js || {
  echo "❌ ERROR: server.js not found in image"
  exit 1
}

echo "🔍 Testing container startup..."
CONTAINER_ID=$(docker run -d -p 3000:3000 \
  -e BACKEND_API_URL=https://test.example.com/api \
  -e NEXT_PUBLIC_API_URL=/api/proxy \
  -e NEXT_PUBLIC_ENV=staging \
  "$IMAGE_NAME")

# Wait for startup
sleep 5

# Check health
if docker ps | grep -q "$CONTAINER_ID"; then
  echo "✅ Container started successfully"
  docker stop "$CONTAINER_ID"
else
  echo "❌ ERROR: Container failed to start"
  docker logs "$CONTAINER_ID"
  docker rm "$CONTAINER_ID"
  exit 1
fi

echo "✅ Docker validation passed"
```

#### Fix 3: Monitoring and Alerting
Add to CI/CD:
```yaml
- name: Deployment health check
  run: |
    echo "Waiting for deployment to stabilize..."
    sleep 60

    # Get container state
    STATE=$(az containerapp show \
      --name ${{ env.CONTAINER_APP_NAME }} \
      --resource-group ${{ env.RESOURCE_GROUP }} \
      --query 'properties.template.containers[0].runningState' -o tsv)

    if [ "$STATE" != "Running" ]; then
      echo "❌ ERROR: Container state is $STATE, expected Running"

      # Try to get logs
      az containerapp logs show \
        --name ${{ env.CONTAINER_APP_NAME }} \
        --resource-group ${{ env.RESOURCE_GROUP }} \
        --tail 100 || echo "Cannot retrieve logs"

      exit 1
    fi

    echo "✅ Container is running"
```

---

## 6. Verification Plan

### 6.1 Pre-Deployment Verification (Local)

**Checklist**:
- [ ] Clean build environment (`rm -rf .next node_modules`)
- [ ] Fresh dependency install (`npm ci`)
- [ ] Production build succeeds (`npm run build`)
- [ ] Standalone output exists (`test -f .next/standalone/server.js`)
- [ ] Static files exist (`test -d .next/static`)
- [ ] Docker build succeeds without cache (`docker build --no-cache`)
- [ ] Container starts locally (`docker run`)
- [ ] Health endpoint responds (`curl http://localhost:3000/api/health`)
- [ ] Home page loads (`curl http://localhost:3000/`)
- [ ] API proxy works (`curl http://localhost:3000/api/proxy/health`)

**Success Criteria**: All checks pass before pushing to GitHub.

### 6.2 CI/CD Verification

**Checklist**:
- [ ] Dependencies install successfully (check logs)
- [ ] TypeScript compilation succeeds (no `continue-on-error`)
- [ ] Unit tests pass
- [ ] Next.js build succeeds
- [ ] Standalone output verification passes
- [ ] Docker build completes (without cache)
- [ ] Image pushes to ACR
- [ ] Container App update succeeds
- [ ] Wait for deployment (60s)
- [ ] Container state is "Running"
- [ ] Health probe succeeds
- [ ] Smoke tests pass

**Success Criteria**: Deployment completes with 0 errors.

### 6.3 Post-Deployment Verification (Azure)

**Checklist**:
```bash
# 1. Check container state
az containerapp show \
  --name lankaconnect-ui-staging \
  --resource-group lankaconnect-staging \
  --query '{healthState:properties.health, runningState:properties.template.containers[0].runningState, replicas:properties.runningStatus}' -o json

# Expected:
# {
#   "healthState": "Healthy",
#   "runningState": "Running",
#   "replicas": "1 replica running"
# }

# 2. Test health endpoint
curl https://lankaconnect-ui-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/api/health

# Expected: HTTP 200, JSON with status: "healthy"

# 3. Test home page
curl https://lankaconnect-ui-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/

# Expected: HTTP 200, HTML content

# 4. Check logs for errors
az containerapp logs show \
  --name lankaconnect-ui-staging \
  --resource-group lankaconnect-staging \
  --tail 50

# Expected: No error messages, server startup logs visible

# 5. Test API proxy
curl https://lankaconnect-ui-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/api/proxy/health

# Expected: HTTP 200, backend health response
```

**Success Criteria**: All health checks pass for 5 minutes.

---

## 7. Prevention Measures

### 7.1 Build Process Improvements

1. **Zero Tolerance for Build Errors**:
   - Remove all `continue-on-error: true` flags
   - Fail fast on any build/test/type error
   - No silent failures allowed

2. **Explicit Build Validation**:
   - Verify `.next/standalone/server.js` exists after every build
   - Check file sizes (server.js should be >1KB)
   - Validate directory structure

3. **Docker Build Best Practices**:
   - Use `--no-cache` for production builds
   - Add intermediate validation steps in Dockerfile
   - Test image locally before pushing

### 7.2 CI/CD Improvements

1. **Build Verification Stage**:
   ```yaml
   - name: Build verification
     run: |
       # Fail if any required file is missing
       required_files=(
         ".next/standalone/server.js"
         ".next/standalone/package.json"
       )
       for file in "${required_files[@]}"; do
         [ -f "$file" ] || { echo "Missing: $file"; exit 1; }
       done
   ```

2. **Deployment Health Gates**:
   - Wait for container to reach "Running" state
   - Verify health endpoint before marking deployment successful
   - Auto-rollback on health check failure

3. **Smoke Test Improvements**:
   - Test all critical endpoints (/api/health, /, /api/proxy/health)
   - Verify response codes and content
   - Check for specific error patterns in logs

### 7.3 Monitoring and Alerting

1. **Container Apps Monitoring**:
   - Alert on "ActivationFailed" state
   - Alert on 0 replicas running
   - Alert on health probe failures

2. **Build Monitoring**:
   - Track build success/failure rates
   - Alert on repeated build failures
   - Monitor build duration anomalies

3. **Deployment Metrics**:
   - Track deployment success rate
   - Monitor time-to-healthy after deployment
   - Alert on rollback frequency

### 7.4 Documentation

1. **Runbook for Deployment Failures**:
   - Step-by-step debugging guide
   - Common failure patterns and solutions
   - Rollback procedures

2. **Architecture Decision Record**:
   - Document why standalone output is required
   - Explain Docker multi-stage build approach
   - Justify health check configuration

---

## 8. Next Steps

### Immediate (Next 1 Hour)

1. **Local Validation**:
   ```bash
   cd c:\Work\LankaConnect\web
   rm -rf .next node_modules
   npm ci
   npm run build
   test -f .next/standalone/server.js || echo "BUILD FAILED"
   ```

2. **If Build Succeeds Locally**:
   - Issue is in CI/CD environment
   - Add build verification to workflow
   - Re-run deployment

3. **If Build Fails Locally**:
   - Check TypeScript errors: `npx tsc --noEmit`
   - Check for missing dependencies
   - Review `next.config.js` configuration
   - Check for recent code changes that broke build

### Short-Term (Next 4 Hours)

1. **Fix CI/CD Workflow**:
   - Add standalone output verification
   - Remove `continue-on-error` flags
   - Add `--no-cache` to Docker build

2. **Deploy to Staging**:
   - Monitor deployment logs carefully
   - Verify container reaches "Running" state
   - Run full smoke test suite

3. **Document Root Cause**:
   - Update this RCA with findings
   - Create incident report
   - Share learnings with team

### Medium-Term (Next 2 Days)

1. **Implement Build Validation Scripts**:
   - `validate-build.sh`
   - `validate-docker.sh`
   - Integrate into CI/CD

2. **Add Monitoring**:
   - Container Apps health alerts
   - Deployment success tracking
   - Build failure notifications

3. **Update Documentation**:
   - Deployment runbook
   - Troubleshooting guide
   - ADR for standalone output

---

## 9. Conclusion

### Root Cause Summary

**The Next.js standalone build is not being created during CI/CD**, causing the Docker container to fail at runtime when attempting to execute a non-existent `server.js` file. Docker build "succeeds" due to layer caching masking the issue.

### Critical Gaps Identified

1. **No build output verification** - Build can fail silently
2. **continue-on-error flags** - Errors suppressed instead of fixed
3. **Docker layer caching** - Stale builds mask current failures
4. **No pre-deployment testing** - Image not validated before deployment

### Recommended Approach

1. **Immediate**: Validate build locally, fix any TypeScript/dependency errors
2. **Short-term**: Add build verification to CI/CD, remove error suppression
3. **Medium-term**: Implement comprehensive validation scripts and monitoring
4. **Long-term**: Establish zero-tolerance policy for build errors

### Success Metrics

- Container reaches "Running" state within 60 seconds of deployment
- Health probe succeeds on first attempt
- No rollbacks required
- Deployment success rate >95%

---

## Appendix A: Diagnostic Commands

### Check Build Output
```bash
# In web directory
ls -la .next/
ls -la .next/standalone/
test -f .next/standalone/server.js && echo "EXISTS" || echo "MISSING"
```

### Test Docker Build
```bash
# Build without cache
docker build --no-cache -t test .

# Inspect image
docker run --rm test ls -la /app/
docker run --rm test ls -la /app/.next/

# Test startup
docker run -d -p 3000:3000 --name test test
docker logs test
curl http://localhost:3000/api/health
docker stop test && docker rm test
```

### Check Container Apps State
```bash
az containerapp show \
  --name lankaconnect-ui-staging \
  --resource-group lankaconnect-staging \
  --query '{health:properties.health, running:properties.template.containers[0].runningState, replicas:properties.runningStatus, image:properties.template.containers[0].image}' -o json
```

### Get Container Logs
```bash
# If container is running
az containerapp logs show \
  --name lankaconnect-ui-staging \
  --resource-group lankaconnect-staging \
  --tail 100

# If container crashed
az containerapp revision show \
  --name lankaconnect-ui-staging \
  --resource-group lankaconnect-staging \
  --query 'properties.template.containers[0].resources'
```

---

## Appendix B: File Checklist

### Required Files After Build

**Next.js Build Output**:
```
web/.next/
├── standalone/
│   ├── server.js              # CRITICAL - Docker CMD target
│   ├── package.json           # Dependencies for standalone
│   ├── node_modules/          # Runtime dependencies
│   └── .next/                 # Build artifacts
├── static/                    # CSS, JS bundles
│   └── chunks/
└── cache/                     # Build cache (not needed in image)
```

**Docker Image Structure**:
```
/app/
├── server.js                  # Copied from .next/standalone/server.js
├── package.json               # Copied from .next/standalone/package.json
├── node_modules/              # Copied from .next/standalone/node_modules/
├── .next/
│   ├── static/                # Copied from .next/static/
│   └── (other build artifacts from standalone)
└── public/                    # Static assets
```

### Files to Verify

1. **After `npm run build`**:
   - [ ] `.next/standalone/server.js` (>1KB)
   - [ ] `.next/standalone/package.json`
   - [ ] `.next/static/` directory exists and has content

2. **In Docker Image**:
   - [ ] `/app/server.js` exists
   - [ ] `/app/.next/static/` exists
   - [ ] `/app/public/` exists (if you have public files)

3. **Runtime**:
   - [ ] Container starts without errors
   - [ ] Process `node server.js` is running
   - [ ] Port 3000 is listening
   - [ ] `/api/health` returns 200

---

**Document End**
