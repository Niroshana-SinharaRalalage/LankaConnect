# Authentication & Session Management Improvement Plan

## Problem Statement

Users are being logged out after **10 minutes** of activity, unlike Facebook/Gmail which keep users logged in for days or weeks. This creates a poor user experience.

## Current System Analysis

### Backend Configuration
- **Access Token Expiration**: 10 minutes (`appsettings.json:73`)
- **Refresh Token Expiration**: 7 days (`appsettings.json:74`)
- **Token Storage**: RefreshToken stored in HttpOnly cookie (✅ Secure!)
- **Clock Skew**: Zero (exact expiration, no grace period)
- **Refresh Endpoint**: `/api/Auth/refresh` (✅ Implemented!)

### Frontend Implementation
- **State Management**: Zustand with persist middleware (✅ Good!)
- **Token Storage**: localStorage for access/refresh tokens
- **Token Refresh**: ❌ **NOT IMPLEMENTED** - This is the main issue!
- **Interceptor**: Has unauthorized callback but doesn't attempt refresh

### Current Flow (Broken)
```
1. User logs in → Gets access token (10 min expiry)
2. After 10 minutes → Access token expires
3. Next API call → Returns 401 Unauthorized
4. Frontend → Redirects to login (no refresh attempt!)
```

## Root Causes

1. **Short access token expiration** (10 minutes is too aggressive)
2. **No automatic token refresh logic** in API client
3. **No proactive refresh** before token expires
4. **No "Remember Me"** functionality for extended sessions

## Solution: Facebook/Gmail-Style Sessions

### Phase 1: Increase Token Expiration ⏱️

**Backend Changes** (`appsettings.json`):
```json
"Jwt": {
  "AccessTokenExpirationMinutes": 30,  // 10 → 30 minutes
  "RefreshTokenExpirationDays": 30     // 7 → 30 days
}
```

**Rationale**:
- 30 minutes allows uninterrupted browsing sessions
- 30-day refresh token enables "Remember Me" functionality
- Balances security with user experience

### Phase 2: Automatic Token Refresh 🔄

**Implementation**: Add refresh interceptor to API client

**Logic**:
```typescript
1. Catch 401 Unauthorized
2. Check if refresh token exists
3. Call /api/Auth/refresh endpoint
4. If successful:
   - Update tokens in store
   - Retry original request
5. If failed:
   - Clear auth and redirect to login
```

**Key Features**:
- **Transparent** to user (happens in background)
- **Retry queue** prevents duplicate refresh calls
- **Race condition handling** for multiple failed requests
- **Token expiry check** before making requests (proactive)

### Phase 3: Proactive Token Refresh ⚡

**Auto-refresh before expiration**:
```typescript
- Decode JWT to get expiry time
- Set timer to refresh 5 minutes before expiry
- Silent refresh in background
- User never experiences 401 errors
```

### Phase 4: "Remember Me" Functionality 💾

**UI**: Add checkbox to login form
```tsx
☑️ Keep me logged in for 30 days
```

**Backend**: Extend refresh token expiration
```csharp
var refreshTokenExpiry = request.RememberMe
  ? DateTime.UtcNow.AddDays(30)
  : DateTime.UtcNow.AddDays(7);
```

**Frontend**: Persist remember me preference

## Implementation Plan

### Step 1: Update Token Expiration
- [ ] Update `appsettings.json`
- [ ] Update `appsettings.Staging.json`
- [ ] Update `appsettings.Production.json`

### Step 2: Implement Token Refresh Interceptor
- [ ] Create `TokenRefreshService`
- [ ] Add retry queue to prevent duplicate refreshes
- [ ] Integrate with API client interceptor
- [ ] Handle edge cases (expired refresh token, network errors)

### Step 3: Add Proactive Refresh
- [ ] Add JWT decoder utility
- [ ] Implement expiry tracking
- [ ] Set up background refresh timer
- [ ] Cancel timer on logout

### Step 4: Remember Me Feature
- [ ] Update `LoginUserCommand` with RememberMe flag
- [ ] Update login form UI
- [ ] Modify token generation logic
- [ ] Update auth repository

### Step 5: Testing
- [ ] Test token refresh on 401
- [ ] Test proactive refresh
- [ ] Test "Remember Me" checkbox
- [ ] Test logout clears all tokens
- [ ] Test concurrent requests during refresh

### Step 6: Documentation
- [ ] Update PROGRESS_TRACKER.md
- [ ] Update STREAMLINED_ACTION_PLAN.md
- [ ] Add inline code documentation
- [ ] Create user-facing documentation

## Security Considerations

### ✅ Maintained Security
1. **Refresh tokens** remain in HttpOnly cookies (XSS protection)
2. **Access tokens** have reasonable expiration (30 min)
3. **Refresh token rotation** (new token on each refresh)
4. **IP tracking** for suspicious activity detection

### ⚠️ Trade-offs
- Longer sessions = slightly higher risk if device compromised
- **Mitigation**: User can always manually logout
- **Mitigation**: Suspicious activity detection remains active

## Benefits

### User Experience
- ✅ Stay logged in for days/weeks (like Facebook/Gmail)
- ✅ Seamless browsing with no interruptions
- ✅ "Remember Me" for trusted devices
- ✅ No mid-task logouts

### Technical
- ✅ Reduced login frequency
- ✅ Better conversion rates (less friction)
- ✅ Improved user retention
- ✅ Professional UX matching industry standards

## Comparison: Before vs After

| Aspect | Before | After |
|--------|--------|-------|
| Access Token Expiry | 10 minutes | 30 minutes |
| Refresh Token Expiry | 7 days | 30 days |
| Session Duration | ~10 minutes | Days/weeks |
| Auto Refresh | ❌ No | ✅ Yes |
| Proactive Refresh | ❌ No | ✅ Yes |
| Remember Me | ❌ No | ✅ Yes |
| User Experience | Poor (frequent logouts) | Excellent (like Gmail) |

## Timeline

- **Phase 1**: 15 minutes (config changes)
- **Phase 2**: 2 hours (token refresh interceptor)
- **Phase 3**: 1 hour (proactive refresh)
- **Phase 4**: 1 hour (Remember Me feature)
- **Testing**: 1 hour
- **Documentation**: 30 minutes

**Total**: ~5-6 hours

## Next Steps

1. Get approval for extended token expiration times
2. Implement in order: Phase 1 → Phase 2 → Phase 3 → Phase 4
3. Test thoroughly in staging environment
4. Deploy to production
5. Monitor authentication metrics

## References

- JWT Best Practices: https://tools.ietf.org/html/rfc8725
- OWASP Session Management: https://cheatsheetseries.owasp.org/cheatsheets/Session_Management_Cheat_Sheet.html
- Current Implementation:
  - Backend: `src/LankaConnect.API/Extensions/AuthenticationExtensions.cs`
  - Frontend: `web/src/infrastructure/api/client/api-client.ts`
  - Auth Store: `web/src/presentation/store/useAuthStore.ts`
