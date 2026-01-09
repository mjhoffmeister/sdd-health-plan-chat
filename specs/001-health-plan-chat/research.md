# Research: Health Plan Chat MVP

This document resolves technical unknowns and records key implementation decisions.

## Decisions

### 1) Architecture style: Clean Architecture (Core / Infrastructure / Presentation)
- Decision: Use a Clean Architecture-inspired project split for the backend, keeping domain/use cases testable without Azure dependencies.
- Rationale: Aligns with the constitution’s testability and separation-of-responsibilities requirements.
- Alternatives considered:
  - “Single project minimal API”: simpler initially, but mixes concerns and makes core logic harder to test.

### 2) Retrieval approach: Azure AI Search (vector + keyword) over synthetic plan documents
- Decision: Index synthetic plan JSON into Azure AI Search and retrieve top chunks for grounding.
- Rationale: Deterministic retrieval surface; good demos; clear citations.
- Alternatives considered:
  - Local in-memory search: simpler, but diverges from the Azure-focused tech decisions.

### 3) Embeddings + chat models: Azure AI Foundry
- Decision: Use `text-embedding-3-small` (global) for embeddings and `gpt-5-mini` (global) for chat completions.
- Rationale: Matches prompt tech decisions; balances quality/cost for demo.
- Alternatives considered:
  - Larger models: higher cost and latency without demo value.

### 4) Chat history persistence: Azure Managed Redis (session scoped)
- Decision: Store chat history in Azure Managed Redis with a TTL per session.
- Rationale: Enables server-side session continuity while keeping the client anonymous; avoids database overhead.
- Alternatives considered:
  - In-memory cache: simplest but not durable across restarts.
  - Database: adds complexity for demo.

### 5) Identity: Managed Identity (runtime) + Workload Identity Federation (CI/CD)
- Decision: Use managed identity for service-to-service calls; use GitHub Actions WIF for deployments.
- Rationale: Satisfies constitution constraint: no secrets committed.
- Alternatives considered:
  - Service principals with secrets: violates “no secrets”.

### 6) Grounding & labeling behavior
- Decision: All assistant responses must include an explicit label: `Grounded` or `General guidance`.
- Rationale: Directly implements FR-003/FR-005 and reduces demo ambiguity.
- Alternatives considered:
  - Implicit behavior: too easy to misunderstand.

## Open Questions (Resolved)

- Q: Where does plan content live?
  - Decision: Source-of-truth is repo JSON (for reproducibility). In Azure, copy to Blob Storage for indexing.

- Q: How is session maintained with anonymous users?
  - Decision: Backend issues a session id (cookie or returned token); Redis key = session id; TTL expires sessions.
