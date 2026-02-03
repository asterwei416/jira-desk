# Copilot Instructions (MANDATORY)

You are contributing to a repository that follows
Specification-Driven Development (SDD) using Spec Kit.

These rules are NON-NEGOTIABLE.

---

## 1. Feature-First Specification Only

- ALL specifications MUST be written as feature-level specs.
- System-level, project-wide, or monolithic specifications are NOT allowed.
- Each feature MUST live under:

  /specs/<domain>/<feature-id>/

---

## 2. Required Files Per Feature

Each feature MUST contain exactly:
- spec.md
- plan.md
- tasks.md

If any file is missing, the output is INVALID.

---

## 3. spec.md Structure (STRICT)

Every spec.md MUST include the following sections:

- Goal
- User Capability
- Scope
  - Included
  - Out of scope
- Constraints
- Relationship to Existing Features

The section title MUST match exactly.

---

## 4. Feature Relationship Declaration (REQUIRED)

The section:

## Relationship to Existing Features

MUST include ALL of the following fields:

- Builds upon:
- Depends on:
- Does not modify:

If none apply, explicitly write "None".

Free-form relationship descriptions are NOT allowed.

---

## 5. Authority Rules

- memory/constitution.md overrides all specs.
- Feature-level specs override any legacy or system-level documents.
- If a conflict is detected, DO NOT guess.
  Report the conflict clearly.

---

## 6. Forbidden Behaviors

- Do NOT introduce future roadmap items.
- Do NOT describe entire system architecture.
- Do NOT modify existing features implicitly.
- Do NOT invent cross-feature behavior.

If uncertain, ask for clarification instead of assuming.
