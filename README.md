# Using Spec Kit and GitHub Copilot to Build a Health Plan Chat App

This repository is a staged demo of spec-driven development (SDD) using
**Spec Kit** and **GitHub Copilot**. It shows how written product intent
(constitution, spec, plan) can guide implementation so AI-assisted development
stays aligned with requirements, quality, and testability.

It’s organized as a sequence you can present end-to-end, or jump ahead during a
live session while still keeping completed, realistic artifacts for earlier
phases.

## Demo Phases

**init → constitution → spec → plan → implement**

Each phase produces durable artifacts that constrain and guide the next phase.

## For Demoers (jump ahead + rehearse safely)

To see what checkpoint you're on:

`git tag --points-at HEAD`

### Phase tags (start-of-phase checkpoints)

This repo uses Git tags to mark the **start** of each demo phase so you can
reset/jump without accidentally “dirtying” a checkpoint.

- `phase/00-init` = starting point before you run `specify init`
- `phase/01-constitution` = starting point for the constitution phase

More phase tags will be added as the demo is built out.

### Jump to a phase (fast)

To return to the start of the constitution phase:

`git fetch --tags`

`git switch --detach phase/01-constitution`

Tip: using `--detach` avoids accidentally moving a branch pointer while
presenting.

### Smoothest live demo: worktrees (multiple phase folders)

Git worktrees let you have multiple folders checked out at different phases at
the same time, so you can jump by switching windows instead of switching
branches.

From the repo root:

`git fetch --tags`

Create a dedicated “live” folder you can edit freely:

`git switch -c demo/live phase/01-constitution`

If you already created `demo/live` before, use:

`git switch demo/live`

`git worktree add ..\sdd-health-plan-chat--live demo/live`

Optional: create a “checkpoint view” folder that you never edit:

`git worktree add --detach ..\sdd-health-plan-chat--01-constitution phase/01-constitution`

### Rehearsal reset (start over quickly)

To run the demo repeatedly, reset your **live** folder back to the start of the
constitution phase (this discards any edits in the live folder):

`cd ..\sdd-health-plan-chat--live`

`git reset --hard phase/01-constitution`

`git clean -fd`

Then regenerate the constitution (Copilot Chat):

`/healthplanchat-constitution`

## Constitution Phase (Phase 01)

Goal: create and ratify the project constitution in
`.specify/memory/constitution.md`.

In Copilot Chat, run:

`/healthplanchat-constitution`

Then review and refine `.specify/memory/constitution.md`.

If you need to rerun init from scratch, jump back to `phase/00-init`.

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