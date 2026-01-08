# Container App Configuration Comparison Analysis

**Date**: 2026-01-07
**Issue**: Frontend UI Container App experiencing ImagePullBackOff errors while Backend API works correctly

## Executive Summary

### Root Cause Identified
The frontend UI container app is configured to use **managed identity authentication** for ACR, but the managed identity lacks the required **AcrPull** role assignment on the Azure Container Registry.

The backend API uses **username/password authentication** (admin credentials stored in secrets), which is why it works.

---

## Critical Configuration Differences

### 1. Registry Authentication Method

| Aspect | Backend API (WORKING) | Frontend UI (FAILING) | Impact |
|--------|----------------------|----------------------|--------|
| **Authentication Type** | Username/Password | Managed Identity | HIGH |
| **Identity Field** | `""` (empty) | `"system"` | CRITICAL |
| **Username** | `"lankaconnectstaging"` | `""` (empty) | CRITICAL |
| **Password Secret** | `"lankaconnectstagingazurecrio-lankaconnectstaging"` | `""` (empty) | CRITICAL |
| **Registry Server** | `lankaconnectstaging.azurecr.io` | `lankaconnectstaging.azurecr.io` | Same |

#### Backend Registry Configuration (Working)
```json
"registries": [
  {
    "identity": "",
    "passwordSecretRef": "lankaconnectstagingazurecrio-lankaconnectstaging",
    "server": "lankaconnectstaging.azurecr.io",
    "username": "lankaconnectstaging"
  }
]
```

#### Frontend Registry Configuration (Failing)
```json
"registries": [
  {
    "identity": "system",
    "passwordSecretRef": "",
    "server": "lankaconnectstaging.azurecr.io",
    "username": ""
  }
]
```

---

### 2. Managed Identity Configuration

| Aspect | Backend API | Frontend UI | Impact |
|--------|------------|-------------|--------|
| **Identity Type** | SystemAssigned | SystemAssigned | Same |
| **Principal ID** | `bf69f3f8-e9c6-464a-9d1f-0038f90e8d03` | `0e2af622-4c29-45fe-a9f4-01c81704074c` | Different |
| **Tenant ID** | `369a3c47-33b7-4baa-98b8-6ddf16a51a31` | `369a3c47-33b7-4baa-98b8-6ddf16a51a31` | Same |
| **ACR Role Assignment** | Not used (uses credentials) | **MISSING - Root Cause** | CRITICAL |

---

### 3. Secrets Configuration

| Aspect | Backend API | Frontend UI | Impact |
|--------|------------|-------------|--------|
| **Secrets Count** | 19 secrets | 0 secrets | HIGH |
| **ACR Password** | Stored as secret | Not configured | CRITICAL |
| **Key Vault Integration** | Yes (system identity) | Not configured | Medium |

#### Backend Secrets (Relevant)
```json
"secrets": [
  {
    "name": "lankaconnectstagingazurecrio-lankaconnectstaging"
  }
  // ... 18 other secrets
]
```

#### Frontend Secrets
```json
"secrets": null
```

---

### 4. Image Configuration

| Aspect | Backend API | Frontend UI | Impact |
|--------|------------|-------------|--------|
| **Image** | `lankaconnectstaging.azurecr.io/lankaconnect-api:80ae974a2204536c38dac13c354cdb03135d4eb2` | `lankaconnectstaging.azurecr.io/lankaconnect-ui:staging` | Different tags |
| **Registry** | Same ACR | Same ACR | Same |
| **Pull Success** | Yes | No (ImagePullBackOff) | CRITICAL |

---

### 5. Other Configuration Differences

| Aspect | Backend API | Frontend UI | Impact |
|--------|------------|-------------|--------|
| **Target Port** | 5000 | 3000 | Normal (different apps) |
| **Min Replicas** | 1 | 1 | Same |
| **Max Replicas** | 1 | 3 | Different scaling |
| **Environment Variables** | 19 vars | 6 vars | Normal (different apps) |
| **Provisioning State** | Succeeded | InProgress | Frontend deploying |
| **Latest Ready Revision** | `lankaconnect-api-staging--0000491` | `lankaconnect-ui-staging--fks5pme` | Backend stable |

---

## Root Cause Analysis

### Problem Statement
The frontend UI container app is configured to authenticate to ACR using its **system-assigned managed identity** (`identity: "system"`), but this identity has not been granted the **AcrPull** role on the Azure Container Registry.

### Why Backend Works
The backend API uses the traditional **username/password authentication** method with ACR admin credentials stored as a secret. This method doesn't require managed identity role assignments.

### Why Frontend Fails
The frontend UI attempts to use managed identity authentication but:
1. The managed identity exists (`0e2af622-4c29-45fe-a9f4-01c81704074c`)
2. The registry configuration specifies `identity: "system"`
3. But the managed identity lacks **AcrPull** role assignment on ACR
4. Result: **ImagePullBackOff** - authentication fails

