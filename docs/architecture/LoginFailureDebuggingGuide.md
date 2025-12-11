# Production Login Failure - Debugging Guide

**Error**: `400 Bad Request: "The request field is required"`
**Status**: Critical - Users Cannot Login
**Date**: 2025-11-30

---

## Quick Diagnosis

The error "The request field is required" typically means ASP.NET Core's model binder is trying to bind a parameter named "request" but the request body is empty or malformed.

**Key Finding**: LoginUserCommand code is CORRECT. The issue is in the HTTP request, not the C# code.

---

## Step-by-Step Debugging

### 1. Verify Frontend Request

#### Check Browser Network Tab

1. Open browser DevTools (F12)
2. Navigate to Network tab
3. Attempt login
4. Find the POST request to `/api/auth/login`
5. Check these details:

**Request Headers** (should include):
```http
POST /api/auth/login HTTP/1.1
Host: your-api-domain.com
Content-Type: application/json    ← CRITICAL: Must be "application/json"
Content-Length: 75                 ← Must be > 0
Origin: http://localhost:3000
```

**Request Payload** (should be):
```json
{
  "email": "user@example.com",
  "password": "pass123",
  "rememberMe": false
}
```

**Common Issues**:
- ❌ Missing `Content-Type: application/json` header
- ❌ Empty request body (Content-Length: 0)
- ❌ Request body sent as FormData instead of JSON
- ❌ CORS preflight failing (OPTIONS request gets 403/404)

---

### 2. Check API Logs (Serilog)

#### Expected Log Entries

**Successful Request**:
```log
[INF] HTTP POST /api/auth/login responded 200 in 45.2 ms
```

**Failed Request** (look for):
```log
[WRN] Login validation failed: ...
[ERR] Error during login for email: ...
[ERR] An error occurred during login
```

#### Add Enhanced Logging

Add this to `AuthController.cs` Login method (before line 94):

```csharp
[HttpPost("login")]
public async Task<IActionResult> Login([FromBody] LoginUserCommand request, ...)
{
    try
    {
        // ADD THIS DEBUGGING CODE
        _logger.LogInformation("Login attempt - Email: {Email}, RememberMe: {RememberMe}, IpAddress: {IpAddress}, HasPassword: {HasPassword}",
            request?.Email ?? "NULL",
            request?.RememberMe ?? false,
            request?.IpAddress ?? "NULL",
            !string.IsNullOrEmpty(request?.Password));

        if (request == null)
        {
            _logger.LogWarning("Login request is NULL - request body might be empty or malformed");
            return BadRequest(new { error = "Request body is required" });
        }

        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage);
            _logger.LogWarning("Login ModelState invalid: {Errors}", string.Join(", ", errors));
            return BadRequest(new { error = "Invalid request", details = errors });
        }
        // EXISTING CODE CONTINUES...
```

---

### 3. Verify Frontend Code

#### Check API Client Configuration

Look at `web/src/infrastructure/api/client/api-client.ts`:

```typescript
// VERIFY THIS CODE
export const apiClient = {
    post: async (url: string, data: any) => {
        const response = await fetch(`${API_BASE_URL}${url}`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',  // ← MUST BE HERE
                // ... other headers
            },
            credentials: 'include',
            body: JSON.stringify(data)  // ← MUST stringify the data
        });
        return response;
    }
};
```

**Common Frontend Issues**:
```typescript
// ❌ WRONG: Missing Content-Type
fetch('/api/auth/login', {
    method: 'POST',
    body: JSON.stringify(data)  // Missing headers!
});

// ❌ WRONG: Sending FormData instead of JSON
fetch('/api/auth/login', {
    method: 'POST',
    body: new FormData(...)  // Should be JSON!
});

// ❌ WRONG: Not stringifying JSON
fetch('/api/auth/login', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: data  // Should be JSON.stringify(data)
});

// ✅ CORRECT:
fetch('/api/auth/login', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    credentials: 'include',
    body: JSON.stringify(data)
});
```

---

### 4. Check CORS Configuration

#### Verify CORS Headers in Response

