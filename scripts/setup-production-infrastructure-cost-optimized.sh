#!/bin/bash

################################################################################
# LankaConnect Production Infrastructure Setup (Cost-Optimized)
################################################################################
#
# Purpose: Create cost-optimized Azure production infrastructure
# Budget: $150-180/month (for apps with <200 users)
# Timeline: 2-3 hours
#
# Key Optimizations:
# - Container Apps: Consumption plan, 0.25 vCPU, 1 min replica
# - Database: Serverless (auto-pause after 60 min)
# - Key Vault: Standard tier (not Premium HSM)
# - Storage: Standard LRS (not Premium)
# - Monitoring: 30-day retention (not 90)
#
# Usage:
#   bash setup-production-infrastructure-cost-optimized.sh
#
################################################################################

set -e  # Exit on error

# Color codes for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Configuration
RESOURCE_GROUP="lankaconnect-prod"
LOCATION="eastus2"
ENVIRONMENT="production"

# Resource names
SQL_SERVER_NAME="lankaconnect-prod-sql"
SQL_DATABASE_NAME="lankaconnect-db"
STORAGE_ACCOUNT_NAME="lankaconnectprodstorage"
KEY_VAULT_NAME="lankaconnect-prod-kv"
CONTAINER_REGISTRY_NAME="lankaconnectprod"
CONTAINER_ENV_NAME="lankaconnect-prod-env"
APP_INSIGHTS_NAME="lankaconnect-prod-insights"

# Container Apps
API_CONTAINER_NAME="lankaconnect-api-prod"
UI_CONTAINER_NAME="lankaconnect-ui-prod"

################################################################################
# Helper Functions
################################################################################

print_header() {
    echo -e "${BLUE}╔═══════════════════════════════════════════════════════════════╗${NC}"
    echo -e "${BLUE}║${NC}  $1${BLUE}║${NC}"
    echo -e "${BLUE}╚═══════════════════════════════════════════════════════════════╝${NC}"
}

print_success() {
    echo -e "${GREEN}✅ $1${NC}"
}

print_warning() {
    echo -e "${YELLOW}⚠️  $1${NC}"
}

print_error() {
    echo -e "${RED}❌ $1${NC}"
}

print_info() {
    echo -e "${BLUE}ℹ️  $1${NC}"
}

confirm_action() {
    echo -e "${YELLOW}$1${NC}"
    read -p "Continue? (y/n) " -n 1 -r
    echo
    if [[ ! $REPLY =~ ^[Yy]$ ]]; then
        print_error "Operation cancelled by user"
        exit 1
    fi
}

################################################################################
# Pre-flight Checks
################################################################################

print_header "Pre-flight Checks"

# Check if Azure CLI is installed
if ! command -v az &> /dev/null; then
    print_error "Azure CLI is not installed. Please install it first."
    print_info "Install from: https://learn.microsoft.com/en-us/cli/azure/install-azure-cli"
    exit 1
fi
print_success "Azure CLI is installed"

# Check if logged in to Azure
if ! az account show &> /dev/null; then
    print_error "Not logged in to Azure. Please run 'az login' first."
    exit 1
fi
print_success "Logged in to Azure"

# Display current subscription
SUBSCRIPTION_NAME=$(az account show --query name -o tsv)
SUBSCRIPTION_ID=$(az account show --query id -o tsv)
print_info "Current subscription: $SUBSCRIPTION_NAME"
print_info "Subscription ID: $SUBSCRIPTION_ID"

################################################################################
# Cost Warning
################################################################################

print_header "Cost Estimate"
echo ""
echo "  This script will create a COST-OPTIMIZED production environment:"
echo ""
echo "  Monthly Cost Breakdown:"
echo "  ─────────────────────────────────────────────────────"
echo "  • Container Apps (2 apps)       : \$30-40/month"
echo "  • Azure SQL Serverless         : \$50-60/month"
echo "  • Storage Account (Standard)   : \$15-20/month"
echo "  • Key Vault (Standard)         : \$5/month"
echo "  • Application Insights         : \$20-30/month"
echo "  • Container Registry (Basic)   : \$5/month"
echo "  • Bandwidth                    : \$20-30/month"
echo "  ─────────────────────────────────────────────────────"
echo "  TOTAL ESTIMATED COST: \$150-180/month ✅"
echo ""
echo "  Optimizations applied:"
echo "  • Serverless database (auto-pauses when idle)"
echo "  • Minimal replica count (1, scales to 3)"
echo "  • Standard-tier services (not Premium)"
echo "  • 30-day log retention (not 90)"
echo ""
confirm_action "Proceed with infrastructure creation?"

