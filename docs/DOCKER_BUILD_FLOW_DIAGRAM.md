# Docker Build Flow Diagram - Next.js Standalone Fix

## Visual Architecture

### Before (Broken) - COPY Order Destroys .next Structure

```
┌─────────────────────────────────────────────────────────────┐
│ BUILDER STAGE OUTPUT                                        │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  /app/.next/standalone/web/                                 │
│  ├── server.js                                              │
│  ├── .next/                    ← Server runtime files       │
│  │   ├── BUILD_ID                                           │
│  │   ├── server/                                            │
│  │   ├── build-manifest.json                                │
│  │   └── routes-manifest.json                               │
│  ├── node_modules/                                          │
│  └── package.json                                           │
│                                                             │
│  /app/.next/static/            ← Static assets (separate!)  │
│  ├── chunks/                                                │
│  ├── media/                                                 │
│  └── K_wQyV6Y817wooD3BITQO/                                 │
│                                                             │
└─────────────────────────────────────────────────────────────┘
                           │
                           ▼
┌─────────────────────────────────────────────────────────────┐
│ RUNNER STAGE - STEP 1                                       │
├─────────────────────────────────────────────────────────────┤
│ COPY --from=builder /app/.next/standalone /tmp/standalone/  │
│                                                             │
│ cp -r /tmp/standalone/web/* /app/                           │
│                                                             │
│ Result: /app/                                               │
│  ├── server.js                                              │
│  ├── .next/           ← Has server files ✓                  │
│  │   ├── BUILD_ID                                           │
│  │   ├── server/                                            │
│  │   ├── build-manifest.json                                │
│  │   └── routes-manifest.json                               │
│  ├── node_modules/                                          │
│  └── package.json                                           │
└─────────────────────────────────────────────────────────────┘
                           │
                           ▼
┌─────────────────────────────────────────────────────────────┐
│ RUNNER STAGE - STEP 2 (BROKEN!)                             │
├─────────────────────────────────────────────────────────────┤
│ COPY --from=builder /app/.next/static ./.next/static        │
│                                                             │
│ ❌ PROBLEM: Docker REPLACES entire .next directory!         │
│                                                             │
│ Result: /app/                                               │
│  ├── server.js                                              │
│  ├── .next/           ← Now ONLY has static/ ❌             │
│  │   └── static/      ← Server files DESTROYED              │
│  │       ├── chunks/                                        │
│  │       └── media/                                         │
│  ├── node_modules/                                          │
│  └── package.json                                           │
│                                                             │
│ Missing: BUILD_ID, server/, manifests                       │
│                                                             │
│ Error at Runtime:                                           │
│ "Could not find a production build in './.next' directory"  │
└─────────────────────────────────────────────────────────────┘
```

---

### After (Fixed) - Preserve .next Structure

