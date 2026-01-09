---
agent: speckit.plan
name: healthplanchat-plan
description: Generate an implementation plan for Health Plan Chat
---

Create an implementation plan for the Health Plan Chat feature described in the
current feature spec.

Keep the plan easy to demo and iterate on.

The plan MUST preserve and implement the key product behaviors from the spec,
including:

- A clean, modern UI with both light and dark modes
- A chat experience grounded in health plan materials so responses stay accurate
  and relevant
- Clear separation between:
  - grounded answers based on plan materials, and
  - general guidance when the plan does not contain the answer
- Chat history maintained within a session

Assume this is a reusable demo: keep it simple and reliable to run, without
cutting corners on correctness, security, or testability.

Follow the project constitution in `.specify/memory/constitution.md`. If the spec
conflicts with the constitution, call it out and propose the smallest spec or
plan adjustment to resolve the conflict.
