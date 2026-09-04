#!/usr/bin/env bash
#
# Deploys the API and client to Azure Container Apps.
#
# Requires the Azure CLI (az, logged in via `az login` with a subscription
# selected) and jq. The containerapp CLI extension is installed
# automatically if missing. Run from the repository root.
#
# Usage:
#   ./deploy.sh
#
# Override defaults with env vars, e.g.:
#   RESOURCE_GROUP=my-rg LOCATION=westus2 ./deploy.sh

set -euo pipefail

APP_NAME="${APP_NAME:-filebrowser}"
RESOURCE_GROUP="${RESOURCE_GROUP:-rg-${APP_NAME}}"
LOCATION="${LOCATION:-eastus}"

command -v jq >/dev/null || { echo "jq is required. Install it and re-run."; exit 1; }

echo "==> Checking Azure CLI login"
az account show >/dev/null || { echo "Run 'az login' first."; exit 1; }

echo "==> Ensuring containerapp CLI extension is installed"
az extension add --name containerapp --upgrade --only-show-errors >/dev/null

echo "==> Creating resource group '$RESOURCE_GROUP' in '$LOCATION'"
az group create --name "$RESOURCE_GROUP" --location "$LOCATION" --only-show-errors >/dev/null

echo "==> Deploying infrastructure (placeholder images for the first pass)"
DEPLOY_OUTPUT=$(az deployment group create \
  --resource-group "$RESOURCE_GROUP" \
  --template-file infra/main.bicep \
  --parameters appName="$APP_NAME" \
  --only-show-errors \
  --output json)

ACR_NAME=$(echo "$DEPLOY_OUTPUT" | jq -r '.properties.outputs.containerRegistryName.value')
ACR_LOGIN_SERVER=$(echo "$DEPLOY_OUTPUT" | jq -r '.properties.outputs.containerRegistryLoginServer.value')
API_APP_NAME=$(echo "$DEPLOY_OUTPUT" | jq -r '.properties.outputs.apiAppName.value')
WEB_APP_NAME=$(echo "$DEPLOY_OUTPUT" | jq -r '.properties.outputs.webAppName.value')
API_URL=$(echo "$DEPLOY_OUTPUT" | jq -r '.properties.outputs.apiUrl.value')
WEB_URL=$(echo "$DEPLOY_OUTPUT" | jq -r '.properties.outputs.webUrl.value')

echo "==> Building and pushing the API image to $ACR_LOGIN_SERVER"
az acr build \
  --registry "$ACR_NAME" \
  --image "filebrowser-api:latest" \
  --file src/FileBrowser.Api/Dockerfile \
  --only-show-errors \
  .

echo "==> Building and pushing the web image to $ACR_LOGIN_SERVER"
az acr build \
  --registry "$ACR_NAME" \
  --image "filebrowser-web:latest" \
  --file Dockerfile \
  --only-show-errors \
  client

echo "==> Pointing the API container app at the real image"
az containerapp update \
  --name "$API_APP_NAME" \
  --resource-group "$RESOURCE_GROUP" \
  --image "$ACR_LOGIN_SERVER/filebrowser-api:latest" \
  --only-show-errors >/dev/null

echo "==> Pointing the web container app at the real image"
az containerapp update \
  --name "$WEB_APP_NAME" \
  --resource-group "$RESOURCE_GROUP" \
  --image "$ACR_LOGIN_SERVER/filebrowser-web:latest" \
  --only-show-errors >/dev/null

echo ""
echo "Done."
echo "  Web:  $WEB_URL"
echo "  API:  $API_URL"
