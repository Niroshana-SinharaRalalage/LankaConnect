# LankaConnect - Monitoring & Alerting Configuration

**Last Updated:** 2025-10-28
**Version:** 1.0
**Environment:** Staging & Production

---

## Overview

This document provides comprehensive monitoring and alerting configuration for LankaConnect Azure deployment.

**Monitoring Stack:**
- Azure Monitor (metrics, logs, alerts)
- Application Insights (APM, distributed tracing)
- Log Analytics (centralized logging)
- Azure Alerts (cost, performance, availability)

---

## 1. Application Insights Setup

### 1.1 Create Application Insights Resource

```bash
# Create Application Insights for staging
az monitor app-insights component create \
  --app lankaconnect-staging-insights \
  --location eastus \
  --resource-group lankaconnect-staging \
  --application-type web \
  --kind web

# Get instrumentation key
INSTRUMENTATION_KEY=$(az monitor app-insights component show \
  --app lankaconnect-staging-insights \
  --resource-group lankaconnect-staging \
  --query instrumentationKey -o tsv)

# Get connection string
CONNECTION_STRING=$(az monitor app-insights component show \
  --app lankaconnect-staging-insights \
  --resource-group lankaconnect-staging \
  --query connectionString -o tsv)

echo "Application Insights Instrumentation Key: $INSTRUMENTATION_KEY"
echo "Application Insights Connection String: $CONNECTION_STRING"
```

### 1.2 Add to Key Vault

```bash
# Store in Key Vault
az keyvault secret set \
  --vault-name lankaconnect-staging-kv \
  --name APPLICATIONINSIGHTS-CONNECTION-STRING \
  --value "$CONNECTION_STRING"
```

### 1.3 Configure Container App

```bash
# Update Container App with Application Insights
az containerapp update \
  --name lankaconnect-api-staging \
  --resource-group lankaconnect-staging \
  --set-env-vars \
    APPLICATIONINSIGHTS_CONNECTION_STRING=secretref:APPLICATIONINSIGHTS-CONNECTION-STRING
```

### 1.4 Add to .NET Application

**File:** `src/LankaConnect.API/Program.cs`

```csharp
// Add Application Insights (if not already added)
builder.Services.AddApplicationInsightsTelemetry(options =>
{
    options.ConnectionString = builder.Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"];
});
```

---

## 2. Key Metrics to Monitor

### 2.1 Application Performance Metrics

#### API Response Time
```kusto
// Query in Log Analytics
requests
| where timestamp > ago(1h)
| summarize
    avg(duration),
    percentile(duration, 50),
    percentile(duration, 95),
    percentile(duration, 99)
    by name
| order by avg_duration desc
```

**Thresholds:**
- P50: < 200ms (good), < 500ms (acceptable), > 500ms (alert)
- P95: < 500ms (good), < 1000ms (acceptable), > 1000ms (alert)
- P99: < 1000ms (good), < 2000ms (acceptable), > 2000ms (alert)

#### API Failure Rate
```kusto
requests
| where timestamp > ago(1h)
| summarize
    total=count(),
    failures=countif(success == false)
| extend failure_rate = (failures * 100.0) / total
```

**Thresholds:**
- < 1%: Good
- 1-5%: Warning
- > 5%: Critical

#### Database Query Performance
```kusto
dependencies
| where type == "SQL"
| where timestamp > ago(1h)
| summarize
    avg(duration),
    percentile(duration, 95)
    by name
| order by avg_duration desc
```

**Thresholds:**
- P95: < 100ms (good), < 500ms (acceptable), > 500ms (investigate)

### 2.2 Infrastructure Metrics

#### Container App CPU Usage
```bash
# Query CPU usage
az monitor metrics list \
  --resource /subscriptions/$(az account show --query id -o tsv)/resourceGroups/lankaconnect-staging/providers/Microsoft.App/containerApps/lankaconnect-api-staging \
  --metric "UsageNanoCores" \
  --aggregation Average \
  --interval PT1H
```

**Thresholds:**
- < 70%: Good
- 70-90%: Warning (consider scaling)
- > 90%: Critical (scale immediately)

#### Container App Memory Usage
```bash
# Query memory usage
az monitor metrics list \
  --resource /subscriptions/$(az account show --query id -o tsv)/resourceGroups/lankaconnect-staging/providers/Microsoft.App/containerApps/lankaconnect-api-staging \
  --metric "WorkingSetBytes" \
  --aggregation Average \
  --interval PT1H
```

