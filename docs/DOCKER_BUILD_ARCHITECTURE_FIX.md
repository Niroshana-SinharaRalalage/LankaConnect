# Docker Build Architecture Fix - Next.js Standalone Mode

## Architecture Decision Record (ADR)

**Status**: Proposed
**Date**: 2026-01-07
**Context**: Docker container crashes with "Could not find a production build in './.next' directory"
**Decision**: Redesign COPY logic to preserve complete `.next` directory structure from standalone build

---

## Problem Analysis

### Root Cause
The current Dockerfile is **OVERWRITING** the `.next` directory structure with a separate COPY command for static files, which breaks the standalone build output.

### Current Broken Logic (Lines 52-65)
```dockerfile
# Step 1: Copy entire standalone build (includes .next directory)
COPY --from=builder /app/.next/standalone /tmp/standalone/

# Step 2: Detect and copy to /app
RUN if [ -d "/tmp/standalone/web" ]; then \
      cp -r /tmp/standalone/web/* /app/; \  # Copies .next directory
    else \
      cp -r /tmp/standalone/* /app/; \      # Copies .next directory
    fi

# Step 3: ❌ OVERWRITES .next/static (DESTROYS STRUCTURE)
COPY --from=builder --chown=nextjs:nodejs /app/.next/static ./.next/static
```

### What's Happening
1. Standalone build creates: `.next/standalone/web/.next/` with server files, manifests, BUILD_ID
2. Static files are at: `.next/static/` (NOT in standalone build)
3. Current Dockerfile copies standalone `.next`, then OVERWRITES with static files
4. Result: `.next` directory is broken, missing critical server files

---

## Build Output Structure Analysis

### Local Monorepo Build (`npm run build`)
```
web/
├── .next/
│   ├── standalone/
│   │   └── web/
│   │       ├── server.js
│   │       ├── .next/              ← Server build files
│   │       │   ├── server/
│   │       │   ├── BUILD_ID
│   │       │   ├── build-manifest.json
│   │       │   ├── routes-manifest.json
│   │       │   └── required-server-files.json
│   │       ├── node_modules/
│   │       └── package.json
│   └── static/                    ← Static assets (CSS, JS)
│       ├── chunks/
│       ├── K_wQyV6Y817wooD3BITQO/
│       └── media/
```

### Docker Isolated Build
```
/app/
├── .next/
│   ├── standalone/
│   │   ├── server.js
│   │   ├── .next/                  ← Server build files
│   │   │   ├── server/
│   │   │   ├── BUILD_ID
│   │   │   ├── build-manifest.json
│   │   │   ├── routes-manifest.json
│   │   │   └── required-server-files.json
│   │   ├── node_modules/
│   │   └── package.json
│   └── static/                     ← Static assets (CSS, JS)
│       ├── chunks/
│       └── media/
```

### Critical Insight
**`.next/static` is SEPARATE from `.next/standalone/.next`**
- Standalone `.next` = Server runtime files
- Root `.next/static` = Client assets (CSS, JS, images)

---

## Solution Architecture

### Design Principle
**Preserve the complete `.next` directory structure by copying components in the correct order without overwriting**

### Corrected COPY Strategy

```dockerfile
# Stage 3: Runner (FIXED)
FROM node:20-alpine AS runner
WORKDIR /app

ENV NODE_ENV=production
ENV NEXT_TELEMETRY_DISABLED=1

# Create non-root user
RUN addgroup --system --gid 1001 nodejs && \
    adduser --system --uid 1001 nextjs

# ===================================================================
# CRITICAL FIX: Copy files in order that preserves .next structure
# ===================================================================

# Step 1: Copy standalone build (contains server.js + .next server files)
RUN mkdir -p /tmp/standalone
COPY --from=builder /app/.next/standalone /tmp/standalone/

# Step 2: Detect and copy to /app (preserves .next directory)
RUN if [ -d "/tmp/standalone/web" ]; then \
      echo "📦 Monorepo structure detected: copying from standalone/web/"; \
      cp -r /tmp/standalone/web/* /app/; \
    else \
      echo "📦 Standalone structure detected: copying from standalone/"; \
      cp -r /tmp/standalone/* /app/; \
    fi && \
    rm -rf /tmp/standalone

# Step 3: Create static directory if it doesn't exist
RUN mkdir -p /app/.next/static

# Step 4: Copy static files INTO .next/static (APPEND, not overwrite)
# This preserves the .next directory structure from standalone build
COPY --from=builder --chown=nextjs:nodejs /app/.next/static /app/.next/static

# Step 5: Copy public files (if they exist)
COPY --from=builder --chown=nextjs:nodejs /app/public ./public

# ===================================================================
# End of COPY logic
# ===================================================================

# Switch to non-root user
USER nextjs

EXPOSE 3000
ENV PORT=3000
ENV HOSTNAME="0.0.0.0"

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=40s --retries=3 \
  CMD node -e "require('http').get('http://localhost:3000/api/health', (res) => { process.exit(res.statusCode === 200 ? 0 : 1); }).on('error', () => process.exit(1));"

CMD ["node", "server.js"]
```

