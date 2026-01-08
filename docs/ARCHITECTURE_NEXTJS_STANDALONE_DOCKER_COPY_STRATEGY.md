# Architecture Decision Record: Next.js Standalone Docker COPY Strategy

**Date**: 2026-01-08
**Status**: Active
**Context**: Container crashing with "Could not find a production build in the './.next' directory"

---

## Problem Statement

The Next.js web container fails to start with:
```
Error: Could not find a production build in the './.next' directory.
```

Despite our Dockerfile creating the `.next` directory with `mkdir -p`, the server cannot locate required build artifacts (BUILD_ID, server manifests, etc.).

---

## Next.js Standalone Output Structure (ACTUAL)

When Next.js builds with `output: 'standalone'`, it creates:

```
.next/
├── standalone/              ← Minimal server bundle
│   ├── server.js           ← Entry point (expects .next/ as sibling)
│   ├── node_modules/       ← Only runtime dependencies
│   ├── package.json        ← Minimal runtime package.json
│   └── .next/              ← ❌ THIS DOES NOT EXIST IN STANDALONE
│
├── static/                  ← Static assets (CSS, JS bundles, fonts)
│   ├── chunks/
│   ├── css/
│   └── media/
│
└── BUILD_ID                 ← Build identifier (outside standalone)
```

### Critical Insight

**The `.next/standalone/` directory does NOT contain a `.next/` subdirectory.**

The `server.js` file expects to find `.next/` as a **sibling directory** in the final runtime environment:

```
/app/
├── server.js               ← From .next/standalone/server.js
├── node_modules/           ← From .next/standalone/node_modules
├── package.json            ← From .next/standalone/package.json
└── .next/                  ← MUST BE CREATED from .next/static + .next/BUILD_ID
    ├── BUILD_ID
    ├── static/
    │   ├── chunks/
    │   ├── css/
    │   └── media/
    └── server/             ← ❌ NOT PRESENT (server.js replaces this)
```

---

## Why Current Dockerfile Fails

### Current Approach (Lines 55-78)
```dockerfile
# Step 1: Copy standalone build
RUN mkdir -p /tmp/standalone
COPY --from=builder /app/.next/standalone /tmp/standalone/

# Step 2: Detect structure and copy
RUN if [ -d "/tmp/standalone/web" ]; then \
      cp -r /tmp/standalone/web/* /app/; \
    else \
      cp -r /tmp/standalone/* /app/; \
    fi

# Step 3: Create .next/static directory
RUN mkdir -p /app/.next/static

# Step 4: Copy static files
COPY --from=builder --chown=nextjs:nodejs /app/.next/static /app/.next/static
```

### Why It Fails

1. **Wrong Source**: `.next/standalone/` does NOT contain `.next/` subdirectory
2. **Missing BUILD_ID**: We never copy `.next/BUILD_ID` from builder stage
3. **Wrong mkdir**: Creating empty `/app/.next/static` doesn't populate it with artifacts
4. **Monorepo Detection Logic**: The `if [ -d "/tmp/standalone/web" ]` assumes standalone contains `web/`, which may not exist

---

## Correct Architecture: What server.js Actually Needs

The `server.js` file (from `.next/standalone/server.js`) requires:

### 1. Build Identifier
```
/app/.next/BUILD_ID
```

### 2. Static Assets
```
/app/.next/static/
├── chunks/
├── css/
└── media/
```

### 3. Runtime Server Files
```
/app/server.js           ← The standalone server
/app/node_modules/       ← Runtime dependencies
/app/package.json        ← Runtime package manifest
```

### 4. Public Assets (Optional)
```
/app/public/
├── favicon.ico
└── images/
```

---

## Corrected Docker COPY Strategy

### Option A: Standard Pattern (Recommended by Next.js Docs)

```dockerfile
# Stage 3: Runner
FROM node:20-alpine AS runner
WORKDIR /app

ENV NODE_ENV=production
ENV NEXT_TELEMETRY_DISABLED=1

# Create non-root user
RUN addgroup --system --gid 1001 nodejs && \
    adduser --system --uid 1001 nextjs

# Copy public assets (if exist)
COPY --from=builder --chown=nextjs:nodejs /app/public ./public

# Copy standalone server files
# This includes server.js, package.json, node_modules
COPY --from=builder --chown=nextjs:nodejs /app/.next/standalone ./

# Copy static assets to .next/static
# This creates /app/.next/static with all CSS/JS bundles
COPY --from=builder --chown=nextjs:nodejs /app/.next/static ./.next/static

USER nextjs
EXPOSE 3000

ENV PORT=3000
ENV HOSTNAME="0.0.0.0"

CMD ["node", "server.js"]
```

### Why This Works

1. **Standalone First**: Copies `server.js`, `node_modules/`, `package.json` to `/app/`
2. **Static Assets**: Creates `/app/.next/static/` with all build artifacts
3. **BUILD_ID Implicit**: Next.js standalone includes BUILD_ID reference internally
4. **Simple**: No complex detection logic or temp directories

