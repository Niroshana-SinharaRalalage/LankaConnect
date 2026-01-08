# Production Deployment Summary - LankaConnect

**Date:** 2026-01-08
**Status:** Ready for Implementation
**Estimated Timeline:** 7-10 days

---

## Executive Summary

This document provides a high-level overview of the LankaConnect production deployment strategy. The recommended approach is to create a **dedicated production environment with blue-green deployment** capability, ensuring zero-downtime updates and instant rollback.

**Key Decision:** Do NOT convert staging to production. Create separate production resources with proper security hardening and deployment automation.

---

## Quick Reference

### Documentation Map

| Document | Purpose | Audience |
|----------|---------|----------|
| **ADR_PRODUCTION_DEPLOYMENT_STRATEGY.md** | Complete architecture decision record with detailed implementation plan | Technical leads, architects |
| **PRODUCTION_DEPLOYMENT_CHECKLIST.md** | Step-by-step checklist for production deployment | Operations team, deployment engineers |
| **setup-production-infrastructure.sh** | Automated script to provision Azure resources | DevOps engineers |
| **deploy-production.yml** | GitHub Actions workflow for API deployment | CI/CD pipeline |
| **deploy-ui-production.yml** | GitHub Actions workflow for UI deployment | CI/CD pipeline |

---

## Recommended Approach

### Option: Separate Production with Blue-Green Deployment ✅

**What This Means:**
1. Create brand new production Azure resources (separate from staging)
2. Configure blue-green deployment using Azure Container Apps revisions
3. Deploy with gradual rollout (0% → 10% → 100% traffic)
4. Keep staging environment intact for future development

**Why This is Best:**
- ✅ Zero-downtime deployments
- ✅ Instant rollback capability (< 30 seconds)
- ✅ Production and staging remain isolated
- ✅ Proper security hardening for production
- ✅ Follows Azure best practices

**Why NOT Convert Staging:**
- ❌ Loss of staging environment
- ❌ Risk of test data in production
- ❌ No easy rollback
- ❌ Violates environment separation best practices

---

## Architecture Overview

### Current State (Staging)
```
┌─────────────────────────────────────┐
│      Staging Environment            │
├─────────────────────────────────────┤
│ Frontend: lankaconnect-ui-staging   │
│ Backend:  lankaconnect-api-staging  │
│ Database: Azure SQL (staging data)  │
│ Registry: lankaconnectstaging.azurecr.io │
│ Branch:   develop                   │
└─────────────────────────────────────┘
```

### Future State (Production + Staging)
```
┌─────────────────────────────────────┐     ┌─────────────────────────────────────┐
│      Staging Environment            │     │     Production Environment          │
├─────────────────────────────────────┤     ├─────────────────────────────────────┤
│ Frontend: lankaconnect-ui-staging   │     │ Frontend: lankaconnect-ui-prod      │
│ Backend:  lankaconnect-api-staging  │     │ Backend:  lankaconnect-api-prod     │
│ Database: Azure SQL (test data)     │     │ Database: Azure SQL (production)    │
│ Registry: lankaconnectstaging       │     │ Registry: lankaconnectprod          │
│ Branch:   develop                   │     │ Branch:   main                      │
│ Purpose:  Testing new features      │     │ Purpose:  Live customer traffic     │
└─────────────────────────────────────┘     └─────────────────────────────────────┘
```

---

## Blue-Green Deployment Flow

