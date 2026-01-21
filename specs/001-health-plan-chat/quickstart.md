# Quickstart: Health Plan Chat MVP

## Prerequisites

- .NET 10 SDK
- Azure CLI (`az`)
- GitHub CLI (`gh`) — for environment bootstrap
- PowerShell 7+ — for setup scripts
- Azure subscription with Contributor + User Access Administrator permissions

## Project Structure

```text
src/
  backend/                    # ASP.NET Core minimal API
    HealthPlanChat.WebApi/    # API host (Program.cs, endpoints)
    HealthPlanChat.Core/      # Domain models, use cases, interfaces
    HealthPlanChat.Infrastructure.*/  # Azure implementations
  frontend/
    HealthPlanChat.Web/       # Blazor WebAssembly app
infra/
  terraform/                  # Azure infrastructure (AzAPI)
data/
  plan-materials/             # Synthetic plan JSON (source of truth)
  demo-questions.json         # SC-001 test questions
scripts/
  setup-github-env.ps1        # GitHub Environment bootstrap
  setup-search-index.ps1      # Search index pipeline setup
```

## Local Development (with Azure Resources)

This repo is **Azure-first**: the app runs locally but connects to Azure resources (Foundry, AI Search, Redis) provisioned via Terraform.

### 1. Configure Local Settings

Create `src/backend/HealthPlanChat.WebApi/appsettings.Development.local.json` (gitignored):

```json
{
  "Foundry": {
    "Endpoint": "https://your-foundry.services.ai.azure.com/api/projects/your-project",
    "DeploymentName": "gpt-4o",
    "SearchConnectionId": "ai-search",
    "SearchIndexName": "plan-materials"
  },
  "Redis": {
    "Endpoint": "your-redis.eastus2.redis.azure.net:10000"
  },
  "Cors": {
    "AllowedOrigins": ["https://localhost:7001", "http://localhost:5001"]
  }
}
```

### 2. Run Backend

```powershell
cd src/backend/HealthPlanChat.WebApi
dotnet run
```

Backend starts at `https://localhost:7000` (or configured port).

### 3. Run Frontend

```powershell
cd src/frontend/HealthPlanChat.Web
dotnet run
```

Frontend starts at `https://localhost:7001`. Configure `appsettings.Development.json` with:

```json
{
  "ApiBaseUrl": "https://localhost:7000"
}
```

### 4. Verify Health Check

```powershell
curl https://localhost:7000/healthz
# Expected: "Healthy"
```

## Azure Deployment (Demo Environment)

**Policy**: All infrastructure and application deployments run via GitHub Actions only.

### One-Time Bootstrap (First Deployment)

The first deployment requires manual steps because WIF credentials are created by Terraform.

#### Step 1: Azure Login

```powershell
az login
az account set --subscription "Your-Subscription-Name"
```

Ensure your account has Contributor + User Access Administrator at subscription scope.

#### Step 2: Run Infrastructure Workflow

In GitHub, trigger `.github/workflows/infra.yml`:
- **Environment**: `demo`
- **Action**: `apply`
- **developer_principal_id** (optional): Your Azure AD Object ID for local debugging RBAC

#### Step 3: Setup Search Index

After Terraform completes, run the search index setup script:

```powershell
# Get values from Terraform outputs
$searchService = "srch-healthplanchat-demo-xxxxx"
$resourceGroup = "rg-healthplanchat-demo"
$storageAccount = "sthpcdemoxxxxx"
$foundryEndpoint = "https://aif-healthplanchat-demo-xxxxx.cognitiveservices.azure.com"

./scripts/setup-search-index.ps1 `
    -SearchServiceName $searchService `
    -ResourceGroupName $resourceGroup `
    -StorageAccountName $storageAccount `
    -FoundryEndpoint $foundryEndpoint `
    -Force
