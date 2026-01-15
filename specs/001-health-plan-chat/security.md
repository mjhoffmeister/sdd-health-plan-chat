# Security Review: Health Plan Chat MVP

This document captures the lightweight threat model and abuse cases required by the project constitution. It is intentionally pragmatic and demo-oriented.

## Scope

- Backend: minimal APIs (`/api/sessions`, `/api/chat`), prompt construction, retrieval, Azure integrations
- Frontend: Blazor WASM (anonymous client)
- External services: Azure AI Foundry, Azure AI Search, Azure Blob Storage, Azure Managed Redis (Redis Enterprise via AzAPI `Microsoft.Cache/redisEnterprise@2025-04-01` + required `Microsoft.Cache/redisEnterprise/databases@2025-04-01`)

## Trust Boundaries

- **Client → API**: untrusted input
- **API → Azure services**: privileged calls (managed identity / federated credentials)
- **Storage/Search content → Prompt**: untrusted content (documents can contain adversarial text)

## Sensitive Data Policy

- No secrets committed to repo.
- Do not log: prompts, user messages, retrieved chunks verbatim, connection strings/keys, auth tokens.
- OK to log: request id, duration, answer type, counts (e.g., retrieved chunks count), high-level error categories.

## Key Assets

- Session history (chat messages) stored in Redis
- Plan materials (synthetic JSON) stored in repo and copied to Blob
- System prompt / grounding prompt logic
- Azure identities and permissions

## Primary Threats & Mitigations

### 1) Prompt injection via plan materials (retrieved chunks)
**Threat**: plan content includes instructions to exfiltrate secrets or override system intent.

**Mitigations**:
- Treat retrieved text as untrusted. Prompt must explicitly instruct the model to ignore instructions in retrieved content.
- Never include secrets in prompt.
- Restrict responses to the question and to grounded content when labeled `Grounded`.

### 2) Prompt injection via user input
**Threat**: user attempts to override system rules or request secrets.

**Mitigations**:
- Strong system prompt: refuse unsafe requests; do not reveal system prompt.
- Safe logging: do not log user text.
- Explicit answer typing: `Grounded` vs `GeneralGuidance`.

### 3) Data exfiltration through logs
**Threat**: logs contain user text, prompts, retrieved chunks, or credentials.

**Mitigations**:
- Centralized logging middleware that redacts/omits content.
- Unit tests around log filters (where practical).
- Review log statements before shipping.

### 4) Session fixation / session id guessing
**Threat**: attacker guesses session IDs and reads/writes another session.

**Mitigations**:
- Use cryptographically strong, unguessable session IDs.
- TTL expiry in Redis.
- Do not expose session history via any endpoint other than continuing the same session flow.

### 5) Over-permissive Azure IAM
**Threat**: managed identity can access unintended resources.

**Mitigations**:
- Least privilege role assignments.
- Separate roles per service (Search, Foundry).
- Avoid broad subscription-level roles.

**App Service Managed Identity — Authorized Roles:**

| Target Resource | Role | Justification |
|-----------------|------|---------------|
| AI Search | Search Index Data Reader | Runtime query of plan-materials index |
| AI Foundry | Cognitive Services User | LLM inference (chat + embeddings) |

**Excluded (not assigned):**

| Target Resource | Role | Reason |
|-----------------|------|--------|
| Blob Storage | Storage Blob Data Reader | App queries Search index (which contains content); blobs are indexed by Search service, not accessed by app |

**Search Service Managed Identity — Authorized Roles (for indexing):**

| Target Resource | Role | Role Definition ID | Justification |
|-----------------|------|-------------------|---------------|
| Blob Storage | Storage Blob Data Reader | `2a2b9908-6ea1-4ae2-8e65-a410df84e7d1` | Read plan material blobs during indexing |
| AI Foundry | Cognitive Services User | `a97b65f3-24c7-4388-baec-2e87135dc908` | Generate embeddings via AzureOpenAIEmbedding skillset |

**GitHub Actions WIF Identity — Authorized Roles (for deployment):**

| Target Resource | Role | Role Definition ID | Justification |
|-----------------|------|-------------------|---------------|
| Blob Storage | Storage Blob Data Contributor | `ba92f5b4-2d11-453d-a403-e96b0029c9fe` | Upload plan materials during CI/CD |

### 6) Excessive resource usage / cost (abuse)
**Threat**: repeated requests cause high token usage and service cost.

**Mitigations**:
- Request throttling / basic rate limiting (future if needed).
- Message count caps per session.
- Retrieval caps (top-k).

## Abuse Cases Checklist

- User asks for secrets or internal configuration → system refuses; logs remain clean.
- Plan material contains malicious instructions → prompt tells model to ignore; response remains grounded.
- User provides extremely long input → request validation and/or truncation strategy.
- Repeated calls from one client → rate limiting strategy (if needed for demo).

## Review Gate

Before implementing new external integrations or widening IAM permissions:

- Update this document with new assets, threats, and mitigations.
- Confirm logging/redaction still complies with the “no sensitive data in logs” policy.
