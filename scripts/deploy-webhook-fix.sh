#!/bin/bash
# Script to deploy webhook authentication fix to Azure Container App
# This script builds and deploys the critical fix for Stripe webhook processing

set -e  # Exit on error

echo "=========================================="
echo "Deploying Webhook Authentication Fix"
echo "=========================================="

# Configuration
RESOURCE_GROUP="LankaConnect-ResourceGroup"
CONTAINER_APP_NAME="lankaconnect-api-staging"
ACR_NAME="lankaconnect"
IMAGE_NAME="lankaconnect-api"
TAG="webhook-fix-$(date +%Y%m%d-%H%M%S)"

echo ""
echo "Configuration:"
echo "  Resource Group: $RESOURCE_GROUP"
echo "  Container App: $CONTAINER_APP_NAME"
echo "  ACR: $ACR_NAME"
echo "  Image: $IMAGE_NAME:$TAG"
echo ""

# Step 1: Build the Docker image
echo "Step 1: Building Docker image..."
cd "$(dirname "$0")/.."
docker build -f src/LankaConnect.API/Dockerfile -t "$ACR_NAME.azurecr.io/$IMAGE_NAME:$TAG" .
echo "✓ Docker image built successfully"
echo ""

# Step 2: Login to Azure Container Registry
echo "Step 2: Logging into Azure Container Registry..."
az acr login --name $ACR_NAME
echo "✓ Logged into ACR successfully"
echo ""

# Step 3: Push image to ACR
echo "Step 3: Pushing image to Azure Container Registry..."
docker push "$ACR_NAME.azurecr.io/$IMAGE_NAME:$TAG"
echo "✓ Image pushed successfully"
echo ""

# Step 4: Update Container App with new image
echo "Step 4: Updating Container App with new image..."
az containerapp update \
  --name $CONTAINER_APP_NAME \
  --resource-group $RESOURCE_GROUP \
  --image "$ACR_NAME.azurecr.io/$IMAGE_NAME:$TAG" \
  --set-env-vars "ASPNETCORE_ENVIRONMENT=Staging"
echo "✓ Container App updated successfully"
echo ""

# Step 5: Wait for deployment to stabilize
echo "Step 5: Waiting for deployment to stabilize (30 seconds)..."
sleep 30
echo "✓ Deployment stabilized"
echo ""

# Step 6: Verify health endpoint
echo "Step 6: Verifying application health..."
HEALTH_URL="https://$CONTAINER_APP_NAME.politebay-79d6e8a2.eastus2.azurecontainerapps.io/health"
HTTP_CODE=$(curl -s -o /dev/null -w "%{http_code}" "$HEALTH_URL")

if [ "$HTTP_CODE" = "200" ]; then
    echo "✓ Health check passed (HTTP $HTTP_CODE)"
else
    echo "⚠ Warning: Health check returned HTTP $HTTP_CODE (expected 200)"
fi
echo ""

# Step 7: Verify webhook endpoint is accessible
echo "Step 7: Verifying webhook endpoint..."
WEBHOOK_URL="https://$CONTAINER_APP_NAME.politebay-79d6e8a2.eastus2.azurecontainerapps.io/api/payments/webhook"
HTTP_CODE=$(curl -s -o /dev/null -w "%{http_code}" -X POST "$WEBHOOK_URL")

if [ "$HTTP_CODE" = "400" ]; then
    echo "✓ Webhook endpoint accessible (HTTP $HTTP_CODE - expected for empty body)"
else
    echo "⚠ Warning: Webhook endpoint returned HTTP $HTTP_CODE (expected 400 for empty body)"
fi
echo ""

echo "=========================================="
echo "Deployment Complete!"
echo "=========================================="
echo ""
echo "Next Steps:"
echo "1. Monitor Azure logs for webhook activity:"
echo "   az containerapp logs show --name $CONTAINER_APP_NAME --resource-group $RESOURCE_GROUP --follow"
echo ""
echo "2. Test webhook with Stripe CLI:"
echo "   stripe trigger checkout.session.completed"
echo ""
echo "3. Look for these log entries:"
echo "   - \"Webhook endpoint reached\""
echo "   - \"Processing webhook event\""
echo "   - \"Successfully completed payment\""
echo ""
echo "Image deployed: $ACR_NAME.azurecr.io/$IMAGE_NAME:$TAG"
echo ""
