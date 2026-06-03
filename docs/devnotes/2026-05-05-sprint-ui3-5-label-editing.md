---
Title: Sprint UI-3.5 — Label Editing (inline rename dialog)
Date: 2026-05-05
Owner: Jeremy
Versions Affected: 1.7.0-dev
Links:
 - Planning: docs/PLANNING.md §Sprint UI-3 Phase UI-3.5
 - PR/SHAs: N/A
---

## Context & Goal

Replace the stub `EditLabel` placeholder in MediaSection with a working rename dialog
using the existing `CustomInputDialog.ShowInput` method. Add double-click-to-edit on
the label row (row 1) of each thumbnail container.

## Decisions & Alternatives

- **Used `CustomInputDialog.ShowInput`** (synchronous) rather than implementing a new async method.
  The existing dialog already handles Enter/Escape, focus management, and theming.
- **Double-click via `MouseLeftButtonDown` with `ClickCount == 2`** — WPF `Grid` has no
  `MouseDoubleClick` event. This is the standard WPF pattern for non-button elements.
- **Empty label auto-hides** — if user submits an empty string, `ShowLabel` is set to false
  and the label row collapses. Prevents blank label rows from cluttering the UI.

## Implementation Notes

### EditLabel (MediaSection.xaml.cs)

Replaced the stub `DialogHelper.ShowInformation` call with:
1. `CustomInputDialog.ShowInput()` — opens modal dialog with current label as default
2. On success: trim input, update `mediaRef.Label`, auto-toggle `ShowLabel`
3. Find the label `TextBlock` in row 1 of the container and update text + visibility
4. Fire `OnMediaChanged` to trigger auto-save

### Double-click handler (SetupContainerInteractions)

Added `MouseLeftButtonDown` handler on the container that checks:
- `e.ClickCount == 2` (double-click, not single)
- `e.OriginalSource is TextBlock` and `Grid.GetRow(label) == 1` (clicked the label row)

Only fires when double-clicking the label text itself, not the image or timestamp.

## Testing & Acceptance

- Right-click → Rename... opens dialog with current label pre-filled
- Enter new label → OK → label updates immediately in thumbnail
- Submit empty string → label row collapses (ShowLabel = false)
- Press Escape → no changes
- Double-click label text → opens rename dialog
- Changes persist via OnMediaChanged → auto-save bridge

## Performance & Limits

- Synchronous dialog blocks the UI thread briefly (acceptable for rename operations)
- Label update is O(1) — just finding a TextBlock in a small container

## Follow-ups

- UI-3.6: Full smoke test of all UI-3 features together
- Consider inline TextBox editing (future enhancement — no dialog needed)

Architecture rules live in docs/PROJECT_MEMORY.md.
This note records implementation details and rationale.
