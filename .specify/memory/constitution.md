<!--
Sync Impact Report

- Version change: N/A (template) -> 1.0.0
- Modified principles: N/A (initial adoption)
- Added sections: Core Principles, Cloud & Identity Constraints,
	Development Workflow & Quality Gates, Governance
- Removed sections: N/A (initial adoption)
- Templates requiring updates:
	- ✅ updated: .specify/templates/plan-template.md
	- ✅ no change needed: .specify/templates/spec-template.md
	- ✅ no change needed: .specify/templates/tasks-template.md
	- ✅ no change needed: .specify/templates/checklist-template.md
	- ✅ no change needed: .specify/templates/agent-file-template.md
- Follow-up TODOs: none
-->

# Health Plan Chat Constitution

## Core Principles

### I. Security and Privacy First (NON-NEGOTIABLE)
The system MUST treat all inputs as untrusted and validate at boundaries.
Sensitive data MUST be minimized, protected in transit and at rest, and never
logged in plaintext. Security reviews MUST be done early (threat modeling,
abuse cases) and repeated when adding new external integrations.

Rationale: This is a health-plan domain demo; safety and trust are foundational.

### II. Simplicity and Demo Reliability
We MUST prefer the simplest design that satisfies the spec and is easy to demo.
Add components, services, or abstractions ONLY when they materially reduce risk
or enable a requirement; complexity MUST be justified in the plan.

Rationale: Demos fail on unnecessary moving parts.

### III. Testability and Determinism
Core logic MUST be testable without network access and without real cloud
dependencies. Time, randomness, and external calls MUST be injectable or
abstracted behind interfaces. Behavior MUST be deterministic for the same
inputs (except where explicitly designed otherwise).

Rationale: Testability enables confident iteration and repeatable demos.

### IV. Clear Separation of Responsibilities
UI and server responsibilities MUST be clearly separated. Privileged
operations (secrets, tokens, external API calls, data access) MUST run on the
server side. The client MUST NOT require secrets to function.

Rationale: Prevents secret leakage and keeps security boundaries crisp.

### V. Maintainable Engineering Practices
Code MUST optimize for readability and changeability.

- Prefer clear, boring code over cleverness.
- Apply SOLID when it improves clarity; avoid over-abstraction.
- Apply DRY thoughtfully; duplication is acceptable when it is clearer.
- Prefer minimal ceremony in request handling and endpoint design.

Rationale: The repo is meant to be reused and taught from.

## Cloud and Identity Constraints
Cloud hosting and managed services SHOULD use Azure.

- Identity SHOULD use managed identity or federated credentials.
- No secrets (keys, connection strings, tokens) may be committed to the repo.
- Local development MAY use environment variables or user-secrets.

## Development Workflow and Quality Gates

- Every change MUST be traceable to a spec/plan item.
- Pull requests MUST include a brief rationale and test/validation notes.
- New behavior MUST include tests when practical; critical paths MUST be
	covered by automated tests.
- Errors MUST be handled deliberately (no silent failures).
- Logging MUST be structured and avoid sensitive data.

## Governance
This constitution is the top-level set of non-negotiable constraints for the
project. If another document conflicts with this constitution, the constitution
wins.

Amendments:

- Propose changes via pull request.
- The PR MUST include: what changes, why it changes, and expected impacts.
- If the change requires migration, the PR MUST include a migration plan.

Versioning policy:

- MAJOR: incompatible governance change or principle removal/redefinition.
- MINOR: new principle/section or materially expanded constraints.
- PATCH: clarifications, wording fixes, or non-semantic refinements.

Compliance review expectations:

- Plans MUST include a "Constitution Check" section that calls out any
	violations and the justification.
- Reviewers MUST block merges that violate NON-NEGOTIABLE items unless the
	constitution is amended first.

**Version**: 1.0.0 | **Ratified**: 2026-01-06 | **Last Amended**: 2026-01-06
