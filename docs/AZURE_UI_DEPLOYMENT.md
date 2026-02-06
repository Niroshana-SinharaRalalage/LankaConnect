# Azure UI Deployment Documentation - LankaConnect

## Overview

This document provides comprehensive instructions for deploying the Next.js UI to Azure Container Apps staging environment.

**Solution**: Azure Container Apps (same platform as backend)
**Cost**: $0-5/month (within free tier)
**Scaling**: 0-3 replicas (scale-to-zero enabled)
**Region**: East US 2

---

## Prerequisites

- Azure CLI installed and configured
- Azure subscription with Container Apps environment
- GitHub repository with secrets configured
- Node.js 20+ installed locally

---

## Initial Container App Creation

**IMPORTANT**: This is a ONE-TIME setup. Run these commands only if the Container App doesn't exist yet.

### Step 1: Verify Azure Login

```bash
az login
az account show
```

### Step 2: Create UI Container App

```bash
az containerapp create \
  --name lankaconnect-ui-staging \
  --resource-group lankaconnect-staging \
  --environment lankaconnect-staging \
  --image mcr.microsoft.com/azuredocs/containerapps-helloworld:latest \
  --target-port 3000 \
  --ingress external \
  --min-replicas 0 \
  --max-replicas 3 \
  --cpu 0.25 \
  --memory 0.5Gi \
  --system-assigned \
  --health-probe-type http \
  --health-probe-path /api/health \
  --health-probe-interval 30 \
  --health-probe-timeout 10 \
  --health-probe-failure-threshold 3
```

**Parameters Explained**:
- `--name`: Container App name (lankaconnect-ui-staging)
- `--resource-group`: Azure resource group (lankaconnect-staging)
- `--environment`: Container Apps environment (lankaconnect-staging)
- `--image`: Initial placeholder image (replaced by GitHub Actions)
- `--target-port`: Port the app listens on (3000 for Next.js)
- `--ingress external`: Public internet access
- `--min-replicas 0`: Enable scale-to-zero for cost savings
- `--max-replicas 3`: Maximum instances under load
- `--cpu 0.25`: 0.25 vCPU per replica
- `--memory 0.5Gi`: 512 MB RAM per replica
- `--system-assigned`: Enable managed identity
- `--health-probe-*`: Health check configuration

### Step 3: Configure Environment Variables

```bash
az containerapp update \
  --name lankaconnect-ui-staging \
  --resource-group lankaconnect-staging \
  --set-env-vars \
    BACKEND_API_URL=https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/api \
    NEXT_PUBLIC_API_URL=/api/proxy \
    NEXT_PUBLIC_ENV=staging \
    NODE_ENV=production \
    NEXT_TELEMETRY_DISABLED=1
```

### Step 4: Get Container App URL

```bash
az containerapp show \
  --name lankaconnect-ui-staging \
  --resource-group lankaconnect-staging \
  --query properties.configuration.ingress.fqdn \
  --output tsv
```

Expected output: `lankaconnect-ui-staging.<random-subdomain>.eastus2.azurecontainerapps.io`

---

## CI/CD Deployment

After initial setup, deployments are automated via GitHub Actions.

### Trigger Deployment

**Automatic**: Push to `develop` branch with changes in `web/` directory
**Manual**: Use GitHub Actions workflow dispatch

### GitHub Secrets Required

The following secrets must be configured in GitHub repository settings:

- `AZURE_CREDENTIALS_STAGING`: Azure service principal JSON
- `ACR_USERNAME_STAGING`: Container registry username
- `ACR_PASSWORD_STAGING`: Container registry password

### Workflow File Location

`.github/workflows/deploy-ui-staging.yml`

### Workflow Steps

1. Checkout code
2. Setup Node.js 20
3. Install dependencies
4. Run linting and type checking
5. Run unit tests (non-blocking)
6. Validate environment variables
7. Build Next.js application
8. Login to Azure and ACR
9. Build and push Docker image
10. Deploy to Container Apps
11. Run smoke tests (health, home page, API proxy)

---

## Monitoring and Troubleshooting

### View Real-Time Logs

```bash
# Follow live logs
az containerapp logs show \
  --name lankaconnect-ui-staging \
  --resource-group lankaconnect-staging \
  --follow

# Filter errors only
az containerapp logs show \
  --name lankaconnect-ui-staging \
  --resource-group lankaconnect-staging \
  --type console \
  --format text | grep ERROR
```

### Check Container Status

```bash
az containerapp show \
  --name lankaconnect-ui-staging \
  --resource-group lankaconnect-staging
```

### View Metrics

