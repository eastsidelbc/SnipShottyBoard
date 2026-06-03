---
Title: Top-edge resize hit area still too narrow on MainWindow
Date: 2026-05-06
Owner: Jeremy
Versions Affected: 1.7.0
Links:
 - Planning: docs/PLANNING.md
 - PR/SHAs: none
---

## Context & Goal

**Goal:** Make window resize hit area consistent across all 4 edges — industry standard 8px.

**Problem:** Top edge resize cursor still requires ~1-2px precision to trigger. All other edges (left, right, bottom) work fine at 8px. The top edge has the `ui:TitleBar` content control that intercepts mouse events.

## Decisions & Alternatives

**Attempted fix:** Added `WM_NCHITTEST` handling in `WindowChromeFix.WndProc` to force 8px resize hit area at the Win32 level, before WPF controls can intercept. Returns `HTTOP` when mouse is within `AppConstants.WindowResizeBorderThickness` (8px) of the top client edge.

**Why it didn't work:** The `ui:TitleBar` control from WPF-UI (FluentWindow) likely has its own WndProc hook or non-client area handling that runs before our hook, or the TitleBar occupies the non-client region rather than the client region. `WM_NCHITTEST` returns the correct hit code but DWM/FluentWindow overrides it because the TitleBar is the native caption area, not client content.

**What was added (still in code):**
- `Data/AppConstants.cs`: `WindowResizeBorderThickness = 8` constant
- `Core/Utils/WindowChromeFix.cs`: `WM_NCHITTEST` handler + `SetResizeBorderThickness()` method

## Implementation Notes

- `WindowChrome.ResizeBorderThickness` works on left/right/bottom edges.
- Top edge is the only edge where `ui:TitleBar` sits directly in the resize zone.
- `WM_NCHITTEST` hook returns `HTTOP` but the resize cursor doesn't appear — likely because FluentWindow's internal hook runs after ours and overrides, or the TitleBar region is already classified as `HTCAPTION` by the default handler before our hook fires.
- All 4 windows use `ui:TitleBar` — MainWindow, ImageViewerWindow, SettingsWindow, NoteListWindow.

## Testing & Acceptance

- Hover near left edge → resize cursor appears at ~8px. **PASS**
- Hover near right edge → resize cursor appears at ~8px. **PASS**
- Hover near bottom edge → resize cursor appears at ~8px. **PASS**
- Hover near top edge (above TitleBar) → resize cursor still requires ~1-2px precision. **FAIL**

## Performance & Limits

N/A — no performance impact.

## Follow-ups

Possible approaches to investigate:
1. Check if FluentWindow has a `ResizeBorderThickness` or `WindowChrome` property we can set directly on the `ui:FluentWindow` or `ui:TitleBar` control.
2. Set `WindowStyle="None"` and `AllowsTransparency="True"` on FluentWindow — this forces full non-client handling through our WndProc hook (but may break DWM effects).
3. Use `DwmSetWindowAttribute` with `DWMWA_EXTENDED_FRAME_BOUNDS` to understand the actual non-client region layout.
4. Subclass or extend the WPF-UI `TitleBar` control to add a transparent margin/padding that extends the hit area.
5. Check WPF-UI 4.0.3 source code for how `FluentWindow` handles `WM_NCHITTEST` — it may be setting `HTCAPTION` on the entire top region including our 8px zone.

## Architecture rules live in docs/PROJECT_MEMORY.md.
This note records implementation details and rationale.