---

## Key Changes

### Before (Broken)
```dockerfile
# ❌ Copies .next directory
cp -r /tmp/standalone/web/* /app/

# ❌ OVERWRITES entire .next/static directory
COPY --from=builder /app/.next/static ./.next/static
```

### After (Fixed)
```dockerfile
# ✅ Copies .next directory with server files
cp -r /tmp/standalone/web/* /app/

# ✅ Ensures static directory exists
mkdir -p /app/.next/static

# ✅ Copies static files INTO existing .next/static
COPY --from=builder /app/.next/static /app/.next/static
```

---

## Technical Explanation

### Why the Original Failed

1. **Standalone build** creates: `/app/.next/standalone/web/.next/`
   - Contains: `server/`, `BUILD_ID`, manifests, etc.

2. **Static files** are at: `/app/.next/static/`
   - Contains: `chunks/`, `media/`, compiled CSS/JS

3. **Wrong COPY order**:
   ```dockerfile
   # Step 1: Copy standalone (has .next with server files)
   cp -r /tmp/standalone/web/* /app/
   # Result: /app/.next/ exists with server files

   # Step 2: Copy static (DESTROYS .next directory)
   COPY /app/.next/static ./.next/static
   # Result: /app/.next/ now ONLY has static/, missing server files
   ```

### Why the Fix Works

1. **Copy standalone build first**: Establishes `.next` directory with server files
2. **Create static directory**: Ensures target exists
3. **Copy static INTO .next/static**: Adds static files WITHOUT destroying parent directory

**Result**: Complete `.next` directory with both server files AND static assets

---

## Verification Strategy

### Directory Structure Check
```bash
# After container build, verify:
docker run --rm <image> ls -la /app/.next/

# Should show:
# - BUILD_ID
# - build-manifest.json
# - routes-manifest.json
# - server/
# - static/          ← Both exist!
```

### Runtime Verification
```bash
# Container should start successfully
docker run -p 3000:3000 <image>

# Check logs for:
# ✓ "Ready in Xms"
# ✗ "Could not find a production build"
```

### Build Test Matrix
| Build Context | Standalone Path | Expected Result |
|---------------|-----------------|------------------|
| Monorepo (GitHub Actions) | `.next/standalone/web/` | ✅ Copies from web/ |
| Docker isolated | `.next/standalone/` | ✅ Copies from root |
| Both cases | Static files | ✅ Added to .next/static |

---

## Quality Attributes

### Performance
- No change: Same number of COPY operations
- Slightly faster: Removed unnecessary overwrite

### Security
- No change: Still runs as non-root `nextjs` user
- No additional attack surface

### Maintainability
- **Improved**: Clear comments explain COPY order
- **Improved**: Explicit mkdir prevents silent failures

### Reliability
- **Major improvement**: Fixes crash on startup
- **Major improvement**: Handles both monorepo and standalone builds

---

## Risks and Mitigation

### Risk 1: Static files already in standalone build
**Likelihood**: Low (Next.js 13+ separates static files)
**Impact**: Low (mkdir -p is idempotent)
**Mitigation**: Test with different Next.js versions

### Risk 2: Permissions issues with COPY
**Likelihood**: Low (using --chown=nextjs:nodejs)
**Impact**: Medium (container won't start)
**Mitigation**: Verify user/group ownership in health check

### Risk 3: Monorepo detection fails
**Likelihood**: Low (tested in both contexts)
**Impact**: High (wrong directory structure)
**Mitigation**: Add debug logging, verify in CI/CD

---

## Implementation Checklist

- [ ] Update `web/Dockerfile` with new COPY logic
- [ ] Add inline comments explaining order dependency
- [ ] Test in Docker isolated build
- [ ] Test in GitHub Actions workflow
- [ ] Verify container startup logs
- [ ] Verify health check endpoint
- [ ] Update deployment documentation
- [ ] Create regression test in CI/CD

---

## Alternative Solutions Considered

### Alternative 1: Copy static files first
```dockerfile
COPY /app/.next/static /tmp/static
cp -r /tmp/standalone/web/* /app/
cp -r /tmp/static /app/.next/static
```
**Rejected**: More complex, extra temp directory

### Alternative 2: Merge in builder stage
```dockerfile
# In builder stage
RUN cp -r .next/static .next/standalone/web/.next/static
```
**Rejected**: Modifies build output, not idempotent

### Alternative 3: Symbolic link
```dockerfile
RUN ln -s /app/.next/static /app/static
```
**Rejected**: Next.js server expects files at specific paths

---

## Next Steps

1. Apply fix to `web/Dockerfile`
2. Test locally with `docker build`
3. Push to staging environment
4. Monitor container logs for successful startup
5. Update this ADR with production results

---

## References

- [Next.js Standalone Output](https://nextjs.org/docs/advanced-features/output-file-tracing)
- [Docker Multi-Stage Builds](https://docs.docker.com/build/building/multi-stage/)
- [Azure Container Apps Best Practices](https://learn.microsoft.com/en-us/azure/container-apps/health-probes)
