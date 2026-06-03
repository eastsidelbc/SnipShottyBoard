---
Title: Full Codebase Audit — modernwpf Branch
Date: 2026-04-23
Owner: Jeremy
Versions Affected: 1.5.0-1.6.0
Links:
 - Planning: docs/PLANNING.md
---

## Context & Goal

Comprehensive audit of the SnipShottyBoard codebase on the `modernwpf` branch (HEAD: 71b83cf).
The build was broken at this point — Core/ files deleted mid-refactoring, csproj pointing to non-existent class library.

## Key Findings

- **Build broken**: Core/ directory deleted, csproj referenced missing SnipShottyBoard.Core project
- **Last good build**: Commit 769b74f (v1.5.0) or ec5d913 (phases 0-5)
- **Missing files**: DataManager, AtomicFileManager, NoteWindowManager, SavedNote, AppSettings, AppConstants all deleted
- **File sizes**: TabManager.cs (1641 lines), MediaSection.xaml.cs (1239 lines) — both too large
- **CHANGELOG confusion**: Top ~600 lines contained planned Avalonia content, not actual history
- **Docs templates**: PROJECT_MEMORY.md, PLANNING.md, DECISIONS.md still had generic React/TS placeholder content
- **VERSION file**: Stale at 1.5.0 while csproj had progressed to 1.6.0

## Resolution

Build was fixed by restoring Core/ files. Audit findings absorbed into:
- docs/PROJECT_MEMORY.md — architecture, patterns, known fragile areas
- docs/PLANNING.md — sprint roadmap and priorities
- .cursor/rules/ — AI behavior rules

Original audit report (modernwpf.md at repo root) deleted — content graduated to PROJECT_MEMORY.md.

Architecture rules live in docs/PROJECT_MEMORY.md.
This note records implementation details and rationale.
