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

## For Demoers (jump ahead + rehearse safely)

To see what checkpoint you're on:

`git tag --points-at HEAD`

### Phase tags (start-of-phase checkpoints)

This repo uses Git tags to mark the **start** of each demo phase so you can
reset/jump without accidentally “dirtying” a checkpoint.

- `phase/00-init` = starting point before you run `specify init`
- `phase/01-constitution` = starting point for generating the constitution
- `phase/02-spec` = starting point for generating the first spec
- `phase/03-plan` = starting point for generating the first plan
- `phase/04-tasks` = starting point for generating the first tasks

### Jump to a phase (fast)

To return to the start of init:

`git fetch --tags`

`git switch --detach phase/00-init`

Tip: using `--detach` avoids accidentally moving a branch pointer while
presenting.

Note: phase tags are maintained by repo maintainers. If you're extending the
demo and managing tags, see MAINTAINERS.md.

### Smoothest live demo: worktrees (multiple phase folders)

Git worktrees let you have multiple folders checked out at different phases at
the same time, so you can jump by switching windows instead of switching
branches.

From the repo root:

`git fetch --tags`

Create a dedicated “live” folder you can edit freely:

`git switch -c demo/live phase/00-init`

If you already created `demo/live` before, use:

`git switch demo/live`

`git worktree add ..\sdd-health-plan-chat--live demo/live`

Optional: create a “checkpoint view” folder that you never edit:

`git worktree add --detach ..\sdd-health-plan-chat--00-init phase/00-init`

### Rehearsal reset (start over quickly)

To run the demo repeatedly, reset your **live** folder back to the start of init
(this discards any edits in the live folder):

`cd ..\sdd-health-plan-chat--live`

`git reset --hard phase/00-init`

`git clean -fd`

Then rerun init:

`specify init --here --script ps --ai copilot`

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
current git branch name. If you are on `main` or a detached phase tag, switch to
your feature branch (e.g., `001-health-plan-chat`) before generating spec/plan/
tasks artifacts. If you are not using git, set `SPECIFY_FEATURE` to the feature
folder name.

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

### Generate the tasks

Use Spec Kit’s out-of-box tasks agent (no additional repo-specific prompt is
needed):

`/speckit.tasks`

Optional: if you want to create GitHub issues from tasks, run:

`/speckit.taskstoissues`
