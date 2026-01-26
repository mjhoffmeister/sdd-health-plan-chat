# Tasks: Health Plan Chat MVP

**Input**: Design documents from `/specs/001-health-plan-chat/`
**Prerequisites**: plan.md (required), spec.md (required), research.md, data-model.md, contracts/

**Tests**: Include automated tests where practical. Critical paths MUST be covered by automated tests (per constitution). Start with Core unit tests for deterministic logic, then add minimal API smoke/integration coverage.

**Deployment policy**: All infrastructure and application deployments (including first deployments) MUST be performed via GitHub Actions. Local commands may be used for validation (e.g., `terraform validate`), but not for deployment.

**Agent Framework policy**: Use Agent Framework via `Microsoft.Agents.AI` pinned to `1.0.0-preview.260108.1`.

**NuGet version policy**: Pin all non-framework NuGet packages via Central Package Management. Use the versions listed in `specs/001-health-plan-chat/plan.md` (do not float versions).

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format

- `- [ ] T### [P?] [US?] Description with file path`
- **[P]**: Can run in parallel (different files, no dependencies)
- **[US#]**: User story label (US1, US2, US3)

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Initialize repo structure and baseline projects.

- [X] T001 Create top-level folders `src/`, `infra/terraform/`, `data/plan-materials/`
- [X] T002 Scaffold backend solution in `src/backend/HealthPlanChat.sln` (Core/Infrastructure/Bootstrapper/WebApi + test projects) and enable Central Package Management in `src/backend/Directory.Packages.props` with pinned versions: `Microsoft.Agents.AI` `1.0.0-preview.260108.1`, `Azure.Search.Documents` `11.7.0`, `Azure.Storage.Blobs` `12.27.0`, `Azure.Identity` `1.17.1`, `Microsoft.Azure.StackExchangeRedis` `3.3.1`, `FluentResults` `4.0.0`, `xunit` `2.9.3`, `xunit.runner.visualstudio` `3.1.5`, `Microsoft.NET.Test.Sdk` `18.0.1`, `FluentAssertions` `8.8.0`, `Moq` `4.20.72`
- [X] T003 [P] Delete generated placeholders (e.g., `src/backend/**/Class1.cs`, `src/backend/**/UnitTest1.cs`)
- [X] T004 [P] Scaffold Blazor WebAssembly app in `src/frontend/HealthPlanChat.Web/` and remove template demo/sample assets (e.g., `src/frontend/HealthPlanChat.Web/wwwroot/sample-data/weather.json`) and any unused template wiring that depends on them
- [X] T005 [P] Add local dev settings template in `src/backend/HealthPlanChat.WebApi/appsettings.Development.json` (no secrets)
- [X] T006 [P] Add repo-level ignore and tooling files (e.g., `.gitignore`, `.editorconfig`) aligned with .NET + Blazor
- [X] T007 [P] Add synthetic plan JSON seed files under `data/plan-materials/` (HMO/PPO/EPO examples for Contoso Health)

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Cross-cutting backend/frontend foundations that all user stories rely on.

- [X] T008 Create shared API contracts (DTOs) in `src/backend/HealthPlanChat.Core/UseCases/Contracts/`
- [X] T009 Create domain models in `src/backend/HealthPlanChat.Core/Domain/Chat/` (ChatSession, ChatMessage, AnswerType, Reference)
- [X] T010 Create external interfaces in `src/backend/HealthPlanChat.Core/ExternalInterfaces/` (IChatSessionStore, IPlanMaterialSearch, IChatAgent)
- [X] T011 Define configuration binding in `src/backend/HealthPlanChat.WebApi/Configuration/` and keep provider-specific option types in their Infrastructure projects (e.g., `src/backend/HealthPlanChat.Infrastructure.Redis/RedisOptions.cs`, `src/backend/HealthPlanChat.Infrastructure.Search/SearchOptions.cs`, `src/backend/HealthPlanChat.Infrastructure.AgentFramework/FoundryOptions.cs`)
- [X] T012 Implement minimal API host skeleton in `src/backend/HealthPlanChat.WebApi/Program.cs` (healthz + routing)
- [X] T013 Implement structured logging + safe error handling middleware in `src/backend/HealthPlanChat.WebApi/Middleware/` and create a lightweight threat model + abuse cases doc in `specs/001-health-plan-chat/security.md` (prompt injection, data exfiltration, logging/redaction, session id handling, Azure integration risks)
- [X] T014 Implement Bootstrapper DI registration in `src/backend/HealthPlanChat.Bootstrapper/ServiceCollectionExtensions.cs`

- [X] T015 [P] Add Terraform provider + backend skeleton in `infra/terraform/providers.tf` (use AzAPI provider pinned to `2.8.0` in `required_providers`)
- [X] T016 [P] Add Terraform remote state bootstrap script for a self-bootstrapping pipeline (Pattern 2) in `infra/terraform/state-bootstrap.ps1` (idempotently create RG + Storage Account + Container for Terraform state and assign `Storage Blob Data Contributor` to the GitHub Actions WIF identity; intended to run from GitHub Actions)
- [X] T017 Configure Terraform remote state backend in `infra/terraform/providers.tf` using `backend "azurerm" {}` and pass concrete backend settings via `-backend-config` from the workflow (single demo environment: one storage account/container/key; avoid hardcoding names in code)
- [X] T018 [P] Add Terraform resources for App Service + plan in `infra/terraform/appservice.tf`
- [X] T019 [P] Add Terraform resources for Static Web Apps in `infra/terraform/swa.tf`
- [X] T020 [P] Add Terraform resources for Azure AI Search in `infra/terraform/search.tf`: (1) Search service with system-assigned managed identity, (2) RBAC role assignments for Search/Foundry/developer identities. Note: Index, data source, skillset, and indexer are data-plane resources created via `scripts/setup-search-index.ps1` after terraform apply.
- [X] T021 [P] Add Terraform resources for Storage account + container in `infra/terraform/storage.tf`
- [X] T022 [P] Add Terraform resources for Azure Managed Redis (Redis Enterprise) in `infra/terraform/redis.tf` using AzAPI: `Microsoft.Cache/redisEnterprise@2025-04-01` AND the required child `Microsoft.Cache/redisEnterprise/databases@2025-04-01` (create `default` database)
- [X] T023 [P] Add Terraform resources for Azure AI Foundry / Azure AI Services account in `infra/terraform/foundry.tf`
- [X] T024 [P] Add Terraform model deployments for `gpt-4o` and `text-embedding-3-small` in `infra/terraform/foundry.deployments.tf` (note: gpt-4o required for `azure_ai_search` tool support)
- [X] T025 [P] Add GitHub Actions Workload Identity Federation (WIF/OIDC) setup in `infra/terraform/identity.tf`: create a new Entra application registration (no pre-existing app), ensure its service principal exists in the tenant, add federated identity credential(s) scoped to this repo, and output values needed by workflows (`AZURE_TENANT_ID`, `AZURE_SUBSCRIPTION_ID`, `AZURE_CLIENT_ID`)
- [X] T026 [P] Add Terraform role assignments co-located with the owning resources: App Service managed identity access in `infra/terraform/appservice.tf` (Search Index Data Reader for AI Search, Cognitive Services User for AI Foundry; no Blob Storage role—indexed content accessed via Search); Search Service managed identity in `infra/terraform/search.tf` (Storage Blob Data Reader for blob access, Cognitive Services User for embedding skillset); any service-specific roles in their respective files (`infra/terraform/foundry.tf`, `infra/terraform/redis.tf`)
- [X] T027 Add GitHub Actions workflow for infra deploy (WIF/OIDC + `workflow_dispatch`) in `.github/workflows/infra.yml` implementing Pattern 2: `azure/login` (OIDC) → run `infra/terraform/state-bootstrap.ps1` → `terraform init` with `-backend-config` → `terraform plan/apply` (no client secrets)
- [X] T028 Add GitHub Actions workflow for app build/deploy (WIF/OIDC + `workflow_dispatch`) in `.github/workflows/app.yml`: use `azure/login` with `client-id/tenant-id/subscription-id`; deploy backend via `azure/webapps-deploy`; deploy frontend via `az staticwebapp deploy` (CLI-based, WIF-compatible—no deployment token needed); sync plan materials via `az storage blob sync`
- [X] T028a [P] Add missing Terraform outputs in `infra/terraform/outputs.tf`: `app_service_name`, `storage_account_name`, `static_web_app_name`, `static_web_app_hostname`, `resource_group_name` (for GitHub Environment variables consumed by `app.yml`)
- [X] T028b Create GitHub Environment bootstrap script in `scripts/setup-github-env.ps1`: read Terraform outputs, create GitHub Environment via `gh api`, populate variables (`AZURE_CLIENT_ID`, `AZURE_TENANT_ID`, `AZURE_SUBSCRIPTION_ID`, `AZURE_APP_SERVICE_NAME`, `AZURE_STORAGE_ACCOUNT_NAME`, `AZURE_SWA_NAME`, `AZURE_SWA_HOSTNAME`, `AZURE_RESOURCE_GROUP_NAME`) from Terraform outputs; requires GitHub CLI and PAT with `admin:repo` scope (for environment creation)

- [X] T029 [P] Implement Azure Managed Redis-backed `IChatSessionStore` using `Microsoft.Azure.StackExchangeRedis` in `src/backend/HealthPlanChat.Infrastructure.Redis/RedisChatSessionStore.cs` (TTL, ordered messages, per-session keying)
- [X] T030 [P] Implement Azure AI Search adapter in `src/backend/HealthPlanChat.Infrastructure.Search/AzureAiSearchPlanMaterialSearch.cs`
- [X] T031 [P] Implement `IChatAgent` using Agent Framework targeting Azure AI Foundry (`Microsoft.Agents.AI` `1.0.0-preview.260108.1`) in `src/backend/HealthPlanChat.Infrastructure.AgentFramework/AgentFrameworkChatAgent.cs`
- [X] T032 Add plan-material blob upload helper in `src/backend/HealthPlanChat.Infrastructure.Storage/PlanMaterialBlobPublisher.cs` (upload JSON from `data/plan-materials/` to Blob; Search indexer auto-triggers on blob changes)
- [X] T033 Add frontend HTTP client wiring in `src/frontend/HealthPlanChat.Web/Program.cs` and `src/frontend/HealthPlanChat.Web/Services/ApiClient.cs`

**Checkpoint**: Foundation ready — user story work can begin.

---

## Phase 3: User Story 1 — Ask Plan Questions (Priority: P1) 🎯 MVP

**Goal**: Users can ask a question and receive a grounded answer with references.

**Independent Test**: With seeded plan materials, calling `/api/sessions` then `/api/chat` returns `answerType=Grounded` and non-empty `references` for in-scope questions.

### Implementation (US1)

- [X] T034 [US1] Define the single Chat use case contracts in `src/backend/HealthPlanChat.Core/UseCases/Chat/` (Request/Response/Boundary/Interactor)
- [X] T035 [US1] Extend `IChatSessionStore` contract in `src/backend/HealthPlanChat.Core/ExternalInterfaces/IChatSessionStore.cs` to support session creation + message history retrieval/append (so `/api/sessions` can be a thin endpoint)
- [X] T036 [US1] Implement `ChatInteractor` in `src/backend/HealthPlanChat.Core/UseCases/Chat/ChatInteractor.cs` (loads session history, retrieves materials, invokes `IChatAgent`, appends assistant message)
- [X] T037 [US1] Ensure `ChatInteractor` always returns explicit `answerType` + `references` (no separate chained use cases)
- [X] T038 [P] [US1] Add presenter for session creation in `src/backend/HealthPlanChat.WebApi/Presenters/CreateSessionPresenter.cs` (thin wrapper over `IChatSessionStore.CreateSession`)
- [X] T039 [P] [US1] Add presenter for chat responses in `src/backend/HealthPlanChat.WebApi/Presenters/ChatPresenter.cs`
- [X] T040 [US1] Map endpoints per OpenAPI in `src/backend/HealthPlanChat.WebApi/Endpoints/ChatEndpoints.cs` (`POST /api/sessions` uses session store directly, `POST /api/chat` calls `ChatInteractor`)
- [X] T041 [US1] Implement prompt construction (grounded answers + citations) in `src/backend/HealthPlanChat.Infrastructure.Prompting/PromptBuilder.cs`
- [X] T042 [US1] Wire Azure implementations behind interfaces in `src/backend/HealthPlanChat.Bootstrapper/ServiceCollectionExtensions.cs`
- [X] T043 [US1] Add minimal runtime configuration + health checks in `src/backend/HealthPlanChat.WebApi/Program.cs` (validate required Azure settings on startup; include `/healthz`)
- [X] T044 [P] [US1] Add safe request/response logging filters in `src/backend/HealthPlanChat.WebApi/Middleware/` to avoid logging prompt/user content
- [X] T045 [US1] Add "sync plan materials" workflow step in `.github/workflows/app.yml`: use `az storage blob sync` to upload only changed files from `data/plan-materials/*.json` to Blob (avoids redundant uploads; Search indexer auto-triggers on blob changes)
- [X] T046 [P] [US1] Implement minimal chat UI page in `src/frontend/HealthPlanChat.Web/Pages/Chat.razor`
- [X] T047 [P] [US1] Implement chat state + session initialization in `src/frontend/HealthPlanChat.Web/Services/ChatSessionService.cs`
- [X] T048 [P] [US1] Render grounded references in UI in `src/frontend/HealthPlanChat.Web/Components/ReferencesList.razor`
- [X] T048a [US1] Add CORS configuration in `src/backend/HealthPlanChat.WebApi/Program.cs` (configurable allowed origins via `Cors:AllowedOrigins` for frontend-backend communication; required for SWA → App Service calls)
- [X] T048b [US1] Add API base URL configuration in `src/frontend/HealthPlanChat.Web/` (use `appsettings.json` + environment-specific override for `ApiBaseUrl`; update `ApiClient.cs` and `Program.cs` to consume setting)
- [X] T048c [US1] Add integration test validating Phase 3 independent test criteria in `src/backend/HealthPlanChat.Infrastructure.IntegrationTests/ChatEndpointsTests.cs` (POST /api/sessions → POST /api/chat returns answerType=Grounded with non-empty references using DI test doubles)
- [X] T048d [US1] Update Redis to use Microsoft Entra Authentication (managed identity) instead of access keys: add App Service as Redis User in `infra/terraform/redis.tf`, update `RedisChatSessionStore.cs` to use `DefaultAzureCredential`, update `RedisOptions.cs` to use `Endpoint` instead of `ConnectionString`, add `Redis__Endpoint` to App Service settings in `infra/terraform/appservice.tf`

**Checkpoint**: US1 works via API (and minimal UI) with grounded answers + references.

---

## Phase 4: User Story 2 — Handle Missing Answers Clearly (Priority: P2)

**Goal**: If materials don’t contain an answer, respond as clearly labeled general guidance.

**Independent Test**: Ask an out-of-scope question; response is `answerType=GeneralGuidance` and references are empty (or explicitly marked as none).

### Implementation (US2)

- [X] T049 [P] [US2] Add retrieval confidence/threshold policy in `src/backend/HealthPlanChat.Core/Domain/Retrieval/RetrievalPolicy.cs` (define confidence as: has at least `MinHits` AND top AI Search score >= `MinTopScore`; defaults + config keys documented, e.g., `Retrieval__MinHits`, `Retrieval__MinTopScore`)
- [X] T050 [US2] Update `ChatInteractor` to apply `RetrievalPolicy` and produce `answerType=GeneralGuidance` when retrieval confidence is below threshold in `src/backend/HealthPlanChat.Core/UseCases/Chat/ChatInteractor.cs`
- [X] T051 [US2] Update prompting to force explicit labeling for all responses in `src/backend/HealthPlanChat.Infrastructure.Prompting/PromptBuilder.cs`
- [X] T052 [US2] Ensure API response always returns `answerType` and `references` in `src/backend/HealthPlanChat.WebApi/Presenters/ChatPresenter.cs`
- [X] T053 [P] [US2] Update UI to show answer type badge in `src/frontend/HealthPlanChat.Web/Components/AnswerTypeBadge.razor`
- [X] T054 [P] [US2] Add UX copy for "general guidance" disclaimer in `src/frontend/HealthPlanChat.Web/Components/AnswerDisclaimer.razor`

**Checkpoint**: US2 behavior is reliable and non-misleading.

---

## Patch: Agent-Native RAG Refactor

**Purpose**: Refactor retrieval from manual (ChatInteractor queries AI Search) to agent-native (Agent Framework uses `AzureAISearchAgentTool` internally). This aligns with Agent Framework best practices where the agent handles retrieval as a tool, not the application.

**Why now**: The current architecture manually queries Azure AI Search in `ChatInteractor` and passes chunks to the agent. The Agent Framework pattern is for the agent to use built-in tools (like `AzureAISearchAgentTool`) to handle retrieval autonomously. Fixing this now avoids carrying incorrect architecture into remaining phases.

**Reference**: [Azure AI Search tool for agents](https://learn.microsoft.com/en-us/azure/ai-foundry/agents/how-to/tools/ai-search)

### Infrastructure

- [X] T067 [P] Create Foundry-to-AI-Search connection via Terraform in `infra/terraform/foundry.tf` (add connection resource linking Foundry project to Search service; output `search_connection_id` for agent configuration)
- [X] T068 [P] Add `Azure.AI.Projects.OpenAI` package to `src/backend/Directory.Packages.props` for `AzureAISearchAgentTool` and `AIProjectClient` support (pin version per NuGet policy)

### Backend Refactor

- [X] T069 Update `FoundryOptions.cs` to include `SearchConnectionId` and `SearchIndexName` configuration in `src/backend/HealthPlanChat.Infrastructure.AgentFramework/FoundryOptions.cs`
- [X] T070 Refactor `AgentFrameworkChatAgent.cs` to use `AzureAISearchAgentTool` with configured index connection; agent handles retrieval internally in `src/backend/HealthPlanChat.Infrastructure.AgentFramework/AgentFrameworkChatAgent.cs`
- [X] T071 Update `IChatAgent` interface to remove `retrievedChunks` parameter (agent handles retrieval internally) in `src/backend/HealthPlanChat.Core/ExternalInterfaces/IChatAgent.cs`
- [X] T072 Simplify `ChatInteractor` to remove `IPlanMaterialSearch` dependency and manual retrieval logic in `src/backend/HealthPlanChat.Core/UseCases/Chat/ChatInteractor.cs`
- [X] T073 Parse `UriCitationMessageAnnotation` from agent response to extract references (title, URL) in `src/backend/HealthPlanChat.Infrastructure.AgentFramework/AgentFrameworkChatAgent.cs`
- [X] T074 Move Grounded vs GeneralGuidance decision to agent prompt instructions (agent decides based on search results quality) in `src/backend/HealthPlanChat.Infrastructure.Prompting/PromptBuilder.cs`
- [X] T075 Update `ServiceCollectionExtensions.cs` DI wiring to remove `IPlanMaterialSearch` from `ChatInteractor` and configure agent with search tool in `src/backend/HealthPlanChat.Bootstrapper/ServiceCollectionExtensions.cs`

### Cleanup

- [X] T076 [P] Update integration tests to reflect new architecture (agent-native search, no manual chunk passing) in `src/backend/HealthPlanChat.Infrastructure.IntegrationTests/ChatEndpointsTests.cs`
- [X] T077 [P] Deprecate `RetrievalPolicy.cs` and related confidence threshold logic (agent handles grounding decisions) in `src/backend/HealthPlanChat.Core/Domain/Retrieval/`
- [X] T078 [P] Evaluate `HealthPlanChat.Infrastructure.Search` project — keep for index maintenance utilities or remove if fully replaced by agent tool

**Checkpoint**: Agent handles retrieval natively via `AzureAISearchAgentTool`. `ChatInteractor` no longer queries search directly. Existing US1/US2 tests still pass (answerType + references work as before).

---

## Deployment Support Tasks

**Purpose**: Additional infrastructure and tooling discovered during deployment debugging.

- [X] T079 [P] Add developer local debugging RBAC in `infra/terraform/redis.tf` and `infra/terraform/search.tf`: optional `developer_principal_id` variable grants Redis Data Owner and Search Index Data Contributor roles for local testing with real Azure resources
- [X] T080 [P] Create Search index setup script in `scripts/setup-search-index.ps1`: creates `plan-materials` index (vector + semantic config with **integrated vectorizer** for query-time embedding), data source (**managed identity** via `ResourceId` connection string), skillset (with Azure OpenAI embedding), and indexer via Search REST API (data-plane resources not supported by ARM/Terraform); supports `-Force` (recreate) and `-ResetIndexer` (reprocess existing blobs) flags
- [X] T081 [P] Add `.local.json` config file loading pattern in `src/backend/HealthPlanChat.WebApi/Program.cs`: loads `appsettings.{Environment}.local.json` (gitignored) for local development with real Azure resources
- [X] T082 [P] Fix Foundry endpoint format in `infra/terraform/appservice.tf`: use project URL format (`https://{name}.services.ai.azure.com/api/projects/{project}`) required by Persistent Agents API
- [X] T083 [P] Add `developer_principal_id` workflow input in `.github/workflows/infra.yml` for provisioning developer RBAC via pipeline
- [X] T084 [P] Fix `Foundry__SearchConnectionId` app setting in `infra/terraform/appservice.tf`: use connection `.name` (`ai-search`) not `.id` (full ARM resource ID) — agent expects connection name only

---

## Phase 5: User Story 3 — Comfortable UI for Demos and Daily Use (Priority: P3)

**Goal**: Clean UI with light/dark mode toggle that does not clear chat history.

**Independent Test**: Toggle theme; chat history remains visible and usable.

### Implementation (US3)

- [X] T055 [P] [US3] Add theme state service with persistence in `src/frontend/HealthPlanChat.Web/Services/ThemeService.cs`
- [X] T056 [P] [US3] Add theme toggle UI in `src/frontend/HealthPlanChat.Web/Components/ThemeToggle.razor`
- [X] T057 [P] [US3] Implement CSS variables/themes in `src/frontend/HealthPlanChat.Web/wwwroot/css/app.css`
- [X] T058 [US3] Ensure chat history remains intact across theme changes in `src/frontend/HealthPlanChat.Web/Pages/Chat.razor`
- [X] T059 [US3] Add "New chat" button that clears visible conversation in `src/frontend/HealthPlanChat.Web/Components/NewChatButton.razor`

**Checkpoint**: US3 demo-ready UI with theme switching.

---

## Phase 6: Polish & Cross-Cutting Concerns (Post-MVP)

**Purpose**: Infrastructure, deployment, and demo hardening. **These tasks are optional stretch goals** — the MVP is functional without them.

### Response Formatting (US1/US2 Polish)

- [X] T085 [P] Strip answer type labels (`**[GROUNDED]**`, `**[GENERAL GUIDANCE]**`) from response text after extraction in `src/backend/HealthPlanChat.Infrastructure.AgentFramework/AgentFrameworkChatAgent.cs` (label is already captured in `AnswerType`; raw marker should not appear in `AnswerText`)
- [X] T086 [P] Strip/replace citation markers (e.g., `【3:0†source】`) from response text in `src/backend/HealthPlanChat.Infrastructure.AgentFramework/AgentFrameworkChatAgent.cs` (citations are already captured in `References`; raw markers should not appear in `AnswerText`)
- [X] T087 [P] Add markdown rendering for assistant messages in `src/frontend/HealthPlanChat.Web/Components/MarkdownRenderer.razor` (render `**bold**`, `*italic*`, lists, etc. using a lightweight markdown parser like Markdig or simple regex for bold/italic only)
- [X] T088 Update `Chat.razor` to use `MarkdownRenderer` for assistant message content in `src/frontend/HealthPlanChat.Web/Pages/Chat.razor`
- [X] T089 Add unit tests for response text sanitization (label stripping, citation marker removal) in `src/backend/HealthPlanChat.Infrastructure.AgentFramework.UnitTests/` or existing test project

### Chat UX Polish (US3)

- [X] T090 [P] Auto-scroll messages container to bottom when new messages arrive or loading state changes in `src/frontend/HealthPlanChat.Web/Pages/Chat.razor` (use JS interop `scrollIntoView` or set `scrollTop` after render; scroll on: user sends message, assistant response received, loading indicator appears)
- [X] T091 [P] Style scrollbar for messages container in `src/frontend/HealthPlanChat.Web/wwwroot/css/app.css` (use `::-webkit-scrollbar` for Chromium/Safari with theme-aware colors; add `scrollbar-color` and `scrollbar-width` for Firefox; subtle rounded track/thumb matching theme)
- [X] T092 Add right padding to messages container in `src/frontend/HealthPlanChat.Web/wwwroot/css/app.css` to create spacing between user message bubbles and scrollbar

### Documentation & Configuration

- [X] T061 Validate Quickstart end-to-end and update `specs/001-health-plan-chat/quickstart.md` with final commands (include a short demo checklist using `data/demo-questions.json` for SC-001 spot-checks)
- [X] T062 Add Core unit tests for `ChatInteractor` (labeling: Grounded vs GeneralGuidance; references shape; deterministic behavior) in `src/backend/HealthPlanChat.Core.UnitTests/UseCases/Chat/ChatInteractorTests.cs`

---

## Patch: App Service Cold-Start Resilience

**Purpose**: Mitigate long cold-start times causing 5xx errors surfaced to the frontend by upgrading the App Service SKU and adding client-side retry logic.

### Infrastructure

- [X] T093 Upgrade App Service Plan from F1 (Free) to B1 (Basic) in `infra/terraform/appservice.tf` (change SKU name/tier)

### Frontend

- [X] T095 Add exponential back-off retry logic in `src/frontend/HealthPlanChat.Web/Services/ApiClient.cs` for transient 5xx/network errors during `SendMessageAsync` (retry up to ~45s with exponential delay; handle 502/503/504 and `HttpRequestException`)

**Checkpoint**: Frontend tolerates backend cold starts without hard 500 errors.

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies
- **Foundational (Phase 2)**: Depends on Setup; blocks all user stories
- **User Stories (Phase 3-4)**: Depend on Foundational
- **Patch (Agent-Native RAG)**: Depends on Phase 4; should be done before Phase 5
- **User Story 3 (Phase 5)**: Depends on Foundational; primarily frontend (can start after Patch)
- **Polish (Phase 6)**: Depends on at least US1 (and is typically done after the app is runnable)

### User Story Dependencies

- **US1 (P1)**: Starts after Foundational
- **US2 (P2)**: Starts after Foundational; builds on the same `/api/chat` flow
- **Patch**: Starts after US2; refactors agent integration before further feature work
- **US3 (P3)**: Starts after Patch; primarily frontend

### Suggested Completion Graph

- Setup (Phase 1) → Foundational (Phase 2) → US1 (Phase 3) → US2 (Phase 4) → **Patch** → US3 (Phase 5) → Polish (Phase 6)

### Parallel Opportunities

- Setup tasks marked [P] can run in parallel
- Foundational Azure adapters (T029–T032) can run in parallel
- US1 frontend tasks (T046–T048) can run in parallel with backend presenters/prompting (T038–T041)
- Terraform/IaC tasks (T015–T026) can be parallelized

---

## Parallel Examples

### User Story 1

- T038 [P] [US1] `src/backend/HealthPlanChat.WebApi/Presenters/CreateSessionPresenter.cs`
- T039 [P] [US1] `src/backend/HealthPlanChat.WebApi/Presenters/ChatPresenter.cs`
- T046 [P] [US1] `src/frontend/HealthPlanChat.Web/Pages/Chat.razor`

### User Story 2

- T049 [P] [US2] `src/backend/HealthPlanChat.Core/Domain/Retrieval/RetrievalPolicy.cs`
- T053 [P] [US2] `src/frontend/HealthPlanChat.Web/Components/AnswerTypeBadge.razor`
- T054 [P] [US2] `src/frontend/HealthPlanChat.Web/Components/AnswerDisclaimer.razor`

### User Story 3

- T055 [P] [US3] `src/frontend/HealthPlanChat.Web/Services/ThemeService.cs`
- T057 [P] [US3] `src/frontend/HealthPlanChat.Web/wwwroot/css/app.css`
- T056 [P] [US3] `src/frontend/HealthPlanChat.Web/Components/ThemeToggle.razor`

---

## Implementation Strategy

### MVP First (US1 Only)

1. Complete Phase 1 (Setup)
2. Complete Phase 2 (Foundational)
3. Complete Phase 3 (US1)
4. Validate via the US1 independent test criteria

### Incremental Delivery

- Add US2 to harden correctness when plan materials are missing
- Add US3 to improve demo comfort and usability

## Notes

- [P] tasks should be implemented in different files to avoid conflicts.
- Keep privileged operations server-side; keep the client anonymous.
- Always return explicit `answerType` and avoid leaking sensitive data in logs.

- `src/backend/HealthPlanChat.Infrastructure.Prompting.UnitTests/` may need to be added as a new test project if it doesn’t exist yet.
