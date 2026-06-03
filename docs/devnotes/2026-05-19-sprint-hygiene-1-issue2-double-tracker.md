---
Title: Sprint HYGIENE-1 — Issue 2: Remove duplicate WindowPositionTracker
Date: 2026-05-19
Owner: Jeremy
Versions Affected: 1.7.0
Links:
 - Planning: docs/PLANNING.md §SPRINT HYGIENE-1
---

## Context & Goal

Every secondary MainWindow opened via `NoteListWindow.OpenNoteWindow()` got a `WindowPositionTracker` from `NoteListWindow`, but the MainWindow already creates its own tracker in `SetupPositionTracking()`. Result: two trackers per window, double disk writes on every drag/resize.

## Decision

Remove the tracker instantiation, storage, and cleanup block from `OpenNoteWindow()`. Each window self-manages its own single tracker.

## Implementation

File: `UI/NoteListWindow.xaml.cs`

Removed:
- `_positionTrackers` dictionary field (no longer needed)
- `WindowPositionTracker` creation in `OpenNoteWindow()` (~26 lines)
- `_positionTrackers[windowId] = tracker` storage
- `noteWindow.Closing` cleanup handler

Kept: `using SnipShottyBoard.Core.Utils;` — still needed for `WindowChromeFix`

## Testing & Acceptance

- `dotnet build` — 0 errors
- Secondary windows still get position tracking from their own `SetupPositionTracking()`
- No double disk writes on drag/resize

## Follow-ups

None — Issue 2 of 4 in HYGIENE-1 complete.

Architecture rules live in docs/PROJECT_MEMORY.md.
This note records implementation details and rationale.
