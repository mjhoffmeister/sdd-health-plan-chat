# Implementation Plan: Health Plan Chat MVP

**Branch**: `001-health-plan-chat` | **Date**: 2026-01-09 | **Spec**: ./spec.md
**Input**: Feature specification from `/specs/001-health-plan-chat/spec.md`

**Note**: This template is filled in by the `/speckit.plan` command.

## Summary

Build a chat experience where users ask health plan questions and receive responses that are either (a) grounded in plan materials with explicit references or (b) clearly labeled general guidance when materials do not contain an answer. Maintain chat history within a session (server-side) and provide a Blazor WebAssembly UI with light/dark themes.

Approach: retrieval-augmented generation (RAG) over synthetic plan JSON documents indexed in Azure AI Search, with session history stored in Azure Managed Redis (Redis Enterprise via AzAPI). The LLM call path is implemented via Agent Framework (agent-first), with Azure AI Foundry primarily as the configured model endpoint/provider.

## Technical Context

**Language/Version**: .NET 10, C# 14  
**Primary Dependencies**: ASP.NET Core minimal APIs; Blazor WebAssembly; Agent Framework (`Microsoft.Agents.AI` `1.0.0-preview.260108.1`); Azure AI Search SDK; Azure Storage SDK; Azure Managed Redis client (`Microsoft.Azure.StackExchangeRedis`); FluentResults  
**Storage**: Repo-backed synthetic plan JSON (source-of-truth) + Azure Blob Storage (for indexing); Azure AI Search (vector/keyword index); Azure Managed Redis (Redis Enterprise via AzAPI `Microsoft.Cache/redisEnterprise@2025-04-01` + required `Microsoft.Cache/redisEnterprise/databases@2025-04-01`) (session chat history)  
**Testing**: xUnit + FluentAssertions + Moq (Core); minimal API integration tests (later); optional bUnit for UI  
**Target Platform**: Azure App Service (backend API) + Azure Static Web Apps (frontend)
**Project Type**: web (frontend + backend + IaC)  
**Performance Goals**: demo responsiveness: p95 end-to-end API latency for `/api/chat` under 5s for at least 95% of questions (SC-004), measured server-side from request start to response serialization  
**Constraints**: no secrets in repo; safe logging; deterministic core logic; anonymous users; grounded vs general guidance labeling required; deployments (including first deployments) run via GitHub Actions only  
**IaC Provider Pinning**: Terraform MUST use the AzAPI provider pinned to `2.8.0` (do not float provider versions).
**Terraform State**: Use Pattern 2 (self-bootstrapping pipeline) so environments are repeatable: the GitHub Actions infra workflow bootstraps the `azurerm` backend storage (RG/SA/container + RBAC for the WIF identity) before `terraform init/plan/apply`.
**Scale/Scope**: single-demo environment; typical sessions ~10+ messages (SC-005)

**NuGet version pinning**: Pin all non-framework NuGet dependencies via Central Package Management (so builds are reproducible and automation doesn’t guess versions). Initial pins:

| Package | Version |
|---|---|
| Microsoft.Agents.AI | 1.0.0-preview.260108.1 |
| Azure.Search.Documents | 11.7.0 |
| Azure.Storage.Blobs | 12.27.0 |
| Azure.Identity | 1.17.1 |
| Microsoft.Azure.StackExchangeRedis | 3.3.1 |
| FluentResults | 4.0.0 |
| xunit | 2.9.3 |
| xunit.runner.visualstudio | 3.1.5 |
| Microsoft.NET.Test.Sdk | 18.0.1 |
| FluentAssertions | 8.8.0 |
| Moq | 4.20.72 |

Note: Avoid directly referencing `StackExchange.Redis` unless required; prefer `Microsoft.Azure.StackExchangeRedis` and let it control compatible transitive versions.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

Evaluate against `.specify/memory/constitution.md`.