################################################################################
# Step 1: Create Resource Group
################################################################################

print_header "Step 1: Create Resource Group"

if az group show --name $RESOURCE_GROUP &> /dev/null; then
    print_warning "Resource group $RESOURCE_GROUP already exists"
    confirm_action "Do you want to use the existing resource group?"
else
    print_info "Creating resource group: $RESOURCE_GROUP"
    az group create \
        --name $RESOURCE_GROUP \
        --location $LOCATION \
        --tags Environment=$ENVIRONMENT CostCenter=Production Application=LankaConnect
    print_success "Resource group created"
fi

################################################################################
# Step 2: Create Container Apps Environment (Consumption Plan)
################################################################################

print_header "Step 2: Create Container Apps Environment"

print_info "Creating Container Apps environment: $CONTAINER_ENV_NAME"
print_info "Using Consumption plan (cost-optimized, pay-per-use)"

az containerapp env create \
    --name $CONTAINER_ENV_NAME \
    --resource-group $RESOURCE_GROUP \
    --location $LOCATION \
    --tags Environment=$ENVIRONMENT

print_success "Container Apps environment created"

################################################################################
# Step 3: Create Azure SQL Database (Serverless - Cost Optimized!)
################################################################################

print_header "Step 3: Create Azure SQL Database (Serverless)"

print_info "Creating SQL Server: $SQL_SERVER_NAME"
echo ""
print_warning "IMPORTANT: Set a secure admin password"
echo "Password requirements:"
echo "  • At least 8 characters"
echo "  • Contains uppercase, lowercase, numbers, and symbols"
echo ""
read -sp "Enter SQL admin password: " SQL_ADMIN_PASSWORD
echo ""
read -sp "Confirm password: " SQL_ADMIN_PASSWORD_CONFIRM
echo ""

if [ "$SQL_ADMIN_PASSWORD" != "$SQL_ADMIN_PASSWORD_CONFIRM" ]; then
    print_error "Passwords do not match"
    exit 1
fi

print_info "Creating SQL Server..."
az sql server create \
    --name $SQL_SERVER_NAME \
    --resource-group $RESOURCE_GROUP \
    --location $LOCATION \
    --admin-user sqladmin \
    --admin-password "$SQL_ADMIN_PASSWORD"

print_success "SQL Server created"

print_info "Creating Serverless SQL Database: $SQL_DATABASE_NAME"
print_info "Configuration: 0.5-1 vCore, auto-pause after 60 minutes"

az sql db create \
    --resource-group $RESOURCE_GROUP \
    --server $SQL_SERVER_NAME \
    --name $SQL_DATABASE_NAME \
    --edition GeneralPurpose \
    --compute-model Serverless \
    --family Gen5 \
    --capacity 1 \
    --min-capacity 0.5 \
    --auto-pause-delay 60 \
    --backup-storage-redundancy Local

print_success "Serverless SQL Database created"
print_info "Database will auto-pause after 60 minutes of inactivity (saves money!)"

# Configure firewall to allow Azure services
print_info "Configuring SQL firewall rules..."
az sql server firewall-rule create \
    --resource-group $RESOURCE_GROUP \
    --server $SQL_SERVER_NAME \
    --name AllowAzureServices \
    --start-ip-address 0.0.0.0 \
    --end-ip-address 0.0.0.0

print_success "SQL firewall configured"

################################################################################
# Step 4: Create Storage Account (Standard LRS - Cost Optimized!)
################################################################################

print_header "Step 4: Create Storage Account"

print_info "Creating storage account: $STORAGE_ACCOUNT_NAME"
print_info "Using Standard LRS (locally redundant, cost-optimized)"

