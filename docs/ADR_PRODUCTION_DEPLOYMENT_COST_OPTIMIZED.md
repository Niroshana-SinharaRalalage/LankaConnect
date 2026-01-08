# ADR: Cost-Optimized Production Deployment Strategy for LankaConnect

**Status:** REVISED - COST OPTIMIZED
**Date:** 2026-01-08
**Decision Makers:** Technical Lead, Operations Team
**Budget Constraint:** $100-200/month for initial production (under 200 users)

---

## Executive Summary

This is a **REVISED** production deployment strategy optimized for **small-scale production** with under 200 users and a budget of **$100-200/month**. This approach maintains production-grade reliability while significantly reducing costs compared to enterprise-scale deployments.

**Key Recommendation:** Create a minimal but production-ready environment that can scale up as your user base grows.

---

## Cost Comparison: Original vs Optimized

### Original Plan (TOO EXPENSIVE)
```
Total: $1,165/month
- Container Apps: $380/mo (over-provisioned)
- Database: $285/mo (General Purpose tier)
- Monitoring: $200/mo (90-day retention)
- Key Vault: $50/mo (Premium HSM)
- Storage: $50/mo
- Bandwidth: $200/mo
```

### **Cost-Optimized Plan (RECOMMENDED)**
```
Total: $150-180/month ✅
- Container Apps: $30-40/mo (Consumption, 0.25 vCPU, 1 replica)
- Database: $50-60/mo (Serverless, auto-pause enabled)
- Monitoring: $20-30/mo (30-day retention)
- Key Vault: $5/mo (Standard tier)
- Storage: $15-20/mo (Standard LRS)
- Bandwidth: $20-30/mo (minimal traffic)
```

**Savings: $985-1,015/month (85% cost reduction!)**

---

## 1. Context and Problem Statement

### Current Situation
- **Current Staging Cost:** ~$135/month
- **Target Production Budget:** $100-200/month
- **Expected Users:** Under 200 users initially
- **Traffic Pattern:** Low to moderate, intermittent usage
- **Data Volume:** Small (few hundred MB)

### Requirements
✅ Production-grade reliability
✅ Zero-downtime deployments
✅ Proper security (SSL, authentication)
✅ Monitoring and alerts
✅ Ability to scale as user base grows
❌ **NOT** over-engineered for enterprise scale

---

## 2. Recommended Solution: Start Small, Scale Smart

### Philosophy: "Production-Ready ≠ Enterprise-Scale"

For under 200 users, you need:
- ✅ **Reliability**: Yes (1-2 replicas with health checks)
- ✅ **Security**: Yes (proper authentication, SSL, secrets management)
- ✅ **Monitoring**: Yes (basic alerts and logs)
- ❌ **Premium HSM**: No (Standard Key Vault is fine)
- ❌ **90-day log retention**: No (30 days is sufficient)
- ❌ **10 auto-scaling replicas**: No (start with 1-2)

---

## 3. Cost-Optimized Architecture

### Production Resources (Minimal Configuration)

#### **3.1 Azure Container Apps (Consumption Plan)**

**Backend API:**
```yaml
Container: lankaconnect-api-prod
vCPU: 0.25
Memory: 0.5 GB
Replicas:
  Min: 1 (for availability)
  Max: 3 (for occasional spikes)
Scale Rule: HTTP requests (scale up at 100 concurrent)
Estimated Cost: $15-20/month
```

**Frontend UI:**
```yaml
Container: lankaconnect-ui-prod
vCPU: 0.25
Memory: 0.5 GB
Replicas:
  Min: 1 (for availability)
  Max: 3 (for occasional spikes)
Scale Rule: HTTP requests (scale up at 100 concurrent)
Estimated Cost: $15-20/month
```

**Total Container Apps: $30-40/month**

#### **3.2 Azure SQL Database (Serverless)**

```yaml
Tier: General Purpose - Serverless
vCores: 0.5-1 (auto-scales)
Max Data Size: 32 GB
Auto-pause delay: 60 minutes
Features:
  - Automatically pauses during inactivity
  - Charges only for compute used
  - Includes point-in-time restore (7 days)
Estimated Cost: $50-60/month
```

