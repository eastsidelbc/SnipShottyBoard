---
Title: B-SAVE — ContentCardStyle Forward Reference Caused Repeating Save Corruption
Date: 2026-05-01
Owner: Jeremy
Versions Affected: v1.7.0 (post Sprint UI-1)
Links:
  - Planning: docs/PLANNING.md §Sprint UI-1
  - Bug Log: docs/BUGS.md#b-save
---

Architecture rules live in docs/PROJECT_MEMORY.md.
This note records implementation details and rationale.

---

## Context & Goal

After the B-THEME session, notes and images were still disappearing on every
restart even with dark mode locked in. The symptom was identical: blank tab
on startup, real data gone. Rolling backup (master-20260501-225631.json,
11,335 bytes, 874 chars + 9 images) confirmed data was intact on disk but
being silently overwritten each session.

---

## Root Cause

During Sprint UI-1, `ContentCardHoverStyle` was rewritten to inherit from
`ContentCardStyle`:

```xml
<Style x:Key="ContentCardHoverStyle" TargetType="Border"
       BasedOn="{StaticResource ContentCardStyle}">
```

`ContentCardHoverStyle` was placed at **line 465** in DarkTheme.xaml.
`ContentCardStyle` was defined at **line 984** — 519 lines later.

WPF `StaticResource` requires the referenced resource to be **already parsed**
at the point of reference. This is a hard ordering constraint: it cannot
forward-reference within the same ResourceDictionary. The parser encounters
`StaticResource ContentCardStyle` at line 465 but hasn't parsed line 984 yet.

At runtime this threw:
```
XamlParseException: Cannot find resource named 'ContentCardStyle'
```

This happened inside `NoteTab.xaml` initialization (when `ContentCardHoverStyle`
is applied to the Border elements). `LoadTabs` caught it and called
`CreateNewTab()` as a fallback — producing one blank tab. Five seconds later
the autosave fired, saved the blank tab to master.json, and the loop was
established for every subsequent restart.

---

## Decisions & Alternatives

**Option A (chosen): Move ContentCardStyle before ContentCardHoverStyle**
Simple file reordering. No logic change. Preserves the `BasedOn` inheritance
(ContentCardHoverStyle still inherits CornerRadius=6 and Margin=0,4 from base).

**Option B: Make ContentCardHoverStyle standalone (no BasedOn)**
Would require duplicating CornerRadius and Margin setters. Slightly more
verbose but immune to ordering issues. Rejected — unnecessary duplication.

**Option C: Change BasedOn to DynamicResource**
Not supported by WPF. `BasedOn` only accepts `StaticResource`. N/A.

---

## Implementation Notes

### Fix 1 — DarkTheme.xaml

Removed `ContentCardStyle` from line 983 and inserted it at line 464,
immediately before `ContentCardHoverStyle`. Both styles are now in correct
order: base defined first, derived second.

New ordering in file:
- Line 465: `ContentCardStyle` (base — CornerRadius 6, ContentCardBrush bg, shadow)
- Line 482: `ContentCardHoverStyle` (derived — transparent bg, glow triggers)

### Fix 2 — TabManager.cs: LoadTabs catch block

Before:
```csharp
catch (Exception ex)
{
    OnLogError?.Invoke("Error loading tabs", ex);
    CreateNewTab(); // Fallback to default tab
}
```

After:
```csharp
catch (Exception ex)
{
    OnLogError?.Invoke("Error loading tabs", ex);
    // Do NOT create a blank tab — rethrow so caller decides safely.
    throw;
}
```

### Fix 3 — MainWindow.xaml.cs: LoadApplicationData

Wrapped `tabManager.LoadTabs(notesToLoad)` in its own try/catch.
If it throws: creates blank tab for the session but sets
`hasUnsavedChanges = false` immediately, preventing the autosave timer
from overwriting real notes on disk.

```csharp
try
{
    tabManager.LoadTabs(notesToLoad);
    tabManager.RefreshTabVisuals();
}
catch (Exception loadEx)
{
    loggingService.LogError("CRITICAL: LoadTabs failed — data preserved on disk, ...", loadEx, "Data");
    tabManager.CreateNewTab();
    hasUnsavedChanges = false; // ← prevents autosave from corrupting disk
}
```

---

## Testing & Acceptance

- dotnet build: 0 errors ✅
- App launched: notes and images loaded correctly ✅
- Data persisted across close + reopen ✅
- Real notes restored from backup confirmed intact ✅

---

## Performance & Limits

No performance impact. Pure ordering fix in XAML + exception handling in C#.

---

## Follow-ups

1. **Sprint R** (Code Health): Audit all `StaticResource` references in
   DarkTheme.xaml for any other forward-reference violations. Any `StaticResource`
   used in a style should have its source defined EARLIER in the file.
2. **Longer term**: Consider switching all non-`BasedOn` resource references
   to `DynamicResource` to be immune to ordering issues.
3. The `LoadTabs` safety pattern (hasUnsavedChanges = false on failure) should
   be documented in PROJECT_MEMORY.md as a rule.
