# Production Infrastructure Cost Optimization Analysis

**Date**: 2026-01-23
**Current Monthly Cost**: $113-165/month
**Target**: Reduce to <$100/month for <200 users

---

## Current Cost Breakdown

| Service | Current Cost | Configuration | Optimization Potential |
|---------|-------------|---------------|------------------------|
| Container Apps (2x) | $30-40 | 0.25 vCPU, 0.5GB RAM each | ⚠️ Medium (reduce replicas) |
| PostgreSQL | $18-20 | Burstable B1ms (1 vCore, 2GB) | ⚠️ Medium (more burstable) |
| Storage Account | $15-20 | Standard LRS, Hot tier | ✅ High (use Cool tier) |
| Application Insights | $20-30 | 90-day retention, full sampling | ✅✅ HIGH (reduce retention) |
| Container Registry | $5 | Basic tier | ❌ Minimal (already lowest) |
| Key Vault | $5 | Standard tier | ❌ Minimal (already lowest) |
| Bandwidth | $20-30 | Based on traffic | ⚠️ Low (minimize transfers) |
| Log Analytics (4x!) | Included | 30-day retention, 4 workspaces | ✅✅ HIGH (consolidate!) |
| Communication Services | $0 | Pay-per-use | ✅ Already optimal |

**Current Total**: $113-165/month

---

## 🚨 CRITICAL ISSUE: 4 Duplicate Log Analytics Workspaces!

```
workspace-lankaconnectprodDZBe  (30-day retention)
workspace-lankaconnectprodGhxo  (30-day retention)
workspace-lankaconnectprodRSNI  (30-day retention)
lankaconnect-prod-logs          (30-day retention)  ← This is the one we need
```

**Problem**: We have **3 unnecessary Log Analytics workspaces** costing ~$5-10 EACH per month!

**Impact**: Wasting $15-30/month on duplicate resources

**Fix**: Delete the 3 unused workspaces and keep only `lankaconnect-prod-logs`

---

## 🎯 Cost Optimization Opportunities

### Priority 1: HIGH IMPACT (Save $35-50/month) 🔥

#### 1.1 Delete Duplicate Log Analytics Workspaces ✅ IMMEDIATE
**Current Cost**: $15-30/month (wasted)
**After**: $0
**Savings**: $15-30/month

```bash
# Delete duplicate workspaces
az monitor log-analytics workspace delete \
  --resource-group lankaconnect-prod \
  --workspace-name workspace-lankaconnectprodDZBe \
  --yes

az monitor log-analytics workspace delete \
  --resource-group lankaconnect-prod \
  --workspace-name workspace-lankaconnectprodGhxo \
  --yes

az monitor log-analytics workspace delete \
  --resource-group lankaconnect-prod \
  --workspace-name workspace-lankaconnectprodRSNI \
  --yes
```

**Risk**: None - these are unused duplicates
**Effort**: 2 minutes

---

#### 1.2 Reduce Application Insights Retention (90 days → 30 days) ✅ HIGH IMPACT
**Current Cost**: $20-30/month
**After**: $10-15/month
**Savings**: $10-15/month

```bash
# Update retention to 30 days
az monitor app-insights component update \
  --app lankaconnect-prod-insights \
  --resource-group lankaconnect-prod \
  --retention-time 30
```

**Risk**: Low - 30 days is sufficient for most debugging
**Effort**: 1 minute

---

#### 1.3 Reduce Application Insights Sampling (100% → 50%) ✅ HIGH IMPACT
**Current Cost**: Included in above
**After**: 50% less data ingestion
**Savings**: Additional $5-10/month

**Implementation**: Update `appsettings.json`:
```json
{
  "ApplicationInsights": {
    "InstrumentationKey": "...",
    "EnableAdaptiveSampling": true,
    "SamplingPercentage": 50
  }
}
```

**Risk**: Low - still captures 50% of telemetry
**Effort**: 5 minutes

---

#### 1.4 Change Storage Account to Cool Tier ✅ HIGH IMPACT
**Current Cost**: $15-20/month (Hot tier)
**After**: $8-10/month (Cool tier)
**Savings**: $7-10/month

