---
agent: speckit.plan
name: healthplanchat-plan
description: Generate an implementation plan for Health Plan Chat
---

Create an implementation plan for the Health Plan Chat feature described in the
current feature spec.

This demo plan SHOULD be explicit about technology choices. Capture decisions
with rationale, alternatives considered, and a rollback path where risk exists.

# Technology Decisions

Use these as defaults unless the spec/constitution requires otherwise. If you
deviate, explain why and what changes.

## Backend

- ASP.NET Core minimal Web API using .NET (.NET 10/C#), following a Clean
  Architecture style.
- Agent orchestration: Agent Framework (preview is acceptable). Pin the version
  and explicitly document the risk and rollback path.
- Azure App Service for hosting the backend API (simple, demo-friendly).
- Azure AI Foundry for model hosting:
  - Embeddings: `text-embedding-3-small`
  - Chat completions: `gpt-5-mini`
- Azure Managed Redis for chat history caching (do not use Azure Cache for
  Redis). See:
  https://learn.microsoft.com/en-us/azure/redis/web-app-aspnet-core-howto?pivots=azure-managed-redis
- Plan materials ingestion (keep simple): start with synthetic plan JSON files
  stored in the repo.
- Optional Azure deployment path: store the synthetic plan JSON docs in Azure
  Blob Storage and index/retrieve them via Azure AI Search.

## Frontend

- Blazor WebAssembly using .NET (.NET 10/C#).
- Azure Static Web Apps for hosting the frontend.

## Data

- For the synthetic plan JSON documents, generate them to cover a variety of
  plan types (HMO, PPO, EPO) and include diverse attributes (coverage details,
  pricing, provider networks). Use Contoso Health as the fictional provider.

## Planning guardrails

- Avoid committing secrets. Prefer managed identity for Azure-to-Azure access.
- Keep local dev straightforward (env vars / user secrets).
- The plan MUST preserve the spec's separation of grounded answers vs general
  guidance.