**Why Serverless?**
- Automatically pauses when inactive (nights/weekends)
- Scales down to 0.5 vCore when idle
- Perfect for apps with intermittent usage
- Can handle 200 users easily

#### **3.3 Azure Storage Account**

```yaml
Type: Standard General Purpose v2
Redundancy: Locally Redundant (LRS)
Storage: 100 GB blob storage
Access Tier: Hot
Estimated Cost: $15-20/month
```

#### **3.4 Azure Key Vault (Standard Tier)**

```yaml
Tier: Standard (NOT Premium HSM)
Operations: ~1,000/month
Secrets: ~20 secrets
Features:
  - Software-protected keys (sufficient for this scale)
  - Secret versioning
  - Access policies
Estimated Cost: $5/month
```

**Why NOT Premium HSM?**
- Premium HSM costs $1,200/year ($100/month!)
- Designed for regulatory compliance (banking, healthcare)
- Standard tier is sufficient for small SaaS apps
- Can upgrade later if needed

#### **3.5 Application Insights & Log Analytics**

```yaml
Data Ingestion: ~2-3 GB/month
Retention: 30 days (not 90)
Alerts: 5 basic alerts
Features:
  - Application performance monitoring
  - Exception tracking
  - Basic dashboards
Estimated Cost: $20-30/month
```

#### **3.6 Container Registry**

```yaml
Tier: Basic (not Premium)
Storage: 10 GB
Features:
  - Private registry for images
  - Webhook integration
  - No geo-replication (not needed yet)
Estimated Cost: $5/month
```

---

## 4. Total Cost Breakdown (Cost-Optimized)

| Component | Configuration | Monthly Cost |
|-----------|--------------|--------------|
| **Container Apps** | 2 apps × 0.25 vCPU × 1 replica | $30-40 |
| **Azure SQL Database** | Serverless (0.5-1 vCore, auto-pause) | $50-60 |
| **Storage Account** | 100 GB Standard LRS | $15-20 |
| **Key Vault** | Standard tier | $5 |
| **Application Insights** | 2-3 GB/month, 30-day retention | $20-30 |
| **Container Registry** | Basic tier | $5 |
| **Bandwidth** | Minimal egress (~100 GB) | $20-30 |
| **TOTAL** | | **$150-180/month** ✅ |

**Comparison:**
- Original Plan: $1,165/month
- Optimized Plan: $150-180/month
- **Savings: 85% ($985-1,015/month)**

---

## 5. When to Scale Up (Growth Triggers)

Start monitoring these metrics. When you hit these thresholds, it's time to scale:

### Trigger Points for Scaling:

| Metric | Current Target | Scale Up At | Action |
|--------|---------------|-------------|--------|
| **Users** | <200 | >500 users | Increase to 2 replicas min |
| **Database CPU** | <30% avg | >60% avg | Increase vCores to 1-2 |
| **Container CPU** | <50% avg | >70% avg | Increase to 0.5 vCPU |
| **Response Time** | <500ms | >2s P95 | Add replicas or vCPU |
| **Storage** | <10 GB | >50 GB | Consider upgrading tier |
| **Monthly Cost** | $150-180 | N/A | Re-evaluate architecture |

---

## 6. Deployment Strategy (Still Blue-Green, Just Cheaper)

### Approach: Lightweight Blue-Green

You still get **zero-downtime deployments** with this cheaper configuration!

**How it works:**
1. Current revision ("blue") runs on 1 replica
2. New revision ("green") deploys with 0% traffic
3. Health checks verify green is healthy
4. Traffic gradually shifts: 10% → 50% → 100%
5. Old revision kept for instant rollback

**Cost Impact:**
- During deployment: Temporarily runs 2 revisions (~2-5 minutes)
- Additional cost during deployment: $0.01-0.05
- Totally worth it for zero-downtime!

---

## 7. Security Considerations (Cost-Effective)

