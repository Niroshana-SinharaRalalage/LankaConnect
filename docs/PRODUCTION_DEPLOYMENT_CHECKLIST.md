# Production Deployment Checklist for LankaConnect

**Version:** 1.0
**Date:** 2026-01-08
**Status:** Active

This checklist must be completed before deploying LankaConnect to production. Each item should be verified and checked off by the appropriate team member.

---

## Table of Contents

1. [Pre-Deployment Phase](#pre-deployment-phase)
2. [Infrastructure Phase](#infrastructure-phase)
3. [Security Phase](#security-phase)
4. [Database Phase](#database-phase)
5. [Application Phase](#application-phase)
6. [Monitoring Phase](#monitoring-phase)
7. [CI/CD Phase](#cicd-phase)
8. [Domain and SSL Phase](#domain-and-ssl-phase)
9. [Business Readiness Phase](#business-readiness-phase)
10. [Go-Live Phase](#go-live-phase)
11. [Post-Deployment Phase](#post-deployment-phase)

---

## Pre-Deployment Phase

### Planning and Documentation

- [ ] **Production deployment ADR reviewed and approved**
  - File: `docs/ADR_PRODUCTION_DEPLOYMENT_STRATEGY.md`
  - Approved by: ___________________
  - Date: ___________________

- [ ] **Cost analysis reviewed and budget approved**
  - Estimated monthly cost: $1,165
  - Budget approved by: ___________________
  - Date: ___________________

- [ ] **Deployment timeline finalized**
  - Target go-live date: ___________________
  - Maintenance window: ___________________
  - Rollback deadline: ___________________

- [ ] **Stakeholder communication plan finalized**
  - Go-live announcement drafted: [ ]
  - Social media posts prepared: [ ]
  - Email notifications ready: [ ]
  - Support team briefed: [ ]

### Team Readiness

- [ ] **Operations team trained on production deployment**
  - Blue-green deployment process: [ ]
  - Rollback procedures: [ ]
  - Monitoring dashboards: [ ]
  - Incident response: [ ]

- [ ] **On-call schedule established**
  - Primary on-call: ___________________
  - Secondary on-call: ___________________
  - Escalation path defined: [ ]

- [ ] **Runbooks created for common scenarios**
  - Deployment rollback: [ ]
  - Database restore: [ ]
  - Service degradation: [ ]
  - Third-party service failures: [ ]

---

## Infrastructure Phase

### Azure Resource Group

- [ ] **Production resource group created**
  ```bash
  Resource Group: lankaconnect-prod
  Location: eastus
  Tags: Environment=Production, Project=LankaConnect
  ```
  - Created by: ___________________
  - Date: ___________________
  - Verified by: ___________________

### Azure Container Registry

- [ ] **Production ACR created and configured**
  ```bash
  Name: lankaconnectprod.azurecr.io
  SKU: Standard (or Premium for geo-replication)
  Admin enabled: true
  ```
  - Registry URL: ___________________
  - Username saved in GitHub Secrets: [ ]
  - Password saved in GitHub Secrets: [ ]
  - Geo-replication configured (if Premium): [ ]

### Container Apps Environment

- [ ] **Container Apps environment created**
  ```bash
  Name: lankaconnect-prod-env
  Location: eastus
  ```
  - Environment ID: ___________________
  - Log Analytics workspace linked: [ ]
  - VNet integration configured (optional): [ ]

### API Container App

- [ ] **API Container App created**
  ```bash
  Name: lankaconnect-api-prod
  Min replicas: 2
  Max replicas: 10
  CPU: 2.0 vCPU
  Memory: 4.0 GB
  ```
  - Container App URL: ___________________
  - Health endpoint accessible: [ ]
  - Auto-scaling rules configured: [ ]

### UI Container App

- [ ] **UI Container App created**
  ```bash
  Name: lankaconnect-ui-prod
  Min replicas: 2
  Max replicas: 10
  CPU: 1.0 vCPU
  Memory: 2.0 GB
  ```
  - Container App URL: ___________________
  - Health endpoint accessible: [ ]
  - Auto-scaling rules configured: [ ]

---

## Security Phase

### Azure Key Vault

- [ ] **Production Key Vault created**
  ```bash
  Name: lc-prod-kv (or similar, 15-char limit)
  SKU: Premium (HSM-backed)
  RBAC enabled: true
  ```
  - Key Vault URL: ___________________
  - Soft delete enabled: [ ]
  - Purge protection enabled: [ ]

- [ ] **All production secrets stored in Key Vault**
  - `database-connection-string`: [ ] ✅
  - `jwt-secret-key`: [ ] ✅ (DIFFERENT from staging)
  - `jwt-issuer`: [ ] ✅ (https://lankaconnect.com)
  - `jwt-audience`: [ ] ✅ (https://lankaconnect.com)
  - `entra-enabled`: [ ] ✅ (true)
  - `entra-tenant-id`: [ ] ✅
  - `entra-client-id`: [ ] ✅
  - `entra-audience`: [ ] ✅ (api://lankaconnect-prod)
  - `azure-email-connection-string`: [ ] ✅
  - `azure-email-sender-address`: [ ] ✅ (noreply@lankaconnect.com)
  - `azure-storage-connection-string`: [ ] ✅
  - `stripe-secret-key`: [ ] ✅ (**LIVE MODE sk_live_...**)
  - `stripe-publishable-key`: [ ] ✅ (**LIVE MODE pk_live_...**)
  - `stripe-webhook-secret`: [ ] ✅ (production webhook secret)

### Managed Identity

- [ ] **System-assigned managed identity configured for API Container App**
  - Identity ID: ___________________
  - Key Vault access granted: [ ]
  - SQL Database access granted: [ ]
  - Storage Account access granted: [ ]

- [ ] **System-assigned managed identity configured for UI Container App**
  - Identity ID: ___________________
  - Key Vault access granted (if needed): [ ]

### Network Security

- [ ] **Firewall rules configured**
  - SQL Database firewall: [ ] (private endpoint or IP restrictions)
  - Storage Account firewall: [ ] (private endpoint or IP restrictions)
  - Key Vault network rules: [ ] (allow Azure services)

- [ ] **TLS/SSL configuration**
  - Minimum TLS version: TLS 1.3 [ ]
  - HTTPS-only enforced: [ ]
  - HSTS header configured: [ ]

### Service Principal

- [ ] **GitHub Actions service principal created**
  ```bash
  Name: lankaconnect-prod-github-sp
  Role: Contributor (scoped to resource group)
  ```
  - Client ID: ___________________
  - JSON saved in GitHub Secrets as `AZURE_CREDENTIALS_PROD`: [ ]
  - Permissions verified (least privilege): [ ]

---

## Database Phase

### Azure SQL Database

- [ ] **Production SQL Database created**
  ```bash
  Server: lankaconnect-sqldb-prod.database.windows.net
  Database: LankaConnectDB
  SKU: P2 (250 DTU)
  ```
  - Server URL: ___________________
  - Admin username: sqladmin
  - Admin password stored securely: [ ]

- [ ] **Database configuration**
  - Geo-replication enabled: [ ]
  - Zone redundancy enabled: [ ]
  - Automated backups enabled (35-day retention): [ ]
  - Point-in-time restore tested: [ ]

- [ ] **Connection string**
  - Connection string stored in Key Vault: [ ]
  - Connection pooling configured: [ ]
  - MultipleActiveResultSets enabled: [ ]

### Database Migration

- [ ] **Migration strategy decided**
  - Option A: Fresh database (RECOMMENDED): [ ]
  - Option B: Copy from staging (if needed): [ ]

- [ ] **EF Core migrations prepared**
  - All migrations reviewed: [ ]
  - Migrations tested in staging: [ ]
  - Backward compatibility verified: [ ]

- [ ] **Data seeding plan**
  - Reference data seeded: [ ]
  - Test users created (if needed): [ ]
  - No staging/test data in production: [ ]

---

## Application Phase

### Backend Configuration

- [ ] **appsettings.Production.json validated**
  - Logging level: Warning/Error [ ]
  - No hardcoded secrets: [ ]
  - Connection strings use placeholders: [ ]
  - ApplicationUrls.FrontendBaseUrl: https://lankaconnect.com [ ]

- [ ] **Environment variables configured in Container App**
  - ASPNETCORE_ENVIRONMENT=Production [ ]
  - All secrets reference Key Vault (secretref:...) [ ]
  - Stripe keys are LIVE mode [ ]
  - Email sender is production domain [ ]

### Frontend Configuration

- [ ] **Next.js production build verified**
  - Standalone build output exists: [ ]
  - Static files directory exists: [ ]
  - No development environment variables: [ ]

- [ ] **Environment variables configured in Container App**
  - BACKEND_API_URL points to production API [ ]
  - NEXT_PUBLIC_API_URL=/api/proxy [ ]
  - NEXT_PUBLIC_ENV=production [ ]
  - NODE_ENV=production [ ]

### Docker Images

- [ ] **Production Docker images built and pushed**
  - API image: `lankaconnectprod.azurecr.io/lankaconnect-api:latest` [ ]
  - UI image: `lankaconnectprod.azurecr.io/lankaconnect-ui:latest` [ ]
  - Images scanned for vulnerabilities: [ ]

### Health Checks

- [ ] **API health endpoint tested**
  - URL: https://.../health
  - Returns 200 OK: [ ]
  - Includes database connectivity check: [ ]

- [ ] **UI health endpoint tested**
  - URL: https://.../api/health
  - Returns 200 OK: [ ]
  - Includes backend connectivity check: [ ]

---

## Monitoring Phase

### Application Insights

- [ ] **Application Insights configured**
  ```bash
  Name: lankaconnect-prod-insights
  Location: eastus
  ```
  - Instrumentation key configured in API: [ ]
  - Instrumentation key configured in UI: [ ]
  - Custom metrics instrumented: [ ]

### Log Analytics

- [ ] **Log Analytics workspace created**
  ```bash
  Name: lankaconnect-prod-logs
  Retention: 90 days
  ```
  - Workspace ID: ___________________
  - Container Apps environment linked: [ ]
  - Log queries saved: [ ]

### Availability Tests

- [ ] **Availability tests configured**
  - API health check (5 regions, every 5 minutes): [ ]
  - UI home page (5 regions, every 5 minutes): [ ]
  - Login flow (every 15 minutes): [ ]

### Alerts

- [ ] **Critical alerts configured**
  - API health check failure (3 consecutive): [ ]
  - Database connectivity failure: [ ]
  - Error rate > 1% for 5 minutes: [ ]
  - Response time > 2 seconds (95th percentile): [ ]
  - No successful deployments in 1 hour: [ ]

- [ ] **Warning alerts configured**
  - CPU > 80% for 10 minutes: [ ]
  - Memory > 90% for 10 minutes: [ ]
  - Slow query detected (> 5 seconds): [ ]
  - Stripe webhook failure: [ ]
  - Email send failure rate > 5%: [ ]

- [ ] **Alert action groups configured**
  - Email notifications: [ ]
  - SMS notifications (optional): [ ]
  - PagerDuty integration (optional): [ ]

### Dashboard

- [ ] **Production monitoring dashboard created**
  - Overview panel (health, request rate, error rate): [ ]
  - Performance panel (response time, CPU, memory): [ ]
  - Business metrics panel (events, registrations, payments): [ ]
  - Error panel (top errors, exceptions): [ ]
  - Database panel (connections, query performance): [ ]
  - Dashboard URL: ___________________

---

## CI/CD Phase

### GitHub Secrets

- [ ] **Production GitHub Secrets configured**
  - `AZURE_CREDENTIALS_PROD`: [ ] ✅
  - `ACR_USERNAME_PROD`: [ ] ✅
  - `ACR_PASSWORD_PROD`: [ ] ✅
  - `AZURE_PROD_SUBSCRIPTION_ID` (optional): [ ]

### GitHub Workflows

- [ ] **API production workflow created**
  - File: `.github/workflows/deploy-production.yml`
  - Triggers on `main` branch only: [ ]
  - Unit tests required to pass: [ ]
  - Database migrations run automatically: [ ]
  - Blue-green deployment configured: [ ]
  - Rollback on failure configured: [ ]

- [ ] **UI production workflow created**
  - File: `.github/workflows/deploy-ui-production.yml`
  - Triggers on `main` branch and `web/**` changes: [ ]
  - Build verification steps included: [ ]
  - Blue-green deployment configured: [ ]
  - Rollback on failure configured: [ ]

### Workflow Testing

- [ ] **Staging workflows verified**
  - Staging API deployment successful: [ ]
  - Staging UI deployment successful: [ ]
  - No errors in GitHub Actions logs: [ ]

- [ ] **Production workflows dry-run tested**
  - Workflow syntax validated: [ ]
  - Azure credentials verified: [ ]
  - Container App update commands tested: [ ]

---

## Domain and SSL Phase

### Domain Registration

- [ ] **Production domain purchased**
  - Domain: lankaconnect.com
  - Registrar: ___________________
  - Renewal date: ___________________
  - Auto-renewal enabled: [ ]

### DNS Configuration

- [ ] **DNS records configured**
  - A record: lankaconnect.com → [Container App IP] [ ]
  - CNAME: www.lankaconnect.com → lankaconnect-ui-prod.eastus.azurecontainerapps.io [ ]
  - CNAME: api.lankaconnect.com → lankaconnect-api-prod.eastus.azurecontainerapps.io (optional) [ ]
  - TXT record for domain verification: [ ]

### SSL Certificate

- [ ] **SSL certificate provisioned**
  - Certificate type: Azure managed (Let's Encrypt) or custom
  - Custom domain added to Container Apps: [ ]
  - SSL binding configured: [ ]
  - Certificate auto-renewal enabled: [ ]
  - HTTPS redirect enabled: [ ]

### Domain Validation

- [ ] **Domain accessibility verified**
  - https://lankaconnect.com resolves correctly: [ ]
  - https://www.lankaconnect.com resolves correctly: [ ]
  - SSL certificate valid (no browser warnings): [ ]
  - HTTP redirects to HTTPS: [ ]

---

## Business Readiness Phase

### Third-Party Services

- [ ] **Stripe account configured for production**
  - Stripe account activated in LIVE mode: [ ]
  - Live API keys generated: [ ]
  - Webhook endpoint configured: [ ]
  - Webhook secret stored in Key Vault: [ ]
  - Test payment completed successfully: [ ]

- [ ] **Email service configured**
  - Azure Email Service provisioned: [ ]
  - Sender domain verified: [ ]
  - SPF/DKIM records configured: [ ]
  - Test email sent successfully: [ ]

- [ ] **Microsoft Entra ID configured**
  - Production app registration created: [ ]
  - Redirect URIs configured: [ ]
  - API permissions granted: [ ]
  - Test login completed successfully: [ ]

### Content and Legal

- [ ] **Legal documents published**
  - Privacy Policy: [ ] (URL: ___________________)
  - Terms of Service: [ ] (URL: ___________________)
  - Cookie Policy: [ ] (URL: ___________________)
  - Refund Policy: [ ] (URL: ___________________)

- [ ] **Content reviewed and approved**
  - Landing page copy: [ ]
  - Email templates: [ ]
  - Error messages: [ ]
  - Help documentation: [ ]

### Support Channels

- [ ] **Customer support established**
  - Support email configured: support@lankaconnect.com [ ]
  - Support ticketing system (optional): [ ]
  - FAQ page published: [ ]
  - Contact form working: [ ]

---

## Go-Live Phase

### Pre-Launch Testing

- [ ] **Functional testing completed**
  - User registration: [ ]
  - Login/logout (Entra ID): [ ]
  - Event creation (all types): [ ]
  - Event registration (free and paid): [ ]
  - Payment flow (Stripe live mode): [ ]
  - Email notifications: [ ]
  - Image upload (Azure Blob): [ ]
  - Search and filtering: [ ]
  - Dashboard functionality: [ ]

- [ ] **Performance testing completed**
  - Load test with 100 concurrent users: [ ]
  - Response time < 500ms (95th percentile): [ ]
  - No memory leaks detected: [ ]
  - Database query performance acceptable: [ ]

- [ ] **Security testing completed**
  - SQL injection tests passed: [ ]
  - XSS protection verified: [ ]
  - CSRF tokens working: [ ]
  - Authentication bypass attempts blocked: [ ]
  - Rate limiting working: [ ]

### Launch Preparation

- [ ] **Rollback plan documented**
  - Rollback commands prepared: [ ]
  - Database restore procedure documented: [ ]
  - Rollback decision criteria defined: [ ]
  - Rollback timeline: < 5 minutes for app, < 15 minutes for database

- [ ] **Communication plan executed**
  - Stakeholders notified of launch time: [ ]
  - Social media posts scheduled: [ ]
  - Email campaign prepared: [ ]
  - Press release drafted (optional): [ ]

- [ ] **Team availability confirmed**
  - Primary on-call available: [ ]
  - Secondary on-call available: [ ]
  - Escalation contacts reachable: [ ]

### Launch Execution

- [ ] **Final deployment to production**
  - API deployed successfully: [ ]
  - UI deployed successfully: [ ]
  - All health checks passing: [ ]
  - No errors in Application Insights: [ ]

- [ ] **Post-deployment verification**
  - Production URL accessible: [ ]
  - SSL certificate valid: [ ]
  - Login flow working: [ ]
  - Test event created: [ ]
  - Test payment processed: [ ]
  - Email notifications sent: [ ]

- [ ] **DNS cutover (if applicable)**
  - DNS records updated: [ ]
  - DNS propagation verified (dig/nslookup): [ ]
  - Old environment traffic redirected: [ ]

---

## Post-Deployment Phase

### Monitoring (First 24 Hours)

- [ ] **Hour 1: Immediate monitoring**
  - No critical errors: [ ]
  - Response times normal: [ ]
  - No spike in error rate: [ ]
  - User traffic as expected: [ ]

- [ ] **Hour 4: Stability check**
  - All services healthy: [ ]
  - Database performance stable: [ ]
  - No resource exhaustion: [ ]
  - User feedback positive: [ ]

- [ ] **Hour 12: Extended validation**
  - Auto-scaling working correctly: [ ]
  - Background jobs running: [ ]
  - Email queue processing: [ ]
  - Payment webhooks functioning: [ ]

- [ ] **Hour 24: Full day review**
  - No incidents reported: [ ]
  - Performance within SLA: [ ]
  - Cost tracking on target: [ ]
  - User engagement metrics reviewed: [ ]

### Cleanup

- [ ] **Old revisions cleaned up**
  - Inactive Container App revisions deleted: [ ]
  - Old Docker images pruned: [ ]
  - Staging environment still functional: [ ]

- [ ] **Documentation updated**
  - Production URLs documented: [ ]
  - Runbooks updated with actual production commands: [ ]
  - Architecture diagrams updated: [ ]
  - Deployment timeline recorded: [ ]

### Retrospective

- [ ] **Post-launch retrospective completed**
  - What went well: ___________________
  - What could be improved: ___________________
  - Action items: ___________________
  - Lessons learned documented: [ ]

---

## Sign-Off

| Role | Name | Signature | Date |
|------|------|-----------|------|
| Technical Lead | ___________________ | ___________________ | ___________________ |
| Operations Lead | ___________________ | ___________________ | ___________________ |
| Security Lead | ___________________ | ___________________ | ___________________ |
| Product Owner | ___________________ | ___________________ | ___________________ |
| Executive Sponsor | ___________________ | ___________________ | ___________________ |

---

## Emergency Contacts

| Role | Name | Phone | Email | Escalation Level |
|------|------|-------|-------|------------------|
| Primary On-Call | ___________________ | ___________________ | ___________________ | Level 1 |
| Secondary On-Call | ___________________ | ___________________ | ___________________ | Level 2 |
| Database Admin | ___________________ | ___________________ | ___________________ | Level 2 |
| Azure Support | ___________________ | ___________________ | ___________________ | Level 3 |
| Stripe Support | ___________________ | ___________________ | ___________________ | Level 3 |

---

## Rollback Procedure (Emergency)

**If critical issues detected, execute rollback immediately:**

```bash
# 1. Rollback API to previous revision
az containerapp ingress traffic set \
  --name lankaconnect-api-prod \
  --resource-group lankaconnect-prod \
  --revision-weight <previous-revision>=100

# 2. Rollback UI to previous revision
az containerapp ingress traffic set \
  --name lankaconnect-ui-prod \
  --resource-group lankaconnect-prod \
  --revision-weight <previous-revision>=100

# 3. Restore database (if migration issues)
az sql db restore \
  --resource-group lankaconnect-prod \
  --server lankaconnect-sqldb-prod \
  --name LankaConnectDB \
  --dest-name LankaConnectDB-restored \
  --time "<timestamp-before-migration>"

# 4. Verify rollback success
curl https://lankaconnect.com/health
```

**Rollback decision criteria:**
- Error rate > 5% for more than 5 minutes
- Critical functionality broken (login, payments, event creation)
- Database corruption or data loss
- Security vulnerability discovered
- Performance degradation > 10x normal

---

## Appendix: Useful Commands

### Check Container App Status
```bash
az containerapp show \
  --name lankaconnect-api-prod \
  --resource-group lankaconnect-prod \
  --query properties.runningStatus
```

### View Container App Logs
```bash
az containerapp logs show \
  --name lankaconnect-api-prod \
  --resource-group lankaconnect-prod \
  --follow
```

### List Active Revisions
```bash
az containerapp revision list \
  --name lankaconnect-api-prod \
  --resource-group lankaconnect-prod \
  --query "[?properties.trafficWeight>0]"
```

### Check Database Connectivity
```bash
az sql db show \
  --resource-group lankaconnect-prod \
  --server lankaconnect-sqldb-prod \
  --name LankaConnectDB \
  --query status
```

### View Application Insights Metrics
```bash
az monitor app-insights metrics show \
  --app lankaconnect-prod-insights \
  --resource-group lankaconnect-prod \
  --metric "requests/count" \
  --aggregation count \
  --interval PT1H
```

---

**Document Version:** 1.0
**Last Updated:** 2026-01-08
**Next Review:** 2026-02-08
