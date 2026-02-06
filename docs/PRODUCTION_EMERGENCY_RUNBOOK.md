# Production Emergency Runbook - LankaConnect

**Version:** 1.0
**Last Updated:** 2026-01-08

This runbook provides quick reference commands for emergency production scenarios. Print and keep handy for on-call engineers.

---

## 🚨 Emergency Contacts

| Role | Name | Phone | Email |
|------|------|-------|-------|
| Primary On-Call | ___________ | ___________ | ___________ |
| Secondary On-Call | ___________ | ___________ | ___________ |
| Technical Lead | ___________ | ___________ | ___________ |
| Azure Support | ___________ | 1-800-642-7676 | ___________ |

---

## 🔥 Critical Scenarios

### Scenario 1: Site Down (Complete Outage)

**Symptoms:** Health check failing, 503 errors, no response

**Immediate Actions:**

```bash
# 1. Check Container App status
az containerapp show \
  --name lankaconnect-api-prod \
  --resource-group lankaconnect-prod \
  --query properties.runningStatus

# 2. Check active revisions
az containerapp revision list \
  --name lankaconnect-api-prod \
  --resource-group lankaconnect-prod \
  --query "[].{Name:name, Active:properties.active, Traffic:properties.trafficWeight, Health:properties.healthState}"

# 3. View recent logs
az containerapp logs show \
  --name lankaconnect-api-prod \
  --resource-group lankaconnect-prod \
  --tail 100

# 4. If bad deployment, rollback immediately
PREVIOUS_REVISION="<get-from-step-2>"
az containerapp ingress traffic set \
  --name lankaconnect-api-prod \
  --resource-group lankaconnect-prod \
  --revision-weight $PREVIOUS_REVISION=100

# 5. Verify health restored
curl https://lankaconnect-api-prod.eastus.azurecontainerapps.io/health
```

**Escalation:** If not resolved in 15 minutes, escalate to Technical Lead

---

### Scenario 2: Database Connection Failure

**Symptoms:** "Unable to connect to database", connection timeout errors

**Immediate Actions:**

```bash
# 1. Check database status
az sql db show \
  --resource-group lankaconnect-prod \
  --server lankaconnect-sqldb-prod \
  --name LankaConnectDB \
  --query status

# 2. Check firewall rules
az sql server firewall-rule list \
  --resource-group lankaconnect-prod \
  --server lankaconnect-sqldb-prod

# 3. Check if database is paused
az sql db show \
  --resource-group lankaconnect-prod \
  --server lankaconnect-sqldb-prod \
  --name LankaConnectDB \
  --query status

# If paused, resume:
az sql db resume \
  --resource-group lankaconnect-prod \
  --server lankaconnect-sqldb-prod \
  --name LankaConnectDB

# 4. Verify connection string in Key Vault
az keyvault secret show \
  --vault-name lc-prod-kv \
  --name database-connection-string \
  --query value -o tsv

# 5. Restart Container App to reset connection pool
az containerapp revision restart \
  --name lankaconnect-api-prod \
  --resource-group lankaconnect-prod \
  --revision <current-revision>
```

**Escalation:** If database is corrupted or unresponsive, restore from backup (see Scenario 7)

---

### Scenario 3: High Error Rate (>5%)

**Symptoms:** Application Insights shows error rate spike, users reporting failures

**Immediate Actions:**

```bash
# 1. Check Application Insights for errors
# Open: https://portal.azure.com/#@<tenant>/resource/.../components/lankaconnect-prod-insights/failures

# 2. View exception details in logs
az containerapp logs show \
  --name lankaconnect-api-prod \
  --resource-group lankaconnect-prod \
  --tail 200 \
  | grep -i "exception\|error"

# 3. Check recent deployments
az containerapp revision list \
  --name lankaconnect-api-prod \
  --resource-group lankaconnect-prod \
  --query "[?properties.active].{Name:name, Created:properties.createdTime, Traffic:properties.trafficWeight}"

# 4. If recent deployment is causing errors, rollback
PREVIOUS_REVISION="<previous-stable-revision>"
az containerapp ingress traffic set \
  --name lankaconnect-api-prod \
  --resource-group lankaconnect-prod \
  --revision-weight $PREVIOUS_REVISION=100

# 5. Monitor error rate after rollback
# Wait 5 minutes and check Application Insights
```