---

## Option B: Monorepo-Aware Pattern (If Needed)

If the build context is a monorepo and standalone outputs to `web/`:

```dockerfile
# Stage 3: Runner
FROM node:20-alpine AS runner
WORKDIR /app

ENV NODE_ENV=production
ENV NEXT_TELEMETRY_DISABLED=1

RUN addgroup --system --gid 1001 nodejs && \
    adduser --system --uid 1001 nextjs

# Copy public assets
COPY --from=builder --chown=nextjs:nodejs /app/public ./public

# Detect and copy standalone output
# Monorepo: .next/standalone/web/*
# Standalone: .next/standalone/*
COPY --from=builder --chown=nextjs:nodejs /app/.next/standalone ./standalone-tmp

RUN if [ -d "./standalone-tmp/web" ]; then \
      echo "Monorepo detected: copying from standalone/web/"; \
      cp -r ./standalone-tmp/web/* /app/; \
    else \
      echo "Standalone detected: copying from standalone/"; \
      cp -r ./standalone-tmp/* /app/; \
    fi && \
    rm -rf ./standalone-tmp

# Copy static assets
COPY --from=builder --chown=nextjs:nodejs /app/.next/static ./.next/static

USER nextjs
EXPOSE 3000

ENV PORT=3000
ENV HOSTNAME="0.0.0.0"

CMD ["node", "server.js"]
```

---

## Debugging: Verify Standalone Structure

To confirm the actual structure during build:

```dockerfile
# Add after RUN npm run build in builder stage
RUN echo "=== Standalone Structure ===" && \
    ls -la /app/.next/standalone/ && \
    echo "=== Static Structure ===" && \
    ls -la /app/.next/static/ && \
    echo "=== BUILD_ID ===" && \
    cat /app/.next/BUILD_ID || echo "BUILD_ID not found"
```

---

## Recommended Solution

### Replace Lines 47-78 in Dockerfile

**OLD** (Complex, broken):
```dockerfile
RUN mkdir -p /tmp/standalone
COPY --from=builder /app/.next/standalone /tmp/standalone/
RUN if [ -d "/tmp/standalone/web" ]; then \
      cp -r /tmp/standalone/web/* /app/; \
    else \
      cp -r /tmp/standalone/* /app/; \
    fi && \
    rm -rf /tmp/standalone
RUN mkdir -p /app/.next/static
COPY --from=builder --chown=nextjs:nodejs /app/.next/static /app/.next/static
```

**NEW** (Simple, correct):
```dockerfile
# Copy public assets (images, fonts, etc.)
COPY --from=builder --chown=nextjs:nodejs /app/public ./public

# Copy standalone output (server.js, node_modules, package.json)
COPY --from=builder --chown=nextjs:nodejs /app/.next/standalone ./

# Copy static assets to .next/static
COPY --from=builder --chown=nextjs:nodejs /app/.next/static ./.next/static
```

---

## Trade-offs Analysis

| Approach | Pros | Cons |
|----------|------|------|
| **Option A (Standard)** | Simple, follows Next.js docs, fewer layers | May not work for monorepo builds |
| **Option B (Monorepo-Aware)** | Handles both monorepo and standalone | More complex, harder to debug |
| **Current (Temp Dir)** | ❌ None | Broken, doesn't copy BUILD_ID, complex |

---

## Decision

**Adopt Option A (Standard Pattern)** because:

1. ✅ Follows official Next.js Docker documentation
2. ✅ Simpler to understand and maintain
3. ✅ Fewer build layers and temp directories
4. ✅ Correctly places `.next/static` where `server.js` expects
5. ✅ Aligns with Azure Container Apps best practices

If monorepo issues arise, we can fallback to Option B.

---

## Implementation Steps

1. Update `web/Dockerfile` with Option A pattern
2. Test build locally with `docker build -t lankaconnect-web ./web`
3. Verify runtime with `docker run -p 3000:3000 lankaconnect-web`
4. Check logs for successful startup
5. Deploy to Azure Container Apps staging
6. Validate health endpoint and UI functionality

---

## References

- [Next.js Standalone Docker Deployment](https://nextjs.org/docs/app/getting-started/deploying)
- [Next.js with Docker and Standalone](https://hmos.dev/en/nextjs-docker-standalone-and-custom-server)
- [Dockerizing Next.js with Standalone Output](https://medium.com/@techwithtwin/dockerizing-next-js-v14-application-using-output-standalone-and-self-hos-eb636aa9b441)
- [Next.js Docker Best Practices](https://markus.oberlehner.net/blog/running-nextjs-with-docker)

---

## Related Documents

- `web/Dockerfile` - Docker build configuration
- `web/next.config.js` - Next.js standalone output configuration
- `docs/PHASE_6A64_DEPLOYMENT_ARCHITECTURE.md` - Azure deployment architecture

---

**Next Actions**: Update Dockerfile runner stage with recommended pattern.