```
1. CURRENT STATE (Blue Active)
   ┌─────────────────────────────┐
   │ Revision: api-prod-001      │ ← 100% traffic (BLUE)
   │ Image: api:abc123           │
   └─────────────────────────────┘

2. DEPLOY NEW VERSION (Green Inactive)
   ┌─────────────────────────────┐
   │ Revision: api-prod-001      │ ← 100% traffic (BLUE)
   │ Revision: api-prod-002      │ ← 0% traffic (GREEN)
   └─────────────────────────────┘

3. CANARY ROLLOUT (10% Traffic)
   ┌─────────────────────────────┐
   │ Revision: api-prod-001      │ ← 90% traffic (BLUE)
   │ Revision: api-prod-002      │ ← 10% traffic (GREEN - Testing)
   └─────────────────────────────┘

4. FULL CUTOVER (100% Green)
   ┌─────────────────────────────┐
   │ Revision: api-prod-001      │ ← 0% traffic (BLUE - Standby)
   │ Revision: api-prod-002      │ ← 100% traffic (GREEN - Active)
   └─────────────────────────────┘

5. ROLLBACK (If Issues)
   ┌─────────────────────────────┐
   │ Revision: api-prod-001      │ ← 100% traffic (BLUE - Restored)
   └─────────────────────────────┘
```

---

## Implementation Timeline

### Phase 1: Infrastructure (Days 1-3)

**Day 1: Core Infrastructure**
- Create resource group
- Provision Container Registry
- Create Key Vault
- Create SQL Database
- Estimated time: 4-6 hours

**Day 2: Container Apps**
- Create Container Apps environment
- Deploy API Container App
- Deploy UI Container App
- Configure networking
- Estimated time: 3-4 hours

**Day 3: Security**
- Configure all Key Vault secrets
- Set up managed identities
- Create service principal for GitHub Actions
- Configure firewall rules
- Estimated time: 2-3 hours

### Phase 2: Database Migration (Day 4)

- Run EF Core migrations on production database
- Seed reference data
- Validate schema
- Estimated time: 2-4 hours

### Phase 3: CI/CD Pipeline (Day 5)

- Create production GitHub workflows
- Configure GitHub Secrets
- Test deployment pipeline with staging images
- Estimated time: 3-5 hours

### Phase 4: Testing (Days 6-7)

**Day 6: Functional Testing**
- Test all user flows
- Verify authentication
- Test payment processing (Stripe LIVE mode)
- Validate email sending

**Day 7: Performance & Security**
- Load testing (100 concurrent users)
- Security scanning
- Penetration testing (optional)

### Phase 5: Go-Live (Days 8-10)

**Day 8: Pre-Launch**
- Final checklist review
- Team briefing
- Communication plan execution

**Day 9: Launch**
- Deploy to production
- DNS cutover
- Monitor for first 6 hours

**Day 10: Stabilization**
- 24-hour monitoring
- Issue resolution
- Performance optimization

---

## Cost Breakdown

### Monthly Costs

| Resource | Staging | Production | Difference |
|----------|---------|------------|------------|
| Container Registry | $5 | $20 | +$15 |
| Container Apps | $45 | $450 | +$405 |
| SQL Database | $50 | $300 | +$250 |
| Key Vault | $5 | $25 | +$20 |
| Storage Account | $5 | $20 | +$15 |
| Application Insights | $10 | $100 | +$90 |
| Log Analytics | $5 | $50 | +$45 |
| Email Service | $5 | $50 | +$45 |
| **Total** | **$135** | **$1,165** | **+$1,030** |

**Note:** Production costs can be optimized by:
- Using Azure Reservations (30% savings)
- Configuring aggressive auto-scaling
- Starting with P1 SQL tier instead of P2
- Estimated optimized cost: $700-900/month

---

## Security Enhancements for Production

### Key Differences from Staging

| Security Control | Staging | Production |
|-----------------|---------|------------|
| Key Vault SKU | Standard | Premium (HSM) |
| SQL Firewall | Allow Azure | Private endpoint |
| TLS Version | 1.2 | 1.3 |
| Managed Identity | System-assigned | User-assigned |
| Secrets Rotation | Manual | Automated (30 days) |
| Backup Retention | 7 days | 35 days |
| Stripe Keys | Test mode (sk_test_) | **LIVE mode (sk_live_)** |

**Critical:** Production uses LIVE Stripe keys that charge real credit cards!

---

## Pre-Production Checklist (Critical Items)

### Must-Have Before Go-Live