### What's STILL Included (Production-Grade):

✅ **SSL/TLS Certificates** - Free via Azure-managed certificates
✅ **Managed Identity** - Free authentication to Azure services
✅ **Key Vault** - Standard tier for secrets (vs. test data in env vars)
✅ **Private Container Registry** - Images not public
✅ **Separate Service Principal** - Production-only access
✅ **Network Security** - Container Apps built-in isolation
✅ **Database Firewall** - IP whitelisting

### What's Scaled Down (But Still Secure):

⚠️ **Key Vault Tier** - Standard (not Premium HSM)
- Still encrypted, still secure
- Just software-protected vs. hardware-protected
- Can upgrade to Premium HSM later ($100/mo) if needed

⚠️ **Log Retention** - 30 days (not 90)
- Still meets most compliance needs
- Can increase retention if required later

⚠️ **No Dedicated VNet** - Using Azure Container Apps default
- Still isolated and secure
- Can add VNet later ($50/mo) if needed

---

## 8. Monitoring & Alerting (Essentials Only)

### Critical Alerts (Free/Cheap)

Configure these 5 essential alerts in Application Insights:

1. **Availability**: Alert if health check fails 3 times in 5 minutes
2. **Response Time**: Alert if P95 latency >3 seconds
3. **Error Rate**: Alert if 5xx errors >5% of requests
4. **Database**: Alert if DTU usage >80%
5. **Container Restarts**: Alert if containers restart >3 times/hour

**Cost**: Included in Application Insights ($20-30/mo)

---

## 9. Implementation Plan (Cost-Optimized)

### Phase 1: Infrastructure Setup (2-3 hours)

```bash
# 1. Create resource group
az group create \
  --name lankaconnect-prod \
  --location eastus2

# 2. Create Container Apps Environment (Consumption plan)
az containerapp env create \
  --name lankaconnect-prod-env \
  --resource-group lankaconnect-prod \
  --location eastus2

# 3. Create Azure SQL Database (Serverless)
az sql server create \
  --name lankaconnect-prod-sql \
  --resource-group lankaconnect-prod \
  --location eastus2 \
  --admin-user sqladmin \
  --admin-password <SECURE_PASSWORD>

az sql db create \
  --resource-group lankaconnect-prod \
  --server lankaconnect-prod-sql \
  --name lankaconnect-db \
  --edition GeneralPurpose \
  --compute-model Serverless \
  --family Gen5 \
  --capacity 1 \
  --auto-pause-delay 60

# 4. Create Storage Account (Standard LRS)
az storage account create \
  --name lankaconnectprodstorage \
  --resource-group lankaconnect-prod \
  --location eastus2 \
  --sku Standard_LRS \
  --kind StorageV2

# 5. Create Key Vault (Standard tier)
az keyvault create \
  --name lankaconnect-prod-kv \
  --resource-group lankaconnect-prod \
  --location eastus2 \
  --sku standard

# 6. Create Container Registry (Basic tier)
az acr create \
  --name lankaconnectprod \
  --resource-group lankaconnect-prod \
  --sku Basic \
  --admin-enabled true

# 7. Create Application Insights
az monitor app-insights component create \
  --app lankaconnect-prod-insights \
  --location eastus2 \
  --resource-group lankaconnect-prod \
  --application-type web \
  --retention-time 30
```

**Time**: 2-3 hours
**Cost Impact**: $150-180/month recurring

---

## 10. Scaling Strategy (Pay-As-You-Grow)

### Growth Path:

**Phase 1: Launch (0-200 users) - Current Plan**
- Cost: $150-180/month
- Configuration: Minimal production

**Phase 2: Growth (200-1000 users)**
- Cost: $300-400/month
- Add:
  - Increase to 2 min replicas
  - Upgrade database to 2 vCores
  - Add 60-day log retention

**Phase 3: Scale (1000-5000 users)**
- Cost: $700-900/month
- Add:
  - 3-5 replicas with auto-scaling
  - Upgrade to Premium Key Vault HSM
  - Add CDN for static assets
  - Add Redis cache