```

#### Step 4: Create GitHub Environment

```powershell
# Requires GitHub CLI and PAT with admin:repo scope
$env:GH_TOKEN = "ghp_your_pat_here"
./scripts/setup-github-env.ps1 -Environment demo
```

#### Step 5: Verify GitHub Environment

Check GitHub repo **Settings → Environments → demo**:
- `AZURE_CLIENT_ID`, `AZURE_TENANT_ID`, `AZURE_SUBSCRIPTION_ID`
- `AZURE_APP_SERVICE_NAME`, `AZURE_STORAGE_ACCOUNT_NAME`
- `AZURE_SWA_NAME`, `AZURE_SWA_HOSTNAME`, `AZURE_RESOURCE_GROUP_NAME`

### Subsequent Deployments

After bootstrap, trigger workflows as needed:

```text
infra.yml  → Terraform plan/apply/destroy
app.yml    → Build, test, deploy backend + frontend + sync plan materials
```

The `app.yml` workflow:
1. Builds and tests the .NET solution
2. Deploys backend to App Service
3. Deploys frontend to Static Web Apps
4. Syncs plan materials to Blob Storage (incremental)
5. Search indexer auto-triggers on blob changes

### Access the Application

After `app.yml` completes:
- **Frontend**: `https://<SWA_HOSTNAME>` (from GitHub Environment)
- **Backend Health**: `https://<APP_SERVICE_NAME>.azurewebsites.net/healthz`
- **API Docs**: POST `/api/sessions` and `/api/chat` per `specs/001-health-plan-chat/contracts/openapi.yaml`

## Demo Checklist (SC-001 Validation)

Use `data/demo-questions.json` for consistent testing. Run these spot-checks:

### Grounded Answers (expect `answerType: Grounded` with references)

| ID | Question | Expected Behavior |
|----|----------|-------------------|
| Q001 | "What is the primary care visit copay for the Contoso Health PPO Silver plan?" | Grounded answer citing copay section |
| Q002 | "Is emergency room care covered and what do I pay?" | Grounded answer citing ER coverage |
| Q003 | "Do I need a referral to see a specialist?" | Grounded answer (may vary by plan type) |
| Q004 | "What is the out-of-pocket maximum?" | Grounded answer with cost-sharing reference |

### General Guidance (expect `answerType: GeneralGuidance`, no references)

| ID | Question | Expected Behavior |
|----|----------|-------------------|
| Q005 | "What is the best plan for my diabetes?" | General guidance (not in plan materials) |
| Q006 | "Should I stop taking my medication?" | General guidance (medical advice) |

### Session Continuity

1. Create a session via `POST /api/sessions`
2. Ask Q001, verify grounded response
3. Ask follow-up: "What about urgent care?" — verify consistent session context
4. Switch theme (light/dark) — verify chat history preserved
5. Click "New chat" — verify conversation clears

### Success Criteria Targets

| Criterion | Target | How to Verify |
|-----------|--------|---------------|
| SC-001 | ≥90% of Grounded questions return Grounded + references | Run Q001-Q004, check `answerType` and `references` |
| SC-002 | 100% responses labeled Grounded or GeneralGuidance | Inspect all API responses |
| SC-003 | Theme switch in 1 action, UI readable | Click toggle, verify |
| SC-004 | <5s response time for 95% of questions | Measure server-side latency |
| SC-005 | 10+ messages in single session | Send 10 messages, verify history visible |

## Troubleshooting

### Backend won't start

```powershell
# Check configuration
dotnet run --project src/backend/HealthPlanChat.WebApi -- --urls=https://localhost:7000
```

Verify `appsettings.Development.local.json` exists with valid Azure endpoints.

### Search returns no results

```powershell
# Check indexer status
az search indexer status show `
    --resource-group $resourceGroup `
    --service-name $searchService `
    --name plan-materials-indexer

# Reset and rerun indexer
./scripts/setup-search-index.ps1 ... -ResetIndexer
```

### Redis connection fails

Ensure your Azure AD identity has `Redis Data Owner` role (set via `developer_principal_id` in `infra.yml`).

### CORS errors

Verify `Cors:AllowedOrigins` includes the frontend URL in backend settings.
