# Docker Build Fix - Test Plan

## Issue Summary

**Problem**: Docker build failing in CI/CD with `"/app/.next/standalone/web": not found`

**Root Cause**: Next.js monorepo detection creates different standalone output structures:
- Workflow (monorepo detected): `.next/standalone/web/server.js`
- Docker (isolated context): `.next/standalone/server.js`

**Solution**: Smart COPY with runtime detection in Dockerfile

## Changes Made

### 1. Dockerfile (c:\Work\LankaConnect\web\Dockerfile)

**Before:**
```dockerfile
COPY --from=builder --chown=nextjs:nodejs /app/.next/standalone/web ./
```

**After:**
```dockerfile
# Smart COPY: Handle both monorepo and standalone structures
RUN mkdir -p /tmp/standalone
COPY --from=builder /app/.next/standalone /tmp/standalone/

# Detect structure and copy to correct location
RUN if [ -d "/tmp/standalone/web" ]; then \
      echo "📦 Monorepo structure detected: copying from standalone/web/"; \
      cp -r /tmp/standalone/web/* /app/; \
    else \
      echo "📦 Standalone structure detected: copying from standalone/"; \
      cp -r /tmp/standalone/* /app/; \
    fi && \
    rm -rf /tmp/standalone
```

### 2. Architecture Decision Record

**Created**: `c:\Work\LankaConnect\docs\ADR-001-DOCKER-MONOREPO-STANDALONE-BUILD.md`

**Key Decisions**:
- Runtime detection over config changes
- No workflow modifications needed
- Self-healing approach for environment differences

## Test Cases

### Test 1: Local Next.js Build (Baseline)

**Objective**: Verify current behavior creates `standalone/web/server.js`

```bash
cd c:/Work/LankaConnect/web
npm run build
ls -la .next/standalone/
ls -la .next/standalone/web/server.js
```

**Expected**:
- ✅ `.next/standalone/web/` directory exists
- ✅ `server.js` at `.next/standalone/web/server.js`
- ✅ NOT at `.next/standalone/server.js`

**Status**: ✅ VERIFIED (2026-01-07)

### Test 2: CI/CD Workflow Build

**Objective**: Verify workflow build succeeds and detects monorepo structure

**Trigger**: Push to `develop` branch with Dockerfile changes

```yaml
# In .github/workflows/deploy-ui-staging.yml
- name: Build and push Docker image
  uses: docker/build-push-action@v5
```

**Expected Build Logs**:
```
Step X: RUN if [ -d "/tmp/standalone/web" ]; then ...
📦 Monorepo structure detected: copying from standalone/web/
```

**Expected**:
- ✅ Docker build succeeds (no "not found" error)
- ✅ Logs show "Monorepo structure detected"
- ✅ Image pushed to ACR
- ✅ Deployment completes
- ✅ Health check passes

### Test 3: Isolated Docker Build (Simulated)

**Objective**: Verify Dockerfile works when monorepo NOT detected

**Setup**: Create isolated copy of web/ directory

```bash
# Create isolated environment
mkdir -p /tmp/web-isolated
cp -r c:/Work/LankaConnect/web/* /tmp/web-isolated/
cd /tmp/web-isolated/

# Build in isolation (no parent package.json)
docker build -t test-standalone .
```

**Expected Build Logs**:
```
Step X: RUN if [ -d "/tmp/standalone/web" ]; then ...
📦 Standalone structure detected: copying from standalone/
```

**Expected**:
- ✅ Build succeeds
- ✅ Logs show "Standalone structure detected"
- ✅ `server.js` copied to `/app/server.js`
- ✅ Container starts successfully

**Status**: ⏸️ PENDING (requires Docker daemon)

### Test 4: Container Runtime Verification

**Objective**: Verify deployed container serves traffic correctly

**After CI/CD Deployment**:

```bash
# Get container URL
CONTAINER_URL="https://lankaconnect-ui-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io"

# Test health endpoint
curl -I $CONTAINER_URL/api/health
# Expected: HTTP/1.1 200 OK

# Test home page
curl -I $CONTAINER_URL
# Expected: HTTP/1.1 200 OK

# Check container logs
az containerapp logs show \
  --name lankaconnect-ui-staging \
  --resource-group lankaconnect-staging \
  --follow
# Expected: No errors, server started on port 3000
```