- Security/privacy: no sensitive data exposure; safe logging.
- Security review: complete `specs/001-health-plan-chat/security.md` before implementing new external integrations and revisit it when adding additional Azure services.
- Simplicity: avoid unnecessary moving parts; justify complexity.
- Testability: deterministic core logic; seams for external dependencies.
- Separation: privileged operations remain server-side.

Status: PASS (no violations expected for the plan as designed).

## Project Structure

### Documentation (this feature)

```text
specs/[###-feature]/
├── plan.md              # This file (/speckit.plan command output)
├── research.md          # Phase 0 output (/speckit.plan command)
├── security.md          # Security review gate (threat model + abuse cases)
├── data-model.md        # Phase 1 output (/speckit.plan command)
├── quickstart.md        # Phase 1 output (/speckit.plan command)
├── contracts/           # Phase 1 output (/speckit.plan command)
└── tasks.md             # Phase 2 output (/speckit.tasks command - NOT created by /speckit.plan)
```

### Source Code (repository root)
```text
src/
  backend/
    HealthPlanChat.sln
    HealthPlanChat.Core/
    HealthPlanChat.Infrastructure.Redis/
    HealthPlanChat.Infrastructure.Search/
    HealthPlanChat.Infrastructure.Foundry/
    HealthPlanChat.Infrastructure.Storage/
    HealthPlanChat.Infrastructure.Prompting/
    HealthPlanChat.Infrastructure.Prompting.UnitTests/
    HealthPlanChat.Bootstrapper/
    HealthPlanChat.WebApi/
    HealthPlanChat.Core.UnitTests/
    HealthPlanChat.Infrastructure.IntegrationTests/

  frontend/
    HealthPlanChat.Web/

infra/
  terraform/

data/
  plan-materials/        # synthetic plan JSON (repo source-of-truth)
```

**Structure Decision**: Web application. Backend uses Clean Architecture projects; frontend is Blazor WebAssembly; infra is Terraform (AzAPI).

## Complexity Tracking

None.

## Phase 0: Research

Outputs:
- ./research.md

Key outcomes:
- Confirmed Clean Architecture split for backend.
- Confirmed RAG via Azure AI Search over synthetic plan JSON.
- Confirmed Azure AI Foundry models: `text-embedding-3-small` + `gpt-5-mini`.
- Confirmed session history stored in Azure Managed Redis (Redis Enterprise) with TTL.

## Phase 1: Design & Contracts

Outputs:
- ./data-model.md
- ./contracts/openapi.yaml
- ./quickstart.md

Design highlights:
- Session: backend issues a session id returned by `POST /api/sessions`; client includes `sessionId` in `POST /api/chat`; server stores ordered messages; TTL-based expiry.
- Retrieval: query → top chunks from AI Search; compute confidence from top hit count + top hit score; if confidence is below a configurable threshold, return `GeneralGuidance` (no references); otherwise build grounded prompt → chat completion. Config keys: `Retrieval__MinHits`, `Retrieval__MinTopScore`.
- Labeling: every response must explicitly return `Grounded` or `GeneralGuidance`.
- References: return a stable set of citations (document id + anchor + short quote).
- Security: managed identity for service-to-service; no secrets committed; log redaction.

Re-check Constitution: PASS (security, simplicity, and testability preserved; external dependencies isolated behind interfaces).

## Phase 2: Implementation Plan (Outline)

1) Scaffold solution/projects for Clean Architecture backend and Blazor WASM frontend.
2) Add Terraform + GitHub Actions early (Azure-first): provision Search/Storage/Redis/Foundry and set up WIF-based workflows for first deployments.
3) Implement Core domain + a single Chat use case: session history load/append (via store), answer typing/labeling, reference formatting.
4) Implement Infrastructure: Agent Framework chat agent (Foundry endpoint), AI Search retrieval, Blob ingestion, Managed Redis session store.
5) Implement Web API endpoints per ./contracts/openapi.yaml.
6) Implement Blazor UI: chat panel, references display, theme toggle, new session button.
7) Add tests: Core unit tests for parsing/labeling/reference formatting; minimal API smoke tests.
