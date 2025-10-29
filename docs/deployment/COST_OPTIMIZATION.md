# Azure Cost Optimization Guide - LankaConnect

**Last Updated:** 2025-10-28
**Version:** 1.0
**Target Audience:** DevOps, Finance, Architecture Teams

---

## Executive Summary

This document provides cost optimization strategies for LankaConnect's Azure deployment, balancing performance, availability, and budget constraints.

**Monthly Cost Estimates:**
- **Staging:** $45-55/month (MVP, minimal redundancy)
- **Production (Basic):** $250-300/month (no HA, basic scaling)
- **Production (HA):** $450-500/month (zone-redundant, full scaling)

---

## Staging Environment Cost Breakdown

### Current Configuration (MVP)

| Resource | SKU/Tier | Monthly Cost | Notes |
|----------|----------|--------------|-------|
| Container Apps Environment | Consumption | $0 | No base charge |
| Container App (1 replica) | 0.25 vCPU, 0.5 GB | $15-20 | Pay per vCPU-second |
| PostgreSQL Flexible (B1ms) | Burstable, 1 vCore, 2 GB | $12 | 32 GB storage included |
| Container Registry (Basic) | 10 GB storage | $5 | Includes 10 GB storage |
| Key Vault (Standard) | - | $3 | 10K transactions free, then $0.03/10K |
| Log Analytics | 30-day retention | $5-10 | 5 GB free, then $2.76/GB |
| Bandwidth | - | $5 | 100 GB free, then $0.087/GB |
| **Total Staging** | | **$45-55/month** | |

### Optimization Strategies (Staging)

#### 1. Scale to Zero During Off-Hours

**Savings:** $8-10/month (40% reduction in Container Apps cost)

```bash
# Automated scaling schedule (Azure Automation or GitHub Actions)

# Scale down at 6 PM EST (end of work day)
az containerapp update \
  --name lankaconnect-api-staging \
  --resource-group lankaconnect-staging \
  --min-replicas 0 \
  --max-replicas 1

# Scale up at 8 AM EST (start of work day)
az containerapp update \
  --name lankaconnect-api-staging \
  --resource-group lankaconnect-staging \
  --min-replicas 1 \
  --max-replicas 3
```

**Implementation:**
```yaml
# .github/workflows/scale-staging.yml
name: Scale Staging Environment

on:
  schedule:
    - cron: '0 1 * * 1-5'  # 6 PM EST Mon-Fri (scale down)
    - cron: '0 13 * * 1-5' # 8 AM EST Mon-Fri (scale up)

jobs:
  scale:
    runs-on: ubuntu-latest
    steps:
      - name: Azure Login
        uses: azure/login@v2
        with:
          creds: ${{ secrets.AZURE_CREDENTIALS_STAGING }}

      - name: Scale Down (Evening)
        if: github.event.schedule == '0 1 * * 1-5'
        run: |
          az containerapp update \
            --name lankaconnect-api-staging \
            --resource-group lankaconnect-staging \
            --min-replicas 0

      - name: Scale Up (Morning)
        if: github.event.schedule == '0 13 * * 1-5'
        run: |
          az containerapp update \
            --name lankaconnect-api-staging \
            --resource-group lankaconnect-staging \
            --min-replicas 1
```

#### 2. Use Development Storage Emulator (Skip Blob Storage)

**Savings:** $10-15/month

```json
// appsettings.Staging.json
{
  "AzureBlobStorage": {
    "ConnectionString": "UseDevelopmentStorage=true"
  }
}
```

**Trade-off:** Cannot test Azure Blob-specific features (CDN, geo-replication).

#### 3. Skip Redis Cache in Staging

**Savings:** $30/month (defer to production)

```json
// Use in-memory caching for staging
{
  "ConnectionStrings": {
    "Redis": "localhost:6379"  // Fallback to in-memory cache
  }
}
```

**Implementation:**
```csharp
// DependencyInjection.cs
services.AddDistributedMemoryCache(); // Use in-memory for staging
// services.AddStackExchangeRedisCache(...); // Only for production
```

