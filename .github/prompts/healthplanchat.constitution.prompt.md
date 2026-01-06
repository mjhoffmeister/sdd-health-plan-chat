---
agent: speckit.constitution
name: healthplanchat-constitution
description: Generate a constraints-level constitution for Health Plan Chat
---

Create principles focused on maintainability, testability, security, and a
user-centric experience. Keep them durable and at the constraints level.

# Code guidelines

1. Shift security left: threat model early; validate inputs; log safely.
1. Prefer simplicity: minimize moving parts, especially for demo reliability.
1. Design for testability: seams for dependency injection; deterministic code.
1. Favor clear, boring code over cleverness; optimize only with evidence.
1. Apply SOLID where it improves clarity; avoid over-abstraction.
1. Apply DRY thoughtfully; prefer duplication over premature indirection.
1. Keep UI and server responsibilities clearly separated.
1. Never expose secrets to clients; keep privileged operations server-side.
1. Prefer minimal ceremony for HTTP endpoints and request handling.

# Cloud guidelines

1. Use Azure for cloud services.
1. Prefer managed identity and federated auth over long-lived secrets.
1. No secrets committed to the repo.
1. Allow secure local development via environment variables or user-secrets.