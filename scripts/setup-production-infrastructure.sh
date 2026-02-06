#!/bin/bash

################################################################################
# LankaConnect Production Infrastructure Setup Script
################################################################################
# This script automates the creation of production Azure resources for
# LankaConnect application following the blue-green deployment strategy.
#
# Prerequisites:
# - Azure CLI installed and authenticated (az login)
# - Appropriate Azure subscription permissions
# - Production secrets prepared (see checklist in ADR)
#
# Usage:
#   ./setup-production-infrastructure.sh [--dry-run]
#
# Options:
#   --dry-run    Show what would be created without creating resources
################################################################################

set -e  # Exit on error
set -u  # Exit on undefined variable

# Color codes for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Script configuration
DRY_RUN=false
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
LOG_FILE="${SCRIPT_DIR}/production-setup-$(date +%Y%m%d-%H%M%S).log"

# Production configuration
SUBSCRIPTION_ID="${AZURE_PROD_SUBSCRIPTION_ID:-}"
RESOURCE_GROUP="lankaconnect-prod"
LOCATION="eastus"
ENVIRONMENT="Production"

# Resource names
CONTAINER_REGISTRY="lankaconnectprod"
KEY_VAULT_NAME="lc-prod-kv"  # 15-char limit
SQL_SERVER_NAME="lankaconnect-sqldb-prod"
SQL_DATABASE_NAME="LankaConnectDB"
STORAGE_ACCOUNT="lankaconnectprodeus"  # 24-char limit, no hyphens
APP_INSIGHTS_NAME="lankaconnect-prod-insights"
LOG_ANALYTICS_NAME="lankaconnect-prod-logs"
CONTAINER_ENV_NAME="lankaconnect-prod-env"
API_CONTAINER_APP="lankaconnect-api-prod"
UI_CONTAINER_APP="lankaconnect-ui-prod"

# Resource SKUs
SQL_SKU="P2"  # 250 DTU, suitable for production
STORAGE_SKU="Standard_GRS"  # Geo-redundant storage
KEY_VAULT_SKU="premium"  # HSM-backed keys
ACR_SKU="Standard"  # Can upgrade to Premium for geo-replication later

################################################################################
# Helper Functions
################################################################################

log() {
    echo -e "${GREEN}[$(date +'%Y-%m-%d %H:%M:%S')]${NC} $*" | tee -a "$LOG_FILE"
}

log_error() {
    echo -e "${RED}[ERROR]${NC} $*" | tee -a "$LOG_FILE"
}

log_warning() {
    echo -e "${YELLOW}[WARNING]${NC} $*" | tee -a "$LOG_FILE"
}

log_info() {
    echo -e "${BLUE}[INFO]${NC} $*" | tee -a "$LOG_FILE"
}

check_prerequisites() {
    log "Checking prerequisites..."

    # Check Azure CLI
    if ! command -v az &> /dev/null; then
        log_error "Azure CLI is not installed. Install from: https://aka.ms/azure-cli"
        exit 1
    fi

    # Check Azure login
    if ! az account show &> /dev/null; then
        log_error "Not logged into Azure. Run: az login"
        exit 1
    fi

    # Check subscription
    if [ -z "$SUBSCRIPTION_ID" ]; then
        log_warning "AZURE_PROD_SUBSCRIPTION_ID not set, using default subscription"
        SUBSCRIPTION_ID=$(az account show --query id -o tsv)
    fi

    log "Using subscription: $SUBSCRIPTION_ID"
    az account set --subscription "$SUBSCRIPTION_ID"

    log "✅ Prerequisites check passed"
}

create_resource_group() {
    log "Creating resource group: $RESOURCE_GROUP in $LOCATION..."

    if [ "$DRY_RUN" = true ]; then
        log_info "[DRY-RUN] Would create resource group: $RESOURCE_GROUP"
        return
    fi

    if az group show --name "$RESOURCE_GROUP" &> /dev/null; then
        log_warning "Resource group already exists"
    else
        az group create \
            --name "$RESOURCE_GROUP" \
            --location "$LOCATION" \
            --tags Environment="$ENVIRONMENT" Project=LankaConnect ManagedBy=Script \
            --output none
        log "✅ Resource group created"
    fi
}