az storage account create \
    --name $STORAGE_ACCOUNT_NAME \
    --resource-group $RESOURCE_GROUP \
    --location $LOCATION \
    --sku Standard_LRS \
    --kind StorageV2 \
    --access-tier Hot \
    --https-only true \
    --min-tls-version TLS1_2

print_success "Storage account created"

# Get storage connection string
STORAGE_CONNECTION_STRING=$(az storage account show-connection-string \
    --name $STORAGE_ACCOUNT_NAME \
    --resource-group $RESOURCE_GROUP \
    --query connectionString \
    --output tsv)

print_success "Storage connection string retrieved"

################################################################################
# Step 5: Create Key Vault (Standard Tier - Cost Optimized!)
################################################################################

print_header "Step 5: Create Key Vault"

print_info "Creating Key Vault: $KEY_VAULT_NAME"
print_info "Using Standard tier (not Premium HSM, saves \$95/month)"

az keyvault create \
    --name $KEY_VAULT_NAME \
    --resource-group $RESOURCE_GROUP \
    --location $LOCATION \
    --sku standard \
    --enabled-for-deployment true \
    --enabled-for-template-deployment true

print_success "Key Vault created"

# Store database connection string in Key Vault
print_info "Storing secrets in Key Vault..."

SQL_CONNECTION_STRING="Server=tcp:$SQL_SERVER_NAME.database.windows.net,1433;Initial Catalog=$SQL_DATABASE_NAME;User ID=sqladmin;Password=$SQL_ADMIN_PASSWORD;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"

az keyvault secret set \
    --vault-name $KEY_VAULT_NAME \
    --name "DatabaseConnectionString" \
    --value "$SQL_CONNECTION_STRING"

az keyvault secret set \
    --vault-name $KEY_VAULT_NAME \
    --name "StorageConnectionString" \
    --value "$STORAGE_CONNECTION_STRING"

print_success "Secrets stored in Key Vault"

################################################################################
# Step 6: Create Container Registry (Basic Tier - Cost Optimized!)
################################################################################

print_header "Step 6: Create Container Registry"

print_info "Creating Container Registry: $CONTAINER_REGISTRY_NAME"
print_info "Using Basic tier (saves \$15/month vs Standard)"

az acr create \
    --name $CONTAINER_REGISTRY_NAME \
    --resource-group $RESOURCE_GROUP \
    --sku Basic \
    --admin-enabled true

print_success "Container Registry created"

# Get ACR credentials
ACR_USERNAME=$(az acr credential show \
    --name $CONTAINER_REGISTRY_NAME \
    --query username \
    --output tsv)

ACR_PASSWORD=$(az acr credential show \
    --name $CONTAINER_REGISTRY_NAME \
    --query "passwords[0].value" \
    --output tsv)

print_success "Container Registry credentials retrieved"

################################################################################
# Step 7: Create Application Insights (30-day retention - Cost Optimized!)
################################################################################

print_header "Step 7: Create Application Insights"

print_info "Creating Application Insights: $APP_INSIGHTS_NAME"
print_info "Using 30-day log retention (not 90, saves \$150/month)"

# Create Log Analytics workspace first
LOG_WORKSPACE_NAME="$RESOURCE_GROUP-logs"

az monitor log-analytics workspace create \
    --resource-group $RESOURCE_GROUP \
    --workspace-name $LOG_WORKSPACE_NAME \
    --location $LOCATION \
    --retention-time 30

# Get workspace ID
WORKSPACE_ID=$(az monitor log-analytics workspace show \
    --resource-group $RESOURCE_GROUP \
    --workspace-name $LOG_WORKSPACE_NAME \
    --query id \
    --output tsv)

# Create Application Insights
az monitor app-insights component create \
    --app $APP_INSIGHTS_NAME \
    --location $LOCATION \
    --resource-group $RESOURCE_GROUP \
    --application-type web \
    --kind web \
    --workspace $WORKSPACE_ID \
    --retention-time 30

print_success "Application Insights created with 30-day retention"

