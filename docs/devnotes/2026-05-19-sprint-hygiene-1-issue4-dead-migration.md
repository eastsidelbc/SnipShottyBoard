---
Title: Sprint HYGIENE-1 — Issue 4: Remove dead snapshot migration code
Date: 2026-05-19
Owner: Jeremy
Versions Affected: 1.7.0
Links:
 - Planning: docs/PLANNING.md §SPRINT HYGIENE-1
---

## Context & Goal

`ApplyCanonicalSnapshotIfNeeded()` was a Nov 2025 one-time migration that copied a dev-era snapshot (`notewindows-20251120-172254.json`) to `notewindows.json`. It ran on every startup (guarded by a flag file), adding unnecessary file I/O and ~50 lines of dead code.

## Decision

Delete the entire migration: path constants, method body, and static ctor call. All users have already run this migration — the code serves no purpose.

## Implementation

File: `Core/Managers/DataManager.cs`

Removed:
- `CanonicalSnapshotPath` constant
- `MigrationFlagPath` constant
- `ApplyCanonicalSnapshotIfNeeded()` method (~50 lines)
- Call from `static DataManager()` constructor

## Testing & Acceptance

- `dotnet build` — 0 errors
- Startup no longer checks for snapshot file or flag file
- Existing `MigrateToMasterIfNeeded()` migration still runs normally

## Follow-ups

None — Issue 4 of 4 in HYGIENE-1 complete. Sprint HYGIENE-1 DONE.

Architecture rules live in docs/PROJECT_MEMORY.md.
This note records implementation details and rationale.