create_container_registry() {
    log "Creating Azure Container Registry: $CONTAINER_REGISTRY..."

    if [ "$DRY_RUN" = true ]; then
        log_info "[DRY-RUN] Would create ACR: $CONTAINER_REGISTRY"
        return
    fi

    if az acr show --name "$CONTAINER_REGISTRY" --resource-group "$RESOURCE_GROUP" &> /dev/null; then
        log_warning "Container Registry already exists"
    else
        az acr create \
            --resource-group "$RESOURCE_GROUP" \
            --name "$CONTAINER_REGISTRY" \
            --sku "$ACR_SKU" \
            --admin-enabled true \
            --location "$LOCATION" \
            --output none
        log "✅ Container Registry created"
    fi

    # Get credentials
    ACR_USERNAME=$(az acr credential show --name "$CONTAINER_REGISTRY" --query username -o tsv)
    ACR_PASSWORD=$(az acr credential show --name "$CONTAINER_REGISTRY" --query passwords[0].value -o tsv)

    log_info "ACR Username: $ACR_USERNAME"
    log_info "ACR Password: [REDACTED - check Azure Portal]"
    echo "ACR_USERNAME_PROD=$ACR_USERNAME" >> "$SCRIPT_DIR/production-secrets.env"
    echo "ACR_PASSWORD_PROD=$ACR_PASSWORD" >> "$SCRIPT_DIR/production-secrets.env"
}

create_key_vault() {
    log "Creating Azure Key Vault: $KEY_VAULT_NAME..."

    if [ "$DRY_RUN" = true ]; then
        log_info "[DRY-RUN] Would create Key Vault: $KEY_VAULT_NAME"
        return
    fi

    if az keyvault show --name "$KEY_VAULT_NAME" &> /dev/null; then
        log_warning "Key Vault already exists"
    else
        az keyvault create \
            --resource-group "$RESOURCE_GROUP" \
            --name "$KEY_VAULT_NAME" \
            --location "$LOCATION" \
            --sku "$KEY_VAULT_SKU" \
            --enable-rbac-authorization false \
            --enabled-for-deployment true \
            --enabled-for-template-deployment true \
            --output none
        log "✅ Key Vault created"
    fi

    # Grant current user access to manage secrets
    CURRENT_USER=$(az account show --query user.name -o tsv)
    az keyvault set-policy \
        --name "$KEY_VAULT_NAME" \
        --upn "$CURRENT_USER" \
        --secret-permissions get list set delete \
        --output none

    log_info "Key Vault created. Secrets must be added manually (see checklist)"
}

create_sql_database() {
    log "Creating Azure SQL Database: $SQL_DATABASE_NAME..."

    if [ "$DRY_RUN" = true ]; then
        log_info "[DRY-RUN] Would create SQL Database: $SQL_DATABASE_NAME"
        return
    fi

    # Prompt for SQL admin password
    read -sp "Enter SQL admin password: " SQL_ADMIN_PASSWORD
    echo
    read -sp "Confirm SQL admin password: " SQL_ADMIN_PASSWORD_CONFIRM
    echo

    if [ "$SQL_ADMIN_PASSWORD" != "$SQL_ADMIN_PASSWORD_CONFIRM" ]; then
        log_error "Passwords do not match"
        exit 1
    fi

    # Create SQL Server
    if az sql server show --name "$SQL_SERVER_NAME" --resource-group "$RESOURCE_GROUP" &> /dev/null; then
        log_warning "SQL Server already exists"
    else
        az sql server create \
            --resource-group "$RESOURCE_GROUP" \
            --name "$SQL_SERVER_NAME" \
            --location "$LOCATION" \
            --admin-user sqladmin \
            --admin-password "$SQL_ADMIN_PASSWORD" \
            --output none
        log "✅ SQL Server created"
    fi

    # Configure firewall (allow Azure services)
    az sql server firewall-rule create \
        --resource-group "$RESOURCE_GROUP" \
        --server "$SQL_SERVER_NAME" \
        --name AllowAzureServices \
        --start-ip-address 0.0.0.0 \
        --end-ip-address 0.0.0.0 \
        --output none

    # Create database
    if az sql db show --name "$SQL_DATABASE_NAME" --server "$SQL_SERVER_NAME" --resource-group "$RESOURCE_GROUP" &> /dev/null; then
        log_warning "SQL Database already exists"
    else
        az sql db create \
            --resource-group "$RESOURCE_GROUP" \
            --server "$SQL_SERVER_NAME" \
            --name "$SQL_DATABASE_NAME" \
            --service-objective "$SQL_SKU" \
            --backup-storage-redundancy Geo \
            --zone-redundant true \
            --output none
        log "✅ SQL Database created"
    fi

    # Generate connection string
    CONNECTION_STRING="Server=tcp:${SQL_SERVER_NAME}.database.windows.net,1433;Initial Catalog=${SQL_DATABASE_NAME};Persist Security Info=False;User ID=sqladmin;Password=${SQL_ADMIN_PASSWORD};MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"

    log_info "Storing connection string in Key Vault..."
    az keyvault secret set \
        --vault-name "$KEY_VAULT_NAME" \
        --name "database-connection-string" \
        --value "$CONNECTION_STRING" \
        --output none

    log "✅ Connection string stored in Key Vault"
}

