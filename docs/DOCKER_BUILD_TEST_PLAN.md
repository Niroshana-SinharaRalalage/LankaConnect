# Docker Build Test Plan - Next.js Standalone Fix

## Test Environment

**Version**: Phase 6A.64 (Junction Table Implementation)
**Date**: 2026-01-07
**Scope**: Verify Docker build correctly assembles Next.js standalone output

---

## Pre-Test Checklist

- [ ] Local Next.js build succeeds (`npm run build` in web/)
- [ ] Docker installed and running
- [ ] No conflicting containers or images
- [ ] Network connectivity for pulling base images

---

## Test Scenarios

### Scenario 1: Docker Isolated Build (Primary Use Case)

**Context**: Building from `web/` directory in isolation (Azure Container Apps workflow)

```bash
# Clean previous builds
docker rmi lankaconnect-web:test 2>/dev/null || true

# Build from isolated context
cd web/
docker build -t lankaconnect-web:test -f Dockerfile .

# Expected: Build succeeds without errors
```

**Expected Directory Structure in Container**:
```
/app/
├── server.js
├── .next/
│   ├── BUILD_ID
│   ├── server/
│   ├── static/          ← Both server AND static files
│   ├── build-manifest.json
│   ├── routes-manifest.json
│   └── required-server-files.json
├── node_modules/
├── package.json
└── public/
```

**Verification**:
```bash
# Run verification script
bash ../scripts/verify-docker-build.sh lankaconnect-web:test

# Manual check
docker run --rm lankaconnect-web:test ls -la /app/.next/
```

**Expected Output**:
```
✅ All verification tests passed!
```

---

### Scenario 2: Monorepo Build (GitHub Actions Use Case)

**Context**: Building from repository root (CI/CD workflow)

```bash
# Clean previous builds
docker rmi lankaconnect-web:monorepo 2>/dev/null || true

# Build from repository root
cd ..  # Back to repository root
docker build -t lankaconnect-web:monorepo -f web/Dockerfile web/

# Expected: Build succeeds, monorepo structure detected
```

**Expected Build Log**:
```
📦 Monorepo structure detected: copying from standalone/web/
```

**Expected Directory Structure in Container**:
```
/app/
├── server.js
├── .next/
│   ├── BUILD_ID
│   ├── server/
│   ├── static/          ← Both server AND static files
│   ├── build-manifest.json
│   ├── routes-manifest.json
│   └── required-server-files.json
├── node_modules/
├── package.json
└── public/
```

**Verification**:
```bash
bash scripts/verify-docker-build.sh lankaconnect-web:monorepo
```

---

### Scenario 3: Container Runtime Test

**Context**: Verify container starts and serves application

```bash
# Start container
docker run -d \
  --name lankaconnect-test \
  -p 3001:3000 \
  -e NEXT_PUBLIC_API_URL=http://localhost:5000/api \
  lankaconnect-web:test

# Wait for startup (30 seconds)
sleep 30

# Check logs
docker logs lankaconnect-test
```

**Expected Log Output**:
```
✓ Ready in 2000ms
```

**NOT Expected** (Failure Indicator):
```
❌ Error: Could not find a production build in the './.next' directory
```

**HTTP Endpoint Tests**:
```bash
# Test 1: Health check
curl -v http://localhost:3001/api/health
# Expected: 200 OK

# Test 2: Homepage
curl -v http://localhost:3001/
# Expected: 200 OK, HTML response

# Test 3: Static assets (CSS)
curl -v http://localhost:3001/_next/static/chunks/main-*.js
# Expected: 200 OK, JavaScript content

# Cleanup
docker stop lankaconnect-test
docker rm lankaconnect-test
```

---

### Scenario 4: Multi-Stage Build Verification

**Context**: Verify each build stage produces correct output

```bash
# Build only deps stage
docker build --target deps -t lankaconnect-web:deps -f web/Dockerfile web/

# Build only builder stage
docker build --target builder -t lankaconnect-web:builder -f web/Dockerfile web/

# Inspect builder output
docker run --rm lankaconnect-web:builder ls -la /app/.next/standalone/
docker run --rm lankaconnect-web:builder ls -la /app/.next/static/
```

**Expected**:
- Deps stage: `node_modules/` populated
- Builder stage: `.next/standalone/` AND `.next/static/` exist separately
- Runner stage: Both merged correctly

---

### Scenario 5: File Permissions Check

**Context**: Verify non-root user can access all files

```bash
docker run --rm lankaconnect-web:test whoami
# Expected: nextjs

docker run --rm lankaconnect-web:test ls -la /app/.next/static/
# Expected: All files owned by nextjs:nodejs

docker run --rm lankaconnect-web:test node -e "console.log(process.getuid())"
# Expected: 1001
```

---

## Regression Tests

### Test 1: Static Files Not Lost

**Verify**: `.next/static/` directory contains CSS/JS bundles

```bash
docker run --rm lankaconnect-web:test find /app/.next/static -name "*.js" | wc -l
# Expected: > 0 (at least some JS files)

docker run --rm lankaconnect-web:test find /app/.next/static/chunks -type f | wc -l
# Expected: > 5 (multiple chunk files)
```

