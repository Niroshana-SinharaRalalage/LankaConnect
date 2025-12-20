#!/bin/bash

# Script to apply email deployment configuration fixes
# Usage: ./apply-fixes.sh [--dry-run] [--environment staging|production|both]

set -e

DRY_RUN=false
ENVIRONMENT="staging"

# Parse arguments
while [[ $# -gt 0 ]]; do
  case $1 in
    --dry-run)
      DRY_RUN=true
      shift
      ;;
    --environment)
      ENVIRONMENT="$2"
      shift 2
      ;;
    *)
      echo "Unknown option: $1"
      echo "Usage: $0 [--dry-run] [--environment staging|production|both]"
      exit 1
      ;;
  esac
done

echo "========================================="
echo "Email Deployment Configuration Fix"
echo "Environment: $ENVIRONMENT"
echo "Dry Run: $DRY_RUN"
echo "========================================="
echo ""

# Get repository root
REPO_ROOT=$(git rev-parse --show-toplevel)
cd "$REPO_ROOT"

# Function to apply fix to a workflow file
apply_fix() {
  local WORKFLOW_FILE="$1"
  local ENV_NAME="$2"

  echo "Processing: $WORKFLOW_FILE"

  if [ ! -f "$WORKFLOW_FILE" ]; then
    echo "⚠️  File not found: $WORKFLOW_FILE"
    echo "   Skipping..."
    return
  fi

  # Create backup
  if [ "$DRY_RUN" = false ]; then
    cp "$WORKFLOW_FILE" "${WORKFLOW_FILE}.backup-$(date +%Y%m%d-%H%M%S)"
    echo "✅ Backup created"
  fi

  # Check if already fixed
  if grep -q "EmailSettings__Provider=Azure" "$WORKFLOW_FILE"; then
    echo "✅ Already using Azure provider - no changes needed"
    return
  fi

  # Show what would change
  echo ""
  echo "Changes to be made:"
  echo "-------------------"
  echo "❌ REMOVE:"
  echo "  EmailSettings__Provider=Smtp"
  echo "  EmailSettings__SmtpServer=secretref:smtp-host"
  echo "  EmailSettings__SmtpPort=secretref:smtp-port"
  echo "  EmailSettings__Username=secretref:smtp-username"
  echo "  EmailSettings__Password=secretref:smtp-password"
  echo "  EmailSettings__SenderEmail=secretref:email-from-address"
  echo "  EmailSettings__EnableSsl=true"
  echo ""
  echo "✅ ADD:"
  echo "  EmailSettings__Provider=Azure"
  echo "  EmailSettings__AzureConnectionString=secretref:azure-email-connection-string"
  echo "  EmailSettings__AzureSenderAddress=secretref:azure-email-sender-address"
  echo ""

  if [ "$DRY_RUN" = true ]; then
    echo "🔍 DRY RUN - No changes applied"
    return
  fi

  # Apply the fix using sed
  # This is a multi-line replacement, so we need to be careful

  # Create a temporary file with the new configuration
  TMP_FILE=$(mktemp)

  # Read the file and replace the email configuration section
  awk '
    /EmailSettings__Provider=Smtp/ {
      # Skip all SMTP-related lines
      while (getline && !/AzureStorage__ConnectionString/) {
        if (/EmailSettings__TemplateBasePath/) {
          template_path = $0
        }
      }
      # Print Azure configuration
      print "              EmailSettings__Provider=Azure \\"
      print "              EmailSettings__AzureConnectionString=secretref:azure-email-connection-string \\"
      print "              EmailSettings__AzureSenderAddress=secretref:azure-email-sender-address \\"
      print "              EmailSettings__SenderName=\"LankaConnect '$ENV_NAME'\" \\"
      if (template_path) print template_path " \\"
      # Print the AzureStorage line we just read
      print
      next
    }
    { print }
  ' ENV_NAME="$ENV_NAME" "$WORKFLOW_FILE" > "$TMP_FILE"

  # Replace original file
  mv "$TMP_FILE" "$WORKFLOW_FILE"
  echo "✅ Changes applied"
}

# Apply fixes based on environment
case $ENVIRONMENT in
  staging)
    apply_fix ".github/workflows/deploy-staging.yml" "Staging"
    ;;
  production)
    apply_fix ".github/workflows/deploy-production.yml" "Production"
    ;;
  both)
    apply_fix ".github/workflows/deploy-staging.yml" "Staging"
    echo ""
    apply_fix ".github/workflows/deploy-production.yml" "Production"
    ;;
  *)
    echo "❌ Invalid environment: $ENVIRONMENT"
    echo "   Must be: staging, production, or both"
    exit 1
    ;;
esac

echo ""
echo "========================================="
echo "Summary"
echo "========================================="

if [ "$DRY_RUN" = true ]; then
  echo "🔍 Dry run completed - no changes made"
  echo ""
  echo "To apply changes, run:"
  echo "  $0 --environment $ENVIRONMENT"
else
  echo "✅ Fix applied successfully!"
  echo ""
  echo "Next steps:"
  echo "1. Review the changes:"
  echo "   git diff .github/workflows/"
  echo ""
  echo "2. Test locally if possible"
  echo ""
  echo "3. Commit the changes:"
  echo "   git add .github/workflows/"
  echo "   git commit -m 'fix: Use Azure Communication Services for email'"
  echo ""
  echo "4. Push to trigger deployment:"
  echo "   git push origin develop  # for staging"
  echo "   git push origin main     # for production"
  echo ""
  echo "5. Monitor deployment:"
  echo "   gh run watch"
  echo ""
  echo "6. Verify email functionality:"
  echo "   - Test user registration"
  echo "   - Test password reset"
  echo "   - Check container app logs"
  echo ""
  echo "Backups created (in case rollback needed):"
  ls -1 .github/workflows/*.backup-* 2>/dev/null || echo "  (none created yet)"
fi