```bash
az monitor metrics list \
  --resource /subscriptions/<subscription-id>/resourceGroups/lankaconnect-staging/providers/Microsoft.App/containerApps/lankaconnect-ui-staging \
  --metric-names Requests,CpuUsage,MemoryUsage
```

### Health Check Endpoint

```bash
# Test health endpoint
curl https://lankaconnect-ui-staging.<fqdn>/api/health
```

Expected response:
```json
{
  "status": "healthy",
  "service": "lankaconnect-ui",
  "timestamp": "2026-01-06T12:34:56.789Z",
  "uptime": "15h 30m 45s",
  "uptimeSeconds": 55845,
  "memory": {
    "rss": 120,
    "heapTotal": 85,
    "heapUsed": 65,
    "external": 10
  },
  "environment": "staging",
  "nodeVersion": "v20.x.x"
}
```

---

## Rollback Procedures

### Option 1: Instant Rollback (< 1 minute)

```bash
# List all revisions
az containerapp revision list \
  --name lankaconnect-ui-staging \
  --resource-group lankaconnect-staging

# Activate previous revision
az containerapp revision activate \
  --revision <previous-revision-name> \
  --resource-group lankaconnect-staging
```

### Option 2: Traffic Splitting (Canary)

```bash
# Route 50% to old, 50% to new
az containerapp ingress traffic set \
  --name lankaconnect-ui-staging \
  --resource-group lankaconnect-staging \
  --revision-weight <old-revision>=50 <new-revision>=50
```

### Option 3: Redeploy Previous Image

```bash
# Redeploy specific commit
az containerapp update \
  --name lankaconnect-ui-staging \
  --resource-group lankaconnect-staging \
  --image lankaconnectstaging.azurecr.io/lankaconnect-ui:<previous-commit-sha>
```

---

## Common Issues and Solutions

### Issue: Container fails to start

**Cause**: Invalid environment variables or port mismatch

**Solution**:
```bash
# Verify environment variables
az containerapp show \
  --name lankaconnect-ui-staging \
  --resource-group lankaconnect-staging \
  --query properties.template.containers[0].env

# Ensure BACKEND_API_URL and NEXT_PUBLIC_API_URL are set
# Ensure --target-port is 3000
```

### Issue: Cookies not working

**Cause**: Using direct backend URL instead of proxy

**Solution**:
- Verify `NEXT_PUBLIC_API_URL=/api/proxy` (not direct backend URL)
- Check browser dev tools → Application → Cookies
- Confirm cookies are set under UI domain

### Issue: Slow cold starts

**Cause**: Large Docker image or many dependencies

**Solution**:
- Multi-stage build already implemented (optimized)
- Consider increasing min replicas to 1 (eliminates cold starts, costs ~$5/month)

### Issue: API proxy 500 errors

**Cause**: Backend URL misconfigured

**Solution**:
```bash
# Check BACKEND_API_URL environment variable
az containerapp show \
  --name lankaconnect-ui-staging \
  --resource-group lankaconnect-staging \
  --query properties.template.containers[0].env

# Should be: https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/api
```

### Issue: Build failures in GitHub Actions

**Cause**: Missing dependencies or type errors

**Solution**:
1. Run `npm ci` locally to verify package-lock.json
2. Run `npm run build` locally to check for errors
3. Fix TypeScript errors before pushing
4. Check GitHub Actions logs for specific error messages

---

## Scaling Configuration

### Current Configuration

- **Min Replicas**: 0 (scale-to-zero enabled)
- **Max Replicas**: 3
- **CPU**: 0.25 vCPU per replica
- **Memory**: 0.5 GB per replica

### Update Scaling Rules

```bash
# Change to always-on (min 1 replica)
az containerapp update \
  --name lankaconnect-ui-staging \
  --resource-group lankaconnect-staging \
  --min-replicas 1 \
  --max-replicas 5

# Revert to scale-to-zero
az containerapp update \
  --name lankaconnect-ui-staging \
  --resource-group lankaconnect-staging \
  --min-replicas 0 \
  --max-replicas 3
```

### Add CPU-Based Autoscaling

```bash
az containerapp update \
  --name lankaconnect-ui-staging \
  --resource-group lankaconnect-staging \
  --scale-rule-name cpu-scale \
  --scale-rule-type cpu \
  --scale-rule-metadata type=Utilization value=70
```

---

## Cost Monitoring

### Current Cost Estimate

- **Staging**: $0-5/month (within free tier)
- **Free Tier Includes**:
  - 180,000 vCPU-seconds/month
  - 360,000 GiB-seconds/month
  - 2 million requests/month