---

### Test 2: Server Files Not Lost

**Verify**: `.next/server/` directory contains runtime code

```bash
docker run --rm lankaconnect-web:test ls -la /app/.next/server/
# Expected: app/, pages/, chunks/, etc.

docker run --rm lankaconnect-web:test test -f /app/.next/BUILD_ID && echo "EXISTS"
# Expected: EXISTS
```

---

### Test 3: Manifests Intact

**Verify**: Build manifests are not corrupted

```bash
docker run --rm lankaconnect-web:test cat /app/.next/build-manifest.json | jq .
# Expected: Valid JSON with "pages" key

docker run --rm lankaconnect-web:test cat /app/.next/routes-manifest.json | jq .
# Expected: Valid JSON with "routes" key
```

---

## Performance Benchmarks

### Build Time

```bash
time docker build --no-cache -t lankaconnect-web:benchmark -f web/Dockerfile web/
```

**Expected**: < 5 minutes on modern hardware

### Image Size

```bash
docker images lankaconnect-web:benchmark --format "{{.Size}}"
```

**Expected**: < 500MB (alpine-based image)

### Startup Time

```bash
docker run --rm lankaconnect-web:benchmark \
  sh -c 'start=$(date +%s); node server.js & sleep 10; end=$(date +%s); echo $((end-start)) seconds'
```

**Expected**: < 10 seconds

---

## Failure Cases to Test

### Failure 1: Missing Static Directory

**Simulate**: Comment out static COPY line

```dockerfile
# COPY --from=builder /app/.next/static /app/.next/static
```

**Expected Error**:
```
Error: Could not load static files
```

---

### Failure 2: Missing Server Files

**Simulate**: Copy only static, skip standalone

```dockerfile
COPY --from=builder /app/.next/static /app/.next/
# Skip standalone copy
```

**Expected Error**:
```
Error: Could not find a production build
```

---

### Failure 3: Wrong Directory Structure

**Simulate**: Copy from wrong path

```dockerfile
COPY --from=builder /app/.next/ /app/.next/
# Instead of standalone
```

**Expected Error**:
```
Error: Cannot find module 'next/dist/server/next-server'
```

---

## Success Criteria

### Build Phase
- ✅ Docker build completes without errors
- ✅ All three stages (deps, builder, runner) succeed
- ✅ Build logs show correct structure detection

### Structure Phase
- ✅ `/app/server.js` exists
- ✅ `/app/.next/BUILD_ID` exists
- ✅ `/app/.next/server/` exists with files
- ✅ `/app/.next/static/` exists with files
- ✅ All manifests present and valid JSON

### Runtime Phase
- ✅ Container starts without errors
- ✅ Logs show "Ready in Xms"
- ✅ Health check endpoint returns 200
- ✅ Homepage loads successfully
- ✅ Static assets serve correctly

### Security Phase
- ✅ Container runs as non-root user
- ✅ Files owned by nextjs:nodejs
- ✅ No elevated privileges required

---

## Automated Test Script

```bash
#!/bin/bash
# test-docker-build.sh

set -e

echo "🧪 Running Docker Build Test Suite"
echo "=================================="

# Test 1: Build
echo "Test 1: Docker build..."
docker build -t lankaconnect-web:test -f web/Dockerfile web/

# Test 2: Structure verification
echo "Test 2: Structure verification..."
bash scripts/verify-docker-build.sh lankaconnect-web:test

# Test 3: Runtime test
echo "Test 3: Runtime test..."
docker run -d --name test-container -p 3001:3000 lankaconnect-web:test
sleep 30

# Test 4: HTTP endpoints
echo "Test 4: HTTP endpoints..."
curl -f http://localhost:3001/api/health || exit 1
curl -f http://localhost:3001/ || exit 1

# Cleanup
docker stop test-container
docker rm test-container

echo "✅ All tests passed!"
```

---

## Rollback Plan

If fix causes issues:

1. Revert Dockerfile to previous version
2. Use temporary workaround:
   ```dockerfile
   # Copy entire .next directory from builder
   COPY --from=builder /app/.next /app/.next
   ```
3. Investigate why standalone build structure differs
4. Update test suite with new findings

---

## Documentation Updates

After successful testing:

- [ ] Update `README.md` with Docker build instructions
- [ ] Update CI/CD pipeline documentation
- [ ] Add verification script to repository
- [ ] Update deployment guide
- [ ] Create troubleshooting section

---

## Test Results Log

| Test | Date | Result | Notes |
|------|------|--------|-------|
| Scenario 1 | YYYY-MM-DD | PASS/FAIL | |
| Scenario 2 | YYYY-MM-DD | PASS/FAIL | |
| Scenario 3 | YYYY-MM-DD | PASS/FAIL | |
| Scenario 4 | YYYY-MM-DD | PASS/FAIL | |
| Scenario 5 | YYYY-MM-DD | PASS/FAIL | |

---

## Contact

**Issue Tracker**: Phase 6A.64 - Docker Build Fix
**Architectural Review**: See `DOCKER_BUILD_ARCHITECTURE_FIX.md`