```
┌─────────────────────────────────────────────────────────────┐
│ BUILDER STAGE OUTPUT (Same as before)                       │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  /app/.next/standalone/web/                                 │
│  ├── server.js                                              │
│  ├── .next/                    ← Server runtime files       │
│  │   ├── BUILD_ID                                           │
│  │   ├── server/                                            │
│  │   ├── build-manifest.json                                │
│  │   └── routes-manifest.json                               │
│  ├── node_modules/                                          │
│  └── package.json                                           │
│                                                             │
│  /app/.next/static/            ← Static assets (separate!)  │
│  ├── chunks/                                                │
│  ├── media/                                                 │
│  └── K_wQyV6Y817wooD3BITQO/                                 │
│                                                             │
└─────────────────────────────────────────────────────────────┘
                           │
                           ▼
┌─────────────────────────────────────────────────────────────┐
│ RUNNER STAGE - STEP 1 (Same as before)                      │
├─────────────────────────────────────────────────────────────┤
│ COPY --from=builder /app/.next/standalone /tmp/standalone/  │
│                                                             │
│ cp -r /tmp/standalone/web/* /app/                           │
│                                                             │
│ Result: /app/                                               │
│  ├── server.js                                              │
│  ├── .next/           ← Has server files ✓                  │
│  │   ├── BUILD_ID                                           │
│  │   ├── server/                                            │
│  │   ├── build-manifest.json                                │
│  │   └── routes-manifest.json                               │
│  ├── node_modules/                                          │
│  └── package.json                                           │
└─────────────────────────────────────────────────────────────┘
                           │
                           ▼
┌─────────────────────────────────────────────────────────────┐
│ RUNNER STAGE - STEP 2 (NEW - Create directory)              │
├─────────────────────────────────────────────────────────────┤
│ mkdir -p /app/.next/static                                  │
│                                                             │
│ Result: /app/                                               │
│  ├── server.js                                              │
│  ├── .next/                                                 │
│  │   ├── BUILD_ID                                           │
│  │   ├── server/                                            │
│  │   ├── build-manifest.json                                │
│  │   ├── routes-manifest.json                               │
│  │   └── static/       ← Empty directory created            │
│  ├── node_modules/                                          │
│  └── package.json                                           │
└─────────────────────────────────────────────────────────────┘
                           │
                           ▼
┌─────────────────────────────────────────────────────────────┐
│ RUNNER STAGE - STEP 3 (FIXED - Copy INTO existing dir)      │
├─────────────────────────────────────────────────────────────┤
│ COPY --from=builder /app/.next/static /app/.next/static     │
│                                                             │
│ ✅ SOLUTION: Copies static files INTO existing directory    │
│                                                             │
│ Result: /app/                                               │
│  ├── server.js                                              │
│  ├── .next/           ← Complete structure! ✓               │
│  │   ├── BUILD_ID                    ← Preserved            │
│  │   ├── server/                     ← Preserved            │
│  │   ├── build-manifest.json         ← Preserved            │
│  │   ├── routes-manifest.json        ← Preserved            │
│  │   └── static/                     ← Added                │
│  │       ├── chunks/                                        │
│  │       ├── media/                                         │
│  │       └── K_wQyV6Y817wooD3BITQO/                         │
│  ├── node_modules/                                          │
│  └── package.json                                           │
│                                                             │
│ ✅ Container starts successfully!                           │
│ ✅ "Ready in 2000ms"                                        │
└─────────────────────────────────────────────────────────────┘
```

---

## Docker COPY Behavior Explained

### Problem: Target Directory Doesn't Exist

```dockerfile
# /app/.next exists (from standalone copy)
# /app/.next/static does NOT exist

COPY /source/static ./.next/static
```

**What happens**:
1. Docker sees target `./.next/static` doesn't exist
2. Docker treats `./.next` as target and REPLACES IT
3. Result: Entire `.next` directory becomes the static content
4. Server files (BUILD_ID, server/, manifests) are LOST

---

### Solution: Pre-Create Target Directory

```dockerfile
# /app/.next exists (from standalone copy)

# Create the subdirectory FIRST
mkdir -p /app/.next/static

# NOW the target exists
COPY /source/static /app/.next/static
```

**What happens**:
1. Target `/app/.next/static` EXISTS
2. Docker copies source INTO existing directory
3. Result: `.next` parent directory is PRESERVED
4. Server files remain intact, static files added

---

## File System Timeline

### Timeline: Before Fix (Broken)

```
T0: Builder Stage Completes
    /app/.next/standalone/web/.next/     [server files]
    /app/.next/static/                    [static files]

T1: Copy standalone to runner
    /app/.next/                           [server files] ✓

T2: Copy static (no mkdir)
    /app/.next/                           [static files only] ❌

    → Server files OVERWRITTEN
    → Runtime error: "Could not find production build"
```

---

### Timeline: After Fix (Working)

```
T0: Builder Stage Completes
    /app/.next/standalone/web/.next/     [server files]
    /app/.next/static/                    [static files]

T1: Copy standalone to runner
    /app/.next/                           [server files] ✓

T2: Create static directory
    /app/.next/static/                    [empty] ✓

T3: Copy static INTO existing directory
    /app/.next/                           [server files] ✓
    /app/.next/static/                    [static files] ✓

    → All files preserved
    → Runtime success: "Ready in 2000ms"
```

---

## Component Interaction Diagram

