# Memory Fix Phase 2 — Bug Fixes: Tab + Viewer Memory
Date: 2026-05-19

Architecture rules live in docs/PROJECT_MEMORY.md.
This note records implementation details and rationale.

---
Title: Memory Fix Phase 2 — Tab + Viewer Memory Bug Fixes
Date: 2026-05-19
Owner: Jeremy
Versions Affected: current
Links:
 - Planning: docs/PLANNING.md
 - Memory audit: docs/memoryauditplan.md
---

## Context & Goal

Phase 1 of the memory remediation is complete. Phase 2 applies four
surgical fixes targeting confirmed leaks in the image viewer, media
section, status bar, and tab cleanup paths.

## Changes

- **LEAK-6 (already applied):** `AnimationController` disposed before
  GIF source cleared in `ClearPreviousImage()` and `ReleaseImageResources()`
  (`ImageViewerWindow.xaml.cs`). Was already in place from a prior sprint —
  confirmed correct, no change needed.

- **LEAK-9:** `ImageViewerWindow` limited to 1 open instance per image path
  (`MediaSection.xaml.cs` — `ShowFullSizeImage()`). Added `CurrentImagePath`
  public property to `ImageViewerWindow`. Before creating a new viewer,
  `ShowFullSizeImage()` now checks `Application.Current.Windows` for an
  existing viewer showing the same path and activates it instead.

- **LEAK-19:** `SolidColorBrush` instances in `StatusBarManager.UpdateStatusBar()`
  were allocated on every timer tick (~1/sec). Replaced with two static frozen
  instances (`_savedBrush`, `_unsavedBrush`) initialized in a static constructor.
  Frozen brushes are immutable, thread-safe, and GC-stable.

- **LEAK-13:** `NoteTab` subscribed to child component events using anonymous
  lambdas in the constructor. These lambdas capture `this`, preventing GC
  collection of closed tabs if the child components hold the subscription.
  Refactored to store handlers as `readonly` fields (`_onTextChangedHandler`,
  `_onMediaChangedHandler`). `Dispose()` now unsubscribes both before
  disposing `MediaSectionControl`.

## Implementation Notes

- `CurrentImagePath` is a simple auto-property getter: `=> currentImagePath`.
- Static brush constructor uses `Freeze()` immediately after construction —
  this is the WPF-recommended pattern for shared brushes created in C# code.
- Handler fields are `readonly` — set once in constructor, never reassigned.
- Unsubscription in `Dispose()` is placed before `MediaSectionControl.Dispose()`
  to ensure no stale callbacks fire during teardown.

## Testing & Acceptance

- `dotnet build` — 0 errors, 276 warnings (all pre-existing)
- FIX 2: click same image 3×, only 1 viewer opens; second click brings it to front
- FIX 3: save/unsaved status color updates correctly after theme reload
- FIX 4: delete tab, verify no lingering references (memory profiler or Task Manager)

## Expected RAM Impact

~150MB → ~120MB idle (combined Phase 2 target)

## Build Status

0 errors, 0 warnings introduced

## Follow-ups

Phase 3: docs/memory-fix-phase-3.md