#### 4. Reduce Log Retention Period

**Savings:** $5/month

```bash
az monitor log-analytics workspace update \
  --resource-group lankaconnect-staging \
  --workspace-name lankaconnect-staging-logs \
  --retention-time 7  # Reduce from 30 to 7 days
```

#### 5. Delete Staging Environment on Weekends

**Savings:** $15-20/month (30-40% total reduction)

```bash
# Friday evening script
az group delete --name lankaconnect-staging --yes --no-wait

# Monday morning script
./scripts/azure/provision-staging.sh
```

**Trade-off:** Requires re-provisioning (30 minutes manual work).

---

## Production Environment Cost Breakdown

### Basic Configuration (No HA)

| Resource | SKU/Tier | Monthly Cost | Notes |
|----------|----------|--------------|-------|
| Container Apps (2-5 replicas) | 0.5 vCPU, 1 GB | $80-120 | Avg 3 replicas |
| PostgreSQL Flexible (D2ds_v5) | 2 vCores, 8 GB | $120 | 64 GB storage |
| Redis Cache (C1) | 1 GB | $30 | Basic tier |
| Blob Storage | 50 GB | $10 | Standard LRS |
| SendGrid | 50K emails/month | $15 | Essentials plan |
| Container Registry (Standard) | 100 GB | $20 | Geo-replication disabled |
| Key Vault | - | $3 | Standard tier |
| Application Insights | 10 GB/month | $20-30 | 5 GB free, then $2.30/GB |
| Bandwidth | 200 GB/month | $20-30 | First 100 GB free |
| **Total Production (Basic)** | | **$318-378/month** | |

### High Availability Configuration

| Resource | Additional Cost | Total Monthly Cost |
|----------|----------------|-------------------|
| PostgreSQL HA (Zone-Redundant) | +$120 | $240 |
| Redis Cache Premium P1 | +$150 | $180 |
| Blob Storage (ZRS) | +$2 | $12 |
| Container Registry (Geo-Replication) | +$20 | $40 |
| **Total Production (HA)** | **+$292** | **$610-670/month** |

---

## Cost Optimization Strategies (Production)

### 1. Defer High Availability Until Scale

**Savings:** $120-150/month initially

**Recommendation:**
- **0-1,000 users:** Use single-zone PostgreSQL (Burstable or General Purpose)
- **1,000-10,000 users:** Enable zone-redundant HA
- **10,000+ users:** Add read replicas

**Monitoring Trigger:**
```bash
# Enable HA when database CPU consistently >60%
az monitor metrics alert create \
  --name "Enable HA Trigger" \
  --resource-group lankaconnect-prod \
  --scopes /subscriptions/.../providers/Microsoft.DBforPostgreSQL/flexibleServers/lankaconnect-prod-db \
  --condition "avg Percentage CPU > 60" \
  --window-size 1h \
  --evaluation-frequency 15m
```

### 2. Use Consumption-Based Container Apps Scaling

**Savings:** $40-60/month (vs always-on replicas)

```bash
# Configure auto-scaling rules
az containerapp update \
  --name lankaconnect-api-prod \
  --resource-group lankaconnect-prod \
  --min-replicas 2 \
  --max-replicas 10 \
  --scale-rule-name http-scaling \
  --scale-rule-type http \
  --scale-rule-http-concurrency 50
```

**Scaling Strategy:**
- **Min Replicas:** 2 (for HA)
- **Max Replicas:** 10 (handle traffic spikes)
- **Scale Trigger:** 50 concurrent requests per replica
- **Cost:** Pay only for active replicas

### 3. Use Azure Reserved Instances (1-Year Commitment)

**Savings:** 30-40% on compute and database

```bash
# Example: Reserve PostgreSQL D2ds_v5 for 1 year
az reservations reservation-order purchase \
  --reservation-order-id YOUR_ORDER_ID \
  --sku-name Standard_D2ds_v5 \
  --term P1Y \
  --billing-frequency Monthly
```