**Decision Matrix:**
- Error rate drops below 1% after rollback → Investigation can wait until morning
- Error rate still high after rollback → Database issue, proceed to Scenario 7
- Intermittent errors → Third-party service issue, check Stripe/Email

---

### Scenario 4: Stripe Payment Failures

**Symptoms:** Users reporting payment failures, Stripe webhooks not received

**Immediate Actions:**

```bash
# 1. Check Stripe API status
# Visit: https://status.stripe.com

# 2. Verify Stripe secrets in Key Vault
az keyvault secret show --vault-name lc-prod-kv --name stripe-secret-key --query value -o tsv
# Verify it starts with "sk_live_" (not sk_test_)

az keyvault secret show --vault-name lc-prod-kv --name stripe-webhook-secret --query value -o tsv

# 3. Check Container App environment variables
az containerapp show \
  --name lankaconnect-api-prod \
  --resource-group lankaconnect-prod \
  --query properties.template.containers[0].env

# 4. Test webhook endpoint manually
curl -X POST https://lankaconnect-api-prod.eastus.azurecontainerapps.io/api/payments/webhook \
  -H "Content-Type: application/json" \
  -d '{"type":"test.event"}'

# 5. Check Stripe dashboard for webhook delivery failures
# https://dashboard.stripe.com/webhooks/<webhook-id>
```

**Temporary Mitigation:**
- If webhooks failing, manually reconcile payments from Stripe dashboard
- Update webhook URL in Stripe if Container App URL changed
- Rotate webhook secret if signature validation fails

---

### Scenario 5: Email Service Down

**Symptoms:** Users not receiving emails, email send failures in logs

**Immediate Actions:**

```bash
# 1. Check Azure Email Service status
# Portal: https://portal.azure.com/#@<tenant>/resource/.../providers/Microsoft.Communication/EmailServices

# 2. Verify email secrets
az keyvault secret show --vault-name lc-prod-kv --name azure-email-connection-string --query value -o tsv
az keyvault secret show --vault-name lc-prod-kv --name azure-email-sender-address --query value -o tsv

# 3. Check Container App logs for email errors
az containerapp logs show \
  --name lankaconnect-api-prod \
  --resource-group lankaconnect-prod \
  --tail 100 \
  | grep -i "email\|smtp"

# 4. Test email sending from Container App
# Use Azure Portal Console or exec into container:
az containerapp exec \
  --name lankaconnect-api-prod \
  --resource-group lankaconnect-prod \
  --command "/bin/sh"
```

**Temporary Mitigation:**
- If Azure Email Service is down, switch to SMTP fallback
- Update environment variable: `EmailSettings__Provider=SMTP`
- Users can still use the platform, emails will be queued

---

### Scenario 6: Out of Memory (OOMKill)

**Symptoms:** Container App crashing, "Out of Memory" errors in logs

**Immediate Actions:**

```bash
# 1. Check container memory usage
az containerapp show \
  --name lankaconnect-api-prod \
  --resource-group lankaconnect-prod \
  --query properties.template.containers[0].resources.memory

# 2. View memory consumption metrics
# Portal: Container Apps → Metrics → Memory Usage

# 3. Scale up memory temporarily
az containerapp update \
  --name lankaconnect-api-prod \
  --resource-group lankaconnect-prod \
  --memory 8.0Gi  # Double from 4GB to 8GB

# 4. Increase replica count to distribute load
az containerapp update \
  --name lankaconnect-api-prod \
  --resource-group lankaconnect-prod \
  --min-replicas 4 \
  --max-replicas 20

# 5. Monitor for memory leaks
# Check Application Insights for memory growth over time
```

**Root Cause Investigation (Later):**
- Review code for memory leaks (unclosed connections, large object retention)
- Check for large file uploads without streaming
- Analyze database query result set sizes

---

### Scenario 7: Database Restore Required

**Symptoms:** Data corruption, failed migration, or accidental data deletion

**⚠️ CRITICAL:** This causes downtime. Coordinate with Technical Lead.

**Immediate Actions:**