create_storage_account() {
    log "Creating Azure Storage Account: $STORAGE_ACCOUNT..."

    if [ "$DRY_RUN" = true ]; then
        log_info "[DRY-RUN] Would create Storage Account: $STORAGE_ACCOUNT"
        return
    fi

    if az storage account show --name "$STORAGE_ACCOUNT" --resource-group "$RESOURCE_GROUP" &> /dev/null; then
        log_warning "Storage Account already exists"
    else
        az storage account create \
            --resource-group "$RESOURCE_GROUP" \
            --name "$STORAGE_ACCOUNT" \
            --location "$LOCATION" \
            --sku "$STORAGE_SKU" \
            --kind StorageV2 \
            --access-tier Hot \
            --https-only true \
            --min-tls-version TLS1_2 \
            --output none
        log "✅ Storage Account created"
    fi

    # Create blob containers
    STORAGE_KEY=$(az storage account keys list --resource-group "$RESOURCE_GROUP" --account-name "$STORAGE_ACCOUNT" --query '[0].value' -o tsv)

    az storage container create \
        --account-name "$STORAGE_ACCOUNT" \
        --account-key "$STORAGE_KEY" \
        --name "event-media" \
        --public-access off \
        --output none

    az storage container create \
        --account-name "$STORAGE_ACCOUNT" \
        --account-key "$STORAGE_KEY" \
        --name "business-images" \
        --public-access off \
        --output none

    # Store connection string in Key Vault
    STORAGE_CONNECTION_STRING=$(az storage account show-connection-string --resource-group "$RESOURCE_GROUP" --name "$STORAGE_ACCOUNT" --query connectionString -o tsv)

    az keyvault secret set \
        --vault-name "$KEY_VAULT_NAME" \
        --name "azure-storage-connection-string" \
        --value "$STORAGE_CONNECTION_STRING" \
        --output none

    log "✅ Storage Account and containers created"
}

create_monitoring() {
    log "Creating monitoring resources..."

    if [ "$DRY_RUN" = true ]; then
        log_info "[DRY-RUN] Would create monitoring resources"
        return
    fi

    # Create Log Analytics workspace
    if az monitor log-analytics workspace show --resource-group "$RESOURCE_GROUP" --workspace-name "$LOG_ANALYTICS_NAME" &> /dev/null; then
        log_warning "Log Analytics workspace already exists"
    else
        az monitor log-analytics workspace create \
            --resource-group "$RESOURCE_GROUP" \
            --workspace-name "$LOG_ANALYTICS_NAME" \
            --location "$LOCATION" \
            --retention-time 90 \
            --output none
        log "✅ Log Analytics workspace created"
    fi

    # Get workspace ID and key
    WORKSPACE_ID=$(az monitor log-analytics workspace show --resource-group "$RESOURCE_GROUP" --workspace-name "$LOG_ANALYTICS_NAME" --query customerId -o tsv)
    WORKSPACE_KEY=$(az monitor log-analytics workspace get-shared-keys --resource-group "$RESOURCE_GROUP" --workspace-name "$LOG_ANALYTICS_NAME" --query primarySharedKey -o tsv)

    # Create Application Insights
    if az monitor app-insights component show --resource-group "$RESOURCE_GROUP" --app "$APP_INSIGHTS_NAME" &> /dev/null; then
        log_warning "Application Insights already exists"
    else
        az monitor app-insights component create \
            --resource-group "$RESOURCE_GROUP" \
            --app "$APP_INSIGHTS_NAME" \
            --location "$LOCATION" \
            --application-type web \
            --workspace "$WORKSPACE_ID" \
            --output none
        log "✅ Application Insights created"
    fi

    echo "WORKSPACE_ID=$WORKSPACE_ID" >> "$SCRIPT_DIR/production-secrets.env"
    echo "WORKSPACE_KEY=$WORKSPACE_KEY" >> "$SCRIPT_DIR/production-secrets.env"
}