- [ ] Production Azure resources created
- [ ] All secrets stored in Key Vault (no hardcoded values)
- [ ] Database migrations tested and applied
- [ ] Stripe configured in LIVE mode with test payment verified
- [ ] Email service configured and test email sent
- [ ] SSL certificate provisioned for custom domain
- [ ] GitHub workflows tested and working
- [ ] Monitoring alerts configured
- [ ] Health checks passing on all services
- [ ] Rollback procedure documented and tested

### Nice-to-Have

- [ ] Custom domain configured (lankaconnect.com)
- [ ] CDN configured for static assets
- [ ] Azure Front Door with WAF
- [ ] Multi-region deployment
- [ ] Automated performance testing

---

## Deployment Commands

### Quick Infrastructure Setup

```bash
# 1. Run automated setup script
cd scripts
./setup-production-infrastructure.sh

# 2. Manually configure secrets in Key Vault
az keyvault secret set --vault-name lc-prod-kv --name database-connection-string --value "..."
az keyvault secret set --vault-name lc-prod-kv --name jwt-secret-key --value "..."
# ... (repeat for all secrets)

# 3. Configure GitHub Secrets
# Add AZURE_CREDENTIALS_PROD from github-service-principal.json
# Add ACR_USERNAME_PROD and ACR_PASSWORD_PROD from production-secrets.env

# 4. Deploy to production
git checkout main
git push origin main  # Triggers production deployment workflow
```

### Manual Rollback (Emergency)

```bash
# Get previous revision name
az containerapp revision list \
  --name lankaconnect-api-prod \
  --resource-group lankaconnect-prod \
  --query "[?properties.trafficWeight==0].name" -o tsv

# Rollback to previous revision
az containerapp ingress traffic set \
  --name lankaconnect-api-prod \
  --resource-group lankaconnect-prod \
  --revision-weight <previous-revision>=100

# Same for UI
az containerapp ingress traffic set \
  --name lankaconnect-ui-prod \
  --resource-group lankaconnect-prod \
  --revision-weight <previous-revision>=100
```

---

## Success Metrics

### Deployment Success Criteria

- ✅ Zero-downtime deployment (no 503 errors)
- ✅ Health checks passing within 2 minutes
- ✅ Error rate < 0.1% after 1 hour
- ✅ Response time < 500ms (95th percentile)
- ✅ All smoke tests passing
- ✅ No critical errors in Application Insights

### Go-Live Success Criteria