**Eligible Resources:**
- PostgreSQL Flexible Server: **30% savings** ($120 → $84/month)
- Container Apps vCPU: **Not eligible** (consumption pricing only)
- Redis Cache: **35% savings** ($30 → $19.50/month)

**Total Savings:** $45-50/month with 1-year commitment.

### 4. Optimize Log Analytics Ingestion

**Savings:** $10-20/month

```csharp
// Reduce verbose logging in production
builder.Services.AddSerilog((services, lc) => lc
    .MinimumLevel.Information()  // Was Debug in staging
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
);
```

**Additional:**
- Archive logs to Azure Blob Storage after 30 days ($0.01/GB vs $2.76/GB)
- Sample high-volume logs (e.g., only 10% of HTTP requests)

### 5. Use Azure CDN for Static Assets

**Savings:** $10-15/month on bandwidth

```bash
# Create Azure CDN endpoint
az cdn endpoint create \
  --resource-group lankaconnect-prod \
  --profile-name lankaconnect-cdn \
  --name lankaconnect-static \
  --origin lankaconnectprodst.blob.core.windows.net \
  --origin-host-header lankaconnectprodst.blob.core.windows.net
```

**Benefits:**
- Reduce bandwidth costs by 50% (CDN caching)
- Improve performance (edge locations)
- Cost: $0.081/GB (CDN) vs $0.087/GB (direct bandwidth)

### 6. Implement Blob Storage Lifecycle Policies

**Savings:** $5-10/month

```json
{
  "rules": [
    {
      "name": "move-old-images-to-cool",
      "type": "Lifecycle",
      "definition": {
        "filters": {
          "blobTypes": ["blockBlob"],
          "prefixMatch": ["business-images/"]
        },
        "actions": {
          "baseBlob": {
            "tierToCool": {
              "daysAfterModificationGreaterThan": 90
            },
            "tierToArchive": {
              "daysAfterModificationGreaterThan": 365
            }
          }
        }
      }
    }
  ]
}
```

**Storage Tier Pricing:**
- **Hot:** $0.0184/GB (first 50 TB)
- **Cool:** $0.01/GB (90+ days old)
- **Archive:** $0.00099/GB (1+ years old)

### 7. Right-Size Database After Launch

**Savings:** $40-60/month (if traffic is lower than expected)

```bash
# Monitor database metrics for 1 month
az monitor metrics list \
  --resource /subscriptions/.../providers/Microsoft.DBforPostgreSQL/flexibleServers/lankaconnect-prod-db \
  --metric "cpu_percent" \
  --start-time 2025-10-01T00:00:00Z \
  --end-time 2025-10-31T23:59:59Z

# If avg CPU < 30%, downgrade to D2s_v3 (save $40/month)
az postgres flexible-server update \
  --resource-group lankaconnect-prod \
  --name lankaconnect-prod-db \
  --sku-name Standard_D2s_v3
```

---

## Cost Monitoring & Alerts

### Set Budget Alerts

```bash
# Create monthly budget for production
az consumption budget create \
  --budget-name lankaconnect-prod-budget \
  --amount 400 \
  --category Cost \
  --time-grain Monthly \
  --start-date 2025-10-01 \
  --end-date 2026-10-01 \
  --resource-group lankaconnect-prod \
  --notification Enabled=true Threshold=80 ContactEmails=["finance@lankaconnect.com"]

# Alert at 80% ($320) and 100% ($400)
az consumption budget create-notification \
  --budget-name lankaconnect-prod-budget \
  --notification-key AlertAt100 \
  --enabled true \
  --threshold 100 \
  --contact-emails finance@lankaconnect.com
```

### Daily Cost Report

```bash
# Get yesterday's cost
az consumption usage list \
  --start-date $(date -d "yesterday" +%Y-%m-%d) \
  --end-date $(date +%Y-%m-%d) \
  --query "[].{Service:properties.meterName, Cost:properties.pretaxCost}" \
  -o table

# Example output:
# Service                  Cost
# -----------------------  ------
# Container Apps vCPU      $2.50
# PostgreSQL Compute       $4.00
# Blob Storage             $0.50
# ... (daily breakdown)
```

