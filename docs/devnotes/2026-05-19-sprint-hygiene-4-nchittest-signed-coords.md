---
Title: HYGIENE-4 — NCHITTEST signed coordinate fix (multi-monitor resize)
Date: 2026-05-19
Owner: Jeremy
Versions Affected: 1.7.0
Links:
 - Planning: docs/PLANNING.md §SPRINT HYGIENE-4
---

## Context & Goal

Fix `WM_NCHITTEST` coordinate extraction so resize hit detection works on all monitor configurations, especially when a secondary monitor sits to the left of primary (negative screen X coordinates).

Closes open bug **B-WF** — White flash + ghost buttons on resize.

## Issue 1 — Unsigned mask breaks negative coordinates

**Root cause:** `WM_NCHITTEST` packs **signed 16-bit** coordinates into lParam. The extraction code used unsigned masking (`& 0xFFFF`) which turns negative X values (e.g., -50) into large positive numbers (e.g., 65486). This completely breaks edge/resize hit detection — the OS can't determine which window edge is being dragged, causing resize ghost buttons and white flash artifacts.

**Fix:** Cast through `(short)` to sign-extend both X and Y coordinates after extraction.

**File:** `Core/Utils/WindowChromeFix.cs` — `HandleNCHitTest()` (~line 125–126)

**BEFORE:**
```csharp
int x = lParam.ToInt32() & 0xFFFF;
int y = lParam.ToInt32() >> 16;
```

**AFTER:**
```csharp
int x = (short)(lParam.ToInt32() & 0xFFFF);
int y = (short)(lParam.ToInt32() >> 16);
```

## Testing & Acceptance

- Build: 0 errors, 0 warnings
- Drag a window to a monitor positioned left of primary — resize edges all respond correctly
- No behavioral change on single-monitor or right-side multi-monitor setups

## Performance & Limits

Zero cost. Two additional casts in a hot path that's already running per-mouse-move during resize. `(short)` is a zero-instruction operation (truncation with sign extension handled by CPU register sizing).

## Follow-ups

None — fix complete. B-WF closed.

Architecture rules live in docs/PROJECT_MEMORY.md.
This note records implementation details and rationale.
