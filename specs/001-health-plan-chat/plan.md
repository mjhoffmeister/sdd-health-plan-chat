# Implementation Plan: Health Plan Chat MVP

**Branch**: `001-health-plan-chat` | **Date**: 2026-01-09 | **Spec**: ./spec.md
**Input**: Feature specification from `/specs/001-health-plan-chat/spec.md`

**Note**: This template is filled in by the `/speckit.plan` command.

## Summary

Build a chat experience where users ask health plan questions and receive responses that are either (a) grounded in plan materials with explicit references or (b) clearly labeled general guidance when materials do not contain an answer. Maintain chat history within a session (server-side) and provide a Blazor WebAssembly UI with light/dark themes.

Approach: retrieval-augmented generation (RAG) over synthetic plan JSON documents indexed in Azure AI Search, with session history stored in Azure Managed Redis.

## Technical Context

<!--
  ACTION REQUIRED: Replace the content in this section with the technical details
  for the project. The structure here is presented in advisory capacity to guide
  the iteration process.
-->

**Language/Version**: .NET 10, C# 14  
**Primary Dependencies**: ASP.NET Core minimal APIs; Blazor WebAssembly; Agent Framework (latest preview); Azure AI Search SDK; Azure Storage SDK; Azure Managed Redis client; FluentResults  
**Storage**: Repo-backed synthetic plan JSON (source-of-truth) + Azure Blob Storage (for indexing); Azure AI Search (vector/keyword index); Azure Managed Redis (session chat history)  
**Testing**: xUnit + FluentAssertions + Moq (Core); minimal API integration tests (later); optional bUnit for UI  
**Target Platform**: Azure App Service (backend API) + Azure Static Web Apps (frontend)
**Project Type**: web (frontend + backend + IaC)  
**Performance Goals**: demo responsiveness: initial answer < 5s for at least 95% of questions (SC-004)  
**Constraints**: no secrets in repo; safe logging; deterministic core logic; anonymous users; grounded vs general guidance labeling required  
**Scale/Scope**: single-demo environment; typical sessions ~10+ messages (SC-005)

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

Evaluate against `.specify/memory/constitution.md`.

- Security/privacy: no sensitive data exposure; safe logging.
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
├── data-model.md        # Phase 1 output (/speckit.plan command)
├── quickstart.md        # Phase 1 output (/speckit.plan command)
├── contracts/           # Phase 1 output (/speckit.plan command)
└── tasks.md             # Phase 2 output (/speckit.tasks command - NOT created by /speckit.plan)
```

### Source Code (repository root)
<!--
  ACTION REQUIRED: Replace the placeholder tree below with the concrete layout
  for this feature. Delete unused options and expand the chosen structure with
  real paths (e.g., apps/admin, packages/something). The delivered plan must
  not include Option labels.
-->

```text
src/
  backend/
  HealthPlanChat.sln
  HealthPlanChat.Core/
  HealthPlanChat.Infrastructure.{Provider}/
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

> **Fill ONLY if Constitution Check has violations that must be justified**

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| [e.g., 4th project] | [current need] | [why 3 projects insufficient] |
| [e.g., Repository pattern] | [specific problem] | [why direct DB access insufficient] |

## Phase 0: Research

Outputs:
- ./research.md

Key outcomes:
- Confirmed Clean Architecture split for backend.
- Confirmed RAG via Azure AI Search over synthetic plan JSON.
- Confirmed Azure AI Foundry models: `text-embedding-3-small` + `gpt-5-mini`.
- Confirmed session history stored in Azure Managed Redis with TTL.

## Phase 1: Design & Contracts

Outputs:
- ./data-model.md
- ./contracts/openapi.yaml
- ./quickstart.md

Design highlights:
- Session: backend issues a session id; server stores ordered messages; TTL-based expiry.
- Retrieval: query → top chunks from AI Search → build grounded prompt → chat completion.
- Labeling: every response must explicitly return `Grounded` or `GeneralGuidance`.
- References: return a stable set of citations (document id + anchor + short quote).
- Security: managed identity for service-to-service; no secrets committed; log redaction.

Re-check Constitution: PASS (security, simplicity, and testability preserved; external dependencies isolated behind interfaces).

## Phase 2: Implementation Plan (Outline)

1) Scaffold solution/projects for Clean Architecture backend and Blazor WASM frontend.
2) Implement Core domain/use cases: session management, answer typing/labeling, reference formatting.
3) Implement Infrastructure: Azure AI Foundry client, AI Search retrieval, Blob ingestion, Managed Redis session store.
4) Implement Web API endpoints per ./contracts/openapi.yaml.
5) Implement Blazor UI: chat panel, references display, theme toggle, new session button.
6) Add tests: Core unit tests for parsing/labeling/reference formatting; minimal API smoke tests.
7) Add Terraform and GitHub Actions per tech decisions (single demo environment).