---

## ROI Analysis: Cost vs Features

### Scenario 1: Bootstrap Startup (Year 1)

**Goal:** Minimize costs while validating product-market fit

**Configuration:**
- Staging: $50/month (scaled to zero on weekends)
- Production (Basic): $300/month (no HA)
- **Total:** $350/month = **$4,200/year**

**Trade-offs:**
- Single-zone database (99.9% SLA = ~8 hours downtime/year)
- Manual scaling during traffic spikes
- No CDN (slower image loading for international users)

**Recommendation:** ✅ Start here, enable HA at 1,000+ users

### Scenario 2: Growth Stage (Year 2)

**Goal:** Support 10,000+ users with high availability

**Configuration:**
- Staging: $50/month
- Production (HA): $550/month
- Reserved Instances (1-year): -$50/month
- **Total:** $550/month = **$6,600/year**

**Benefits:**
- Zone-redundant database (99.99% SLA = ~52 minutes downtime/year)
- Auto-scaling 2-10 replicas
- CDN for global performance
- Premium Redis for session management

**Recommendation:** ✅ Enable after 1,000 users

### Scenario 3: Enterprise (Year 3+)

**Goal:** Multi-region, global scale

**Configuration:**
- Staging: $50/month
- Production Primary (East US): $550/month
- Production Secondary (West Europe): $450/month (read replicas only)
- Azure Front Door (WAF, DDoS): $100/month
- **Total:** $1,150/month = **$13,800/year**

**Benefits:**
- Multi-region failover (<5 minutes RTO)
- Global load balancing
- DDoS protection
- Geo-distributed read replicas

**Recommendation:** ⏸ Defer until 50,000+ users

---

## Quick Wins (Immediate Savings)

1. **Enable auto-shutdown for staging:** -$10/month
2. **Use in-memory cache in staging:** -$30/month
3. **Reduce log retention to 7 days:** -$5/month
4. **Skip Blob Storage in staging:** -$10/month
5. **Scale Container Apps to consumption model:** -$20/month

**Total Immediate Savings:** $75/month = **$900/year**

---

## Cost Comparison: Azure vs Competitors

| Provider | Staging | Production (Basic) | Production (HA) |
|----------|---------|-------------------|-----------------|
| **Azure** | $50 | $320 | $550 |
| AWS (ECS + RDS) | $60 | $380 | $620 |
| GCP (Cloud Run + Cloud SQL) | $45 | $300 | $500 |
| Heroku (Standard tier) | $100 | $500 | N/A |
| DigitalOcean (App Platform) | $40 | $250 | $400 |

**Verdict:** Azure is competitive, especially with Reserved Instances.

---

## Appendix: Cost Formulas

### Container Apps

```
Monthly Cost = (vCPU-seconds × $0.000012) + (GB-seconds × $0.000012)

Example (1 replica, 0.5 vCPU, 1 GB, 24/7):
= (0.5 × 2,592,000 × $0.000012) + (1 × 2,592,000 × $0.000012)
= $15.55 + $31.10
= $46.65/month per replica
```

### PostgreSQL Flexible Server

```
Burstable B1ms: $0.0162/hour = $12/month
General Purpose D2ds_v5: $0.163/hour = $120/month
HA (Zone-Redundant): 2× compute cost
Storage: $0.138/GB/month (32 GB = $4.42)
Backup: Free (7-35 days retention)
```

---

## Support

**Azure Cost Management:** https://portal.azure.com/#blade/Microsoft_Azure_CostManagement/Menu/overview
**Azure Pricing Calculator:** https://azure.microsoft.com/pricing/calculator
**LankaConnect Finance Team:** finance@lankaconnect.com

---

**Document Version:** 1.0
**Last Updated:** 2025-10-28
**Next Review:** Monthly (align with budget review)
