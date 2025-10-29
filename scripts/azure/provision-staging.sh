#!/bin/bash

################################################################################
# LankaConnect - Azure Staging Environment Provisioning Script
#
# This script provisions all Azure resources required for the staging environment.
#
# Prerequisites:
# - Azure CLI installed (az --version)
# - Logged in to Azure (az login)
# - Subscription set (az account set --subscription "YOUR_SUBSCRIPTION_ID")
#
# Usage:
#   chmod +x scripts/azure/provision-staging.sh
#   ./scripts/azure/provision-staging.sh
#
################################################################################

set -euo pipefail  # Exit on error, undefined variable, or pipe failure

# Color codes for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Logging functions
log_info() {
    echo -e "${BLUE}[INFO]${NC} $1"
}

log_success() {
    echo -e "${GREEN}[SUCCESS]${NC} $1"
}

log_warning() {
    echo -e "${YELLOW}[WARNING]${NC} $1"
}

log_error() {
    echo -e "${RED}[ERROR]${NC} $1"
}

# Configuration Variables
LOCATION="eastus"
STAGING_RG="lankaconnect-staging"
ACR_NAME="lankaconnectstaging"  # Must be globally unique, lowercase, no hyphens
POSTGRES_SERVER="lankaconnect-staging-db"
POSTGRES_ADMIN="adminuser"
POSTGRES_PASSWORD=""  # Will be prompted securely
KEY_VAULT_NAME="lankaconnect-staging-kv"  # Must be globally unique
CONTAINERAPPS_ENV="lankaconnect-staging"
CONTAINER_APP_NAME="lankaconnect-api-staging"
LOG_ANALYTICS_WORKSPACE="lankaconnect-staging-logs"

################################################################################
# Step 0: Pre-flight Checks
################################################################################

log_info "Starting LankaConnect Staging Environment Provisioning..."
log_info "Target Location: $LOCATION"
log_info "Resource Group: $STAGING_RG"
echo ""

# Check if Azure CLI is installed
if ! command -v az &> /dev/null; then
    log_error "Azure CLI is not installed. Please install it first:"
    log_error "https://learn.microsoft.com/cli/azure/install-azure-cli"
    exit 1
fi

# Check if logged in to Azure
if ! az account show &> /dev/null; then
    log_error "Not logged in to Azure. Please run: az login"
    exit 1
fi

# Display current subscription
SUBSCRIPTION_NAME=$(az account show --query name -o tsv)
SUBSCRIPTION_ID=$(az account show --query id -o tsv)
log_info "Current Subscription: $SUBSCRIPTION_NAME ($SUBSCRIPTION_ID)"
echo ""

# Prompt for confirmation
read -p "Do you want to continue with this subscription? (yes/no): " CONFIRM
if [ "$CONFIRM" != "yes" ]; then
    log_warning "Provisioning cancelled by user."
    exit 0
fi

# Prompt for PostgreSQL password
echo ""
log_info "Please enter a strong password for PostgreSQL admin user (min 8 chars, uppercase, lowercase, number, special char):"
read -s POSTGRES_PASSWORD
echo ""
read -s -p "Confirm password: " POSTGRES_PASSWORD_CONFIRM
echo ""

if [ "$POSTGRES_PASSWORD" != "$POSTGRES_PASSWORD_CONFIRM" ]; then
    log_error "Passwords do not match!"
    exit 1
fi