---

## Solution Options

### Option 1: Add AcrPull Role to Managed Identity (RECOMMENDED)
This is the modern, secure approach using Azure RBAC.

**Pros**:
- No credentials to manage or rotate
- More secure (no stored passwords)
- Azure-recommended best practice
- Uses Azure AD authentication

**Cons**:
- Requires role assignment configuration
- Slightly more complex initial setup

### Option 2: Switch to Username/Password Authentication (QUICK FIX)
Match the backend's authentication method.

**Pros**:
- Quick fix (matches working backend)
- Proven to work
- No role assignment needed

**Cons**:
- Requires managing ACR admin credentials
- Less secure than managed identity
- Credentials need rotation

---

## Recommended Fix (Option 1)

### Step 1: Grant AcrPull Role to Frontend Managed Identity

```bash
# Get the ACR resource ID
ACR_ID=$(az acr show --name lankaconnectstaging --resource-group lankaconnect-staging --query id --output tsv)

# Assign AcrPull role to the frontend's managed identity
az role assignment create \
  --assignee 0e2af622-4c29-45fe-a9f4-01c81704074c \
  --role AcrPull \
  --scope $ACR_ID
```

### Step 2: Verify Role Assignment

```bash
# Verify the role assignment
az role assignment list \
  --assignee 0e2af622-4c29-45fe-a9f4-01c81704074c \
  --scope $ACR_ID \
  --output table
```

### Step 3: Trigger New Deployment

```bash
# Force a new revision to retry image pull
az containerapp revision copy \
  --name lankaconnect-ui-staging \
  --resource-group lankaconnect-staging
```

---

## Alternative Fix (Option 2)

### Update Frontend to Use Username/Password Authentication

```bash
# Get ACR admin password (if admin enabled)
ACR_PASSWORD=$(az acr credential show --name lankaconnectstaging --resource-group lankaconnect-staging --query "passwords[0].value" --output tsv)

# Update the container app with credentials
az containerapp update \
  --name lankaconnect-ui-staging \
  --resource-group lankaconnect-staging \
  --set-env-vars "DUMMY=update" \
  --registry-server lankaconnectstaging.azurecr.io \
  --registry-username lankaconnectstaging \
  --registry-password "$ACR_PASSWORD"
```

**Note**: This requires ACR admin account to be enabled:
```bash
# Check if admin is enabled
az acr show --name lankaconnectstaging --query adminUserEnabled

# If false, enable it
az acr update --name lankaconnectstaging --admin-enabled true
```

---

## Verification Steps

After applying the fix:

1. **Check role assignment**:
```bash
az role assignment list --assignee 0e2af622-4c29-45fe-a9f4-01c81704074c --output table
```

2. **Check container app status**:
```bash
az containerapp show --name lankaconnect-ui-staging --resource-group lankaconnect-staging --query properties.runningStatus
```

3. **Check revision provisioning**:
```bash
az containerapp revision list --name lankaconnect-ui-staging --resource-group lankaconnect-staging --query "[0].properties.provisioningState"
```

4. **Check replica status**:
```bash
az containerapp replica list --name lankaconnect-ui-staging --resource-group lankaconnect-staging --revision lankaconnect-ui-staging--0000006
```

5. **Check logs for image pull**:
```bash
az containerapp logs show --name lankaconnect-ui-staging --resource-group lankaconnect-staging --tail 50
```

---

## Additional Observations

### Environment Variables Issue (Secondary)
The frontend has a misconfigured environment variable:
```json
{
  "name": "NEXT_PUBLIC_API_URL",
  "value": "C:/Program Files/Git/api/proxy"  // Windows path - likely incorrect
}
```

This should probably be:
```bash
az containerapp update \
  --name lankaconnect-ui-staging \
  --resource-group lankaconnect-staging \
  --set-env-vars "NEXT_PUBLIC_API_URL=/api/proxy"
```

### Scaling Configuration
Frontend has autoscaling enabled (1-3 replicas) but no scaling rules defined. Backend has HTTP-based scaling rules. Consider adding:
```bash
az containerapp update \
  --name lankaconnect-ui-staging \
  --resource-group lankaconnect-staging \
  --scale-rule-name http-scaler \
  --scale-rule-type http \
  --scale-rule-http-concurrency 10
```

---

## Summary

**The fix is simple**: Grant the **AcrPull** role to the frontend's managed identity.

The frontend is trying to use modern managed identity authentication (which is good), but the identity wasn't granted permission to pull images from ACR. The backend works because it uses the older username/password method.

**Recommended command**:
```bash
az role assignment create \
  --assignee 0e2af622-4c29-45fe-a9f4-01c81704074c \
  --role AcrPull \
  --scope $(az acr show --name lankaconnectstaging --resource-group lankaconnect-staging --query id --output tsv)
```

Then force a new revision to retry the image pull.
