# Microsoft Entra External ID - Production Configuration Guide

**Last Updated:** 2025-10-28
**Version:** 1.0
**Status:** Production Ready ✅

---

## Overview

This document provides step-by-step instructions for configuring Microsoft Entra External ID authentication in LankaConnect production environment.

## Prerequisites

- Azure subscription with Entra External ID enabled
- LankaConnect application deployed
- Database migration `20251028_AddEntraExternalIdSupport` applied
- Environment variable management system (Azure Key Vault, AWS Secrets Manager, etc.)

---

## 1. Azure Entra Configuration

### 1.1 Create Application Registration

1. Navigate to [Azure Portal](https://portal.azure.com)
2. Go to **Microsoft Entra ID** → **App registrations** → **New registration**
3. Configure:
   - **Name**: `LankaConnect API - Production`
   - **Supported account types**: `Accounts in any organizational directory and personal Microsoft accounts`
   - **Redirect URI**: `https://app.lankaconnect.com/signin-oidc` (adjust to your domain)
4. Click **Register**

### 1.2 Configure API Permissions

1. Go to **API permissions** → **Add a permission**
2. Select **Microsoft Graph**
3. Add permissions:
   - `openid` (Delegated)
   - `profile` (Delegated)
   - `email` (Delegated)
   - `User.Read` (Delegated)
4. Click **Grant admin consent**

### 1.3 Generate Client Secret

1. Go to **Certificates & secrets** → **New client secret**
2. **Description**: `LankaConnect Production Secret`
3. **Expires**: `24 months` (recommended)
4. Click **Add**
5. **IMPORTANT**: Copy the secret value immediately (shown only once)

### 1.4 Configure Token Configuration

1. Go to **Token configuration** → **Add optional claim**
2. Token type: **ID**
3. Add claims:
   - `email`
   - `given_name`
   - `family_name`
   - `preferred_username`

---

## 2. Environment Variables

### 2.1 Required Environment Variables

Set these environment variables in your production environment:

```bash
# Database
export DATABASE_CONNECTION_STRING="Host=prod-db.postgres.azure.com;Database=LankaConnectDB;Username=admin;Password=<SECRET>"

# JWT Configuration
export JWT_SECRET_KEY="<GENERATE_STRONG_SECRET_256_BITS>"
export JWT_ISSUER="https://api.lankaconnect.com"
export JWT_AUDIENCE="https://app.lankaconnect.com"

# Entra External ID
export ENTRA_ENABLED="true"
export ENTRA_TENANT_ID="369a3c47-33b7-4baa-98b8-6ddf16a51a31"
export ENTRA_CLIENT_ID="957e9865-fca0-4236-9276-a8643a7193b5"
export ENTRA_AUDIENCE="api://957e9865-fca0-4236-9276-a8643a7193b5"

# Azure Storage
export AZURE_STORAGE_CONNECTION_STRING="DefaultEndpointsProtocol=https;AccountName=<NAME>;AccountKey=<KEY>"

# Email
export SMTP_HOST="smtp.sendgrid.net"
export SMTP_PORT="587"
export SMTP_USERNAME="apikey"
export SMTP_PASSWORD="<SENDGRID_API_KEY>"
export EMAIL_FROM_ADDRESS="noreply@lankaconnect.com"

# Redis
export REDIS_CONNECTION_STRING="redis-prod.cache.windows.net:6380,password=<SECRET>,ssl=True"
```

### 2.2 Generate Strong Secrets

```bash
# Generate JWT secret (256-bit)
openssl rand -base64 32

# Example output: "kQ7vJ9xP2mN8cR4wF6yH1zT5gL3dS0aU"
```

---

## 3. Database Migration

### 3.1 Apply Migration to Production

**IMPORTANT**: Backup database before applying migration!

```bash
# 1. Backup production database
pg_dump -h prod-db.postgres.azure.com -U admin -d LankaConnectDB > backup_$(date +%Y%m%d_%H%M%S).sql

# 2. Apply migration using generated SQL script
psql -h prod-db.postgres.azure.com -U admin -d LankaConnectDB -f docs/deployment/migrations/20251028_AddEntraExternalIdSupport.sql

# 3. Verify migration
psql -h prod-db.postgres.azure.com -U admin -d LankaConnectDB -c "\d users"
```

### 3.2 Verify Migration

```sql
-- Check columns were added
SELECT column_name, data_type, is_nullable
FROM information_schema.columns
WHERE table_name = 'users'
  AND column_name IN ('IdentityProvider', 'ExternalProviderId');

-- Expected output:
-- IdentityProvider    | integer            | NO
-- ExternalProviderId  | character varying  | YES

-- Verify default value (existing users should have IdentityProvider = 0)
SELECT "IdentityProvider", COUNT(*)
FROM "Users"
GROUP BY "IdentityProvider";

-- Expected output:
-- IdentityProvider | count
-- 0               | <number of existing users>
```

---

## 4. Application Deployment

### 4.1 Docker Deployment

```dockerfile
# Dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 5000 5001

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["src/LankaConnect.API/LankaConnect.API.csproj", "src/LankaConnect.API/"]
COPY ["src/LankaConnect.Application/LankaConnect.Application.csproj", "src/LankaConnect.Application/"]
COPY ["src/LankaConnect.Domain/LankaConnect.Domain.csproj", "src/LankaConnect.Domain/"]
COPY ["src/LankaConnect.Infrastructure/LankaConnect.Infrastructure.csproj", "src/LankaConnect.Infrastructure/"]
RUN dotnet restore "src/LankaConnect.API/LankaConnect.API.csproj"
COPY . .
WORKDIR "/src/src/LankaConnect.API"
RUN dotnet build "LankaConnect.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "LankaConnect.API.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENV ASPNETCORE_ENVIRONMENT=Production
ENTRYPOINT ["dotnet", "LankaConnect.API.dll"]
```

### 4.2 Kubernetes Deployment

```yaml
# k8s/deployment.yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: lankaconnect-api
spec:
  replicas: 3
  selector:
    matchLabels:
      app: lankaconnect-api
  template:
    metadata:
      labels:
        app: lankaconnect-api
    spec:
      containers:
      - name: api
        image: lankaconnect.azurecr.io/api:latest
        ports:
        - containerPort: 5000
        env:
        - name: ASPNETCORE_ENVIRONMENT
          value: "Production"
        - name: DATABASE_CONNECTION_STRING
          valueFrom:
            secretKeyRef:
              name: lankaconnect-secrets
              key: database-connection-string
        - name: JWT_SECRET_KEY
          valueFrom:
            secretKeyRef:
              name: lankaconnect-secrets
              key: jwt-secret-key
        - name: ENTRA_ENABLED
          value: "true"
        - name: ENTRA_TENANT_ID
          value: "369a3c47-33b7-4baa-98b8-6ddf16a51a31"
        - name: ENTRA_CLIENT_ID
          value: "957e9865-fca0-4236-9276-a8643a7193b5"
        resources:
          requests:
            memory: "512Mi"
            cpu: "500m"
          limits:
            memory: "1Gi"
            cpu: "1000m"
        livenessProbe:
          httpGet:
            path: /health
            port: 5000
          initialDelaySeconds: 30
          periodSeconds: 10
        readinessProbe:
          httpGet:
            path: /health
            port: 5000
          initialDelaySeconds: 10
          periodSeconds: 5
```

---

## 5. Smoke Testing

### 5.1 Health Check

```bash
curl https://api.lankaconnect.com/health

# Expected response:
{
  "status": "Healthy",
  "service": "Authentication",
  "timestamp": "2025-10-28T20:00:00Z"
}
```

### 5.2 Test Entra Login Endpoint

```bash
# Get valid Entra access token from frontend login flow
# Then test backend endpoint

curl -X POST https://api.lankaconnect.com/api/auth/login/entra \
  -H "Content-Type: application/json" \
  -d '{
    "accessToken": "<VALID_ENTRA_TOKEN>",
    "ipAddress": "203.0.113.1"
  }'

# Expected response (200 OK):
{
  "user": {
    "userId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "email": "user@example.com",
    "fullName": "John Doe",
    "role": "User",
    "isNewUser": true
  },
  "accessToken": "eyJ0eXAiOiJKV1QiLCJhbGciOiJIUzI1NiJ9...",
  "tokenExpiresAt": "2025-10-28T20:15:00Z"
}
```

### 5.3 Verify Database

```sql
-- Check new Entra users are being created
SELECT "Id", "Email", "IdentityProvider", "ExternalProviderId", "CreatedAt"
FROM "Users"
WHERE "IdentityProvider" = 1 -- EntraExternal
ORDER BY "CreatedAt" DESC
LIMIT 10;
```

---

## 6. Monitoring

### 6.1 Key Metrics to Monitor

```
- Entra login success rate (target: >99%)
- Entra login latency (target: <500ms p95)
- Token validation errors (alert threshold: >1% error rate)
- Auto-provisioning failures (alert threshold: >0)
- Profile sync failures (log only, non-blocking)
```

### 6.2 Logging

```csharp
// Successful Entra login
2025-10-28 20:00:00.000 +00:00 [INF] User logged in with Entra External ID: user@example.com (IsNewUser: True)

// Failed token validation
2025-10-28 20:00:01.000 +00:00 [WRN] Entra login failed: Invalid access token

// Profile sync
2025-10-28 20:00:02.000 +00:00 [INF] Syncing profile changes from Entra for user {UserId} (FirstName: Old→New, LastName: Old→New)
```

---

## 7. Rollback Procedures

### 7.1 Disable Entra (Emergency)

```bash
# Set environment variable to disable Entra
export ENTRA_ENABLED="false"

# Restart application
kubectl rollout restart deployment/lankaconnect-api

# Users can still login with local passwords
```

### 7.2 Rollback Migration

```bash
# Restore from backup
psql -h prod-db.postgres.azure.com -U admin -d LankaConnectDB < backup_20251028_150000.sql

# Verify rollback
psql -h prod-db.postgres.azure.com -U admin -d LankaConnectDB -c "\d users"
```

---

## 8. Security Considerations

### 8.1 Secrets Management

✅ **DO**:
- Store secrets in Azure Key Vault or AWS Secrets Manager
- Rotate client secrets every 6-12 months
- Use managed identities for Azure resources
- Enable audit logging for secret access

❌ **DON'T**:
- Hardcode secrets in appsettings.json
- Commit secrets to git repository
- Share secrets via email or Slack
- Reuse secrets across environments

### 8.2 Token Validation

```json
{
  "EntraExternalId": {
    "ValidateIssuer": true,          // ✅ Always true in production
    "ValidateAudience": true,        // ✅ Always true in production
    "ValidateLifetime": true,        // ✅ Always true in production
    "ValidateIssuerSigningKey": true,// ✅ Always true in production
    "ClockSkew": "00:05:00"          // ✅ 5 minutes tolerance for clock drift
  }
}
```

---

## 9. Troubleshooting

### 9.1 Common Issues

**Issue**: `Invalid access token` error
**Solution**: Verify token is not expired, check audience matches `api://YOUR_CLIENT_ID`

**Issue**: `Email already registered with different provider`
**Solution**: This is expected behavior. User must use original login method.

**Issue**: `Entra External ID authentication is not enabled`
**Solution**: Check `ENTRA_ENABLED` environment variable is set to `"true"`

**Issue**: Database column not found
**Solution**: Verify migration was applied: `SELECT column_name FROM information_schema.columns WHERE table_name = 'users'`

### 9.2 Diagnostic Queries

```sql
-- Count users by provider
SELECT
  CASE "IdentityProvider"
    WHEN 0 THEN 'Local'
    WHEN 1 THEN 'EntraExternal'
    ELSE 'Unknown'
  END as Provider,
  COUNT(*) as UserCount
FROM "Users"
GROUP BY "IdentityProvider";

-- Find users with missing ExternalProviderId
SELECT "Id", "Email", "IdentityProvider", "ExternalProviderId"
FROM "Users"
WHERE "IdentityProvider" = 1 AND "ExternalProviderId" IS NULL;
```

---

## 10. Support Contacts

**Azure Entra Support**: https://azure.microsoft.com/support
**LankaConnect Team**: devops@lankaconnect.com
**Documentation**: https://docs.lankaconnect.com/entra

---

**Document Version**: 1.0
**Last Reviewed**: 2025-10-28
**Next Review**: 2025-11-28