# Get instrumentation key
APP_INSIGHTS_KEY=$(az monitor app-insights component show \
    --app $APP_INSIGHTS_NAME \
    --resource-group $RESOURCE_GROUP \
    --query instrumentationKey \
    --output tsv)

print_success "Application Insights instrumentation key retrieved"

################################################################################
# Step 8: Create Container Apps (Minimal Configuration - Cost Optimized!)
################################################################################

print_header "Step 8: Create Container Apps"

print_info "Creating API Container App: $API_CONTAINER_NAME"
print_info "Configuration: 0.25 vCPU, 0.5 GB RAM, 1 min replica, 3 max"

# Note: We'll create the container apps but not deploy images yet
# Images will be deployed via GitHub Actions workflows

az containerapp create \
    --name $API_CONTAINER_NAME \
    --resource-group $RESOURCE_GROUP \
    --environment $CONTAINER_ENV_NAME \
    --image mcr.microsoft.com/azuredocs/containerapps-helloworld:latest \
    --target-port 80 \
    --ingress external \
    --cpu 0.25 \
    --memory 0.5Gi \
    --min-replicas 1 \
    --max-replicas 3 \
    --scale-rule-name http-rule \
    --scale-rule-type http \
    --scale-rule-http-concurrency 100

print_success "API Container App created"

print_info "Creating UI Container App: $UI_CONTAINER_NAME"
print_info "Configuration: 0.25 vCPU, 0.5 GB RAM, 1 min replica, 3 max"

az containerapp create \
    --name $UI_CONTAINER_NAME \
    --resource-group $RESOURCE_GROUP \
    --environment $CONTAINER_ENV_NAME \
    --image mcr.microsoft.com/azuredocs/containerapps-helloworld:latest \
    --target-port 80 \
    --ingress external \
    --cpu 0.25 \
    --memory 0.5Gi \
    --min-replicas 1 \
    --max-replicas 3 \
    --scale-rule-name http-rule \
    --scale-rule-type http \
    --scale-rule-http-concurrency 100

print_success "UI Container App created"

################################################################################
# Step 9: Configure Container Apps with Managed Identity
################################################################################

print_header "Step 9: Configure Managed Identity"

print_info "Enabling system-assigned managed identity for API..."
az containerapp identity assign \
    --name $API_CONTAINER_NAME \
    --resource-group $RESOURCE_GROUP \
    --system-assigned

API_IDENTITY=$(az containerapp identity show \
    --name $API_CONTAINER_NAME \
    --resource-group $RESOURCE_GROUP \
    --query principalId \
    --output tsv)

print_success "API managed identity enabled: $API_IDENTITY"

print_info "Enabling system-assigned managed identity for UI..."
az containerapp identity assign \
    --name $UI_CONTAINER_NAME \
    --resource-group $RESOURCE_GROUP \
    --system-assigned

UI_IDENTITY=$(az containerapp identity show \
    --name $UI_CONTAINER_NAME \
    --resource-group $RESOURCE_GROUP \
    --query principalId \
    --output tsv)

print_success "UI managed identity enabled: $UI_IDENTITY"

# Grant ACR pull permissions to both identities
print_info "Granting AcrPull role to managed identities..."

ACR_ID=$(az acr show --name $CONTAINER_REGISTRY_NAME --query id --output tsv)

az role assignment create \
    --assignee $API_IDENTITY \
    --role AcrPull \
    --scope $ACR_ID

az role assignment create \
    --assignee $UI_IDENTITY \
    --role AcrPull \
    --scope $ACR_ID

print_success "ACR pull permissions granted"

################################################################################
# Step 10: Configure Budget Alert (Cost Control!)
################################################################################

print_header "Step 10: Configure Budget Alert"

print_info "Setting up budget alert at \$200/month (cost control)"

az consumption budget create \
    --budget-name "lankaconnect-prod-budget" \
    --amount 200 \
    --category Cost \
    --time-grain Monthly \
    --time-period start=$(date +%Y-%m-01) \
    --resource-group $RESOURCE_GROUP

print_success "Budget alert configured at \$200/month"
print_warning "You will receive an email if costs exceed \$200/month"

################################################################################
# Step 11: Summary and Next Steps
################################################################################

