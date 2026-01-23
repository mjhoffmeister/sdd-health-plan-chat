---
name: demo
description: Bootstrap and navigate the spec-kit demo environment. Use this when asked to set up the demo, bootstrap the demo, jump to a phase, go to a phase, switch phases, or clean up a demo.
---

# Demo Skill

This skill manages the spec-kit staged demo environment using git worktrees and phase tags.

## When to Use

Use this skill when the user asks to:
- Set up / bootstrap / prepare the demo
- Jump to / go to / switch to a phase
- Clean up / delete / remove a demo
- Reset the demo

## Setup

Run from the repository root to create the demo environment:

```powershell
# Auto-numbered (demo-01, demo-02, etc.)
.\.github\skills\demo\setup-demo.ps1

# Named demo
.\.github\skills\demo\setup-demo.ps1 -Name mcaps
```

This creates a demo folder (e.g., `../sdd-health-plan-chat-mcaps/`) containing:
1. `live/` — Editable worktree for the demo
2. `phase-00-init/`, `phase-01-constitution/`, etc. — Checkpoint worktrees (for offline resilience)

### Setup Options

| Option | Description |
|--------|-------------|
| `-Name <name>` | Name for the demo (creates `<repo>-<name>`) |
| `-SkipCheckpoints` | Create only the live worktree |
| `-Reset` | Remove existing worktrees and recreate |
| `-WorktreeRoot <path>` | Override full path for demo folder |

## Jump to Phase

From within the live worktree, jump between phases while preserving work:

```powershell
.\.github\skills\demo\jump-to-phase.ps1 -Phase <phase>
```

**Safety:**
- The script refuses to run on `main`/`master`.
- In a demo worktree, it runs without extra prompts.
- Outside a worktree, it prints a warning (because changes may affect your main repo).

**State preservation:** When jumping away from a phase, uncommitted changes are stashed. When returning, they're automatically restored.

**Stash identity:** Stashes are labeled as `demo[<name>]:<phase-tag>`.
- If you are on a `demo/<name>` branch, `<name>` comes from the branch.
- Otherwise (common in spec-kit demos), `<name>` is derived from the demo folder name (e.g., `../sdd-health-plan-chat-dry-run/live` → `dry-run`).

### Phase Identifiers

| Number | Name | Tag |
|--------|------|-----|
| `0` | `init` | `phase/00-init` |
| `1` | `constitution` | `phase/01-constitution` |
| `2` | `spec` | `phase/02-spec` |
| `3` | `plan` | `phase/03-plan` |
| `4` | `tasks` | `phase/04-tasks` |
| `5.1` | `setup` | `phase/05-implement/01-setup` |
| `5.2` | `foundational` | `phase/05-implement/02-foundational` |
| `5.3` | `us1` | `phase/05-implement/03-ask-plan-questions` |
| `5.4` | `us2` | `phase/05-implement/04-handle-missing-answers` |
| `5.5` | `us3` | `phase/05-implement/05-ui` |
| `5.6` | `docs` | `phase/05-implement/06-documentation` |

### Jump Options

| Option | Description |
|--------|-------------|
| `-Phase <id>` | Phase number, name, or full tag |
| `-Reset` | Discard changes instead of stashing; skip restore |

### Examples

```powershell
# Jump to plan phase (preserves current work)
.\.github\skills\demo\jump-to-phase.ps1 -Phase plan

# Jump to phase 5.3 and discard changes
.\.github\skills\demo\jump-to-phase.ps1 -Phase 5.3 -Reset

# Jump using full tag name
.\.github\skills\demo\jump-to-phase.ps1 -Phase phase/03-plan
```

## After Setup

1. Open the live worktree: `code ../sdd-health-plan-chat-<name>/live`
2. Jump between phases as needed during the demo
3. Use `-Reset` on setup-demo.ps1 to start fresh

## Cleanup

Remove a demo environment (folders, branch, and stashes):

```powershell
# Clean up a specific demo
.\.github\skills\demo\cleanup-demo.ps1 -Name mcaps

# Clean up all demos
.\.github\skills\demo\cleanup-demo.ps1 -All
```

### Cleanup Options

| Option | Description |
|--------|-------------|
| `-Name <name>` | Name of the demo to remove |
| `-All` | Remove all demo environments |
| `-KeepStashes` | Preserve stashes (only remove folders and branches) |
