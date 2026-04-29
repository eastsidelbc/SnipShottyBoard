---
Title: Sprint C Phase 2 — Startup Silent Restore from Recovery Snapshot
Date: 2026-04-29
Owner: Jeremy
Versions Affected: 1.7.0 (unreleased)
Links:
 - Planning: docs/PLANNING.md §Sprint C Phase C.2
 - PR/SHAs: pending
---

## Context & Goal

Phase C.1 implemented the 2-second recovery journal timer that writes `master.json.recovery` when text is dirty. Phase C.2 implements the startup side: when the app launches, it silently checks if a recovery snapshot exists, and if so, merges the recovered text back into the saved data before any window loads.

**Goal:** Zero user friction. No modals, no dialogs, no "recover your work?" prompts. The text just appears.

## Decisions & Alternatives

1. **Merge in `App.OnStartup()` before window loads** — Chosen approach. The recovery restore happens in `DataManager.TryRestoreFromRecovery()` called from `App.OnStartup()`. This ensures `master.json` is updated before `NoteWindowManager` loads windows, so the singleton picks up merged data automatically.

2. **Match windows by ID, notes by title** — Since JSON deserialization creates new object instances, reference equality doesn't work. Windows are matched by `Guid Id`, notes by `string Title`. If a note exists in both but has different text, the recovery text wins (it's newer).

3. **Add lost windows/notes** — If a window or note exists in the recovery but not in saved data, it's added to the saved data. This handles the edge case where a new window was created but never auto-saved.

4. **Alternative considered: replace saved with recovery entirely** — Rejected. The recovery snapshot may be up to 2 seconds behind the last auto-save for windows that weren't the active one. Merging is safer than replacing.

## Implementation Notes

### DataManager.cs — `TryRestoreFromRecovery()`

- Loads recovery snapshot via existing `LoadRecoverySnapshot()` (checks age < 1 hour)
- Loads saved master data
- Calls `MergeRecoveryIntoSaved()` to merge
- Saves merged data to `master.json`
- Deletes recovery file via `ClearRecoverySnapshot()`
- Returns `true` if recovery was applied, `false` otherwise

### DataManager.cs — `MergeRecoveryIntoSaved()`

- Iterates recovered windows, matches by `Id`
- For each matched window: iterates recovered notes, matches by `Title`
- If note exists in both: updates `TextContent` from recovery if different
- If note only in recovery: adds to saved window
- If window only in recovery: adds to saved windows list
- Updates `LastModified` on merged windows
- Logs recovery stats (number of notes recovered)

### App.xaml.cs

- Added call to `DataManager.TryRestoreFromRecovery()` in `OnStartup()`, after logging initialization, before main window creation
- Logs "Unsaved text recovered silently on startup" if recovery was applied

### Edge Cases Handled

- Clean close: recovery file deleted by `ClearRecoverySnapshot()` → no recovery needed
- Stale snapshot (>1 hour): `LoadRecoverySnapshot()` returns null → no recovery
- Corrupted recovery file: caught by try/catch → logged, no recovery
- Empty recovery (no notes): `MergeRecoveryIntoSaved()` handles null/empty gracefully
- Multiple windows: each window matched by ID, notes merged independently

## Testing & Acceptance

1. **Force kill test:** Type text → wait 2s → `taskkill /F /IM SnipShottyBoard.exe` → reopen → text should be there
2. **Clean close test:** Close normally → `master.json.recovery` should not exist
3. **Stale test:** Manually create `master.json.recovery` with old timestamp → reopen → recovery ignored
4. **No data test:** Empty app → force kill → reopen → no crash, no recovery

## Performance & Limits

- Recovery merge runs on startup, adds ~5ms to launch time (file I/O + JSON parse)
- Only triggers when recovery file exists (<1% of launches in normal use)
- No performance impact during normal operation

## Follow-ups

- Sprint C is now complete. Next: Sprint D (Professional GIF Viewer)
- Consider adding a subtle status bar message "Recovered unsaved text" for user awareness (low priority)

Architecture rules live in docs/PROJECT_MEMORY.md.
This note records implementation details and rationale.