if [ ${#POSTGRES_PASSWORD} -lt 8 ]; then
    log_error "Password must be at least 8 characters long!"
    exit 1
fi

log_success "Password set successfully."
echo ""

################################################################################
# Step 1: Create Resource Group
################################################################################

log_info "Step 1: Creating Resource Group..."

if az group exists --name "$STAGING_RG" | grep -q "true"; then
    log_warning "Resource group '$STAGING_RG' already exists. Skipping creation."
else
    az group create \
        --name "$STAGING_RG" \
        --location "$LOCATION" \
        --tags Environment=Staging Project=LankaConnect Owner=DevTeam \
        --output none

    log_success "Resource group '$STAGING_RG' created successfully."
fi

echo ""

################################################################################
# Step 2: Create Container Registry
################################################################################

log_info "Step 2: Creating Azure Container Registry..."

if az acr show --name "$ACR_NAME" --resource-group "$STAGING_RG" &> /dev/null; then
    log_warning "Container Registry '$ACR_NAME' already exists. Skipping creation."
else
    az acr create \
        --name "$ACR_NAME" \
        --resource-group "$STAGING_RG" \
        --sku Basic \
        --admin-enabled true \
        --location "$LOCATION" \
        --output none

    log_success "Container Registry '$ACR_NAME' created successfully."
fi

# Get ACR credentials
log_info "Retrieving ACR credentials..."
ACR_USERNAME=$(az acr credential show --name "$ACR_NAME" --resource-group "$STAGING_RG" --query username -o tsv)
ACR_PASSWORD=$(az acr credential show --name "$ACR_NAME" --resource-group "$STAGING_RG" --query "passwords[0].value" -o tsv)

log_success "ACR Credentials:"
echo "  Username: $ACR_USERNAME"
echo "  Password: $ACR_PASSWORD"
echo "  Save these for GitHub Secrets!"
echo ""

################################################################################
# Step 3: Create PostgreSQL Flexible Server
################################################################################

log_info "Step 3: Creating PostgreSQL Flexible Server..."

if az postgres flexible-server show --name "$POSTGRES_SERVER" --resource-group "$STAGING_RG" &> /dev/null; then
    log_warning "PostgreSQL server '$POSTGRES_SERVER' already exists. Skipping creation."
else
    log_info "This may take 5-10 minutes..."

    az postgres flexible-server create \
        --name "$POSTGRES_SERVER" \
        --resource-group "$STAGING_RG" \
        --location "$LOCATION" \
        --admin-user "$POSTGRES_ADMIN" \
        --admin-password "$POSTGRES_PASSWORD" \
        --sku-name Standard_B1ms \
        --tier Burstable \
        --storage-size 32 \
        --version 15 \
        --backup-retention 7 \
        --high-availability Disabled \
        --public-access 0.0.0.0 \
        --tags Environment=Staging \
        --output none

    log_success "PostgreSQL server '$POSTGRES_SERVER' created successfully."
fi

# Create database
log_info "Creating database 'LankaConnectDB'..."
if az postgres flexible-server db show \
    --resource-group "$STAGING_RG" \
    --server-name "$POSTGRES_SERVER" \
    --database-name LankaConnectDB &> /dev/null; then
    log_warning "Database 'LankaConnectDB' already exists. Skipping creation."
else
    az postgres flexible-server db create \
        --resource-group "$STAGING_RG" \
        --server-name "$POSTGRES_SERVER" \
        --database-name LankaConnectDB \
        --output none

    log_success "Database 'LankaConnectDB' created successfully."
fi

# Enable PgBouncer connection pooling
log_info "Enabling PgBouncer connection pooling..."
az postgres flexible-server parameter set \
    --resource-group "$STAGING_RG" \
    --server-name "$POSTGRES_SERVER" \
    --name pgbouncer.enabled \
    --value on \
    --output none

log_success "PgBouncer enabled."

# Configure firewall (allow Azure services)
log_info "Configuring firewall rules..."
if az postgres flexible-server firewall-rule show \
    --resource-group "$STAGING_RG" \
    --name "$POSTGRES_SERVER" \
    --rule-name AllowAzureServices &> /dev/null; then
    log_warning "Firewall rule 'AllowAzureServices' already exists. Skipping."
else
    az postgres flexible-server firewall-rule create \
        --resource-group "$STAGING_RG" \
        --name "$POSTGRES_SERVER" \
        --rule-name AllowAzureServices \
        --start-ip-address 0.0.0.0 \
        --end-ip-address 0.0.0.0 \
        --output none

    log_success "Firewall rule 'AllowAzureServices' created."
fi

# Build connection string
POSTGRES_HOST="${POSTGRES_SERVER}.postgres.database.azure.com"
POSTGRES_CONNECTION_STRING="Host=${POSTGRES_HOST};Database=LankaConnectDB;Username=${POSTGRES_ADMIN};Password=${POSTGRES_PASSWORD};SslMode=Require;Pooling=true;MinPoolSize=2;MaxPoolSize=20"

log_success "PostgreSQL Connection String:"
echo "  $POSTGRES_CONNECTION_STRING"
echo ""

################################################################################
# Step 4: Create Key Vault
################################################################################

log_info "Step 4: Creating Azure Key Vault..."

if az keyvault show --name "$KEY_VAULT_NAME" --resource-group "$STAGING_RG" &> /dev/null; then
    log_warning "Key Vault '$KEY_VAULT_NAME' already exists. Skipping creation."
else
    az keyvault create \
        --name "$KEY_VAULT_NAME" \
        --resource-group "$STAGING_RG" \
        --location "$LOCATION" \
        --sku standard \
        --enable-soft-delete true \
        --soft-delete-retention-days 90 \
        --enable-rbac-authorization false \
        --output none

    log_success "Key Vault '$KEY_VAULT_NAME' created successfully."
fi

echo ""

################################################################################
# Step 5: Populate Key Vault Secrets
################################################################################

log_info "Step 5: Populating Key Vault with secrets..."

# Generate JWT secret (256-bit)
JWT_SECRET=$(openssl rand -base64 32)

# Set secrets
log_info "Setting DATABASE-CONNECTION-STRING..."
az keyvault secret set \
    --vault-name "$KEY_VAULT_NAME" \
    --name DATABASE-CONNECTION-STRING \
    --value "$POSTGRES_CONNECTION_STRING" \
    --output none

log_info "Setting JWT-SECRET-KEY..."
az keyvault secret set \
    --vault-name "$KEY_VAULT_NAME" \
    --name JWT-SECRET-KEY \
    --value "$JWT_SECRET" \
    --output none

log_info "Setting JWT-ISSUER..."
az keyvault secret set \
    --vault-name "$KEY_VAULT_NAME" \
    --name JWT-ISSUER \
    --value "https://lankaconnect-api-staging.azurewebsites.net" \
    --output none

log_info "Setting JWT-AUDIENCE..."
az keyvault secret set \
    --vault-name "$KEY_VAULT_NAME" \
    --name JWT-AUDIENCE \
    --value "https://lankaconnect-staging.azurewebsites.net" \
    --output none

log_info "Setting ENTRA-ENABLED..."
az keyvault secret set \
    --vault-name "$KEY_VAULT_NAME" \
    --name ENTRA-ENABLED \
    --value "true" \
    --output none

log_info "Setting ENTRA-TENANT-ID..."
read -p "Enter your Entra Tenant ID: " ENTRA_TENANT_ID
az keyvault secret set \
    --vault-name "$KEY_VAULT_NAME" \
    --name ENTRA-TENANT-ID \
    --value "$ENTRA_TENANT_ID" \
    --output none

log_info "Setting ENTRA-CLIENT-ID..."
read -p "Enter your Entra Client ID: " ENTRA_CLIENT_ID
az keyvault secret set \
    --vault-name "$KEY_VAULT_NAME" \
    --name ENTRA-CLIENT-ID \
    --value "$ENTRA_CLIENT_ID" \
    --output none

log_info "Setting ENTRA-AUDIENCE..."
az keyvault secret set \
    --vault-name "$KEY_VAULT_NAME" \
    --name ENTRA-AUDIENCE \
    --value "api://$ENTRA_CLIENT_ID" \
    --output none

log_info "Setting SMTP-HOST..."
az keyvault secret set \
    --vault-name "$KEY_VAULT_NAME" \
    --name SMTP-HOST \
    --value "smtp.sendgrid.net" \
    --output none

log_info "Setting SMTP-PORT..."
az keyvault secret set \
    --vault-name "$KEY_VAULT_NAME" \
    --name SMTP-PORT \
    --value "587" \
    --output none

log_info "Setting SMTP-USERNAME..."
az keyvault secret set \
    --vault-name "$KEY_VAULT_NAME" \
    --name SMTP-USERNAME \
    --value "apikey" \
    --output none

log_info "Setting SMTP-PASSWORD..."
read -s -p "Enter your SendGrid API Key (or press Enter to skip): " SENDGRID_API_KEY
echo ""
if [ -n "$SENDGRID_API_KEY" ]; then
    az keyvault secret set \
        --vault-name "$KEY_VAULT_NAME" \
        --name SMTP-PASSWORD \
        --value "$SENDGRID_API_KEY" \
        --output none
else
    az keyvault secret set \
        --vault-name "$KEY_VAULT_NAME" \
        --name SMTP-PASSWORD \
        --value "PLACEHOLDER" \
        --output none
    log_warning "SMTP-PASSWORD set to PLACEHOLDER. Update later."
fi

log_info "Setting EMAIL-FROM-ADDRESS..."
az keyvault secret set \
    --vault-name "$KEY_VAULT_NAME" \
    --name EMAIL-FROM-ADDRESS \
    --value "noreply-staging@lankaconnect.com" \
    --output none

log_info "Setting AZURE-STORAGE-CONNECTION-STRING..."
az keyvault secret set \
    --vault-name "$KEY_VAULT_NAME" \
    --name AZURE-STORAGE-CONNECTION-STRING \
    --value "UseDevelopmentStorage=true" \
    --output none

log_success "All secrets populated successfully."
echo ""

################################################################################
# Step 6: Create Container Apps Environment
################################################################################

log_info "Step 6: Creating Container Apps Environment..."

# Create Log Analytics Workspace
if az monitor log-analytics workspace show \
    --resource-group "$STAGING_RG" \
    --workspace-name "$LOG_ANALYTICS_WORKSPACE" &> /dev/null; then
    log_warning "Log Analytics workspace already exists. Skipping."
else
    az monitor log-analytics workspace create \
        --resource-group "$STAGING_RG" \
        --workspace-name "$LOG_ANALYTICS_WORKSPACE" \
        --location "$LOCATION" \
        --output none

    log_success "Log Analytics workspace '$LOG_ANALYTICS_WORKSPACE' created."
fi

# Get workspace credentials
LOG_ANALYTICS_WORKSPACE_ID=$(az monitor log-analytics workspace show \
    --resource-group "$STAGING_RG" \
    --workspace-name "$LOG_ANALYTICS_WORKSPACE" \
    --query customerId -o tsv)

LOG_ANALYTICS_WORKSPACE_KEY=$(az monitor log-analytics workspace get-shared-keys \
    --resource-group "$STAGING_RG" \
    --workspace-name "$LOG_ANALYTICS_WORKSPACE" \
    --query primarySharedKey -o tsv)

# Create Container Apps environment
if az containerapp env show \
    --name "$CONTAINERAPPS_ENV" \
    --resource-group "$STAGING_RG" &> /dev/null; then
    log_warning "Container Apps environment already exists. Skipping."
else
    log_info "Creating Container Apps environment (this may take 5 minutes)..."

    az containerapp env create \
        --name "$CONTAINERAPPS_ENV" \
        --resource-group "$STAGING_RG" \
        --location "$LOCATION" \
        --logs-workspace-id "$LOG_ANALYTICS_WORKSPACE_ID" \
        --logs-workspace-key "$LOG_ANALYTICS_WORKSPACE_KEY" \
        --output none

    log_success "Container Apps environment '$CONTAINERAPPS_ENV' created."
fi

echo ""

################################################################################
# Step 7: Create Container App with Managed Identity
################################################################################

log_info "Step 7: Creating Container App..."

if az containerapp show \
    --name "$CONTAINER_APP_NAME" \
    --resource-group "$STAGING_RG" &> /dev/null; then
    log_warning "Container App '$CONTAINER_APP_NAME' already exists. Skipping."
else
    log_info "Creating Container App with temporary image..."

    az containerapp create \
        --name "$CONTAINER_APP_NAME" \
        --resource-group "$STAGING_RG" \
        --environment "$CONTAINERAPPS_ENV" \
        --image mcr.microsoft.com/azuredocs/containerapps-helloworld:latest \
        --target-port 80 \
        --ingress external \
        --min-replicas 1 \
        --max-replicas 3 \
        --cpu 0.25 \
        --memory 0.5Gi \
        --system-assigned \
        --output none

    log_success "Container App '$CONTAINER_APP_NAME' created."
fi

# Get managed identity principal ID
MANAGED_IDENTITY_ID=$(az containerapp show \
    --name "$CONTAINER_APP_NAME" \
    --resource-group "$STAGING_RG" \
    --query identity.principalId -o tsv)

log_info "Managed Identity Principal ID: $MANAGED_IDENTITY_ID"

# Grant Container App access to Key Vault
log_info "Granting Container App access to Key Vault..."
az keyvault set-policy \
    --name "$KEY_VAULT_NAME" \
    --object-id "$MANAGED_IDENTITY_ID" \
    --secret-permissions get list \
    --output none

log_success "Key Vault access granted."

# Get Container App URL
CONTAINER_APP_URL=$(az containerapp show \
    --name "$CONTAINER_APP_NAME" \
    --resource-group "$STAGING_RG" \
    --query properties.configuration.ingress.fqdn -o tsv)

log_success "Container App URL: https://$CONTAINER_APP_URL"
echo ""

################################################################################
# Step 8: Create Service Principal for GitHub Actions
################################################################################

log_info "Step 8: Creating Service Principal for GitHub Actions..."

SP_NAME="lankaconnect-github-actions-staging"

# Check if service principal already exists
if az ad sp list --display-name "$SP_NAME" --query "[0].appId" -o tsv &> /dev/null; then
    log_warning "Service Principal '$SP_NAME' already exists. Skipping creation."
    log_warning "If you need new credentials, delete the old SP first: az ad sp delete --id \$(az ad sp list --display-name '$SP_NAME' --query '[0].appId' -o tsv)"
else
    log_info "Creating service principal with Contributor role on resource group..."

    SP_OUTPUT=$(az ad sp create-for-rbac \
        --name "$SP_NAME" \
        --role Contributor \
        --scopes /subscriptions/$SUBSCRIPTION_ID/resourceGroups/$STAGING_RG \
        --sdk-auth)

    log_success "Service Principal created successfully."
    echo ""
    log_success "GitHub Secret: AZURE_CREDENTIALS_STAGING"
    log_info "Copy the entire JSON below and paste it as a GitHub secret:"
    echo "────────────────────────────────────────────────────────────────"
    echo "$SP_OUTPUT"
    echo "────────────────────────────────────────────────────────────────"
fi

echo ""

################################################################################
# Final Summary
################################################################################

log_success "╔════════════════════════════════════════════════════════════════╗"
log_success "║  LankaConnect Staging Environment Provisioned Successfully!   ║"
log_success "╚════════════════════════════════════════════════════════════════╝"
echo ""

log_info "Resource Summary:"
echo "  Resource Group:        $STAGING_RG"
echo "  Location:              $LOCATION"
echo "  Container Registry:    $ACR_NAME"
echo "  PostgreSQL Server:     $POSTGRES_SERVER"
echo "  Key Vault:             $KEY_VAULT_NAME"
echo "  Container Apps Env:    $CONTAINERAPPS_ENV"
echo "  Container App:         $CONTAINER_APP_NAME"
echo "  Container App URL:     https://$CONTAINER_APP_URL"
echo ""

log_info "Next Steps:"
echo "  1. Save ACR credentials for GitHub Secrets:"
echo "     - ACR_USERNAME_STAGING: $ACR_USERNAME"
echo "     - ACR_PASSWORD_STAGING: $ACR_PASSWORD"
echo ""
echo "  2. Save Service Principal JSON for GitHub Secret: AZURE_CREDENTIALS_STAGING"
echo ""
echo "  3. Apply database migration:"
echo "     psql \"$POSTGRES_CONNECTION_STRING\" -f docs/deployment/migrations/20251028_AddEntraExternalIdSupport.sql"
echo ""
echo "  4. Configure GitHub Actions workflow:"
echo "     - Update .github/workflows/deploy-staging.yml"
echo "     - Add GitHub Secrets"
echo ""
echo "  5. Deploy to staging:"
echo "     git push origin develop"
echo ""

log_warning "Important: Save all credentials in a secure location (e.g., 1Password, Azure Key Vault)."
log_warning "Do NOT commit credentials to git repository."
echo ""

log_success "Provisioning script completed successfully!"
