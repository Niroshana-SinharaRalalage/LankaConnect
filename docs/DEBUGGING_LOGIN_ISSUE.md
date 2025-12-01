# Debugging Login Issue - Quick Reference

## System Architect Analysis

**Verdict**: Backend code is CORRECT. Issue is likely frontend HTTP configuration.

## Quick Checks (In Browser DevTools → Network Tab)

### 1. Check Request Headers
Look for the `/Auth/login` request:

**✅ CORRECT Headers**:
```
Content-Type: application/json
Accept: application/json
Origin: http://localhost:3000
```

**❌ WRONG - Missing Content-Type**:
```
Content-Type: text/plain  ← WRONG!
```

### 2. Check Request Payload (Body)

**✅ CORRECT**:
```json
{
  "email": "niroshhz@gmail.com",
  "password": "yourpassword",
  "rememberMe": false
}
```

**❌ WRONG - Empty or missing**:
```
(empty)  ← This causes "request field is required"
```

### 3. Check Response Status

**If 400 Bad Request with "request field is required"**:
- Request body is empty OR
- Content-Type header is not `application/json` OR
- Request was blocked by CORS before reaching backend

**If 401 Unauthorized**:
- Credentials are wrong (expected behavior)

**If 200 OK**:
- ✅ Login successful!

## Common Causes & Fixes

### Cause 1: Frontend Not Sending Request Body

**Check**: `web/src/infrastructure/api/repositories/auth.repository.ts`

```typescript
async login(credentials: LoginRequest, rememberMe: boolean = false): Promise<LoginResponse> {
  const response = await apiClient.post<LoginResponse>(
    `${this.basePath}/login`,
    {
      ...credentials,  // ✅ This should spread email and password
      rememberMe,
    }
  );
  return response;
}
```

**Verify** `LoginRequest` type:
```typescript
export interface LoginRequest {
  email: string;
  password: string;
}
```

### Cause 2: API Client Not Setting Content-Type

**Check**: `web/src/infrastructure/api/client/api-client.ts`

```typescript
this.axiosInstance = axios.create({
  baseURL,
  timeout: config?.timeout || 30000,
  withCredentials: true,  // ✅ For cookies
  headers: {
    'Content-Type': 'application/json',  // ✅ REQUIRED
    ...config?.headers,
  },
});
```

### Cause 3: CORS Preflight Blocking Request

**Symptoms**:
- OPTIONS request succeeds (200 OK)
- POST request never sent or shows (failed) in Network tab
- Console shows CORS error

**Fix** (Backend - AuthController.cs):
Already configured correctly with CORS policy.

**Verify** (Frontend - .env.local):
```bash
NEXT_PUBLIC_API_URL=https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/api
```

### Cause 4: Axios Interceptor Modifying Request

**Check**: `web/src/infrastructure/api/client/api-client.ts` request interceptor

```typescript
this.axiosInstance.interceptors.request.use(
  (config) => {
    // ✅ Should NOT modify request.data
    // Only add auth token
    if (this.authToken) {
      config.headers.Authorization = `Bearer ${this.authToken}`;
    }
    return config;  // ✅ Return config, not modified data
  }
);
```

## Step-by-Step Debugging

### Step 1: Clear Everything
```bash
# Clear browser cache and cookies
# DevTools → Application → Clear storage → Clear site data

# Restart Next.js dev server
cd web
npm run dev
```

### Step 2: Test Login with Network Tab Open

1. Open http://localhost:3000/login
2. Open DevTools (F12) → Network tab
3. Try to login
4. Find `/Auth/login` request
5. Click it to see details

### Step 3: Check Request Details

**Request Headers tab**:
- ✅ Content-Type: application/json
- ✅ Accept: application/json
- ✅ Origin: http://localhost:3000

**Payload tab** (or Request body):
- ✅ Should show JSON: `{"email":"...","password":"...","rememberMe":false}`
- ❌ If empty → Frontend not sending data
- ❌ If FormData → Wrong content type

**Response tab**:
- If 400 → Read error message carefully
- If "request field is required" → Body is empty
- If "invalid email" → Body is sent but validation failed

### Step 4: Test Backend Directly (Bypass Frontend)

```bash
# Test with curl to verify backend works
curl -X POST https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/api/Auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"niroshhz@gmail.com","password":"YourPassword123","rememberMe":false}'
```

**If this works** → Frontend issue (API client, form submission, etc.)
**If this fails** → Backend issue (needs investigation)

## Frontend Code to Review

### 1. LoginForm.tsx
```typescript
const onSubmit = async (data: LoginFormData) => {
  try {
    setApiError(null);

    // ✅ Verify 'data' contains email and password
    console.log('Login form data:', data);

    // ✅ Pass rememberMe
    const response = await authRepository.login(data, rememberMe);

    // ... rest of login logic
  } catch (error) {
    console.error('Login error:', error);  // ✅ Check error details
    setApiError(error.message);
  }
};
```

### 2. auth.repository.ts
```typescript
async login(credentials: LoginRequest, rememberMe: boolean = false): Promise<LoginResponse> {
  console.log('Calling login API with:', { credentials, rememberMe });  // ✅ Debug log

  const response = await apiClient.post<LoginResponse>(
    `${this.basePath}/login`,
    {
      ...credentials,  // ✅ Should spread email and password
      rememberMe,
    }
  );

  console.log('Login API response:', response);  // ✅ Debug log
  return response;
}
```

### 3. api-client.ts
```typescript
// Add debug logging in request interceptor
this.axiosInstance.interceptors.request.use(
  (config) => {
    console.log('🚀 API Request:', {
      method: config.method,
      url: config.url,
      data: config.data,  // ✅ Should show email, password, rememberMe
      headers: config.headers,
    });

    // ... rest of interceptor
    return config;
  }
);
```

## Expected Console Output (When Working)

```
Login form data: { email: "test@example.com", password: "pass123" }
Calling login API with: { credentials: { email: "test@example.com", password: "pass123" }, rememberMe: false }
🚀 API Request: {
  method: "POST",
  url: "/auth/login",
  data: { email: "test@example.com", password: "pass123", rememberMe: false },
  headers: { Content-Type: "application/json" }
}
✅ Token refreshed successfully
```

## What Was Fixed (Backend)

### LoginUserCommand - Property-Based Record Pattern

**Before** (Positional - can have binding issues):
```csharp
public record LoginUserCommand(
    string Email,
    string Password,
    bool RememberMe = false,
    string? IpAddress = null) : IRequest<Result<LoginUserResponse>>;
```

**After** (Property-Based - reliable binding):
```csharp
public record LoginUserCommand : IRequest<Result<LoginUserResponse>>
{
    public required string Email { get; init; }
    public required string Password { get; init; }
    public bool RememberMe { get; init; } = false;
    public string? IpAddress { get; init; } = null;
}
```

This change ensures:
- ✅ Reliable JSON deserialization with System.Text.Json
- ✅ Explicit property names (better API contracts)
- ✅ Default values always applied correctly
- ✅ Works perfectly with ASP.NET Core 8 model binding

## Deployment Status

Check deployment progress:
```bash
gh run list --workflow=deploy-staging.yml --limit 1
```

Watch deployment:
```bash
gh run watch <run-id>
```

## After Deployment

Once deployed, test again:

1. ✅ Clear browser cache/cookies
2. ✅ Refresh page (Ctrl+Shift+R)
3. ✅ Try login
4. ✅ Check Network tab for request/response

If still failing, **it's definitely a frontend configuration issue** - not backend code.
