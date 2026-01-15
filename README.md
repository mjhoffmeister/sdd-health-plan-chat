# Using Spec Kit and GitHub Copilot to Build a Health Plan Chat App

This repository is a staged demo of spec-driven development (SDD) using
**Spec Kit** and **GitHub Copilot**. It shows how written product intent
(constitution, spec, plan) can guide implementation so AI-assisted development
stays aligned with requirements, quality, and testability.

It’s organized as a sequence you can present end-to-end, or jump ahead during a
live session while still keeping completed, realistic artifacts for earlier
phases.

## Demo Phases

**init → constitution → spec → plan → tasks → implement**

Each phase produces durable artifacts that constrain and guide the next phase.

## Init Phase (Phase 00)

Goal: generate the Spec Kit files so you can begin the constitution phase.

### 1) Install (or upgrade) the `specify` CLI

Install:

`uv tool install specify-cli --from git+https://github.com/github/spec-kit.git`

Upgrade:

`uv tool install specify-cli --force --from git+https://github.com/github/spec-kit.git`

### 2) Initialize Spec Kit in this repo

Run from PowerShell (`pwsh`):

`specify init --here --script ps --ai copilot`

## Constitution Phase (Phase 01)

Goal: create and ratify the project constitution in
`.specify/memory/constitution.md`.

### Generate the constitution

This repo includes a reusable Copilot prompt for constitution generation.
In Copilot Chat, run:

`/healthplanchat-constitution`

Then review and refine the output in `.specify/memory/constitution.md`.

## Spec Phase (Phase 02)

Goal: create the first feature specification in
`specs/<feature-branch>/spec.md`.

Note: Spec Kit scripts write to `specs/<feature-branch>/...` based on your
current git branch name. If you are on `main`, switch to your feature branch
(e.g., `001-health-plan-chat`) before generating spec/plan/tasks artifacts. If
you are not using git, set `SPECIFY_FEATURE` to the feature folder name.

### Generate the spec

This repo includes a reusable Copilot prompt for spec generation. In Copilot
Chat, run:

`/healthplanchat-specify`

## Plan Phase (Phase 03)

Goal: create the first implementation plan in `specs/<feature-branch>/plan.md`.

### Generate the plan

This repo includes a reusable Copilot prompt for plan generation. In Copilot
Chat, run:

`/healthplanchat-plan`

## Tasks Phase (Phase 04)

Goal: turn the plan into actionable implementation tasks in
`specs/<feature-branch>/tasks.md`.
Note: If you are on `main`, switch to your feature branch (e.g.,
`001-health-plan-chat`) before running `/speckit.tasks`, or set `SPECIFY_FEATURE`
to the feature folder name. The default branch may already include a sample
generated tasks file.

### Generate the tasks

Use Spec Kit’s built-in tasks agent (no additional repo-specific prompt is
needed):

`/speckit.tasks`

Optional: if you want to create GitHub issues from tasks, run:

`/speckit.taskstoissues`

## Implement Phase (Phase 05)

Goal: implement the feature by executing tasks in `specs/<feature-branch>/tasks.md`.

### Start implementation: Phase 1 (Setup)

1) Create a working branch for coding (recommended):

`git switch -c impl/setup`

2) Implement the Setup tasks (Phase 1) from `specs/001-health-plan-chat/tasks.md`:

- T001–T007

3) Check in your Setup implementation.

### Pre-flight

- Ensure you are on your feature branch (e.g., `001-health-plan-chat`) so Spec Kit writes to the right `specs/<feature-branch>/...` folder, or set `SPECIFY_FEATURE` to the feature folder name.
- Confirm you have a generated tasks file at `specs/<feature-branch>/tasks.md`.

Optional quality gate (recommended before writing code):

`/speckit.analyze`

If it reports CRITICAL issues, resolve those before continuing.

### Implement tasks

Use Spec Kit’s built-in implement agent:

`/speckit.implement`

In your prompt, specify which task IDs to implement (for example: “Implement T002–T006”). For smoother progress, implement in small batches and keep the working tree green (build/tests passing) between batches.

Tip: tasks marked with `[P]` are intended to be parallelizable; for a solo demo, you can still implement them serially.
