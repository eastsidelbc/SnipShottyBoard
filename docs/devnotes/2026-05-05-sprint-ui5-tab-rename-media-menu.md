---
Title: Sprint UI-5 — Tab Rename Edit Visuals + MediaSection Global Menu
Date: 2026-05-05
Owner: Jeremy
Versions Affected: 1.7.0-dev
Links:
 - Planning: docs/PLANNING.md §COMPLETED SPRINTS
---

## Context & Goal

Tab rename didn't visually distinguish edit mode — transparent TextBox with
hardcoded blue border (#4A90E2) didn't match accent (#6366F1). MediaSection
lacked bulk operations for all images.

## Decisions & Alternatives

- Tab rename uses template elements (TabBorder, ActiveUnderline) rather than
  button-level properties — matches existing selected-state visual language
- Global context menu built dynamically in ContextMenuOpening handler — avoids
  XAML bloat, reuses CreateIcon pattern from per-image menu
- Delete All uses DialogHelper.ShowConfirmation — consistent with codebase pattern

## Implementation Notes

### Step 1 — Tab Rename Visuals (TabManager.cs)

RenameTab method updated:
- TabBorder gets 12% indigo tint (Color.FromArgb(30, 99, 102, 241))
- TabBorder border: 1.5px AccentBrush
- ActiveUnderline opacity set to 1 (accent gradient shows)
- AccentGlowEffect applied via template lookup
- TextBox CaretBrush set to AccentBrush for visibility
- RestoreTabAppearance restores template elements (TabBorder bg/border,
  ActiveUnderline opacity, button Effect)

Key: access template elements via `tabButton.Template?.FindName("TabBorder", tabButton)`

### Step 2 — MediaSection XAML (MediaSection.xaml)

Added `ContextMenuOpening="MediaSection_ContextMenuOpening"` to root Grid.

### Step 3 — Global Menu + Bulk Ops (MediaSection.xaml.cs)

New methods added:
- `MediaSection_ContextMenuOpening` — walks visual tree to check if click hit
  an image container; if not, builds global menu
- `SetSizeForAll(int)` — iterates containers, sets ThumbnailSize, rebuilds each
- `ToggleHiddenForAll(bool)` — if any hidden → show all, else hide all
- `DeleteAllImages()` — confirmation dialog, deletes files, clears collections

## Testing & Acceptance

- [ ] Double-click tab → accent border + glow + underline visible
- [ ] Type in rename box → accent-colored caret
- [ ] Enter/Escape → indicators gone, tab looks normal
- [ ] Right-click empty MediaSection → global menu (Size/Hide All/Delete All)
- [ ] Right-click image → per-image menu (unchanged)
- [ ] Size → all thumbnails resize
- [ ] Hide All → all show placeholder dots
- [ ] Delete All → confirmation → all images removed

## Performance & Limits

- SetSizeForAll/ ToggleHiddenForAll iterate all containers synchronously —
  acceptable for typical image counts (<100)
- DeleteAll clears files then UI — no async needed for bulk delete

## Follow-ups

- Consider animating size transitions (future polish)
- Delete All could use "Delete Hidden Only" variant

Architecture rules live in docs/PROJECT_MEMORY.md.
This note records implementation details and rationale.
