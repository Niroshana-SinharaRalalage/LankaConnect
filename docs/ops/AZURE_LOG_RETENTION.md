# Azure Log Retention — Container Apps → Log Analytics

**Phase**: 6A.148.W5.6.B.OBS4
**Owner**: Niroshana
**Last updated**: 2026-05-23

---

## Why this exists

LankaConnect's Azure Container App (`lankaconnect-api-staging`) writes stdout/stderr to the
ephemeral container log driver, which retains **~25 minutes** of history. Every refund-flow
post-mortem to date (operator-UAT bugs E1/E2/E3, the 4th-report regression on RR `86d0a7dc`)
hit the same wall: by the time the operator noticed the bug and pinged us, the logs were
gone.

This runbook routes container stdout to a **Log Analytics workspace** (Azure-managed,
queryable via KQL, retention configurable from days to years). Combined with the durable
`communications.email_dispatch_log` table (Phase 6A.148.W5.6.B.OBS1–3), this closes the
"can't reproduce, no evidence" gap for refund-flow operator support.

---

## Target resources

| Resource | Value |
|---|---|
| Resource group | `lankaconnect-rg` |
| Container app | `lankaconnect-api-staging` |
| Log Analytics workspace name | `lankaconnect-staging-logs` |
| Log Analytics workspace customer-id | `b1d673c4-4467-4022-b666-807690c33729` |
| Diagnostic setting name | `refund-flow-retention` |
| Retention (days) | `30` (staging) / `90` (prod when promoted) |

---

## One-shot Azure CLI commands

> Run from a shell logged in to the right subscription (`az login`, `az account set -s …`).
> All commands are idempotent — safe to re-run.

### 1. Locate the workspace resource id

```bash
WS_ID=$(az monitor log-analytics workspace show \
  --resource-group lankaconnect-rg \
  --workspace-name lankaconnect-staging-logs \
  --query id -o tsv)
echo "Workspace: $WS_ID"
```

If the workspace does not exist, create it once:

```bash
az monitor log-analytics workspace create \
  --resource-group lankaconnect-rg \
  --workspace-name lankaconnect-staging-logs \
  --location eastus2 \
  --retention-time 30
```

### 2. Locate the container app resource id

```bash
APP_ID=$(az containerapp show \
  --resource-group lankaconnect-rg \
  --name lankaconnect-api-staging \
  --query id -o tsv)
echo "Container app: $APP_ID"
```

### 3. Create or update the diagnostic setting

```bash
az monitor diagnostic-settings create \
  --name refund-flow-retention \
  --resource "$APP_ID" \
  --workspace "$WS_ID" \
  --logs '[
    {"category": "ContainerAppConsoleLogs", "enabled": true, "retentionPolicy": {"days": 30, "enabled": true}},
    {"category": "ContainerAppSystemLogs",  "enabled": true, "retentionPolicy": {"days": 30, "enabled": true}}
  ]'
```

### 4. Verify

```bash
az monitor diagnostic-settings list --resource "$APP_ID" -o table
```

Expected output includes a row named `refund-flow-retention` pointing at the
`lankaconnect-staging-logs` workspace.

---

## Smoke-test KQL queries

Once a refund happens (or you push a deliberate test log line), confirm ingest in the
Azure Portal → Log Analytics workspace → Logs tab.

### "What did this refund send?" (workflow + dispatch-log correlation)

```kql
ContainerAppConsoleLogs_CL
| where TimeGenerated > ago(2h)
| where Log_s contains "[Phase 6A.148.W5.6.B]"
   or  Log_s contains "[6A.148.W5.D2 DISP]"
   or  Log_s contains "[Phase 6A.148.D9]"
| order by TimeGenerated asc
| project TimeGenerated, ContainerName_s, Log_s
```

### "Did the new event fire?" (race-fix evidence)

```kql
ContainerAppConsoleLogs_CL
| where TimeGenerated > ago(1d)
| where Log_s contains "RefundRequestCompleted START"
   or  Log_s contains "RefundRequestCompleted email SENT"
| order by TimeGenerated asc
```

### "Did a suppression row get written?" (OBS3 audit verification)

The dispatch-log table itself is in the application database (not Log Analytics).
Query directly:

```sql
SELECT id, correlation_id, template_name, recipient_email, entity_type, entity_id,
       suppressed, suppression_reason, dispatched_at, provider_status
FROM   communications.email_dispatch_log
WHERE  dispatched_at >= now() - interval '1 day'
ORDER  BY dispatched_at DESC;
```

---

## Cost note

Azure Log Analytics ingest billing applies. For staging traffic volume this is well under
the free tier (5GB/month). For prod, monitor ingest at workspace-cost dashboard and adjust
retention or sampling if cost becomes load-bearing.

---

## Rollback

Delete the diagnostic setting:

```bash
az monitor diagnostic-settings delete \
  --name refund-flow-retention \
  --resource "$APP_ID"
```

Container app logs continue to flow to the ephemeral driver — operators just lose the
30-day window.
