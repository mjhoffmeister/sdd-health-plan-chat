# Using Spec Kit and GitHub Copilot to Build a Health Plan Chat App

This repository is a staged demo of spec-driven development (SDD) using
**Spec Kit** and **GitHub Copilot**. It shows how written product intent
(constitution, spec, plan) can guide implementation so AI-assisted development
stays aligned with requirements, quality, and testability.

It’s organized as a sequence you can present end-to-end, or **jump ahead** during a live session while still keeping realistic artifacts “already done” for earlier phases.

## Demo Phases

**init → constitution → spec → plan → implement**

Each phase produces durable artifacts that constrain and guide the next phase.

## Quick Start

### 1) Install (or upgrade) the `specify` CLI

Install:

`uv tool install specify-cli --from git+https://github.com/github/spec-kit.git`

Upgrade:

`uv tool install specify-cli --force --from git+https://github.com/github/spec-kit.git`

### 2) Initialize Spec Kit in this repo

Run from PowerShell (`pwsh`):

`specify init --here --script ps --ai copilot`