**Phase 4: Enterprise (5000+ users)**
- Cost: $1,500-2,000/month
- Add:
  - Dedicated VNet
  - 10+ replicas
  - Multi-region deployment
  - Advanced security features

---

## 11. Comparison: Staging vs Production (Cost-Optimized)

| Aspect | Staging | Production (Optimized) |
|--------|---------|------------------------|
| **Cost** | $135/mo | **$150-180/mo** ✅ |
| **Purpose** | Testing | Live customers |
| **Data** | Test data | Real customer data |
| **Stripe Keys** | sk_test_... | **sk_live_...** ⚠️ |
| **Replicas** | 1 | 1 (can scale to 3) |
| **Database** | Standard | Serverless (auto-pause) |
| **Key Vault** | Shared | Dedicated (Standard) |
| **Monitoring** | 7-day logs | 30-day logs + alerts |
| **Backups** | 7-day PITR | 7-day PITR (sufficient) |
| **SSL** | Azure-managed | Azure-managed |
| **Scaling** | Manual | Auto-scale to 3 replicas |

**Key Difference**: Only $15-45/month more for production! 🎉

---

## 12. What You DON'T Need Yet (Common Misconceptions)

### Premature Optimizations to Avoid:

❌ **Premium Key Vault HSM** ($100/mo)
- Only needed for regulatory compliance
- Standard Key Vault is secure enough

❌ **90-day Log Retention** ($150/mo extra)
- 30 days is sufficient for small apps
- Can increase later if needed

❌ **Multiple Regions** ($500+/mo)
- Overkill for 200 users
- Single region is fine for now

❌ **Dedicated VNet** ($50+/mo)
- Container Apps are already isolated
- Add later if needed

❌ **CDN** ($50+/mo)
- Not needed until you have global users
- Azure Container Apps has built-in edge caching

❌ **Redis Cache** ($70+/mo)
- Database is fast enough for 200 users
- Add when you see performance issues

**Potential Savings: $920/month by not over-engineering!**

---

## 13. Decision: Start Small, Scale Smart

### Recommendation: **APPROVED ✅**

**Answer to User's Questions:**

**Q1: Can we simply push staging changes to production after creating relevant prod resources?**

✅ **YES** - Create **cost-optimized** production resources ($150-180/mo, not $1,165/mo)

**Q2: Can we convert current staging to production?**

❌ **NO** - Still not recommended (for the same reasons: lose test environment, security risks)

### Final Architecture Decision:

```
Create separate, cost-optimized production environment:
- Total Cost: $150-180/month ✅ (within budget!)
- Production-ready: Yes ✅
- Zero-downtime deploys: Yes ✅
- Can scale as you grow: Yes ✅
- Over-engineered: No ✅
```

---

## 14. Pre-Production Checklist (Simplified)

### Essential Checks Only:

**Infrastructure** (30 minutes)
- [ ] Azure resources created with correct tiers
- [ ] Database auto-pause configured (60 min)
- [ ] Container Apps set to 1 min replica

**Security** (30 minutes)
- [ ] Stripe LIVE keys in Key Vault (sk_live_...)
- [ ] Database firewall rules configured
- [ ] Service Principal has least-privilege access

**Configuration** (30 minutes)
- [ ] Environment variables set correctly
- [ ] Connection strings in Key Vault
- [ ] GitHub Secrets configured

**Testing** (1 hour)
- [ ] Health endpoint responds
- [ ] Can create user account
- [ ] Stripe payment test (live mode, refund immediately)
- [ ] Email notifications work

**Monitoring** (30 minutes)
- [ ] 5 critical alerts configured
- [ ] Application Insights dashboard set up
- [ ] Test alerts fire correctly

**Total Time**: 3 hours (not 2 days!)

---

## 15. Emergency Rollback (Simplified)

If something goes wrong after deployment:

