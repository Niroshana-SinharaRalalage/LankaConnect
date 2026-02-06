# ADR-001: Docker Build Failure Due to Next.js Monorepo Detection

## Status
**ACCEPTED** - 2026-01-07

## Context

### The Problem
Docker build failing in CI/CD with error:
```
ERROR: failed to build: "/app/.next/standalone/web": not found
```

### Evidence Analysis

**Local/Workflow Build (✅ Works):**
```bash
# Context: c:\Work\LankaConnect\web\
# Detected monorepo root: c:\Work\LankaConnect\
# Output: .next/standalone/web/server.js
```

**Docker Build (❌ Fails):**
```dockerfile
# Context: ./web (isolated from parent)
# No monorepo detection
# Expected output: .next/standalone/server.js
# Actual Dockerfile COPY: /app/.next/standalone/web/
# Result: NOT FOUND
```

### Root Cause: Monorepo Detection Discrepancy

**Next.js Standalone Output Behavior:**
1. **When monorepo detected**: Creates `standalone/[workspace-name]/server.js`
2. **When NOT detected**: Creates `standalone/server.js`

**Detection Mechanism:**
- Next.js looks for `package-lock.json` in parent directories
- Detects workspace structure to determine output path
- Creates subdirectory matching workspace name in monorepo

**Workflow vs Docker Context:**

| Environment | Context | Parent Visible | Monorepo Detected | Output Path |
|-------------|---------|----------------|-------------------|-------------|
| Workflow | `./web` | ✅ Yes (`LankaConnect/`) | ✅ Yes | `standalone/web/` |
| Docker | `./web` | ❌ No (isolated) | ❌ No | `standalone/` |

### The Mismatch

```
Workflow Build:          Docker Build:
┌─────────────────┐      ┌─────────────────┐
│ LankaConnect/   │      │ /app/ (web/)    │
│ ├── package.json│      │ ├── package.json│
│ ├── web/        │      │ ├── src/        │
│     ├── .next/  │      │ └── .next/      │
│         └── standalone/ │        └── standalone/
│             └── web/   │            └── server.js
│                 └── server.js  │                    (NO web/)
└─────────────────┘      └─────────────────┘
```

## Decision

**Chosen Solution: Option 3 - Dual-Path Dockerfile with Build-Time Detection**

### Rationale

| Option | Pros | Cons | Verdict |
|--------|------|------|---------|
| **1. Multi-stage build copying workflow output** | ✅ Guaranteed consistency<br>✅ Uses verified build | ❌ Duplicates build step<br>❌ Longer CI/CD time<br>❌ Complex workflow | ❌ Rejected |
| **2. Disable monorepo detection via next.config.js** | ✅ Forces consistent output<br>✅ Simple change | ❌ Breaks local dev workflow<br>❌ Loses monorepo benefits<br>❌ May affect tooling | ❌ Rejected |
| **3. Smart COPY with build-time detection** | ✅ Works in both contexts<br>✅ No workflow changes<br>✅ Self-healing<br>✅ Production-ready | ⚠️ Slightly more complex Dockerfile | ✅ **ACCEPTED** |
| **4. Include parent context in Docker build** | ✅ True monorepo context | ❌ Exposes unnecessary files<br>❌ Security concerns<br>❌ Build cache issues | ❌ Rejected |

### Implementation Strategy

**Modified Dockerfile Runner Stage:**
```dockerfile
FROM node:20-alpine AS runner
WORKDIR /app

ENV NODE_ENV=production
ENV NEXT_TELEMETRY_DISABLED=1

RUN addgroup --system --gid 1001 nodejs && \
    adduser --system --uid 1001 nextjs

# Smart COPY: Handle both monorepo (standalone/web/) and standalone (standalone/) structures
# This allows the same Dockerfile to work in workflow (monorepo detected) and Docker (isolated)
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

# Copy static files and public directory
COPY --from=builder --chown=nextjs:nodejs /app/.next/static ./.next/static
COPY --from=builder /app/public ./public

USER nextjs
EXPOSE 3000
ENV PORT=3000
ENV HOSTNAME="0.0.0.0"

HEALTHCHECK --interval=30s --timeout=10s --start-period=40s --retries=3 \
  CMD node -e "require('http').get('http://localhost:3000/api/health', (res) => { process.exit(res.statusCode === 200 ? 0 : 1); }).on('error', () => process.exit(1));"

CMD ["node", "server.js"]
```

### How It Works

1. **Copy to Temporary Location**: Copy entire `standalone/` directory to `/tmp/standalone`
2. **Runtime Detection**: Check if `web/` subdirectory exists
3. **Conditional Copy**:
   - If `standalone/web/` exists → Copy from `web/` (monorepo)
   - If not → Copy from root (standalone)