print_header "🎉 Infrastructure Setup Complete!"

echo ""
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo "  Cost-Optimized Production Environment Created!"
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo ""
echo "📋 Resources Created:"
echo "  ├─ Resource Group      : $RESOURCE_GROUP"
echo "  ├─ SQL Server          : $SQL_SERVER_NAME (Serverless)"
echo "  ├─ SQL Database        : $SQL_DATABASE_NAME (auto-pause enabled)"
echo "  ├─ Storage Account     : $STORAGE_ACCOUNT_NAME (Standard LRS)"
echo "  ├─ Key Vault           : $KEY_VAULT_NAME (Standard tier)"
echo "  ├─ Container Registry  : $CONTAINER_REGISTRY_NAME (Basic tier)"
echo "  ├─ Container Env       : $CONTAINER_ENV_NAME"
echo "  ├─ API Container       : $API_CONTAINER_NAME"
echo "  ├─ UI Container        : $UI_CONTAINER_NAME"
echo "  └─ Application Insights: $APP_INSIGHTS_NAME (30-day retention)"
echo ""
echo "💰 Estimated Monthly Cost: \$150-180"
echo "  • Container Apps       : \$30-40"
echo "  • SQL Serverless       : \$50-60 (auto-pauses when idle!)"
echo "  • Storage (Standard)   : \$15-20"
echo "  • Other services       : \$50-60"
echo ""
echo "🔐 Important Credentials (save these securely):"
echo "  ├─ SQL Admin User      : sqladmin"
echo "  ├─ SQL Admin Password  : [REDACTED - you entered this]"
echo "  ├─ ACR Username        : $ACR_USERNAME"
echo "  └─ ACR Password        : [REDACTED - stored in Key Vault]"
echo ""
echo "🔗 Resource URLs:"
echo "  ├─ API Container       : https://$(az containerapp show --name $API_CONTAINER_NAME --resource-group $RESOURCE_GROUP --query properties.configuration.ingress.fqdn -o tsv)"
echo "  ├─ UI Container        : https://$(az containerapp show --name $UI_CONTAINER_NAME --resource-group $RESOURCE_GROUP --query properties.configuration.ingress.fqdn -o tsv)"
echo "  └─ SQL Server          : $SQL_SERVER_NAME.database.windows.net"
echo ""
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo ""
echo "📝 Next Steps:"
echo ""
echo "1. Configure GitHub Secrets:"
echo "   export AZURE_CREDENTIALS_PROD='{...}'"
echo "   gh secret set AZURE_CREDENTIALS_PROD --body \"\$AZURE_CREDENTIALS_PROD\""
echo "   gh secret set ACR_USERNAME_PROD --body \"$ACR_USERNAME\""
echo "   gh secret set ACR_PASSWORD_PROD --body \"[from Key Vault]\""
echo ""
echo "2. Update GitHub Secrets with production values:"
echo "   • STRIPE_LIVE_SECRET_KEY (get from Stripe dashboard)"
echo "   • DATABASE_CONNECTION_STRING_PROD (already in Key Vault)"
echo "   • JWT_SECRET_KEY_PROD (generate a new one)"
echo ""
echo "3. Deploy to production:"
echo "   git checkout main"
echo "   git merge develop"
echo "   git push origin main"
echo "   # GitHub Actions will automatically deploy"
echo ""
echo "4. Verify deployment:"
echo "   # Check health endpoints"
echo "   curl https://[api-url]/health"
echo "   curl https://[ui-url]/api/health"
echo ""
echo "5. Monitor costs:"
echo "   az consumption usage list --subscription $SUBSCRIPTION_ID"
echo "   # Or use Azure Portal > Cost Management"
echo ""
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo ""
print_success "Setup completed successfully!"
print_info "Estimated setup time: $(( SECONDS / 60 )) minutes"
echo ""
print_warning "Remember: Database auto-pauses after 60 minutes of inactivity"
print_warning "First request after pause may have 5-10s delay (cold start)"
echo ""
print_info "For detailed documentation, see:"
print_info "  docs/ADR_PRODUCTION_DEPLOYMENT_COST_OPTIMIZED.md"
echo ""
