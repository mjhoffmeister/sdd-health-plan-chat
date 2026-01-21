---
agent: speckit.plan
name: healthplanchat-plan
description: Generate an implementation plan for Health Plan Chat
---

Create an implementation plan for the Health Plan Chat feature described in the
current feature spec.

# Technology Decisions

## Versions
| Component | Version |
|---|---|
| .NET | 10 |
| C# | 14 |
| AzAPI Terraform provider | 2.8.0 |

## Backend

- ASP.NET Core minimal Web API using .NET, following a Clean Architecture style.
- Agent orchestration: use the lastest preview version of Agent Framework.
- Azure App Service for hosting the backend API.
- Azure AI Foundry for AI model hosting. Use text-embedding-3-small global for
  embeddings and gpt-4o global for chat completions (gpt-4o required for azure_ai_search tool).
- Azure Managed Redis for chat history caching (do not use Azure Cache for
  Redis). See:
  https://learn.microsoft.com/en-us/azure/redis/web-app-aspnet-core-howto?pivots=azure-managed-redis
- Azure AI Search for plan material indexing and retrieval, sourced from Azure
  Blob Storage which stores synthetic plan JSON documents.

## Frontend

- Blazor WebAssembly.
- Azure Static Web Apps for hosting the frontend.

## Data

- For the synthetic plan JSON documents, generate them to cover a variety of
  plan types (HMO, PPO, EPO) and include diverse attributes (coverage details,
  pricing, provider networks). Use Contoso Health as the fictional provider.

## Security

- Use Managed Identity for all service-to-service authentication.
- Use anonymous access for the frontend static web app (no users).

## Infrastructure

- Use Infrastructure as Code (IaC) with Terraform for all Azure resources using
  the AzAPI provider.
- Create a single demo environment in Azure for hosting all components.

## CI/CD

- Use GitHub Actions for CI/CD pipelines.
- Use separate pipelines for application and infrastructure deployments.
- Use Workload Identity Federation for secure deployments without secrets.