**Expected Response Headers**:
```http
HTTP/1.1 200 OK
Access-Control-Allow-Origin: http://localhost:3000
Access-Control-Allow-Credentials: true
Content-Type: application/json
```

**Check for CORS Errors** in browser console:
```
Access to fetch at 'https://api.com/auth/login' from origin 'http://localhost:3000'
has been blocked by CORS policy: Response to preflight request doesn't pass
access control check: No 'Access-Control-Allow-Origin' header is present...
```

#### Verify CORS Policy (Program.cs Lines 127-155)

```csharp
// DEVELOPMENT
policy.WithOrigins("http://localhost:3000", "https://localhost:3001")
      .AllowAnyMethod()
      .AllowAnyHeader()
      .AllowCredentials();  // ← CRITICAL for cookies

// STAGING
policy.WithOrigins(
          "http://localhost:3000",
          "https://localhost:3001",
          "https://lankaconnect-staging.azurestaticapps.net")  // ← Must match frontend URL
```

**Check which policy is active**:
```csharp
// Program.cs Lines 221-226
if (app.Environment.IsDevelopment())
    app.UseCors("Development");  // ← Which one is running?
else if (app.Environment.IsStaging())
    app.UseCors("Staging");
else
    app.UseCors("Production");
```

---

### 5. Check Model Binding

#### Add Request Body Logging Middleware

Add this to `Program.cs` (before `app.UseRouting()` or similar):

```csharp
// Add BEFORE controllers are mapped
app.Use(async (context, next) =>
{
    if (context.Request.Method == "POST" &&
        context.Request.Path.StartsWithSegments("/api/auth/login"))
    {
        // Enable buffering so we can read the body multiple times
        context.Request.EnableBuffering();

        // Read the request body
        using var reader = new StreamReader(
            context.Request.Body,
            encoding: System.Text.Encoding.UTF8,
            detectEncodingFromByteOrderMarks: false,
            leaveOpen: true);

        var body = await reader.ReadToEndAsync();

        // Log the raw request body
        var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
        logger.LogInformation("LOGIN REQUEST BODY: {Body}", body);
        logger.LogInformation("LOGIN CONTENT-TYPE: {ContentType}",
            context.Request.ContentType ?? "NULL");

        // Reset the stream position for the next middleware
        context.Request.Body.Position = 0;
    }

    await next();
});
```

**Expected Log Output**:
```log
[INF] LOGIN REQUEST BODY: {"email":"user@example.com","password":"pass123","rememberMe":false}
[INF] LOGIN CONTENT-TYPE: application/json; charset=utf-8
```

**If you see**:
```log
[INF] LOGIN REQUEST BODY:
[INF] LOGIN CONTENT-TYPE: NULL
```
Then the frontend is NOT sending the request body or Content-Type header!

---

### 6. Test with curl/Postman

#### Test API Directly

**curl Command**:
```bash
curl -X POST https://your-api.com/api/auth/login \
  -H "Content-Type: application/json" \
  -H "Origin: http://localhost:3000" \
  -d '{
    "email": "test@example.com",
    "password": "Test123!",
    "rememberMe": false
  }' \
  -v
```

**Expected Response**:
```json
{
  "user": {
    "userId": "...",
    "email": "test@example.com",
    ...
  },
  "accessToken": "eyJ...",
  "tokenExpiresAt": "2025-12-01T03:30:00Z"
}
```

**If curl works but frontend doesn't**: Problem is in the frontend code.

**If curl fails too**: Problem is in the API configuration.

---

### 7. Common Root Causes & Solutions

#### Issue 1: Missing Content-Type Header

**Symptom**: Error "The request field is required"

**Diagnosis**:
```log
[INF] LOGIN CONTENT-TYPE: NULL
```

**Fix**: Add `Content-Type: application/json` to frontend request:
```typescript
headers: {
    'Content-Type': 'application/json',
}
```

---

#### Issue 2: Empty Request Body

**Symptom**: Error "The request field is required"

**Diagnosis**:
```log
[INF] LOGIN REQUEST BODY:
[INF] LOGIN CONTENT-TYPE: application/json
```

**Fix**: Ensure body is stringified:
```typescript
body: JSON.stringify({
    email: email,
    password: password,
    rememberMe: rememberMe
})
```