create_container_apps_environment() {
    log "Creating Container Apps environment..."

    if [ "$DRY_RUN" = true ]; then
        log_info "[DRY-RUN] Would create Container Apps environment"
        return
    fi

    # Get workspace credentials
    WORKSPACE_ID=$(az monitor log-analytics workspace show --resource-group "$RESOURCE_GROUP" --workspace-name "$LOG_ANALYTICS_NAME" --query customerId -o tsv)
    WORKSPACE_KEY=$(az monitor log-analytics workspace get-shared-keys --resource-group "$RESOURCE_GROUP" --workspace-name "$LOG_ANALYTICS_NAME" --query primarySharedKey -o tsv)

    if az containerapp env show --name "$CONTAINER_ENV_NAME" --resource-group "$RESOURCE_GROUP" &> /dev/null; then
        log_warning "Container Apps environment already exists"
    else
        az containerapp env create \
            --resource-group "$RESOURCE_GROUP" \
            --name "$CONTAINER_ENV_NAME" \
            --location "$LOCATION" \
            --logs-workspace-id "$WORKSPACE_ID" \
            --logs-workspace-key "$WORKSPACE_KEY" \
            --output none
        log "✅ Container Apps environment created"
    fi
}

create_container_apps() {
    log "Creating Container Apps (API and UI)..."

    if [ "$DRY_RUN" = true ]; then
        log_info "[DRY-RUN] Would create Container Apps"
        return
    fi

    # Get ACR credentials
    ACR_USERNAME=$(az acr credential show --name "$CONTAINER_REGISTRY" --query username -o tsv)
    ACR_PASSWORD=$(az acr credential show --name "$CONTAINER_REGISTRY" --query passwords[0].value -o tsv)

    # Create API Container App
    log "Creating API Container App: $API_CONTAINER_APP..."

    if az containerapp show --name "$API_CONTAINER_APP" --resource-group "$RESOURCE_GROUP" &> /dev/null; then
        log_warning "API Container App already exists"
    else
        # Note: Initial deployment uses placeholder image
        # Actual deployment happens via GitHub Actions
        az containerapp create \
            --resource-group "$RESOURCE_GROUP" \
            --name "$API_CONTAINER_APP" \
            --environment "$CONTAINER_ENV_NAME" \
            --image "mcr.microsoft.com/azuredocs/containerapps-helloworld:latest" \
            --target-port 80 \
            --ingress external \
            --min-replicas 2 \
            --max-replicas 10 \
            --cpu 2.0 \
            --memory 4.0Gi \
            --registry-server "${CONTAINER_REGISTRY}.azurecr.io" \
            --registry-username "$ACR_USERNAME" \
            --registry-password "$ACR_PASSWORD" \
            --env-vars "PLACEHOLDER=true" \
            --output none

        log "✅ API Container App created (placeholder image)"
        log_warning "Deploy actual API image via GitHub Actions workflow"
    fi

    # Create UI Container App
    log "Creating UI Container App: $UI_CONTAINER_APP..."

    if az containerapp show --name "$UI_CONTAINER_APP" --resource-group "$RESOURCE_GROUP" &> /dev/null; then
        log_warning "UI Container App already exists"
    else
        az containerapp create \
            --resource-group "$RESOURCE_GROUP" \
            --name "$UI_CONTAINER_APP" \
            --environment "$CONTAINER_ENV_NAME" \
            --image "mcr.microsoft.com/azuredocs/containerapps-helloworld:latest" \
            --target-port 80 \
            --ingress external \
            --min-replicas 2 \
            --max-replicas 10 \
            --cpu 1.0 \
            --memory 2.0Gi \
            --registry-server "${CONTAINER_REGISTRY}.azurecr.io" \
            --registry-username "$ACR_USERNAME" \
            --registry-password "$ACR_PASSWORD" \
            --env-vars "PLACEHOLDER=true" \
            --output none

        log "✅ UI Container App created (placeholder image)"
        log_warning "Deploy actual UI image via GitHub Actions workflow"
    fi
}

