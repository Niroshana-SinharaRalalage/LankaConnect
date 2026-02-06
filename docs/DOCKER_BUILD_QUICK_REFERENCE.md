# Docker Build Quick Reference - Next.js Standalone

## The Fix in 30 Seconds

**Problem**: Container crashes with "Could not find a production build in './.next' directory"

**Root Cause**: Docker COPY was overwriting `.next` directory, destroying server files

**Solution**: Pre-create `/app/.next/static` before copying static files

---

## Updated Dockerfile (Runner Stage)

```dockerfile
# Step 1: Copy standalone build
COPY --from=builder /app/.next/standalone /tmp/standalone/
RUN cp -r /tmp/standalone/[web/]* /app/

# Step 2: ⭐ CREATE static directory (CRITICAL!)
RUN mkdir -p /app/.next/static

# Step 3: Copy static files INTO existing directory
COPY --from=builder /app/.next/static /app/.next/static

# Step 4: Copy public files
COPY --from=builder /app/public ./public
```

---

## What Changed

| Before | After |
|--------|-------|
| Copy standalone → Copy static | Copy standalone → **mkdir static** → Copy static |
| Static COPY overwrites .next | Static COPY adds to existing .next |
| Server files lost | Server files preserved |
| Container crashes | Container works |

---

## Expected Directory Structure

```
/app/
├── server.js
├── .next/
│   ├── BUILD_ID           ← From standalone
│   ├── server/            ← From standalone
│   ├── build-manifest.json ← From standalone
│   ├── routes-manifest.json ← From standalone
│   └── static/            ← From separate COPY
│       ├── chunks/
│       └── media/
├── public/
├── node_modules/
└── package.json
```

---

## Build & Test Commands

```bash
# Build image
cd web/
docker build -t lankaconnect-web:test .

# Verify structure
bash ../scripts/verify-docker-build.sh lankaconnect-web:test

# Run container
docker run -d -p 3000:3000 --name test lankaconnect-web:test

# Check logs (should see "Ready in Xms")
docker logs test

# Test endpoints
curl http://localhost:3000/api/health
curl http://localhost:3000/

# Cleanup
docker stop test && docker rm test
```

---

## Troubleshooting

### Symptom: "Could not find a production build"
**Cause**: `.next` directory missing server files
**Fix**: Verify `mkdir -p /app/.next/static` exists before static COPY

### Symptom: "Cannot find module 'next/dist/server/next-server'"
**Cause**: `node_modules` not copied from standalone
**Fix**: Ensure `cp -r /tmp/standalone/[web/]* /app/` copies all files

### Symptom: Static assets (CSS/JS) return 404
**Cause**: `.next/static` directory missing or empty
**Fix**: Verify `COPY /app/.next/static /app/.next/static` succeeds

### Symptom: Monorepo build works, Docker isolated fails
**Cause**: Detection logic not handling both cases
**Fix**: Verify both `if [ -d "/tmp/standalone/web" ]` branches work

---

## Key Files

- **Dockerfile**: `c:\Work\LankaConnect\web\Dockerfile`
- **Architecture Doc**: `c:\Work\LankaConnect\docs\DOCKER_BUILD_ARCHITECTURE_FIX.md`
- **Test Plan**: `c:\Work\LankaConnect\docs\DOCKER_BUILD_TEST_PLAN.md`
- **Verification Script**: `c:\Work\LankaConnect\scripts\verify-docker-build.sh`

---

## Docker COPY Gotcha

```dockerfile
# ❌ WRONG: If static/ doesn't exist, replaces entire .next/
COPY /source/static ./.next/static

# ✅ RIGHT: Create directory first, then copy into it
RUN mkdir -p /app/.next/static
COPY /source/static /app/.next/static
```

**Why**: Docker treats non-existent subdirectories as targets, potentially replacing parent directories.

---

## CI/CD Integration

**GitHub Actions**: No changes needed. Fix handles both monorepo and isolated builds.

**Azure Container Apps**: Container will now start successfully.

**Local Development**: Use verification script to test before pushing.

---

## Success Indicators

- Build completes without errors
- Container starts in < 10 seconds
- Logs show "Ready in Xms"
- Health endpoint returns 200
- Homepage loads successfully
- Static assets (CSS/JS) serve correctly

---

## Emergency Rollback

If this fix causes issues, revert to:

```dockerfile
# Temporary workaround (not recommended long-term)
COPY --from=builder /app/.next /app/.next
COPY --from=builder /app/server.js /app/server.js
COPY --from=builder /app/node_modules /app/node_modules
COPY --from=builder /app/package.json /app/package.json
COPY --from=builder /app/public /app/public
```

Then investigate why standalone build structure differs from expected.

---

## Questions?

Contact: System Architecture Team
Phase: 6A.64 (Junction Table Implementation)
Date: 2026-01-07
