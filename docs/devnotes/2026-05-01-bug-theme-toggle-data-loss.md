---
Title: B-THEME — Theme Toggle Data Loss + Save Corruption Fix
Date: 2026-05-01
Owner: Jeremy
Versions Affected: 1.7.0
Links:
  - Planning: docs/PLANNING.md §Sprint UI-2
  - Bugs: docs/BUGS.md §B-THEME
---

Architecture rules live in docs/PROJECT_MEMORY.md.
This note records implementation details and rationale.

## Context & Goal

During Sprint UI-2 testing, clicking the Toggle Theme title bar button
caused the app to go white. After closing and reopening, all notes and
images were gone. A second issue: even fresh notes typed after the
incident did not persist across restarts.

## Root Cause Chain

### Step 1 — LightTheme.xaml is incomplete
LightTheme.xaml was missing 21 resource keys defined in DarkTheme.xaml:
ContentCardStyle, SubtleDividerBrush, EditorSurfaceStyle, AccentGradientBrush,
EditorFocusGlow, HoverGlow, AppFontFamily, and 14 others. Identified by
diffing x:Key names between both files via PowerShell.

### Step 2 — NoteTab.xaml crashed on load
When LightTheme was applied, TabManager.LoadTabs() called NoteTab constructor
which called InitializeComponent(). XAML parser threw XamlParseException:
"Cannot find resource named 'ContentCardStyle'". Line 465 of NoteTab.xaml.

### Step 3 — Fallback created empty tab
The catch block in LoadTabs() called CreateNewTab() as a fallback.
This created one blank empty tab. The real notes were still on disk
in master.json but were no longer rendered or tracked in memory.

### Step 4 — Autosave overwrote real data with empty tab
The autosave timer (5 seconds) fired. SaveApplicationData() called
tabManager.GetSaveData() which returned the 1 blank tab. This was
written to WindowData.Notes and saved to master.json via
NoteWindowManager.SaveNoteWindows(). All real note content overwritten.

### Step 5 — Settings saved Light theme, creating a permanent loop
SaveApplicationData() includes:
  currentSettings.Theme = themeManager.IsDarkMode ? "Dark" : "Light"
This saved isDarkMode=false to settings.json. Every subsequent startup
loaded LightTheme → LoadTabs failed → blank tab → autosave overwrote
data → repeat. The loop was self-sustaining.

### Additional factor — Double OnThemeChanged fire
ThemeManager.ToggleTheme() invoked OnThemeChanged twice per toggle:
once inside ApplyTheme() at line 60, and again in ToggleTheme() at line 21.
This fired hasUnsavedChanges=true sooner, accelerating the autosave trigger.

## Data Recovery

AtomicFileManager rolling backups preserved pre-damage state.
Timeline identified via backup file sizes and timestamps:

  21:04:31 — 41,645 bytes (good, pre-toggle)
  21:05:48 — 41,805 bytes (last good save, 3 notes + 13 images)
  21:05:53 — 3,217 bytes ← DATA LOSS (blank tab autosaved)
  21:05:57 — 3,218 bytes (subsequent saves, all empty)

Recovery file: master-20260501-210553.json (41,805 bytes)
Restored via PowerShell Copy-Item WHILE APP WAS FULLY STOPPED.
First restore attempt failed because dotnet run's in-memory state
wrote the bad data back over the restore within seconds.

## Fix Applied

### 1. Removed Toggle Theme button entirely
Removed from MainWindow.xaml (Button block) and MainWindow.xaml.cs
(ToggleTheme_Click handler). LightTheme is not ready for use.
ThemeManager and ThemeManager.OnThemeChanged remain for future use
when LightTheme is complete.

### 2. Fixed settings.json on disk
Set isDarkMode=true, theme="Dark" via PowerShell while app was stopped.
This breaks the startup loop immediately.

### 3. Fixed PackIcon name
Kind="Pushpin" → "ThumbTack" (attempt 1, still wrong) → "PinOutline"
(correct). Verified via binary search of the MDIX 5.3.1 DLL:
  $text.Contains("ThumbTack") → False
  $text.Contains("PinOutline") → True

## Testing & Acceptance

- Build: 0 errors, 0 warnings (after fix)
- App opens correctly on dark theme
- LoadTabs succeeds (ContentCardStyle present in DarkTheme)
- Notes load and display correctly from master.json
- Write note → close → reopen: data persists correctly
- Theme toggle button gone from title bar

## Performance & Limits

No performance impact. This was a configuration and UI correctness fix.

## Follow-ups

- LightTheme.xaml needs all 21 missing keys added before a toggle can
  be safely re-exposed. Keys required:
  AccentGlowEffect, AccentGradientBrush, ActiveGlow, AppFontFamily,
  BodyFontSize, ContentCardBrush, ContentCardHoverStyle, ContentCardStyle,
  DangerGlow, EditorFocusGlow, EditorSurfaceStyle, HeaderCloseButtonStyle,
  HeaderPinButtonStyle, HeadingFontFamily, HeadingFontSize, HoverGlow,
  ModernScrollViewerStyle, NativeContextMenuStyle, SectionHoverGlow,
  SmallFontSize, SubtleDividerBrush

- LoadTabs catch block should not call CreateNewTab() when existing notes
  are present on disk. It should bail out and show an error, not silently
  overwrite data with an empty tab.

- ThemeManager.ApplyTheme() fires OnThemeChanged when it should not —
  ToggleTheme() already fires it. Remove the extra invoke from ApplyTheme()
  to prevent double-fire side effects.

## Graduated to CR

None. No reusable rules changed. See docs/BUGS.md §B-THEME for full log.