```bash
# Instant rollback (< 30 seconds)
az containerapp revision set-mode \
  --name lankaconnect-ui-prod \
  --resource-group lankaconnect-prod \
  --mode single

az containerapp ingress traffic set \
  --name lankaconnect-ui-prod \
  --resource-group lankaconnect-prod \
  --revision-weight previous-revision=100
```

**Cost of Rollback**: $0 (no additional charges)

---

## 16. Consequences

### Positive:
✅ Within budget ($150-180/mo vs $100-200/mo target)
✅ Production-grade reliability
✅ Can scale as you grow (pay-as-you-grow model)
✅ Zero-downtime deployments
✅ Proper security and monitoring
✅ 85% cost savings vs. over-engineered approach

### Negative:
⚠️ Will need to scale up as user base grows (expected)
⚠️ 30-day logs instead of 90 (sufficient for now)
⚠️ Standard Key Vault instead of HSM (fine for SaaS)
⚠️ No multi-region redundancy yet (can add later)

### Risks & Mitigations:

| Risk | Likelihood | Impact | Mitigation |
|------|------------|--------|------------|
| **Cost overrun** | Low | Medium | Set budget alerts at $200/mo |
| **Performance issues** | Low | Medium | Monitor metrics, scale up if needed |
| **Outage** | Low | High | Health checks + auto-restart |
| **Data loss** | Very Low | Critical | 7-day PITR + automated backups |

---

## 17. Summary: The Right-Sized Production

### Key Principle: "Production-Ready ≠ Over-Engineered"

For an app with under 200 users, you need:
- ✅ **Reliability**: Yes (1-2 replicas)
- ✅ **Security**: Yes (proper secrets, SSL, auth)
- ✅ **Monitoring**: Yes (basic alerts)
- ✅ **Scalability**: Yes (can grow as needed)
- ❌ **Enterprise features**: No (wait until you need them)

**Result**: $150-180/month production environment that's:
- Reliable enough for real customers
- Secure enough for production data
- Cheap enough to be sustainable
- Scalable enough to grow with you

**This is the sweet spot for small SaaS apps!** 🎯

---

## Appendix A: Quick Commands for Cost Optimization

### Check Current Costs
```bash
# View current month's cost
az consumption usage list \
  --subscription <subscription-id> \
  --start-date 2026-01-01 \
  --end-date 2026-01-31

# Set budget alert
az consumption budget create \
  --budget-name lankaconnect-prod-budget \
  --amount 200 \
  --time-grain Monthly \
  --time-period start=2026-01-01 \
  --category Cost
```

### Monitor Database Auto-Pause
```bash
# Check if database is paused
az sql db show \
  --resource-group lankaconnect-prod \
  --server lankaconnect-prod-sql \
  --name lankaconnect-db \
  --query "status"

# Result: "Online" (running) or "Paused" (saving money!)
```

### Scale Down During Off-Hours (Optional)
```bash
# Scale to 0 min replicas at night (ultra-cheap mode)
az containerapp update \
  --name lankaconnect-ui-prod \
  --resource-group lankaconnect-prod \
  --min-replicas 0  # Scale to zero!

# Note: First request after scale-to-zero has cold start (5-10s)
```

---

## Appendix B: Cost Monitoring Dashboard

Create a simple cost dashboard:

1. **Azure Portal** → **Cost Management + Billing**
2. Create alert when cost exceeds $200/month
3. Monitor daily to catch unexpected spikes

---

## References

- Azure Container Apps Pricing: https://azure.microsoft.com/en-us/pricing/details/container-apps/
- Azure SQL Serverless: https://learn.microsoft.com/en-us/azure/azure-sql/database/serverless-tier-overview
- Azure Cost Optimization: https://learn.microsoft.com/en-us/azure/cost-management-billing/costs/cost-mgt-best-practices

---

**Status**: REVISED - Ready for implementation ✅
**Budget**: $150-180/month (within $100-200 target) ✅
**Timeline**: 1 day setup, 2 days testing, go live in 3-5 days
**Scalability**: Can handle growth to 1000+ users with incremental upgrades

**Approved by**: Planner Agent (Cost-Optimized Strategy)