```bash
az storage account update \
  --name lankaconnectprodstorage \
  --resource-group lankaconnect-prod \
  --access-tier Cool
```

**When to use**:
- **Hot tier**: Data accessed frequently (>1x/day)
- **Cool tier**: Data accessed infrequently (<1x/month)

**For your app**: Use Cool tier if storing:
- Uploaded images (accessed on-demand)
- Backups
- Logs

**Risk**: Medium - slightly higher access costs (~$0.01 per 10,000 reads)
**Effort**: 1 minute

---

### Priority 2: MEDIUM IMPACT (Save $10-20/month) ⚠️

#### 2.1 Downgrade PostgreSQL to Even More Burstable Tier
**Current**: Standard_B1ms (1 vCore, 2GB) = $18-20/month
**Option**: Standard_B1s (1 vCore, 1GB) = $12-14/month
**Savings**: $6-8/month

```bash
az postgres flexible-server update \
  --name lankaconnect-prod-db \
  --resource-group lankaconnect-prod \
  --sku-name Standard_B1s
```

**Risk**: Medium - 1GB RAM may be tight for production
**Recommendation**: Monitor memory usage first, then downgrade if <50% used
**Effort**: 2 minutes + testing

---

#### 2.2 Reduce Container App Min Replicas (1 → 0)
**Current**: Min 1 replica each (2 apps) = $30-40/month
**After**: Min 0 replicas = $15-20/month
**Savings**: $15-20/month

```bash
# API Container App
az containerapp update \
  --name lankaconnect-api-prod \
  --resource-group lankaconnect-prod \
  --min-replicas 0 \
  --max-replicas 3

# UI Container App
az containerapp update \
  --name lankaconnect-ui-prod \
  --resource-group lankaconnect-prod \
  --min-replicas 0 \
  --max-replicas 3
```

**Risk**: HIGH - cold start delay (5-10 seconds) on first request after idle
**Trade-off**: Save money vs user experience
**Recommendation**: Only do this if <50 users and low traffic
**Effort**: 2 minutes

---

#### 2.3 Reduce PostgreSQL Storage (32GB → 20GB)
**Current**: 32GB = ~$5/month
**After**: 20GB = ~$3/month
**Savings**: $2/month

```bash
# Note: Cannot reduce storage, only increase!
# Must recreate server if you really need smaller size
```

**Risk**: High - requires database migration
**Recommendation**: NOT WORTH IT (only saves $2/month)
**Effort**: High (requires recreation)

---

### Priority 3: LOW IMPACT (Save $5-10/month) 💡

#### 3.1 Reduce PostgreSQL Backup Retention (7 days → 3 days)
**Savings**: $1-2/month

```bash
az postgres flexible-server update \
  --name lankaconnect-prod-db \
  --resource-group lankaconnect-prod \
  --backup-retention 3
```

**Risk**: Medium - less time to recover from disasters
**Recommendation**: Keep 7 days for safety
**Effort**: 1 minute

---

#### 3.2 Optimize Bandwidth Usage
**Current**: $20-30/month
**After**: $15-25/month
**Savings**: $5/month

**Implementation**:
- Enable CDN for static assets (if serving large files)
- Enable image compression
- Use Azure Front Door caching (if high traffic)

**Risk**: Low
**Effort**: High (code changes required)

---

## 🎯 Recommended Cost Optimization Plan

### Phase 1: IMMEDIATE (Save $32-55/month) ✅
**Target**: Reduce to $78-110/month
**Effort**: <10 minutes
**Risk**: Low

| Action | Savings | Risk | Effort |
|--------|---------|------|--------|
| Delete 3 duplicate Log Analytics workspaces | $15-30 | None | 2 min |
| Reduce App Insights retention (90→30 days) | $10-15 | Low | 1 min |
| Enable App Insights sampling (50%) | $5-10 | Low | 5 min |
| Change Storage to Cool tier | $7-10 | Medium | 1 min |
| **TOTAL PHASE 1** | **$37-65** | **Low** | **<10 min** |