create_service_principal() {
    log "Creating Service Principal for GitHub Actions..."

    if [ "$DRY_RUN" = true ]; then
        log_info "[DRY-RUN] Would create Service Principal"
        return
    fi

    SP_NAME="lankaconnect-prod-github-sp"

    # Create service principal with Contributor role (scoped to resource group)
    SP_OUTPUT=$(az ad sp create-for-rbac \
        --name "$SP_NAME" \
        --role Contributor \
        --scopes "/subscriptions/$SUBSCRIPTION_ID/resourceGroups/$RESOURCE_GROUP" \
        --sdk-auth)

    log "✅ Service Principal created"
    log_warning "Add this JSON to GitHub Secrets as AZURE_CREDENTIALS_PROD:"
    echo "$SP_OUTPUT" | tee -a "$SCRIPT_DIR/github-service-principal.json"
}

generate_summary() {
    log ""
    log "════════════════════════════════════════════════════════════════"
    log "✅ Production Infrastructure Setup Complete!"
    log "════════════════════════════════════════════════════════════════"
    log ""
    log "Resource Group: $RESOURCE_GROUP"
    log "Location: $LOCATION"
    log ""
    log "Created Resources:"
    log "  - Container Registry: ${CONTAINER_REGISTRY}.azurecr.io"
    log "  - Key Vault: $KEY_VAULT_NAME"
    log "  - SQL Database: $SQL_DATABASE_NAME"
    log "  - Storage Account: $STORAGE_ACCOUNT"
    log "  - Application Insights: $APP_INSIGHTS_NAME"
    log "  - Container Apps Environment: $CONTAINER_ENV_NAME"
    log "  - API Container App: $API_CONTAINER_APP"
    log "  - UI Container App: $UI_CONTAINER_APP"
    log ""
    log "Next Steps:"
    log "  1. Configure Key Vault secrets (see checklist in ADR)"
    log "  2. Add GitHub Secrets:"
    log "     - AZURE_CREDENTIALS_PROD (from github-service-principal.json)"
    log "     - ACR_USERNAME_PROD (from production-secrets.env)"
    log "     - ACR_PASSWORD_PROD (from production-secrets.env)"
    log "  3. Create GitHub workflow: .github/workflows/deploy-production.yml"
    log "  4. Push to 'main' branch to trigger first deployment"
    log "  5. Configure custom domain and SSL certificate"
    log "  6. Configure monitoring alerts"
    log ""
    log "Configuration files created:"
    log "  - $SCRIPT_DIR/production-secrets.env"
    log "  - $SCRIPT_DIR/github-service-principal.json"
    log "  - $LOG_FILE"
    log ""
    log "⚠️  IMPORTANT: Keep these files secure and DO NOT commit to git!"
    log "════════════════════════════════════════════════════════════════"
}

################################################################################
# Main Execution
################################################################################

main() {
    # Parse arguments
    for arg in "$@"; do
        case $arg in
            --dry-run)
                DRY_RUN=true
                log_info "Running in DRY-RUN mode (no resources will be created)"
                ;;
            --help)
                echo "Usage: $0 [--dry-run]"
                echo ""
                echo "Options:"
                echo "  --dry-run    Show what would be created without creating resources"
                echo "  --help       Show this help message"
                exit 0
                ;;
            *)
                log_error "Unknown argument: $arg"
                echo "Use --help for usage information"
                exit 1
                ;;
        esac
    done

    log "Starting LankaConnect Production Infrastructure Setup"
    log "Log file: $LOG_FILE"

    # Check prerequisites
    check_prerequisites

    # Confirm production setup
    if [ "$DRY_RUN" = false ]; then
        echo ""
        log_warning "⚠️  WARNING: This will create PRODUCTION resources in Azure"
        log_warning "⚠️  This will incur costs (~\$1,165/month)"
        read -p "Are you sure you want to continue? (yes/no): " -r
        echo
        if [[ ! $REPLY =~ ^[Yy][Ee][Ss]$ ]]; then
            log "Setup cancelled by user"
            exit 0
        fi
    fi

    # Create resources in order
    create_resource_group
    create_container_registry
    create_key_vault
    create_sql_database
    create_storage_account
    create_monitoring
    create_container_apps_environment
    create_container_apps
    create_service_principal

    # Generate summary
    generate_summary
}

# Run main function
main "$@"
