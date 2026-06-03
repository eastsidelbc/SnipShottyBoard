---
Title: Sprint HYGIENE-1 — Issue 3: Sync AssemblyVersion to 1.7.0
Date: 2026-05-19
Owner: Jeremy
Versions Affected: 1.7.0
Links:
 - Planning: docs/PLANNING.md §SPRINT HYGIENE-1
---

## Context & Goal

`AssemblyVersion` and `FileVersion` were stuck at 1.6.0.0 while `<Version>` was already 1.7.0. Mismatch causes confusion in file properties and deployment.

## Decision

Sync both to 1.7.0.0 to match `<Version>`.

## Implementation

File: `SnipShottyBoard.csproj` (lines 15-16)

Changed:
- `<AssemblyVersion>1.6.0.0</AssemblyVersion>` → `1.7.0.0`
- `<FileVersion>1.6.0.0</FileVersion>` → `1.7.0.0`

## Testing & Acceptance

- `dotnet build` — 0 errors
- Built exe shows version 1.7.0.0 in file properties

## Follow-ups

None — Issue 3 of 4 in HYGIENE-1 complete.

Architecture rules live in docs/PROJECT_MEMORY.md.
This note records implementation details and rationale.
