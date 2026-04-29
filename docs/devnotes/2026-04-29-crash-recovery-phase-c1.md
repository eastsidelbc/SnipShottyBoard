---
Title: Sprint C Phase C.1 — Recovery Journal Model & Timer
Date: 2026-04-29
Owner: Jeremy
Versions Affected: 1.7.0
Links:
 - Planning: docs/PLANNING.md §SPRINT C — PHASE C.1
---

## Context & Goal

Add silent background crash-recovery journaling so unsaved text is captured every 2 seconds. On crash/force-kill, the `.recovery` file persists. On clean save, it's cleared. Zero UI friction.

## Decisions & Alternatives

- **Decision:** Recovery snapshot serializes full `MasterData` (not just text). This simplifies restore logic since it's a complete app state snapshot.
- **Decision:** Use existing `AtomicFileManager.AtomicSave` for writes — no new file I/O pattern needed.
- **Decision:** 2-second interval (via `RecoveryJournalIntervalSeconds` constant) balances disk I/O with data safety.
- **Decision:** 1-hour max age (`RecoveryJournalMaxAgeHours`) — older snapshots are stale and ignored.

## Implementation Notes

### Constants (AppConstants.cs)
- `RecoveryJournalIntervalSeconds = 2` — timer tick interval
- `RecoveryJournalMaxAgeHours = 1` — stale threshold

### DataManager Methods
- `SaveRecoverySnapshot(MasterData)` — atomic write to `master.json.recovery`
- `ClearRecoverySnapshot()` — deletes `.recovery` file if exists
- `LoadRecoverySnapshot()` — returns `MasterData?`, null if no snapshot or too old

### MainWindow Timer
- New `DispatcherTimer recoveryTimer` at 2s interval
- Fires `SaveRecoverySnapshot` only when `hasUnsavedChanges == true`
- `SaveApplicationData()` calls `ClearRecoverySnapshot()` on success
- Timer stopped in `MainWindow_Closing`

### File Location
- `master.json.recovery` in `%APPDATA%\SnipShottyBoard\`

## Testing & Acceptance

1. Type text in a note
2. Wait 2+ seconds → `master.json.recovery` appears in AppData folder
3. Wait 5 seconds → auto-save fires → `.recovery` is deleted
4. Force kill (Task Manager) while dirty → `.recovery` persists
5. Verify no compiler errors

## Performance & Limits

- 2s interval = max 2.5 seconds of text loss in crash scenario
- Atomic write is fast (~1-2ms) — no perceptible I/O impact
- Max 2 rolling backups kept for recovery file

## Follow-ups

- Phase C.2: Startup silent restore logic (read `.recovery` on app launch, merge silently)

---
Architecture rules live in docs/PROJECT_MEMORY.md.
This note records implementation details and rationale.