### Monitor Usage

```bash
# View usage metrics
az monitor metrics list \
  --resource /subscriptions/<subscription-id>/resourceGroups/lankaconnect-staging/providers/Microsoft.App/containerApps/lankaconnect-ui-staging \
  --metric-names CpuUsage,MemoryUsage,Requests \
  --start-time 2026-01-01T00:00:00Z \
  --end-time 2026-01-31T23:59:59Z
```

---

## Security Configuration

### Current Security Features

- ✅ Non-root container (runs as user `nextjs`, UID 1001)
- ✅ HTTPS-only (automatic TLS from Container Apps)
- ✅ HttpOnly cookies (prevents XSS attacks)
- ✅ Managed identity (no passwords for Azure resources)
- ✅ Minimal base image (Alpine Linux, ~50 MB compressed)

### Future Enhancements (Production)

- [ ] VNet integration (internal communication with backend)
- [ ] Azure Front Door with WAF (Web Application Firewall)
- [ ] Key Vault integration for secrets
- [ ] Application Insights for advanced monitoring
- [ ] Custom domain with SSL certificate

---

## Testing Checklist

### Automated Tests (CI/CD)

- [x] ESLint linting
- [x] TypeScript type checking
- [x] Vitest unit tests
- [x] Build validation
- [x] Health check endpoint
- [x] Home page load test
- [x] API proxy connectivity test

### Manual Tests (Post-Deployment)

- [ ] Navigate to UI URL
- [ ] Login flow with HttpOnly cookies
- [ ] Cookie persistence across page navigation
- [ ] Token refresh functionality
- [ ] Image uploads via proxy
- [ ] Event CRUD operations (create, read, update, delete)
- [ ] Verify no CORS errors in browser console
- [ ] Check Container Apps logs for errors
- [ ] Test scale-to-zero (wait 30 min, verify cold start)
- [ ] Verify page load times (<3s for home page)

---

## Useful Commands Reference

### Container App Management

```bash
# List all container apps
az containerapp list --resource-group lankaconnect-staging --output table

# Show specific container app details
az containerapp show --name lankaconnect-ui-staging --resource-group lankaconnect-staging

# Restart container app
az containerapp revision restart --name lankaconnect-ui-staging --resource-group lankaconnect-staging

# List revisions
az containerapp revision list --name lankaconnect-ui-staging --resource-group lankaconnect-staging --output table

# Delete container app (USE WITH CAUTION)
az containerapp delete --name lankaconnect-ui-staging --resource-group lankaconnect-staging --yes
```

### Image Management

```bash
# List images in Container Registry
az acr repository list --name lankaconnectstaging

# List tags for specific image
az acr repository show-tags --name lankaconnectstaging --repository lankaconnect-ui --output table

# Delete specific image tag
az acr repository delete --name lankaconnectstaging --image lankaconnect-ui:<tag> --yes
```

### Environment Variables

```bash
# Show all environment variables
az containerapp show \
  --name lankaconnect-ui-staging \
  --resource-group lankaconnect-staging \
  --query properties.template.containers[0].env

# Update environment variable
az containerapp update \
  --name lankaconnect-ui-staging \
  --resource-group lankaconnect-staging \
  --set-env-vars KEY=VALUE
```

---

## Support and Documentation

### Internal Documentation

- [PROGRESS_TRACKER.md](./PROGRESS_TRACKER.md) - Project progress tracking
- [STREAMLINED_ACTION_PLAN.md](./STREAMLINED_ACTION_PLAN.md) - Action items and phases
- [TASK_SYNCHRONIZATION_STRATEGY.md](./TASK_SYNCHRONIZATION_STRATEGY.md) - Phase synchronization
- [Deployment Plan](../C:\Users\Niroshana\.claude\plans\golden-munching-allen.md) - Comprehensive deployment plan

### External Resources

- [Azure Container Apps Documentation](https://learn.microsoft.com/en-us/azure/container-apps/)
- [Azure Container Apps Pricing](https://azure.microsoft.com/en-us/pricing/details/container-apps/)
- [Next.js Deployment](https://nextjs.org/docs/app/building-your-application/deploying)
- [Next.js Standalone Output](https://nextjs.org/docs/app/api-reference/next-config-js/output)

---

## Deployment History

| Date | Commit SHA | Deployed By | Status | Notes |
|------|------------|-------------|--------|-------|
| 2026-01-06 | (pending) | GitHub Actions | Pending | Initial UI deployment to Azure Container Apps |

---

**Last Updated**: 2026-01-06
**Maintained By**: LankaConnect Development Team