```
┌─────────────────────────────────────────────────────────┐
│                    Next.js Build                        │
│                   (npm run build)                       │
└────────────────┬────────────────────────────────────────┘
                 │
                 ▼
        ┌────────────────────┐
        │  output: standalone│
        └────────┬───────────┘
                 │
                 ├─────────────────┬─────────────────┐
                 ▼                 ▼                 ▼
         ┌───────────────┐  ┌──────────┐   ┌────────────┐
         │ .next/        │  │ .next/   │   │ public/    │
         │ standalone/   │  │ static/  │   │            │
         │               │  │          │   │            │
         │ Contains:     │  │ Contains:│   │ Contains:  │
         │ • server.js   │  │ • chunks/│   │ • images   │
         │ • .next/      │  │ • media/ │   │ • fonts    │
         │   - server/   │  │ • CSS    │   │            │
         │   - BUILD_ID  │  │ • JS     │   │            │
         │   - manifests │  │          │   │            │
         │ • node_modules│  │          │   │            │
         └───────┬───────┘  └────┬─────┘   └──────┬─────┘
                 │               │                 │
                 │               │                 │
                 ▼               ▼                 ▼
         ┌───────────────────────────────────────────────┐
         │         Docker COPY Operations                │
         │                                               │
         │  Step 1: COPY standalone → /app/              │
         │          (Establishes base structure)         │
         │                                               │
         │  Step 2: mkdir -p /app/.next/static           │
         │          (Prevents overwrite)                 │
         │                                               │
         │  Step 3: COPY static → /app/.next/static      │
         │          (Adds to existing structure)         │
         │                                               │
         │  Step 4: COPY public → /app/public            │
         │          (Independent directory)              │
         │                                               │
         └────────────────┬──────────────────────────────┘
                          │
                          ▼
                 ┌────────────────┐
                 │  Final Image   │
                 │                │
                 │  /app/         │
                 │  ├── server.js │
                 │  ├── .next/    │
                 │  │   ├── server/
                 │  │   ├── static/
                 │  │   └── ...    │
                 │  ├── public/   │
                 │  └── ...        │
                 └────────────────┘
```

---

## Decision Flow Chart

```
┌─────────────────────────────────────┐
│  Start: Docker COPY static files    │
└─────────────┬───────────────────────┘
              │
              ▼
      ┌───────────────────┐
      │ Does /app/.next/  │
      │ static/ exist?    │
      └───────┬───────────┘
              │
    ┌─────────┴─────────┐
    │                   │
   YES                 NO
    │                   │
    ▼                   ▼
┌────────────┐    ┌──────────────────┐
│ Copy files │    │ Docker creates   │
│ INTO       │    │ .next/static as  │
│ existing   │    │ TARGET and       │
│ directory  │    │ REPLACES parent  │
│            │    │ .next directory  │
│ ✅ Server  │    │                  │
│ files      │    │ ❌ Server files  │
│ preserved  │    │ destroyed        │
└────┬───────┘    └────┬─────────────┘
     │                 │
     ▼                 ▼
┌────────────┐    ┌──────────────────┐
│ SUCCESS    │    │ FAILURE          │
│ Container  │    │ "Could not find  │
│ starts     │    │ production build"│
└────────────┘    └──────────────────┘
```

**Solution**: Always create `/app/.next/static` BEFORE copying static files

---

## Key Architectural Insight

**The Problem Was Not With Next.js Standalone Mode**

Next.js correctly creates:
- `.next/standalone/` with server runtime
- `.next/static/` with client assets

**The Problem Was Docker COPY Semantics**

```dockerfile
# If target directory doesn't exist:
COPY source target/subdir
# → Creates target/subdir and copies INTO it (expected)

# BUT if target exists and subdir doesn't:
COPY source target/subdir
# → Docker may REPLACE target with source contents (unexpected!)
```

**Solution**: Pre-create all subdirectories to ensure Docker appends instead of replaces.

---

## References

- Docker COPY documentation: https://docs.docker.com/engine/reference/builder/#copy
- Next.js standalone mode: https://nextjs.org/docs/advanced-features/output-file-tracing
- ADR: `DOCKER_BUILD_ARCHITECTURE_FIX.md`
- Test Plan: `DOCKER_BUILD_TEST_PLAN.md`