---

#### Issue 3: CORS Preflight Blocking Request

**Symptom**: Browser shows CORS error, request never reaches API

**Diagnosis**: OPTIONS request gets 403/404/500

**Fix**: Ensure CORS middleware runs BEFORE authentication:
```csharp
// Program.cs - CORRECT ORDER:
app.UseCors("Development");      // ← FIRST
app.UseHttpsRedirection();
app.UseAuthentication();         // ← AFTER CORS
app.UseAuthorization();
app.MapControllers();
```

---

#### Issue 4: Middleware Consuming Request Body

**Symptom**: Body is empty when it reaches the controller

**Diagnosis**: Check for custom middleware that reads `Request.Body`

**Fix**: Call `context.Request.EnableBuffering()` and reset stream position:
```csharp
context.Request.EnableBuffering();
// ... read body ...
context.Request.Body.Position = 0;  // ← CRITICAL
```

---

#### Issue 5: Model Validation Failure

**Symptom**: ModelState.IsValid is false

**Diagnosis**: Check required properties
```csharp
public record LoginUserCommand
{
    public required string Email { get; init; }  // ← If missing in JSON, validation fails
}
```

**Fix**: Ensure frontend sends all required fields

---

### 8. Environment-Specific Checks

#### Development Environment
- ✅ API running on `https://localhost:5001` or similar
- ✅ Frontend running on `http://localhost:3000`
- ✅ CORS policy includes `http://localhost:3000`
- ✅ HTTPS certificate trusted (or HTTP allowed in dev)

#### Staging Environment
- ✅ API deployed to staging URL
- ✅ Frontend deployed to `https://lankaconnect-staging.azurestaticapps.net`
- ✅ CORS policy includes staging frontend URL
- ✅ HTTPS configured correctly
- ✅ Connection string points to staging database

#### Production Environment
- ✅ API deployed to production URL
- ✅ Frontend deployed to `https://lankaconnect.com`
- ✅ CORS policy includes ONLY production domains
- ✅ HTTPS enforced (no HTTP)
- ✅ Connection string points to production database
- ✅ Environment variable `ASPNETCORE_ENVIRONMENT=Production`

---

## Quick Fix Checklist

Use this checklist to quickly diagnose the issue:

### Frontend Checklist
- [ ] Browser Network tab shows POST request to `/api/auth/login`
- [ ] Request has `Content-Type: application/json` header
- [ ] Request body is not empty (Content-Length > 0)
- [ ] Request body is valid JSON
- [ ] Request body includes `email`, `password`, `rememberMe` fields
- [ ] No CORS errors in browser console
- [ ] Response headers include `Access-Control-Allow-Origin`

### Backend Checklist
- [ ] API is running and healthy (`/health` endpoint returns 200)
- [ ] CORS policy includes the frontend origin
- [ ] CORS middleware runs BEFORE authentication
- [ ] No custom middleware consuming request body
- [ ] Logs show the request reaching `AuthController.Login`
- [ ] LoginUserCommand properties match JSON field names (case-insensitive)
- [ ] All tests pass (`dotnet test --filter LoginUser`)

### Infrastructure Checklist
- [ ] Correct environment is running (Development/Staging/Production)
- [ ] Database connection is working
- [ ] No reverse proxy/load balancer issues
- [ ] SSL certificate is valid (if using HTTPS)
- [ ] Firewall/security groups allow traffic

---

## Resolution Steps

**Step 1**: Add logging middleware to capture raw request body

**Step 2**: Test login with curl/Postman to isolate frontend vs backend issue

**Step 3**: If curl works, problem is in frontend - check network tab

**Step 4**: If curl fails, problem is in backend - check CORS and model binding

**Step 5**: Compare working request (curl) vs failing request (frontend)

**Step 6**: Fix the identified issue

**Step 7**: Verify fix with integration tests

---

## Contact Points

- **Serilog Logs**: Check application logs for detailed error information
- **Health Check**: `GET /health` to verify API is running
- **Swagger UI**: `GET /swagger` (dev/staging only) to test endpoints manually

---

**Last Updated**: 2025-11-30
**Document Version**: 1.0
