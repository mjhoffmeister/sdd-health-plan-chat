# Quickstart: Health Plan Chat MVP

## Prereqs

- .NET 10 SDK
- Node.js (only if needed for tooling; otherwise optional)
- Azure subscription (for deployed demo)

## Local dev (no Azure)

1) Generate or place synthetic plan JSON documents
- Put JSON files under a repo folder (planned): `data/plan-materials/`

2) Run backend
- From repo root (once code exists): `dotnet run --project src/backend/...`

3) Run frontend
- From repo root (once code exists): `dotnet run --project src/frontend/...`

Local mode uses:
- In-memory plan material loader
- In-memory session store (optional) if Redis isn’t available

## Azure deployment (demo environment)

Provision:
- Terraform (AzAPI provider) creates:
  - Azure App Service (backend)
  - Azure Static Web Apps (frontend)
  - Azure AI Foundry resources/deployments
  - Azure AI Search + index
  - Azure Blob Storage (plan materials)
  - Azure Managed Redis (session history)

Deploy:
- GitHub Actions pipelines
  - `infra` pipeline: applies Terraform
  - `app` pipeline: builds and deploys API + frontend

Run:
- Upload plan JSON documents to Blob
- Trigger indexing (indexer or app-startup ingest)
- Open the Static Web App URL and start chatting

## Demo checklist

- Ask a question answered by plan docs → response labeled `Grounded` + references.
- Ask an out-of-scope question → response labeled `General guidance`.
- Ask a follow-up question → consistent response using session history.
