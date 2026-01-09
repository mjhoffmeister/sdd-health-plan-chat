# Data Model: Health Plan Chat MVP

This model describes the conceptual entities for the MVP. Implementation may use DTOs and infrastructure-specific records, but core invariants should remain in the backend Core layer.

## Entities

### ChatSession
- Fields:
  - `ChatSessionId` (string/uuid)
  - `CreatedAtUtc` (datetime)
  - `LastUpdatedAtUtc` (datetime)
  - `ExpiresAtUtc` (datetime)
  - `Messages` (ordered list of ChatMessage)
- Relationships:
  - 1 ChatSession → many ChatMessage
- Validation:
  - `ExpiresAtUtc` must be > `CreatedAtUtc`
  - max message count (configurable) to control cost/latency

### ChatMessage
- Fields:
  - `ChatMessageId` (string/uuid)
  - `ChatSessionId`
  - `Role` (User | Assistant | System)
  - `Text` (string)
  - `CreatedAtUtc` (datetime)
- Validation:
  - non-empty `Text`
  - max length (configurable)

### PlanDocument
- Fields:
  - `PlanDocumentId`
  - `PlanName` (e.g., "Contoso Health PPO Silver")
  - `PlanType` (HMO | PPO | EPO)
  - `Year` (int)
  - `SourceUri` (blob uri or repo-relative path)
  - `Content` (structured JSON)

### IndexedChunk (search index representation)
- Fields:
  - `ChunkId`
  - `PlanDocumentId`
  - `Section` / `Heading`
  - `Text` (chunk text)
  - `PageOrAnchor` (string)
  - `Vector` (embedding)

### Answer
- Fields:
  - `AnswerText`
  - `AnswerType` (Grounded | GeneralGuidance)
  - `References` (0..n Reference)

### Reference
- Fields:
  - `PlanDocumentId`
  - `ChunkId` or `PageOrAnchor`
  - `Quote` (short snippet)

## State Transitions

### ChatSession lifecycle
- `New` → `Active` when first message is stored
- `Active` → `Expired` after TTL elapses (Redis eviction)

## Notes

- Session is server-side only; client stays anonymous.
- Redis storage format should be deterministic and versioned (e.g., JSON schema version field) to keep demo reproducible.
