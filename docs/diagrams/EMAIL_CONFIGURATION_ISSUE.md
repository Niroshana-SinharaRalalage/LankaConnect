# Email Configuration Issue - Visual Diagrams

## Configuration Override Flow

```
┌─────────────────────────────────────────────────────────────────┐
│                   ASP.NET Core Configuration                     │
│                        (Priority Order)                          │
└─────────────────────────────────────────────────────────────────┘

Priority 1 (Lowest):  appsettings.json
                      └─> Base configuration
                          ❌ Not used in our case

Priority 2:           appsettings.Staging.json / appsettings.Production.json
                      └─> Environment-specific settings
                          ✅ Sets: "Provider": "Azure"
                          ✅ Sets: "AzureConnectionString": "${AZURE_EMAIL_CONNECTION_STRING}"
                          ✅ Sets: "AzureSenderAddress": "${AZURE_EMAIL_SENDER_ADDRESS}"

Priority 3 (HIGHEST): Environment Variables (Container App)
                      └─> Deployment workflow sets these
                          ❌ OVERRIDES TO: EmailSettings__Provider=Smtp
                          ❌ OVERRIDES TO: EmailSettings__SmtpServer=...
                          ❌ Result: Azure config IGNORED!

┌─────────────────────────────────────────────────────────────────┐
│                       ACTUAL RUNTIME CONFIG                      │
├─────────────────────────────────────────────────────────────────┤
│ Provider: Smtp          ← From environment variable (WRONG!)    │
│ SmtpServer: ...         ← From environment variable             │
│ AzureConnectionString:  ← IGNORED (Provider=Smtp)              │
│ AzureSenderAddress:     ← IGNORED (Provider=Smtp)              │
└─────────────────────────────────────────────────────────────────┘
```

## Problem Flow Diagram

```
┌──────────────────────┐
│  Developer Intent    │
│                      │
│  "Use Azure Email"   │
└──────────┬───────────┘
           │
           ▼
┌──────────────────────────────────────┐
│  appsettings.Production.json         │
│  appsettings.Staging.json            │
├──────────────────────────────────────┤
│  {                                   │
│    "EmailSettings": {                │
│      "Provider": "Azure",  ✅        │
│      "AzureConnectionString": "..." │
│      "AzureSenderAddress": "..."    │
│    }                                 │
│  }                                   │
└──────────┬───────────────────────────┘
           │
           │ Deployment via GitHub Actions
           ▼
┌────────────────────────────────────────────────┐
│  .github/workflows/deploy-staging.yml          │
│                                                 │
│  az containerapp update \                      │
│    --replace-env-vars \                        │
│      EmailSettings__Provider=Smtp ❌ OVERRIDE! │
│      EmailSettings__SmtpServer=...             │
│      EmailSettings__SmtpPort=...               │
└────────────┬───────────────────────────────────┘
             │
             │ Environment variables have HIGHEST priority
             ▼
┌──────────────────────────────────────┐
│  Runtime Configuration               │
├──────────────────────────────────────┤
│  Provider: Smtp          ❌ WRONG!   │
│  SmtpServer: smtp.host               │
│  SmtpPort: 587                       │
│                                      │
│  (Azure settings ignored)            │
└──────────┬───────────────────────────┘
           │
           │ Email send attempted
           ▼
┌──────────────────────────────────────┐
│  EmailService.SendAsync()            │
│                                      │
│  if (Provider == "Smtp")             │
│    └─> Use SmtpClient                │
│  else if (Provider == "Azure")       │
│    └─> Use ACS (NOT REACHED)         │
└──────────┬───────────────────────────┘
           │
           │ SMTP credentials incomplete/invalid
           ▼
┌──────────────────────────────────────┐
│         ❌ EMAIL FAILS                │
│                                      │
│  SmtpClient throws exception         │
│  User never receives email           │
└──────────────────────────────────────┘
```

## Solution Flow Diagram