**Thresholds:**
- < 400 MB: Good (0.5 GB limit for staging)
- 400-450 MB: Warning
- > 450 MB: Critical (memory leak or need to scale)

#### PostgreSQL Connection Count
```bash
# Query active connections
az postgres flexible-server show \
  --resource-group lankaconnect-staging \
  --name lankaconnect-staging-db \
  --query "storage.storageSizeGb"
```

**Thresholds:**
- < 50 connections: Good
- 50-80 connections: Warning (check for connection leaks)
- > 80 connections: Critical (max for B1ms is ~100)

#### PostgreSQL Storage Usage
```bash
# Query storage usage
az monitor metrics list \
  --resource /subscriptions/$(az account show --query id -o tsv)/resourceGroups/lankaconnect-staging/providers/Microsoft.DBforPostgreSQL/flexibleServers/lankaconnect-staging-db \
  --metric "storage_percent" \
  --aggregation Average
```

**Thresholds:**
- < 70%: Good
- 70-85%: Warning (plan capacity upgrade)
- > 85%: Critical (upgrade storage immediately)

---

## 3. Alerting Rules

### 3.1 High Error Rate Alert

```bash
# Create alert for API error rate > 5%
az monitor metrics alert create \
  --name "LankaConnect-Staging-High-Error-Rate" \
  --resource-group lankaconnect-staging \
  --scopes /subscriptions/$(az account show --query id -o tsv)/resourceGroups/lankaconnect-staging/providers/Microsoft.App/containerApps/lankaconnect-api-staging \
  --condition "total Requests where ResultCode >= 500 > 5" \
  --window-size 5m \
  --evaluation-frequency 1m \
  --severity 2 \
  --description "Alert when API error rate exceeds 5%"
```

### 3.2 High Response Time Alert

```bash
# Create alert for P95 response time > 2 seconds
az monitor metrics alert create \
  --name "LankaConnect-Staging-Slow-Response" \
  --resource-group lankaconnect-staging \
  --scopes /subscriptions/$(az account show --query id -o tsv)/resourceGroups/lankaconnect-staging/providers/Microsoft.App/containerApps/lankaconnect-api-staging \
  --condition "avg Duration > 2000" \
  --window-size 5m \
  --evaluation-frequency 1m \
  --severity 3 \
  --description "Alert when API response time exceeds 2 seconds"
```

### 3.3 Database Connection Failure Alert

```bash
# Create alert for database connection failures
az monitor metrics alert create \
  --name "LankaConnect-Staging-DB-Connection-Failed" \
  --resource-group lankaconnect-staging \
  --scopes /subscriptions/$(az account show --query id -o tsv)/resourceGroups/lankaconnect-staging/providers/Microsoft.DBforPostgreSQL/flexibleServers/lankaconnect-staging-db \
  --condition "total failed_connections > 10" \
  --window-size 5m \
  --evaluation-frequency 1m \
  --severity 1 \
  --description "Alert when database connection failures exceed 10 in 5 minutes"
```

### 3.4 Cost Budget Alert

```bash
# Create budget alert for monthly spending > $60
az consumption budget create \
  --budget-name "LankaConnect-Staging-Budget-Alert" \
  --resource-group lankaconnect-staging \
  --amount 60 \
  --time-grain Monthly \
  --start-date $(date -u +%Y-%m-01T00:00:00Z) \
  --end-date $(date -u -d '+1 year' +%Y-%m-01T00:00:00Z) \
  --notifications \
    '{
      "80-percent-notification": {
        "enabled": true,
        "operator": "GreaterThan",
        "threshold": 80,
        "contactEmails": ["devops@lankaconnect.com"],
        "contactRoles": [],
        "thresholdType": "Actual"
      },
      "100-percent-notification": {
        "enabled": true,
        "operator": "GreaterThan",
        "threshold": 100,
        "contactEmails": ["devops@lankaconnect.com"],
        "contactRoles": [],
        "thresholdType": "Actual"
      }
    }'
```

### 3.5 Container App Unavailable Alert

