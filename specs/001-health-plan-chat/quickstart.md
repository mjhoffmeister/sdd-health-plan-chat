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

Provision:
- Terraform (AzAPI provider, pinned to `2.8.0`) creates:
  - Azure App Service (backend)
  - Azure Static Web Apps (frontend)
  - Azure AI Foundry resources/deployments
  - Azure AI Search + index
  - Azure Blob Storage (plan materials)
  - Azure Managed Redis (Redis Enterprise cluster + `default` database) (session history)

Deploy:
- GitHub Actions pipelines
  - `infra` pipeline: applies Terraform
  - `app` pipeline: builds and deploys API + frontend

Notes:
- Use `workflow_dispatch` to run the `infra` pipeline for first-time provisioning.
- Avoid manual `terraform apply` from a developer machine.
- The `infra` pipeline bootstraps Terraform remote state (RG/Storage/Container + RBAC for the GitHub Actions WIF identity) before running `terraform init/plan/apply`.

Run:
- Upload plan JSON documents to Blob
- Trigger indexing (indexer or app-startup ingest)
- Open the Static Web App URL and start chatting

## Demo checklist

- Ask a question answered by plan docs → response labeled `Grounded` + references.
- Ask an out-of-scope question → response labeled `General guidance`.
- Ask a follow-up question → consistent response using session history.