```bash
# 1. Stop traffic to avoid further damage
az containerapp update \
  --name lankaconnect-api-prod \
  --resource-group lankaconnect-prod \
  --min-replicas 0 \
  --max-replicas 0

# 2. List available backups
az sql db list-restores \
  --resource-group lankaconnect-prod \
  --server lankaconnect-sqldb-prod \
  --database LankaConnectDB

# 3. Restore database to specific point in time
# ⚠️ Replace <timestamp> with time BEFORE corruption
az sql db restore \
  --resource-group lankaconnect-prod \
  --server lankaconnect-sqldb-prod \
  --name LankaConnectDB \
  --dest-name LankaConnectDB-restored \
  --time "<timestamp>"  # Format: 2026-01-08T10:30:00Z

# 4. Verify restored database
az sql db show \
  --resource-group lankaconnect-prod \
  --server lankaconnect-sqldb-prod \
  --name LankaConnectDB-restored \
  --query status

# 5. Update connection string to point to restored database
NEW_CONNECTION_STRING="Server=tcp:lankaconnect-sqldb-prod.database.windows.net,1433;Initial Catalog=LankaConnectDB-restored;..."

az keyvault secret set \
  --vault-name lc-prod-kv \
  --name database-connection-string \
  --value "$NEW_CONNECTION_STRING"

# 6. Restart Container App with new connection string
az containerapp update \
  --name lankaconnect-api-prod \
  --resource-group lankaconnect-prod \
  --min-replicas 2 \
  --max-replicas 10

az containerapp revision restart \
  --name lankaconnect-api-prod \
  --resource-group lankaconnect-prod \
  --revision <current-revision>

# 7. Verify application is working
curl https://lankaconnect-api-prod.eastus.azurecontainerapps.io/health
```

**Estimated Downtime:** 15-30 minutes (depending on database size)

**Post-Restore Actions:**
- Communicate downtime to users
- Document what caused corruption
- Review backup retention policy

---

### Scenario 8: SSL Certificate Expired

**Symptoms:** Browser shows "Certificate expired" warning, HTTPS fails

**Immediate Actions:**

```bash
# 1. Check certificate status
az containerapp hostname list \
  --name lankaconnect-ui-prod \
  --resource-group lankaconnect-prod

# 2. If using Azure Managed Certificate, it should auto-renew
# Manually trigger renewal:
az containerapp hostname bind \
  --name lankaconnect-ui-prod \
  --resource-group lankaconnect-prod \
  --hostname lankaconnect.com \
  --environment lankaconnect-prod-env \
  --validation-method HTTP

# 3. If using custom certificate, upload new certificate
az containerapp ssl upload \
  --name lankaconnect-ui-prod \
  --resource-group lankaconnect-prod \
  --certificate-file /path/to/cert.pfx \
  --certificate-password <password>

# 4. Verify HTTPS is working
curl -v https://lankaconnect.com 2>&1 | grep "SSL certificate"
```

**Temporary Mitigation:**
- Users can access via Container App URL (*.azurecontainerapps.io) while certificate renews

---

## 📊 Monitoring Quick Links

| Resource | URL |
|----------|-----|
| **Production API** | https://lankaconnect-api-prod.eastus.azurecontainerapps.io |
| **Production UI** | https://lankaconnect-ui-prod.eastus.azurecontainerapps.io |
| **Application Insights** | [Portal Link] |
| **Container Apps Logs** | [Portal Link] |
| **SQL Database** | [Portal Link] |
| **Key Vault** | [Portal Link] |
| **Stripe Dashboard** | https://dashboard.stripe.com |

---

## 🔍 Diagnostic Commands

### Check Overall Health

```bash
# API Health
curl https://lankaconnect-api-prod.eastus.azurecontainerapps.io/health

# UI Health
curl https://lankaconnect-ui-prod.eastus.azurecontainerapps.io/api/health

# Database Status
az sql db show \
  --resource-group lankaconnect-prod \
  --server lankaconnect-sqldb-prod \
  --name LankaConnectDB \
  --query "{Name:name, Status:status, DTU:currentServiceObjectiveName}"
```

### View Real-Time Logs

```bash
# API Logs (follow)
az containerapp logs show \
  --name lankaconnect-api-prod \
  --resource-group lankaconnect-prod \
  --follow

# UI Logs (follow)
az containerapp logs show \
  --name lankaconnect-ui-prod \
  --resource-group lankaconnect-prod \
  --follow
```