```bash
# Create alert for Container App unavailability
az monitor metrics alert create \
  --name "LankaConnect-Staging-App-Down" \
  --resource-group lankaconnect-staging \
  --scopes /subscriptions/$(az account show --query id -o tsv)/resourceGroups/lankaconnect-staging/providers/Microsoft.App/containerApps/lankaconnect-api-staging \
  --condition "total Replicas == 0" \
  --window-size 5m \
  --evaluation-frequency 1m \
  --severity 0 \
  --description "CRITICAL: Container App has zero replicas running"
```

---

## 4. Log Queries (Kusto Query Language)

### 4.1 Recent Errors

```kusto
// Find all errors in last 1 hour
traces
| where timestamp > ago(1h)
| where severityLevel >= 3  // Error or Critical
| project timestamp, severityLevel, message, customDimensions
| order by timestamp desc
| take 100
```

### 4.2 Slow Queries

```kusto
// Find slow database queries (> 1 second)
dependencies
| where type == "SQL"
| where duration > 1000
| where timestamp > ago(1h)
| project timestamp, duration, name, data, resultCode
| order by duration desc
```

### 4.3 User Login Activity

```kusto
// Track Entra External ID logins
customEvents
| where name == "UserLogin"
| where customDimensions.IdentityProvider == "EntraExternal"
| where timestamp > ago(24h)
| summarize count() by bin(timestamp, 1h), tostring(customDimensions.IsNewUser)
| render timechart
```

### 4.4 API Endpoint Usage

```kusto
// Top 10 most frequently called endpoints
requests
| where timestamp > ago(24h)
| summarize count() by name
| top 10 by count_ desc
| render barchart
```

### 4.5 Failed Requests by Status Code

```kusto
// Failed requests grouped by status code
requests
| where timestamp > ago(1h)
| where success == false
| summarize count() by resultCode
| render piechart
```

---

## 5. Dashboard Configuration

### 5.1 Create Azure Dashboard

```bash
# Create dashboard JSON file
cat > staging-dashboard.json << 'EOF'
{
  "lenses": {
    "0": {
      "order": 0,
      "parts": {
        "0": {
          "position": {"x": 0, "y": 0, "rowSpan": 4, "colSpan": 6},
          "metadata": {
            "type": "Extension/HubsExtension/PartType/MonitorChartPart",
            "settings": {
              "content": {
                "options": {
                  "chart": {
                    "metrics": [
                      {
                        "resourceMetadata": {
                          "id": "/subscriptions/SUBSCRIPTION_ID/resourceGroups/lankaconnect-staging/providers/Microsoft.App/containerApps/lankaconnect-api-staging"
                        },
                        "name": "Requests",
                        "aggregationType": "Total"
                      }
                    ],
                    "title": "API Requests",
                    "titleKind": 1
                  }
                }
              }
            }
          }
        }
      }
    }
  }
}
EOF

# Import dashboard
az portal dashboard create \
  --name "LankaConnect-Staging-Dashboard" \
  --resource-group lankaconnect-staging \
  --input-path staging-dashboard.json
```

### 5.2 Key Dashboard Widgets

**Widget 1: API Request Rate**
- Chart type: Line chart
- Metric: Requests per minute
- Aggregation: Count
- Time range: Last 24 hours

**Widget 2: Error Rate**
- Chart type: Area chart
- Metric: Failed requests (HTTP 5xx)
- Aggregation: Percentage
- Time range: Last 24 hours

**Widget 3: Response Time (P95)**
- Chart type: Line chart
- Metric: Response time (milliseconds)
- Aggregation: 95th percentile
- Time range: Last 24 hours

**Widget 4: Database Connections**
- Chart type: Line chart
- Metric: Active PostgreSQL connections
- Aggregation: Average
- Time range: Last 6 hours

**Widget 5: Container App Replicas**
- Chart type: Line chart
- Metric: Running replicas
- Aggregation: Average
- Time range: Last 6 hours

**Widget 6: Cost (Month-to-Date)**
- Chart type: Single value
- Metric: Accumulated cost
- Aggregation: Sum
- Time range: Current month

---

## 6. Notification Channels

### 6.1 Email Notifications

```bash
# Create action group for email notifications
az monitor action-group create \
  --name "LankaConnect-Staging-Email-Alerts" \
  --resource-group lankaconnect-staging \
  --short-name "LC-Email" \
  --email-receiver \
    name="DevOps Team" \
    email-address="devops@lankaconnect.com" \
    use-common-alert-schema=true
```