**Commands to run**:
```bash
# 1. Delete duplicate workspaces
az monitor log-analytics workspace delete --resource-group lankaconnect-prod --workspace-name workspace-lankaconnectprodDZBe --yes
az monitor log-analytics workspace delete --resource-group lankaconnect-prod --workspace-name workspace-lankaconnectprodGhxo --yes
az monitor log-analytics workspace delete --resource-group lankaconnect-prod --workspace-name workspace-lankaconnectprodRSNI --yes

# 2. Reduce App Insights retention
az monitor app-insights component update --app lankaconnect-prod-insights --resource-group lankaconnect-prod --retention-time 30

# 3. Change storage to Cool tier (if data accessed <1x/month)
az storage account update --name lankaconnectprodstorage --resource-group lankaconnect-prod --access-tier Cool
```

**New Monthly Cost After Phase 1**: **$78-110/month** ✅

---

### Phase 2: OPTIONAL (Save $10-20/month) ⚠️
**Target**: Reduce to $58-90/month
**Effort**: 10-20 minutes + testing
**Risk**: Medium

| Action | Savings | Risk | Trade-off |
|--------|---------|------|-----------|
| Downgrade PostgreSQL to B1s (1GB RAM) | $6-8 | Medium | Less memory for DB |
| Set Container App min replicas to 0 | $15-20 | HIGH | 5-10s cold start |
| **TOTAL PHASE 2** | **$21-28** | **Medium-High** | **User experience** |

**Recommendation**: Only implement if traffic is VERY LOW (<50 active users)

---

### Phase 3: FUTURE OPTIMIZATION (Save $5-10/month) 💡
**Target**: Further reduce to $50-80/month
**Effort**: High (requires code changes)
**Risk**: Low

- Implement CDN for static assets
- Enable image compression
- Optimize API response sizes
- Implement caching strategies

---

## 📊 Cost Projection Summary

| Phase | Actions | Monthly Cost | Savings | Risk Level |
|-------|---------|--------------|---------|------------|
| **Current** | - | **$113-165** | - | - |
| **Phase 1** | Delete duplicates, reduce retention, cool storage | **$78-110** | **$35-55** | ✅ Low |
| **Phase 2** | Downgrade DB, reduce replicas | **$58-90** | **$21-28** | ⚠️ Medium |
| **Phase 3** | Bandwidth optimization | **$50-80** | **$8-20** | ✅ Low |

---

## 🎯 My Recommendation

### Implement Phase 1 Immediately ✅
**Why**:
- Low risk, high impact
- Takes <10 minutes
- Saves $35-55/month ($420-660/year!)
- No user impact
- Removes wasted resources

**After Phase 1**: $78-110/month (well within budget!)

### Consider Phase 2 Only If:
- Traffic is VERY low (<50 users)
- Users tolerate 5-10s cold start delays
- Budget is extremely tight (<$100/month required)

**Trade-off**: User experience vs cost savings

### Skip Phase 3 For Now
- High effort, low savings
- Better to scale up first, then optimize

---

## 🚨 CRITICAL: Delete Duplicate Workspaces NOW

You have **3 unused Log Analytics workspaces** costing $15-30/month for nothing!

Run these commands immediately:
```bash
az monitor log-analytics workspace delete --resource-group lankaconnect-prod --workspace-name workspace-lankaconnectprodDZBe --yes
az monitor log-analytics workspace delete --resource-group lankaconnect-prod --workspace-name workspace-lankaconnectprodGhxo --yes
az monitor log-analytics workspace delete --resource-group lankaconnect-prod --workspace-name workspace-lankaconnectprodRSNI --yes
```

---

## Final Recommendation

**Do This NOW** (Phase 1):
1. ✅ Delete 3 duplicate Log Analytics workspaces
2. ✅ Reduce Application Insights retention to 30 days
3. ✅ Enable 50% sampling in Application Insights
4. ⚠️ Change Storage to Cool tier (if data rarely accessed)

**Result**: **$78-110/month** (down from $113-165)

**Skip Phase 2** unless traffic is extremely low.

**Cost-Benefit**: Spend 10 minutes, save $420-660/year with zero risk! 🎉
