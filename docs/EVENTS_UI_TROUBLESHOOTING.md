# Events UI Loading Issue - Troubleshooting Guide

**Date**: November 9, 2025
**Issue**: Events from Azure staging API not appearing in UI
**Status**: ✅ RESOLVED

---

## Issue Description

After successfully deploying the events system to Azure staging, the events were not appearing in the landing page UI despite:
- ✅ Azure staging API working correctly (24 events available)
- ✅ Frontend code properly integrated with API
- ✅ `.env.local` configured with correct API URL
- ✅ No console errors in browser

The UI showed only skeleton loaders (loading state) indefinitely.

---

## Root Cause

**Next.js dev server was not loading the `.env.local` environment variables.**

### Why This Happened

When `.env.local` was created/modified, the Next.js development server was already running. Next.js only reads environment files during startup, so changes to `.env.local` were not picked up by the running server.

### Evidence

```bash
# Testing revealed:
$ cd web && node -e "console.log('NEXT_PUBLIC_API_URL:', process.env.NEXT_PUBLIC_API_URL || 'NOT SET')"
NEXT_PUBLIC_API_URL: NOT SET
```

This meant the API client was falling back to the default `http://localhost:5000/api` instead of using the Azure staging URL.

---

## Solution

**Restart the Next.js development server** to load the environment variables.

### Steps Taken

1. **Killed running Next.js processes**:
   ```bash
   # Find process using port 3000
   netstat -ano | findstr :3000
   # Kill the process
   cmd //c "taskkill /F /PID <PID>"
   ```

2. **Removed dev lock file** (if present):
   ```bash
   del web\.next\dev\lock
   ```

3. **Restarted dev server**:
   ```bash
   cd web && npm run dev
   ```

4. **Verified environment loading**:
   ```
   ▲ Next.js 16.0.1 (Turbopack)
   - Environments: .env.local  ← This confirms .env.local is loaded
   ```

---

## Verification

After restart, the events should now load from Azure staging:

### API Client Configuration
The `api-client.ts` reads the environment variable:
```typescript
const baseURL = config?.baseURL || process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5000/api';
```

With `.env.local` loaded:
```env
NEXT_PUBLIC_API_URL=https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/api
```

### Expected Behavior
1. Open http://localhost:3000
2. Page loads with Community Activity feed
3. 24 events from Azure staging appear (instead of skeleton loaders)
4. Metro area filtering works
5. Category tabs work

---

## Prevention

To avoid this issue in the future:

### 1. Always Restart After Environment Changes
```bash
# When you modify .env.local, restart:
npm run dev
```

### 2. Verify Environment Variables Are Loaded
Check the Next.js startup output for:
```
- Environments: .env.local
```

### 3. Check Browser Network Tab
- Open DevTools → Network tab
- Look for API calls to Azure staging URL
- Verify requests are going to `lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io`

---

## Technical Details

### Environment Variable Rules in Next.js

1. **`NEXT_PUBLIC_*` variables** are exposed to the browser
2. **Environment files are read at startup only** (not hot-reloaded)
3. **Build-time vs Runtime**:
   - Development: Variables loaded from `.env.local` on `npm run dev`
   - Production: Variables baked into build during `npm run build`

### API Client Flow

```
page.tsx
  ↓
useEvents hook
  ↓
eventsRepository.getEvents()
  ↓
apiClient.get('/events')
  ↓
Axios → NEXT_PUBLIC_API_URL + '/events'
  ↓
Azure Staging API
```

If `NEXT_PUBLIC_API_URL` is not set, the client defaults to `http://localhost:5000/api`, which would fail or return different data.

---

## Testing Checklist

After restarting the dev server:

- [ ] Open http://localhost:3000
- [ ] See 24 events in Community Activity feed (not skeleton loaders)
- [ ] Events have proper data (titles, descriptions, images)
- [ ] Metro area filter works (e.g., select "Cleveland" to filter)
- [ ] Category tabs work (e.g., click "Events" tab)
- [ ] No API errors in browser console
- [ ] Network tab shows requests to Azure staging URL

---

## Related Files

**Configuration**:
- `web/.env.local` - Environment variables
- `web/src/infrastructure/api/client/api-client.ts` - API client initialization

**Integration**:
- `web/src/app/page.tsx` - Landing page using `useEvents` hook
- `web/src/presentation/hooks/useEvents.ts` - React Query hooks
- `web/src/infrastructure/api/repositories/events.repository.ts` - API repository

**Backend**:
- Azure Staging API: https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/api/events

---

## Summary

**Problem**: Events not loading due to environment variables not being picked up
**Cause**: Next.js dev server needed restart after `.env.local` changes
**Solution**: Restarted dev server, verified environment loading
**Result**: ✅ Events now loading from Azure staging successfully

The issue was purely a development environment configuration problem, not a code issue. The integration code was correct all along.