### 6.2 SMS Notifications (Optional)

```bash
# Add SMS receiver to action group
az monitor action-group update \
  --name "LankaConnect-Staging-Email-Alerts" \
  --resource-group lankaconnect-staging \
  --add receivers \
    sms-receivers \
      name="On-Call Engineer" \
      country-code="1" \
      phone-number="555-123-4567"
```

### 6.3 Webhook Notifications (Slack/Teams)

```bash
# Add webhook for Slack/Teams integration
az monitor action-group update \
  --name "LankaConnect-Staging-Email-Alerts" \
  --resource-group lankaconnect-staging \
  --add receivers \
    webhook-receivers \
      name="Slack-DevOps-Channel" \
      service-uri="https://hooks.slack.com/services/YOUR/WEBHOOK/URL" \
      use-common-alert-schema=true
```

---

## 7. Health Checks

### 7.1 Application Health Endpoint

**Endpoint:** `/health`

**Expected Response:**
```json
{
  "status": "Healthy",
  "service": "Authentication",
  "timestamp": "2025-10-28T22:00:00Z"
}
```

### 7.2 Liveness Probe

**Container App Configuration:**
```yaml
probes:
  liveness:
    httpGet:
      path: /health
      port: 5000
    initialDelaySeconds: 10
    periodSeconds: 10
    timeoutSeconds: 3
    failureThreshold: 3
```

### 7.3 Readiness Probe

**Container App Configuration:**
```yaml
probes:
  readiness:
    httpGet:
      path: /health
      port: 5000
    initialDelaySeconds: 5
    periodSeconds: 5
    timeoutSeconds: 3
    failureThreshold: 3
```

---

## 8. Monitoring Checklist

### Daily Monitoring

- [ ] Check Container App replicas (should be 1-3)
- [ ] Review error rate (should be < 1%)
- [ ] Check response times (P95 < 500ms)
- [ ] Verify database connections (< 50 active)
- [ ] Review cost dashboard (on track for ~$50/month)

### Weekly Monitoring

- [ ] Review Application Insights for trends
- [ ] Check slow queries in Log Analytics
- [ ] Verify all alerts are functioning
- [ ] Review security logs for anomalies
- [ ] Check storage usage (database, blob storage)

### Monthly Monitoring

- [ ] Cost analysis and optimization review
- [ ] Performance trending analysis
- [ ] Capacity planning review
- [ ] Alert tuning (reduce false positives)
- [ ] Dashboard updates

---

## 9. Production Monitoring Enhancements

**Additional for Production:**

1. **Availability Monitoring**
   - Multi-region health checks
   - Synthetic transactions every 5 minutes
   - Uptime SLA tracking (target: 99.9%)

2. **Advanced APM**
   - Distributed tracing across services
   - Custom metrics (business KPIs)
   - User journey tracking

3. **Security Monitoring**
   - Failed authentication attempts
   - Unusual access patterns
   - DDOS attack detection

4. **Compliance Monitoring**
   - GDPR compliance checks
   - Data retention policy enforcement
   - Audit log completeness

---

## 10. Troubleshooting Queries

### High Memory Usage Investigation

```kusto
// Find processes consuming high memory
performanceCounters
| where name == "% Process\\Working Set"
| where timestamp > ago(1h)
| summarize avg(value) by bin(timestamp, 5m), cloud_RoleInstance
| render timechart
```

### Failed Database Connections

```kusto
// Database connection failures
dependencies
| where type == "SQL"
| where success == false
| where timestamp > ago(1h)
| project timestamp, name, resultCode, duration, customDimensions
| order by timestamp desc
```

### Memory Leak Detection

```kusto
// Detect memory leaks (increasing memory over time)
performanceCounters
| where name == "\\Process(w3wp)\\Working Set"
| where timestamp > ago(6h)
| summarize avg(value) by bin(timestamp, 10m)
| render timechart
```

---

## 📞 Alert Contact Information

**Email:** devops@lankaconnect.com
**On-Call Rotation:** See PagerDuty schedule
**Escalation:** CTO for critical alerts (severity 0-1)

---

**Document Version:** 1.0
**Last Reviewed:** 2025-10-28
**Next Review:** 2025-11-28