**Expected**:
- ✅ Health check returns 200
- ✅ Home page loads successfully
- ✅ No "Cannot find module" errors in logs
- ✅ No "ENOENT" file not found errors

### Test 5: Rollback Verification

**Objective**: Ensure rollback path exists if fix fails

**Rollback Dockerfile** (if needed):
```dockerfile
# Emergency rollback: Force standalone structure
COPY --from=builder /app/.next/standalone /tmp/standalone/
RUN cp -r /tmp/standalone/web/* /app/ 2>/dev/null || cp -r /tmp/standalone/* /app/
```

**Rollback Steps**:
1. Revert Dockerfile changes
2. Trigger workflow manually
3. Verify deployment

## Test Results Template

### Test Execution Record

| Test | Date | Executor | Result | Notes |
|------|------|----------|--------|-------|
| Local Next.js Build | 2026-01-07 | System | ✅ PASS | Confirmed `standalone/web/server.js` |
| CI/CD Workflow Build | _pending_ | _pending_ | ⏳ PENDING | Awaiting push to develop |
| Isolated Docker Build | _pending_ | _pending_ | ⏸️ SKIPPED | Docker daemon not available |
| Container Runtime | _pending_ | _pending_ | ⏳ PENDING | After deployment |
| Rollback Verification | - | - | N/A | Only if needed |

## Success Criteria

**Minimum Requirements** (before marking complete):
- ✅ Test 1: Local build verified
- ⏳ Test 2: CI/CD build succeeds
- ⏳ Test 4: Container serves traffic
- ✅ ADR documented
- ✅ Dockerfile updated

**Optional** (nice to have):
- ⏸️ Test 3: Isolated build tested
- ⏸️ Test 5: Rollback tested

## Monitoring Post-Deployment

### Week 1 (Days 1-7)

**Daily Checks**:
- Container health status
- Error rate in Application Insights
- Response time metrics
- Container restart count

**Commands**:
```bash
# Check container status
az containerapp show \
  --name lankaconnect-ui-staging \
  --resource-group lankaconnect-staging \
  --query "properties.runningStatus"

# Check logs for errors
az containerapp logs show \
  --name lankaconnect-ui-staging \
  --resource-group lankaconnect-staging \
  --tail 100 | grep -i error
```

### Week 2-4 (Days 8-30)

**Weekly Checks**:
- Deployment success rate
- No regression in build times
- Docker layer cache efficiency

## Rollback Trigger Conditions

**Immediately rollback if**:
1. CI/CD build fails with same error
2. Container fails to start after deployment
3. Health check fails consistently
4. Critical functionality broken

**Rollback Process**:
1. Revert `c:\Work\LankaConnect\web\Dockerfile` to previous version
2. Commit with message: `revert: Rollback Docker monorepo fix`
3. Push to `develop` branch
4. Monitor deployment
5. Investigate root cause offline

## Documentation Updates Needed

**After Successful Deployment**:

1. ✅ Create ADR-001 (completed)
2. ⏳ Update PROGRESS_TRACKER.md
3. ⏳ Update STREAMLINED_ACTION_PLAN.md
4. ⏳ Update TASK_SYNCHRONIZATION_STRATEGY.md
5. ⏳ Create Phase Summary document
6. ⏳ Update PHASE_6A_MASTER_INDEX.md

## Next Steps

1. **Immediate**: Push changes to `develop` branch
2. **Monitor**: Watch CI/CD build logs for detection message
3. **Verify**: Check deployment succeeds and container starts
4. **Validate**: Run smoke tests on deployed container
5. **Document**: Update tracking documents with results

## References

- **ADR**: `c:\Work\LankaConnect\docs\ADR-001-DOCKER-MONOREPO-STANDALONE-BUILD.md`
- **Dockerfile**: `c:\Work\LankaConnect\web\Dockerfile`
- **Workflow**: `c:\Work\LankaConnect\.github\workflows\deploy-ui-staging.yml`
- **Deployment**: https://lankaconnect-ui-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io

---

**Created**: 2026-01-07
**Last Updated**: 2026-01-07
**Status**: Ready for CI/CD testing
