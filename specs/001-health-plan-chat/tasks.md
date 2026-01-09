# Tasks: Health Plan Chat MVP

**Input**: Design documents from `/specs/001-health-plan-chat/`
**Prerequisites**: plan.md (required), spec.md (required), research.md, data-model.md, contracts/

**Tests**: Automated test tasks are **not included** (the spec does not explicitly require TDD). Add them later if you want a test-first workflow.

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format

- `- [ ] T### [P?] [US?] Description with file path`
- **[P]**: Can run in parallel (different files, no dependencies)
- **[US#]**: User story label (US1, US2, US3)

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Initialize repo structure and baseline projects.

- [ ] T001 Create top-level folders `src/`, `infra/terraform/`, `data/plan-materials/`
- [ ] T002 Scaffold backend solution in `src/backend/HealthPlanChat.sln` (Core/Infrastructure/Bootstrapper/WebApi + test projects)
- [ ] T003 [P] Delete generated placeholders (e.g., `src/backend/**/Class1.cs`, `src/backend/**/UnitTest1.cs`)
- [ ] T004 [P] Scaffold Blazor WebAssembly app in `src/frontend/HealthPlanChat.Web/`
- [ ] T005 [P] Add local dev settings template in `src/backend/HealthPlanChat.WebApi/appsettings.Development.json` (no secrets)
- [ ] T006 [P] Add repo-level ignore and tooling files (e.g., `.gitignore`, `.editorconfig`) aligned with .NET + Blazor
- [ ] T007 [P] Add synthetic plan JSON seed files under `data/plan-materials/` (HMO/PPO/EPO examples for Contoso Health)

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Cross-cutting backend/frontend foundations that all user stories rely on.

- [ ] T008 Create shared API contracts (DTOs) in `src/backend/HealthPlanChat.Core/UseCases/Contracts/`
- [ ] T009 Create domain models in `src/backend/HealthPlanChat.Core/Domain/Chat/` (ChatSession, ChatMessage, AnswerType, Reference)
- [ ] T010 Create external interfaces in `src/backend/HealthPlanChat.Core/ExternalInterfaces/` (IChatSessionStore, IPlanMaterialSearch, IChatCompletionClient)
- [ ] T011 Create configuration options in `src/backend/HealthPlanChat.Core/ExternalInterfaces/Options/` (RedisOptions, SearchOptions, FoundryOptions)
- [ ] T012 Implement minimal API host skeleton in `src/backend/HealthPlanChat.WebApi/Program.cs` (healthz + routing)
- [ ] T013 Implement structured logging + safe error handling middleware in `src/backend/HealthPlanChat.WebApi/Middleware/`
- [ ] T014 Implement Bootstrapper DI registration in `src/backend/HealthPlanChat.Bootstrapper/ServiceCollectionExtensions.cs`
- [ ] T015 [P] Implement in-memory session store for local dev in `src/backend/HealthPlanChat.Infrastructure.Local/InMemoryChatSessionStore.cs`
- [ ] T016 [P] Implement in-memory plan material search for local dev in `src/backend/HealthPlanChat.Infrastructure.Local/InMemoryPlanMaterialSearch.cs`
- [ ] T017 Add JSON plan-material loader (repo `data/plan-materials/`) in `src/backend/HealthPlanChat.Infrastructure.Local/PlanMaterialLoader.cs`
- [ ] T018 Wire local-dev infrastructure into DI in `src/backend/HealthPlanChat.Bootstrapper/ServiceCollectionExtensions.cs`
- [ ] T019 Add frontend HTTP client wiring in `src/frontend/HealthPlanChat.Web/Program.cs` and `src/frontend/HealthPlanChat.Web/Services/ApiClient.cs`

**Checkpoint**: Foundation ready — user story work can begin.

---

## Phase 3: User Story 1 — Ask Plan Questions (Priority: P1) 🎯 MVP

**Goal**: Users can ask a question and receive a grounded answer with references.

**Independent Test**: With seeded plan materials, calling `/api/sessions` then `/api/chat` returns `answerType=Grounded` and non-empty `references` for in-scope questions.

### Implementation (US1)

- [ ] T020 [US1] Define use case contracts in `src/backend/HealthPlanChat.Core/UseCases/SendChatMessage/` (Request/Response/Boundary/Interactor)
- [ ] T021 [US1] Define use case contracts in `src/backend/HealthPlanChat.Core/UseCases/CreateSession/` (Request/Response/Boundary/Interactor)
- [ ] T022 [US1] Implement `CreateSessionInteractor` in `src/backend/HealthPlanChat.Core/UseCases/CreateSession/CreateSessionInteractor.cs`
- [ ] T023 [US1] Implement `SendChatMessageInteractor` in `src/backend/HealthPlanChat.Core/UseCases/SendChatMessage/SendChatMessageInteractor.cs`
- [ ] T024 [P] [US1] Add presenter for session creation in `src/backend/HealthPlanChat.WebApi/Presenters/CreateSessionPresenter.cs`
- [ ] T025 [P] [US1] Add presenter for chat responses in `src/backend/HealthPlanChat.WebApi/Presenters/SendChatMessagePresenter.cs`
- [ ] T026 [US1] Map endpoints per OpenAPI in `src/backend/HealthPlanChat.WebApi/Endpoints/ChatEndpoints.cs` (`POST /api/sessions`, `POST /api/chat`)
- [ ] T027 [US1] Implement prompt construction (grounded answers + citations) in `src/backend/HealthPlanChat.Infrastructure.Prompting/PromptBuilder.cs`
- [ ] T028 [P] [US1] Implement Azure AI Foundry chat client wrapper in `src/backend/HealthPlanChat.Infrastructure.Foundry/FoundryChatCompletionClient.cs`
- [ ] T029 [P] [US1] Implement Azure AI Search query adapter in `src/backend/HealthPlanChat.Infrastructure.Search/AzureAiSearchPlanMaterialSearch.cs`
- [ ] T030 [P] [US1] Implement Azure Blob plan-material ingestion (upload + indexing trigger) in `src/backend/HealthPlanChat.Infrastructure.Storage/PlanMaterialBlobPublisher.cs`
- [ ] T031 [US1] Wire Azure implementations behind interfaces in `src/backend/HealthPlanChat.Bootstrapper/ServiceCollectionExtensions.cs`
- [ ] T032 [US1] Implement minimal chat UI page in `src/frontend/HealthPlanChat.Web/Pages/Chat.razor`
- [ ] T033 [US1] Implement chat state + session initialization in `src/frontend/HealthPlanChat.Web/Services/ChatSessionService.cs`
- [ ] T034 [US1] Render grounded references in UI in `src/frontend/HealthPlanChat.Web/Components/ReferencesList.razor`

**Checkpoint**: US1 works via API (and minimal UI) with grounded answers + references.

---

## Phase 4: User Story 2 — Handle Missing Answers Clearly (Priority: P2)

**Goal**: If materials don’t contain an answer, respond as clearly labeled general guidance.

**Independent Test**: Ask an out-of-scope question; response is `answerType=GeneralGuidance` and references are empty (or explicitly marked as none).

### Implementation (US2)

- [ ] T035 [US2] Add retrieval confidence/threshold policy in `src/backend/HealthPlanChat.Core/Domain/Retrieval/RetrievalPolicy.cs`
- [ ] T036 [US2] Update `SendChatMessageInteractor` fallback path in `src/backend/HealthPlanChat.Core/UseCases/SendChatMessage/SendChatMessageInteractor.cs`
- [ ] T037 [US2] Update prompting to force explicit labeling for all responses in `src/backend/HealthPlanChat.Infrastructure.Prompting/PromptBuilder.cs`
- [ ] T038 [US2] Ensure API response always returns `answerType` and `references` in `src/backend/HealthPlanChat.WebApi/Presenters/SendChatMessagePresenter.cs`
- [ ] T039 [US2] Update UI to show answer type badge in `src/frontend/HealthPlanChat.Web/Components/AnswerTypeBadge.razor`
- [ ] T040 [US2] Add UX copy for “general guidance” disclaimer in `src/frontend/HealthPlanChat.Web/Components/AnswerDisclaimer.razor`

**Checkpoint**: US2 behavior is reliable and non-misleading.

---

## Phase 5: User Story 3 — Comfortable UI for Demos and Daily Use (Priority: P3)

**Goal**: Clean UI with light/dark mode toggle that does not clear chat history.

**Independent Test**: Toggle theme; chat history remains visible and usable.

### Implementation (US3)

- [ ] T041 [US3] Add theme state service with persistence in `src/frontend/HealthPlanChat.Web/Services/ThemeService.cs`
- [ ] T042 [US3] Add theme toggle UI in `src/frontend/HealthPlanChat.Web/Components/ThemeToggle.razor`
- [ ] T043 [US3] Implement CSS variables/themes in `src/frontend/HealthPlanChat.Web/wwwroot/css/app.css`
- [ ] T044 [US3] Ensure chat history remains intact across theme changes in `src/frontend/HealthPlanChat.Web/Pages/Chat.razor`
- [ ] T045 [US3] Add “New chat” button that clears visible conversation in `src/frontend/HealthPlanChat.Web/Components/NewChatButton.razor`

**Checkpoint**: US3 demo-ready UI with theme switching.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Infrastructure, deployment, and demo hardening.

- [ ] T046 [P] Add Terraform provider + backend skeleton in `infra/terraform/providers.tf`
- [ ] T047 [P] Add Terraform resources for App Service + plan in `infra/terraform/appservice.tf`
- [ ] T048 [P] Add Terraform resources for Static Web Apps in `infra/terraform/swa.tf`
- [ ] T049 [P] Add Terraform resources for Azure AI Search + index in `infra/terraform/search.tf`
- [ ] T050 [P] Add Terraform resources for Storage account + container in `infra/terraform/storage.tf`
- [ ] T051 [P] Add Terraform resources for Azure Managed Redis in `infra/terraform/redis.tf`
- [ ] T052 Add GitHub Actions workflow for infra deploy (WIF) in `.github/workflows/infra.yml`
- [ ] T053 Add GitHub Actions workflow for app build/deploy in `.github/workflows/app.yml`
- [ ] T054 Add runtime configuration docs in `specs/001-health-plan-chat/quickstart.md` (Azure env vars and expected settings)
- [ ] T055 Validate Quickstart end-to-end and update `specs/001-health-plan-chat/quickstart.md` with final commands

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies
- **Foundational (Phase 2)**: Depends on Setup; blocks all user stories
- **User Stories (Phase 3+)**: Depend on Foundational
- **Polish (Phase 6)**: Depends on whichever stories you intend to deploy

### User Story Dependencies

- **US1 (P1)**: Starts after Foundational
- **US2 (P2)**: Starts after Foundational; builds on the same `/api/chat` flow
- **US3 (P3)**: Starts after Foundational; primarily frontend

### Parallel Opportunities

- Setup tasks marked [P] can run in parallel
- Foundational local adapters (T015, T016) can run in parallel
- Azure infrastructure adapters (T028–T030) can be parallelized by provider
- Terraform resource files (T046–T051) can be parallelized

---

## Parallel Examples

### User Story 1

- T028 [P] [US1] `src/backend/HealthPlanChat.Infrastructure.Foundry/FoundryChatCompletionClient.cs`
- T029 [P] [US1] `src/backend/HealthPlanChat.Infrastructure.Search/AzureAiSearchPlanMaterialSearch.cs`
- T030 [P] [US1] `src/backend/HealthPlanChat.Infrastructure.Storage/PlanMaterialBlobPublisher.cs`

### User Story 3

- T041 [US3] `src/frontend/HealthPlanChat.Web/Services/ThemeService.cs`
- T043 [US3] `src/frontend/HealthPlanChat.Web/wwwroot/css/app.css`
- T042 [US3] `src/frontend/HealthPlanChat.Web/Components/ThemeToggle.razor`

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
