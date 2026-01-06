# Maintainer notes

This file is for people evolving the staged demo itself (adding new phases,
refreshing prompts, moving tags). It is intentionally not part of the normal
"run the demo" path.

## Tag strategy

Phase tags mark the *start* of a phase.

- `phase/00-init` is the start of init.
- `phase/01-constitution` is the start of constitution (init complete).
- Future phases follow the same convention.

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