```
┌──────────────────────┐
│  Fix Applied         │
│                      │
│  "Update Workflow"   │
└──────────┬───────────┘
           │
           ▼
┌────────────────────────────────────────────────┐
│  .github/workflows/deploy-staging.yml (FIXED)  │
│                                                 │
│  az containerapp update \                      │
│    --replace-env-vars \                        │
│      EmailSettings__Provider=Azure ✅ CORRECT! │
│      EmailSettings__AzureConnectionString=...  │
│      EmailSettings__AzureSenderAddress=...     │
└────────────┬───────────────────────────────────┘
             │
             │ Environment variables now match intent
             ▼
┌──────────────────────────────────────┐
│  Runtime Configuration               │
├──────────────────────────────────────┤
│  Provider: Azure         ✅ CORRECT! │
│  AzureConnectionString: ...          │
│  AzureSenderAddress: ...             │
│                                      │
│  (SMTP settings ignored)             │
└──────────┬───────────────────────────┘
           │
           │ Email send attempted
           ▼
┌──────────────────────────────────────┐
│  EmailService.SendAsync()            │
│                                      │
│  if (Provider == "Smtp")             │
│    └─> Use SmtpClient (NOT USED)     │
│  else if (Provider == "Azure")       │
│    └─> Use ACS ✅ THIS PATH!         │
└──────────┬───────────────────────────┘
           │
           │ Azure Communication Services
           ▼
┌──────────────────────────────────────┐
│    ✅ EMAIL SENT SUCCESSFULLY         │
│                                      │
│  Azure Communication Services        │
│  User receives email                 │
└──────────────────────────────────────┘
```

## Configuration Hierarchy Diagram

```
┌─────────────────────────────────────────────────────────────┐
│              Configuration Source Priority                   │
│            (Higher number = Higher priority)                 │
└─────────────────────────────────────────────────────────────┘

     5 │ Command-line arguments
       │ └─> Not used in Container Apps
       │
     4 │ Environment Variables ⚠️ ISSUE IS HERE
       │ ├─> Set by workflow: EmailSettings__Provider=Smtp
       │ ├─> Set by Container App: --replace-env-vars
       │ └─> HIGHEST PRACTICAL PRIORITY
       │
     3 │ User Secrets (Development only)
       │ └─> Not applicable to deployed environments
       │
     2 │ appsettings.{Environment}.json ✅ WHAT WE WANT
       │ ├─> appsettings.Staging.json
       │ ├─> appsettings.Production.json
       │ └─> Sets Provider=Azure (but OVERRIDDEN by #4)
       │
     1 │ appsettings.json
       │ └─> Base configuration
       │
     0 │ Default values in code
       │ └─> Fallback if nothing else set

┌─────────────────────────────────────────────────────────────┐
│                         THE ISSUE                            │
├─────────────────────────────────────────────────────────────┤
│ Level 2 (JSON file) says:  "Provider": "Azure"              │
│ Level 4 (Env vars) says:   EmailSettings__Provider=Smtp     │
│                                                              │
│ Result: Level 4 wins → Provider=Smtp → Emails fail          │
└─────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────┐
│                       THE SOLUTION                           │
├─────────────────────────────────────────────────────────────┤
│ Level 2 (JSON file) says:  "Provider": "Azure"              │
│ Level 4 (Env vars) says:   EmailSettings__Provider=Azure    │
│                                                              │
│ Result: Both agree → Provider=Azure → Emails work ✅         │
└─────────────────────────────────────────────────────────────┘
```

## Deployment Architecture