- ✅ Production URL accessible (https://lankaconnect.com)
- ✅ SSL certificate valid (no browser warnings)
- ✅ User registration working
- ✅ Login via Entra ID working
- ✅ Event creation working
- ✅ Payment processing working (Stripe LIVE mode)
- ✅ Email notifications sending
- ✅ No critical alerts in first 24 hours

---

## Rollback Strategy

### When to Rollback

**Immediate rollback if:**
- Error rate > 5% for more than 5 minutes
- Critical functionality broken (login, payments, event creation)
- Security vulnerability discovered
- Database corruption detected

**Considered rollback if:**
- Error rate between 1-5% for more than 10 minutes
- Performance degradation > 5x normal
- Third-party service failures (Stripe, email)

### Rollback Time Targets

- **Application Rollback:** < 30 seconds (traffic split)
- **Database Rollback:** < 15 minutes (restore from backup)
- **Full System Rollback:** < 20 minutes (app + database)

---

## Monitoring and Alerts

### Critical Alerts (PagerDuty/SMS)

- API health check failure (3 consecutive)
- Database connectivity failure
- Error rate > 1% for 5 minutes
- Response time > 2 seconds (95th percentile)
- Payment processing failure rate > 5%

### Warning Alerts (Email)

- CPU > 80% for 10 minutes
- Memory > 90% for 10 minutes
- Slow query detected (> 5 seconds)
- Email send failure rate > 5%
- Stripe webhook delivery failure

### Dashboard Panels

1. **Overview:** Health status, request rate, error rate
2. **Performance:** Response time, CPU, memory, database
3. **Business:** Events created, registrations, payments
4. **Errors:** Top errors, exception traces
5. **Infrastructure:** Container replicas, database DTU, storage

---

## Team Responsibilities

### Technical Lead
- Architecture decisions
- Deployment strategy approval
- Rollback decisions
- Post-mortem facilitation

### Operations Lead
- Infrastructure provisioning
- Monitoring configuration
- On-call schedule management
- Incident response coordination

### Security Lead
- Security review and approval
- Secrets management
- Firewall configuration
- Compliance verification

### Product Owner
- Business requirements validation
- Go-live approval
- Stakeholder communication
- Success metrics tracking

---

## Common Issues and Solutions

### Issue: Container App Not Becoming Healthy

**Symptoms:** Revision stays in "Provisioning" or "Unhealthy" state

**Solutions:**
1. Check Container App logs: `az containerapp logs show ...`
2. Verify environment variables are correct
3. Check database connectivity from container
4. Verify Key Vault secrets are accessible
5. Review Application Insights for startup errors

### Issue: Database Migration Fails

**Symptoms:** EF Core migrations fail with timeout or connection errors

**Solutions:**
1. Check SQL firewall allows GitHub Actions IP
2. Verify connection string is correct
3. Run migrations with increased timeout
4. Check for conflicting migrations
5. Manually apply migrations if needed

### Issue: Stripe Webhook Not Working

**Symptoms:** Payments succeed but webhooks not received

**Solutions:**
1. Verify webhook secret in Key Vault
2. Check webhook endpoint URL in Stripe dashboard
3. Test webhook with Stripe CLI: `stripe listen --forward-to ...`
4. Check Container App logs for webhook errors
5. Verify webhook signature validation logic

---

## Next Steps After Go-Live

### Week 1: Monitoring and Stabilization
- Monitor all metrics 24/7
- Address any performance issues
- Optimize auto-scaling rules
- Collect user feedback

### Week 2-4: Optimization
- Review cost optimization opportunities
- Fine-tune alert thresholds
- Implement additional monitoring
- Performance profiling

### Month 2: Enhancements
- Implement CDN for static assets
- Add multi-region deployment
- Enhance disaster recovery
- Automated scaling policies

---

## Support and Escalation

### Level 1: Self-Service
- Documentation: All docs in `docs/` folder
- Runbooks: Common procedures documented
- Dashboard: Real-time monitoring

### Level 2: On-Call Engineer
- Primary on-call: Responds within 15 minutes
- Secondary on-call: Responds within 30 minutes
- Escalation: After 1 hour or if critical

### Level 3: Vendor Support
- Azure Support: For infrastructure issues
- Stripe Support: For payment issues
- Microsoft Support: For Entra ID issues

---

## Conclusion

**Production deployment for LankaConnect is ready to proceed with the following approach:**

1. ✅ Create dedicated production environment (do NOT convert staging)
2. ✅ Implement blue-green deployment for zero-downtime updates
3. ✅ Follow comprehensive checklist for all phases
4. ✅ Use automated scripts for infrastructure provisioning
5. ✅ Deploy via GitHub Actions with built-in rollback
6. ✅ Monitor continuously with comprehensive alerting

**Estimated timeline:** 7-10 days from start to production launch

**Estimated monthly cost:** $1,165 (optimizable to $700-900)

**Deployment risk:** LOW (blue-green deployment with instant rollback)

**Recommended go-live date:** TBD (after completing checklist)

---

## Quick Links

- [Complete ADR](./ADR_PRODUCTION_DEPLOYMENT_STRATEGY.md)
- [Deployment Checklist](./PRODUCTION_DEPLOYMENT_CHECKLIST.md)
- [Infrastructure Setup Script](../scripts/setup-production-infrastructure.sh)
- [API Deployment Workflow](../.github/workflows/deploy-production.yml)
- [UI Deployment Workflow](../.github/workflows/deploy-ui-production.yml)

---

**Document Owner:** Technical Lead
**Last Updated:** 2026-01-08
**Next Review:** After production go-live
