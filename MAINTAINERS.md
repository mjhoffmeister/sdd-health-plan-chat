# Maintainer notes

This file is for people evolving the staged demo itself (adding new phases,
refreshing prompts, moving tags). It is intentionally not part of the normal
"run the demo" path.

## Tag strategy

Phase tags mark the *start* of a phase.

Current phase tags on `origin`:

- `phase/00-init` = start of init
- `phase/01-constitution` = start of constitution (init complete)
- `phase/02-spec` = start of spec
- `phase/03-plan` = start of plan
- `phase/04-tasks` = start of tasks
- `phase/05-implement/01-setup` = start of implement (Phase 1: Setup)

### Hierarchical tags ("folders")

This repo uses `/` in tag names to keep related checkpoints grouped (for example, `phase/05-implement/01-setup`).

Important constraint: you cannot have both `phase/05-implement` and `phase/05-implement/01-setup` at the same time. Git stores tags like paths under `refs/tags/`, so the parent tag blocks creating child tags.

### Implement phase checkpoints

To keep semantics consistent with earlier phases (start-of-phase checkpoints), the implement phase uses hierarchical tags.

Existing implement tag(s):

- `phase/05-implement/01-setup` = start of implementation Phase 1 (Setup)
- `phase/05-implement/02-foundational` = start of implementation Phase 2 (Foundational)
- `phase/05-implement/03-ask-plan-questions` = start of implementation Phase 3 (Ask Plan Questions)

Planned next tags (created as implementation progresses):

- `phase/05-implement/04-us1` = start of implementation Phase 4 (US1)
- `phase/05-implement/05-us2` = start of implementation Phase 5 (US2)
- `phase/05-implement/06-us3` = start of implementation Phase 6 (US3)
- `phase/05-implement/07-polish` = start of implementation Phase 7 (Polish)

Workflow rule: when you finish a phase and check in, tag the *start of the next phase* at that commit.

Example (after finishing Setup):

`git tag -a phase/05-implement/02-foundational -m "Start Implement Phase: 02-foundational"`

`git push origin phase/05-implement/02-foundational`

If you need to rename away from the old flat `phase/05-implement` tag to this hierarchical layout, delete the old tag first (local and remote).

### Deleting a tag

Delete locally:

`git tag -d phase/00-init`

Delete on origin:

`git push origin :refs/tags/phase/00-init`

### Pushing tags

Tags are separate refs. Pushing a branch does not push tags.

Also, `git push --follow-tags` only pushes annotated tags. If you create
lightweight tags (for example, `git tag phase/01-constitution`), push them
explicitly:

`git push origin phase/01-constitution`

If you see "Everything up-to-date" after creating a lightweight tag, it usually
means the tag was not pushed.

### Moving a tag (early repo only)

If you need to retarget a phase tag while the repo is still new:

- `git tag -f phase/00-init`
- `git push -f origin phase/00-init`

Avoid force-moving tags once other people are consuming them.

## Demoer workflows (jumping/checkpoints)

### See what checkpoint you're on

`git tag --points-at HEAD`

### Jump to a phase (fast)

`git fetch --tags`

`git switch --detach phase/00-init`

Tip: using `--detach` avoids accidentally moving a branch pointer while presenting.

### Worktrees (multiple phase folders)

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

To run the demo repeatedly, reset your **live** folder back to a checkpoint (this discards any edits in the live folder):

`cd ..\sdd-health-plan-chat--live`

`git reset --hard phase/00-init`

`git clean -fd`
