# Quickstart: Health Plan Chat MVP

## Prereqs

- .NET 10 SDK
- Node.js (only if needed for tooling; otherwise optional)
- Azure subscription (for deployed demo)

## Local dev (runs against Azure)

This repo is **Azure-first**: the app can run locally, but it is expected to talk to Azure resources (Foundry, AI Search, Storage, Redis) provisioned for the environment.

1) Generate or place synthetic plan JSON documents
- Put JSON files under: `data/plan-materials/`

2) Run backend
- From repo root (once code exists): `dotnet run --project src/backend/...`

3) Run frontend
- From repo root (once code exists): `dotnet run --project src/frontend/...`

Notes:
- Configure required settings via environment variables (or user secrets for local dev).
- Prefer managed identity where supported. If any secrets are truly unavoidable, store them as GitHub Actions environment secrets and inject them via App Service/SWA app settings at deploy time (do not add Key Vault for this demo).

## Azure deployment (demo environment)

**Policy**: Infrastructure and application deployments (including the first provisioning/deployment) are performed via GitHub Actions only.

### One-time bootstrap (first deployment only)

The first deployment requires manual steps because WIF credentials are created by Terraform.

**Step 1: Azure login**

```powershell
az login
az account set --subscription "Your-Subscription-Name"
```

Ensure your account has Contributor + User Access Administrator at subscription scope.

**Step 2: Run infra workflow**

In GitHub, trigger the `infra.yml` workflow with:
- Environment: `demo`
- Action: `apply`

The workflow uses your Azure CLI auth context for the first run.

**Step 3: Create GitHub Environment**

After Terraform completes, run the bootstrap script locally:

```powershell
# Requires GitHub CLI (gh) and a PAT with admin:repo scope
$env:GH_TOKEN = "ghp_your_pat_here"
./scripts/setup-github-env.ps1 -Environment demo
```

This creates the `demo` environment and populates all required variables from Terraform outputs.

**Step 4: Verify**

Check GitHub repo Settings → Environments → `demo`. You should see:
- `AZURE_CLIENT_ID`, `AZURE_TENANT_ID`, `AZURE_SUBSCRIPTION_ID`
- `AZURE_APP_SERVICE_NAME`, `AZURE_STORAGE_ACCOUNT_NAME`
- `AZURE_SWA_NAME`, `AZURE_SWA_HOSTNAME`, `AZURE_RESOURCE_GROUP_NAME`

### Subsequent deployments

After bootstrap, all deployments use WIF credentials automatically:
- `infra.yml` — Terraform plan/apply/destroy
- `app.yml` — Build, test, and deploy application

### What gets provisioned

Terraform (AzAPI provider, pinned to `2.8.0`) creates:
- Azure App Service (backend)
- Azure Static Web Apps (frontend)
- Azure AI Foundry resources/deployments
- Azure AI Search + index + indexer pipeline
- Azure Blob Storage (plan materials)
- Azure Managed Redis (Redis Enterprise cluster + `default` database)

### What gets deployed

GitHub Actions pipelines:
- `infra.yml` — applies Terraform
- `app.yml` — builds backend/frontend, deploys to App Service and SWA, syncs plan materials to Blob

Run:
- Plan materials are synced to Blob automatically by `app.yml`
- Search indexer auto-triggers on blob changes
- Open the Static Web App URL and start chatting

## Demo checklist

- Ask a question answered by plan docs → response labeled `Grounded` + references.
- Ask an out-of-scope question → response labeled `General guidance`.
- Ask a follow-up question → consistent response using session history.