```
┌────────────────────────────────────────────────────────────────┐
│                      GitHub Actions Workflow                    │
│                                                                 │
│  1. Build .NET Application                                     │
│  2. Run Tests                                                   │
│  3. Build Docker Image                                          │
│  4. Push to Azure Container Registry                           │
│  5. Run EF Migrations                                           │
│  6. Update Container App ⚠️ ISSUE HERE                         │
│     └─> Sets environment variables                             │
│         ├─> EmailSettings__Provider=Smtp ❌                    │
│         ├─> EmailSettings__SmtpServer=...                      │
│         └─> (Should be Azure config instead)                   │
└────────────┬───────────────────────────────────────────────────┘
             │
             │ Deploys to
             ▼
┌────────────────────────────────────────────────────────────────┐
│              Azure Container Apps Environment                   │
│                                                                 │
│  ┌──────────────────────────────────────────────────────────┐  │
│  │  Container App: lankaconnect-api-staging                  │  │
│  │                                                            │  │
│  │  Environment Variables (from workflow):                   │  │
│  │  ├─> ASPNETCORE_ENVIRONMENT=Staging                       │  │
│  │  ├─> ConnectionStrings__DefaultConnection=secretref:...   │  │
│  │  ├─> EmailSettings__Provider=Smtp ❌ WRONG               │  │
│  │  └─> (Other settings...)                                  │  │
│  │                                                            │  │
│  │  Application Files:                                        │  │
│  │  ├─> appsettings.json                                     │  │
│  │  ├─> appsettings.Staging.json ✅ Says Provider=Azure     │  │
│  │  └─> (But overridden by env vars)                         │  │
│  └──────────────────────────────────────────────────────────┘  │
│                                                                 │
│  ┌──────────────────────────────────────────────────────────┐  │
│  │  Key Vault: lankaconnect-staging-kv                       │  │
│  │                                                            │  │
│  │  Current Secrets:                                          │  │
│  │  ✅ DATABASE-CONNECTION-STRING                            │  │
│  │  ✅ JWT-SECRET-KEY                                        │  │
│  │  ✅ SMTP-HOST (old, for SMTP)                            │  │
│  │  ✅ SMTP-PASSWORD (old, for SMTP)                        │  │
│  │  ❌ AZURE-EMAIL-CONNECTION-STRING (MISSING!)              │  │
│  │  ❌ AZURE-EMAIL-SENDER-ADDRESS (MISSING!)                 │  │
│  └──────────────────────────────────────────────────────────┘  │
│                                                                 │
│  ┌──────────────────────────────────────────────────────────┐  │
│  │  Azure Communication Services (Assumed to exist)          │  │
│  │                                                            │  │
│  │  ❓ Resource Name: (unknown)                              │  │
│  │  ❓ Connection String: (need to get)                      │  │
│  │  ❓ Sender Address: (need to get)                         │  │
│  └──────────────────────────────────────────────────────────┘  │
└────────────────────────────────────────────────────────────────┘
```

## Email Service Code Path

```
┌────────────────────────────────────────────────────────────────┐
│                  EmailService.SendAsync()                       │
├────────────────────────────────────────────────────────────────┤
│                                                                 │
│  var provider = _emailSettings.Provider; // From config        │
│                                                                 │
│  if (provider == "Azure")                                      │
│  {                                                              │
│      // Use Azure Communication Services                       │
│      var client = new EmailClient(                             │
│          _emailSettings.AzureConnectionString                  │
│      );                                                         │
│      ✅ THIS PATH SHOULD BE USED                               │
│  }                                                              │
│  else if (provider == "Smtp")                                  │
│  {                                                              │
│      // Use SMTP                                               │
│      var client = new SmtpClient(                              │
│          _emailSettings.SmtpServer,                            │
│          _emailSettings.SmtpPort                               │
│      );                                                         │
│      ❌ THIS PATH IS CURRENTLY USED (WRONG!)                   │
│  }                                                              │
│                                                                 │
└────────────────────────────────────────────────────────────────┘

Current State:                    Desired State:
provider = "Smtp"                provider = "Azure"
└─> Uses SMTP path               └─> Uses Azure path
    └─> Fails (bad credentials)      └─> Works! ✅
```

## Secret Management Flow

```
┌────────────────────────────────────────────────────────────────┐
│                    Secret Reference Pattern                     │
└────────────────────────────────────────────────────────────────┘

Container App Environment Variable:
  EmailSettings__AzureConnectionString=secretref:azure-email-connection-string
                                       └────────┬────────────────┘
                                                │
                                                │ References Key Vault secret
                                                ▼
┌────────────────────────────────────────────────────────────────┐
│  Azure Key Vault: lankaconnect-staging-kv                       │
│                                                                 │
│  Secret Name: azure-email-connection-string                    │
│  Secret Value: endpoint=https://xxx.communication.azure.com/;  │
│                accesskey=xxxxxxxxxxxxx                         │
└────────────────────────────────────────────────────────────────┘
                                                │
                                                │ Retrieved at runtime
                                                ▼
┌────────────────────────────────────────────────────────────────┐
│  Application Runtime                                            │
│                                                                 │
│  _emailSettings.AzureConnectionString =                        │
│    "endpoint=https://xxx.communication.azure.com/;accesskey=xxx"│
│                                                                 │
│  Used by EmailClient to connect to Azure Communication Services│
└────────────────────────────────────────────────────────────────┘

Benefits:
✅ Secrets not in code
✅ Secrets not in workflow files
✅ Automatic secret rotation support
✅ Access control via Azure RBAC
✅ Audit logging
```