4. **Cleanup**: Remove temporary directory
5. **Result**: `server.js` always at `/app/server.js`

### Build Output Logging

The Dockerfile now logs which structure was detected:
- `📦 Monorepo structure detected: copying from standalone/web/`
- `📦 Standalone structure detected: copying from standalone/`

This provides visibility during build and helps diagnose future issues.

## Consequences

### Positive

1. **Environment Agnostic**: Works in both workflow and Docker contexts
2. **Self-Healing**: Automatically adapts to Next.js output structure
3. **No Breaking Changes**: Existing workflow, config, and local dev unchanged
4. **Production Ready**: No performance impact, runs once at build time
5. **Future Proof**: Handles Next.js updates that might change detection logic
6. **Debuggable**: Clear logging shows which path was taken

### Negative

1. **Slight Complexity**: Adds conditional logic to Dockerfile
2. **Layer Size**: Temporary copy adds one additional layer (cleaned up immediately)
3. **Build Time**: Negligible increase (~1-2 seconds for cp operations)

### Mitigations

- **Documentation**: This ADR and inline Dockerfile comments explain the logic
- **Testing**: Verify both paths work (local build + Docker build)
- **Monitoring**: Build logs show which structure was detected

## Alternatives Considered

### Option 1: Multi-Stage Copy from Workflow

```yaml
# Workflow builds, Docker just copies
- name: Build Next.js
  run: npm run build

- name: Docker build (copy only)
  dockerfile:
    COPY ./web/.next/standalone/web /app/
```

**Rejected because:**
- Duplicates build effort (workflow + Docker)
- Tightly couples workflow to Dockerfile
- Makes local Docker builds impossible without running workflow first

### Option 2: Force Standalone Mode via Config

```javascript
// next.config.js
const nextConfig = {
  output: 'standalone',
  experimental: {
    outputStandalone: true,
    standaloneOutputDirectory: '.next/standalone', // Force root
  }
}
```

**Rejected because:**
- Configuration options don't override monorepo detection
- Would break local development workflow
- Not officially supported/documented by Next.js

### Option 4: Expand Docker Context

```yaml
- name: Build Docker
  with:
    context: .  # Root instead of ./web
    file: ./web/Dockerfile
```

**Rejected because:**
- Exposes entire repository to Docker build
- Security concern (all files accessible)
- Breaks Docker layer caching
- Increases build context size unnecessarily

## Verification Plan

### Test Cases

1. **Local Docker Build**
   ```bash
   cd web/
   docker build -t test-local .
   docker run -p 3000:3000 test-local
   # Verify: Server starts, health check responds
   ```

2. **CI/CD Workflow Build**
   ```bash
   # Triggered by: push to develop
   # Expected: Build succeeds, deployment completes
   # Verify: Logs show "Monorepo structure detected"
   ```

3. **Standalone Build Test**
   ```bash
   # Create isolated copy of web/ directory
   cp -r web/ /tmp/web-isolated/
   cd /tmp/web-isolated/
   docker build -t test-standalone .
   # Verify: Logs show "Standalone structure detected"
   ```

### Success Criteria

- ✅ Local `docker build` succeeds
- ✅ CI/CD workflow build succeeds
- ✅ Container starts and serves traffic
- ✅ Health check endpoint responds
- ✅ No "not found" errors in logs
- ✅ Build logs clearly indicate detected structure

## References

- **Next.js Standalone Output**: https://nextjs.org/docs/app/api-reference/next-config-js/output
- **Docker Multi-Stage Builds**: https://docs.docker.com/build/building/multi-stage/
- **GitHub Issue**: Deployment #20770729101 failure
- **Related Docs**:
  - `web/Dockerfile` - Modified Docker build
  - `.github/workflows/deploy-ui-staging.yml` - CI/CD workflow
  - `web/next.config.js` - Next.js configuration

## Implementation Checklist

- [ ] Update `web/Dockerfile` with smart COPY logic
- [ ] Add inline comments explaining detection mechanism
- [ ] Test local Docker build (should detect standalone)
- [ ] Test CI/CD build (should detect monorepo)
- [ ] Verify container starts successfully in both scenarios
- [ ] Update deployment workflow if needed
- [ ] Document in project README
- [ ] Create summary in tracking documents

## Notes

**Key Insight**: The root cause was **environmental context difference**, not a code bug. The same Dockerfile must adapt to different Next.js output structures based on whether monorepo is detected during the build.

**Future Considerations**: If Next.js changes monorepo detection logic or introduces new config options, this Dockerfile should continue working due to runtime detection. Monitor Next.js release notes for standalone output changes.

---

**Decision Made By**: System Architecture Designer
**Date**: 2026-01-07
**Supersedes**: Previous hardcoded path approach (`.next/standalone/web/`)
**Related ADRs**: None (first ADR for this project)
