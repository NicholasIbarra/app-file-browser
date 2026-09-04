# Deploying to Azure

The API and client deploy as two separate Azure Container Apps:

- **API** (`FileBrowser.Api`) reads/writes files through the Azure Blob
  Storage provider (`FileBrowser:Provider = Azure`), talking to a Storage
  Account provisioned by `infra/main.bicep` via a user-assigned managed
  identity (no secrets/connection strings involved).
- **Web** (the Vite client) is built and served as static files by nginx,
  pointed at the API's URL through an env var injected at container start.

## One-time setup

- [Azure CLI](https://learn.microsoft.com/cli/azure/install-azure-cli),
  logged in (`az login`) with the right subscription selected
  (`az account set --subscription <id>`).
- [`jq`](https://jqlang.org/) installed.
- Permission to create resource groups and role assignments in the
  target subscription.

## Deploy

```bash
./deploy.sh
```

This:
1. Creates a resource group (`rg-filebrowser` by default).
2. Deploys `infra/main.bicep` — Container Apps Environment, Azure Container
   Registry, Storage Account + blob container, two Container Apps (starting
   on a placeholder public image so the environment stands up cleanly).
3. Builds both Docker images remotely via `az acr build` (no local Docker
   needed) and pushes them to the new registry.
4. Points each Container App at its real image.
5. Prints the web app's public URL — that's the link to share.

Override the defaults with env vars:

```bash
RESOURCE_GROUP=my-rg LOCATION=westus2 APP_NAME=myfilebrowser ./deploy.sh
```

## Uploading files to browse

The Azure provider serves whatever is in the `files` blob container. Seed
it with, e.g.:

```bash
az storage blob upload-batch \
  --account-name <storageAccountName from the bicep outputs> \
  --auth-mode login \
  -d files \
  -s ./some-local-folder
```

(Blob "folders" are inferred from `/` in blob names — there's no separate
mkdir step.)

## Redeploying after a code change

```bash
az acr build --registry <acrName> --image filebrowser-api:latest \
  --file src/FileBrowser.Api/Dockerfile .
az containerapp update --name <apiAppName> --resource-group <rg> \
  --image <acrLoginServer>/filebrowser-api:latest
```

(swap in the web Dockerfile/app name for client changes) — or just re-run
`./deploy.sh`, which is idempotent.

## Alternative: `azd up`

`azure.yaml` is also provided for anyone with the [Azure Developer
CLI](https://learn.microsoft.com/azure/developer/azure-developer-cli/)
installed who'd rather use `azd up`. It wasn't possible to fully verify
azd's own image-build wiring in the environment these changes were made
in, so `deploy.sh` (plain `az` CLI, no azd dependency) is the verified,
recommended path — treat `azure.yaml` as a convenience starting point.

## What this does not (yet) do

- No custom domain / TLS cert beyond the default `*.azurecontainerapps.io`
  hostname.
- No CI/CD — `deploy.sh` is meant to be run by hand.
- No autoscale tuning beyond a 1–3 replica range on both apps.