## Fix Validation Flow

```
┌────────────────────────────────────────────────────────────────┐
│                      Pre-Deployment Checks                      │
└────────────────────────────────────────────────────────────────┘

Step 1: Run verify-secrets.sh
  ├─> Check Key Vault exists ✅
  ├─> Check azure-email-connection-string exists
  │   ├─> ✅ Found → Continue
  │   └─> ❌ Not found → Show how to add
  └─> Check azure-email-sender-address exists
      ├─> ✅ Found → Continue
      └─> ❌ Not found → Show how to add

Step 2: Apply workflow fix
  ├─> Backup current file ✅
  ├─> Replace SMTP config with Azure config ✅
  └─> Show diff for review ✅

Step 3: Deploy
  ├─> Push to develop branch ✅
  ├─> GitHub Actions triggers ✅
  └─> Monitor deployment logs ✅

┌────────────────────────────────────────────────────────────────┐
│                    Post-Deployment Validation                   │
└────────────────────────────────────────────────────────────────┘

Step 4: Check container app
  ├─> View environment variables
  │   └─> Verify EmailSettings__Provider=Azure ✅
  ├─> Check logs
  │   └─> Look for "Email provider initialized: Azure" ✅
  └─> Health check
      └─> Verify app started successfully ✅

Step 5: Test email functionality
  ├─> User registration (verification email)
  │   ├─> POST /api/auth/register
  │   ├─> Check logs: "Email sent via Azure"
  │   └─> Verify email received ✅
  ├─> Password reset
  │   ├─> POST /api/auth/forgot-password
  │   └─> Verify email received ✅
  └─> Event notification
      ├─> POST /api/events/{id}/signup
      └─> Verify email received ✅

Step 6: Monitoring
  ├─> Application Insights: No errors ✅
  ├─> Container App metrics: Normal ✅
  └─> Email send rate: Within limits ✅

┌────────────────────────────────────────────────────────────────┐
│                        SUCCESS CRITERIA                         │
├────────────────────────────────────────────────────────────────┤
│ ✅ All secrets exist in Key Vault                              │
│ ✅ Workflow updated successfully                               │
│ ✅ Deployment completed without errors                         │
│ ✅ Provider shows as "Azure" in logs                           │
│ ✅ All email types sending successfully                        │
│ ✅ Emails received within 2 minutes                            │
│ ✅ No errors in monitoring                                     │
└────────────────────────────────────────────────────────────────┘
```

## Architecture Decision Record

```
┌────────────────────────────────────────────────────────────────┐
│                  ADR: Email Provider Selection                  │
├────────────────────────────────────────────────────────────────┤
│                                                                 │
│ Decision: Use Azure Communication Services for email           │
│                                                                 │
│ Rationale:                                                      │
│ • Native Azure service (better integration)                    │
│ • Automatic SPF/DKIM configuration                            │
│ • Free tier: 500 emails/month                                  │
│ • Paid tier: $0.25 per 1000 emails (cost-effective)           │
│ • Built-in rate limiting and spam protection                   │
│ • No need to manage SMTP server credentials                    │
│ • Better deliverability (Azure reputation)                     │
│                                                                 │
│ Alternatives Considered:                                        │
│ • SMTP (SendGrid, Mailgun, etc.)                               │
│   - Pros: Provider agnostic, easy to switch                    │
│   - Cons: Additional service, more credentials to manage       │
│ • Self-hosted SMTP                                             │
│   - Pros: Full control                                         │
│   - Cons: Maintenance burden, deliverability challenges        │
│                                                                 │
│ Implementation:                                                 │
│ • Provider configured in appsettings.{Environment}.json        │
│ • Secrets stored in Azure Key Vault                            │
│ • EmailService supports both SMTP and Azure (provider pattern) │
│                                                                 │
│ Monitoring:                                                     │
│ • Application Insights logs all email sends                    │
│ • Alert on send failures > 5 in 5 minutes                     │
│                                                                 │
│ Rollback Plan:                                                  │
│ • Can switch to SMTP by changing environment variable          │
│ • SMTP credentials kept in Key Vault as backup                 │
│                                                                 │
└────────────────────────────────────────────────────────────────┘
```