### Check Resource Usage

```bash
# CPU and Memory
az containerapp show \
  --name lankaconnect-api-prod \
  --resource-group lankaconnect-prod \
  --query "properties.template.containers[0].resources"

# Current Replica Count
az containerapp replica list \
  --name lankaconnect-api-prod \
  --resource-group lankaconnect-prod
```

---

## 🔄 Rollback Procedures

### Application Rollback (< 30 seconds)

```bash
# 1. List revisions
az containerapp revision list \
  --name lankaconnect-api-prod \
  --resource-group lankaconnect-prod \
  --query "[].{Name:name, Active:properties.active, Traffic:properties.trafficWeight, Created:properties.createdTime}"

# 2. Identify previous stable revision
PREVIOUS_REVISION="<revision-name>"

# 3. Shift 100% traffic to previous revision
az containerapp ingress traffic set \
  --name lankaconnect-api-prod \
  --resource-group lankaconnect-prod \
  --revision-weight $PREVIOUS_REVISION=100

# 4. Verify rollback success
curl https://lankaconnect-api-prod.eastus.azurecontainerapps.io/health

# 5. Deactivate failed revision
az containerapp revision deactivate \
  --name lankaconnect-api-prod \
  --resource-group lankaconnect-prod \
  --revision <failed-revision>
```

### Database Rollback (15-30 minutes)

See Scenario 7 above for complete procedure.

---

## 🚦 Escalation Criteria

### Immediate Escalation (Call Secondary On-Call)
- Complete site outage (all services down)
- Data corruption or loss detected
- Security breach suspected
- Database unresponsive or corrupted

### Escalate in 30 Minutes (Email/Slack)
- Partial outage affecting critical features
- High error rate (>5%) not resolved by rollback
- Payment processing failures
- Performance degradation (>10x normal response time)

### Escalate in 1 Hour
- Non-critical service degradation
- Email delivery failures
- Elevated error rate (1-5%)
- Third-party service integration issues

### Can Wait Until Morning
- Non-critical bugs
- UI display issues (no functionality impact)
- Warning alerts (CPU, memory in acceptable range)
- Individual user issues

---

## 📝 Post-Incident Checklist

After resolving an incident:

- [ ] Document what happened (timeline, root cause)
- [ ] Update this runbook if new scenario discovered
- [ ] Create post-mortem document
- [ ] Identify preventive measures
- [ ] Update monitoring/alerting if gaps found
- [ ] Communicate resolution to stakeholders
- [ ] Schedule post-mortem meeting within 48 hours

---

## 🔐 Access Requirements

**Ensure you have:**
- [ ] Azure CLI installed and authenticated
- [ ] Access to production Azure subscription
- [ ] Access to production Key Vault
- [ ] Stripe dashboard access (for payment issues)
- [ ] GitHub repository access (for redeployment)
- [ ] This runbook accessible offline (print or save locally)

---

## 📞 Vendor Support

### Azure Support

```bash
# Check Azure service health
az rest --method get --uri "https://management.azure.com/subscriptions/{subscriptionId}/providers/Microsoft.ResourceHealth/availabilityStatuses?api-version=2020-05-01"

# Create support ticket (if needed)
# Portal: https://portal.azure.com/#blade/Microsoft_Azure_Support/HelpAndSupportBlade/newsupportrequest
```

**Azure Support Phone:** 1-800-642-7676

### Stripe Support

**Dashboard:** https://dashboard.stripe.com/support
**Phone:** Check Stripe dashboard for phone number
**Status Page:** https://status.stripe.com

### Microsoft Entra ID Support

**Portal:** https://portal.azure.com/#blade/Microsoft_AAD_IAM/ActiveDirectoryMenuBlade/SupportRequest

---

## 🧪 Testing in Staging First

**Before making ANY changes in production, test in staging:**

```bash
# Staging API
curl https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/health

# Staging UI
curl https://lankaconnect-ui-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/api/health
```

**Never test experimental commands in production!**

---

**Emergency Runbook Version:** 1.0
**Last Updated:** 2026-01-08
**Next Review:** After first production incident

**Print this document and keep it accessible for on-call shifts.**
