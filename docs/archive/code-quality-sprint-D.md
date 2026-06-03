# Code Quality Sprint D — Dead Code, Hygiene, Dependencies, Style
# Issues: §5, §10, §23, §29
# Apply after Sprint C passes build.

Read every file listed before touching anything.

## RULES
- One fix at a time, build after each
- Do NOT delete anything load-bearing without verifying zero references first
- Do NOT change any public API surface used across files without checking all call sites
- Build gate: `dotnet build` — 0 errors, 0 warnings after every fix
- Write devnote after all fixes pass build (format at bottom)

---

## FIX 1 — Exclude Examples/ folder from compiled assembly
**File:** `SnipShottyBoard.csproj`
**Finding:** §5 Dead Code, §28 Release Readiness

The `Examples/` folder at the repo root (if present) is compiled into the shipping
assembly. It should be documentation only, not compiled code.

Read `SnipShottyBoard.csproj`. Check if an `Examples/` directory exists:

```
ls C:\Users\Soy\Documents\Repos\SnipShottyBoard\Examples\
```

If it exists and contains `.cs` files, add to csproj inside the first `<PropertyGroup>`:
```xml
<Compile Remove="Examples\**\*.cs" />
<EmbeddedResource Remove="Examples\**" />
<None Remove="Examples\**" />
<None Include="Examples\**" />
```

If `Examples/` doesn't exist or contains only .md files, skip this fix.

---

## FIX 2 — Remove duplicate XML doc comments in DataManager
**File:** `Core/Managers/DataManager.cs`
**Finding:** §10 Codebase Hygiene

`SaveNotes()` and `SaveNoteWindows()` each have two `<summary>` blocks stacked on top
of each other — one from an earlier sprint and one added later. Find and remove the
duplicate (keep the more detailed one, remove the shorter first one).

Search for `/// <summary>` followed immediately by another `/// <summary>` on the next
non-blank line. Remove the duplicate block. Do not change any actual code.

---

## FIX 3 — Fix version mismatch in csproj
**File:** `SnipShottyBoard.csproj`
**Finding:** §10 Codebase Hygiene, §21 Hygiene

Read the csproj. Find `<AssemblyVersion>` and `<FileVersion>`. If they say `1.6.0.0`
while `<Version>` says `1.7.0`, sync them:

```xml
<!-- Old: -->
<Version>1.7.0</Version>
<AssemblyVersion>1.6.0.0</AssemblyVersion>
<FileVersion>1.6.0.0</FileVersion>

<!-- New: -->
<Version>1.7.0</Version>
<AssemblyVersion>1.7.0.0</AssemblyVersion>
<FileVersion>1.7.0.0</FileVersion>
```

Also check `VERSION` file at repo root — if it exists and says `1.6.0`, update to `1.7.0`.

---

## FIX 4 — Remove CommunityToolkit.Mvvm if only ObservableCollection is used
**File:** `SnipShottyBoard.csproj`
**Finding:** §23 Dependencies

`ObservableCollection<T>` is in `System.Collections.ObjectModel` (BCL) — it does NOT
require CommunityToolkit.Mvvm. If CommunityToolkit is only imported for this type,
the package is dead weight: longer build times, larger binary.

**Step 1 — verify usage:** Search the entire codebase for `CommunityToolkit` in using
statements and `[ObservableProperty]`, `[RelayCommand]`, `ObservableObject`,
`ObservableValidator`, or any other Toolkit-specific attribute/base class.

```
grep -r "CommunityToolkit" --include="*.cs" .
grep -r "ObservableObject\|RelayCommand\|ObservableProperty\|ObservableValidator" --include="*.cs" .
```

**Step 2:** If zero results (or only `ObservableCollection` which is BCL):
- Remove from csproj: `<PackageReference Include="CommunityToolkit.Mvvm" .../>`
- Verify `NoteWindowManager.cs` uses `System.Collections.ObjectModel.ObservableCollection`
  (already in BCL — no package needed)
- Build and verify

**Step 3:** If any Toolkit-specific features ARE used: do not remove the package.
Document which features are used in the devnote instead.

---

## FIX 5 — Standardize private field naming to _camelCase
**Files:** `UI/TabManager.cs`, `UI/MediaSection.xaml.cs`, `UI/Views/MainWindow.xaml.cs`
**Finding:** §29 Code Style

New code uses `_camelCase`. Old code uses bare `camelCase` for private fields.
Mixed convention makes the codebase inconsistent.

Read each file. Rename private instance fields that use bare `camelCase` to `_camelCase`.

**TabManager.cs — fields to rename:**
```csharp
// These are private instance fields — rename:
tabs → _tabs
selectedTab → _selectedTab
isDragging → _isDragging
dragStartPoint → _dragStartPoint
draggedTab → _draggedTab
dragCanvas → _dragCanvas
dragVisual → _dragVisual
dropIndicator → _dropIndicator
draggedTabOriginalIndex → _draggedTabOriginalIndex
dropTargetIndex → _dropTargetIndex
lastDropTargetIndex → _lastDropTargetIndex
appSettings → _appSettings
skipDeleteConfirmation → _skipDeleteConfirmation
tabHeaderPanel → _tabHeaderPanel (readonly)
tabContentArea → _tabContentArea (readonly)
```

**MainWindow.xaml.cs — fields to rename:**
```csharp
tabManager → _tabManager
themeManager → _themeManager
statusBarManager → _statusBarManager
keyboardHandler → _keyboardHandler
helpManager → _helpManager
settingsManager → _settingsManager
currentSettings → _currentSettings
autoSaveTimer → _autoSaveTimer
statusTimer → _statusTimer
recoveryTimer → _recoveryTimer
hasUnsavedChanges → _hasUnsavedChanges
```

**IMPORTANT:** These are private fields. Update ALL references within the same file.
Do not change: public properties, method names, local variables, parameters.
Do not rename fields in other files unless you've read them in full first.

After renaming in each file: build immediately. Fix any missed references before moving
to the next file.

---

## FIX 6 — Extract hardcoded resource key strings to ResourceKeys class
**Finding:** §29 Code Style

Resource keys like `"AccentBrush"`, `"AppForegroundBrush"`, `"TabButtonStyle"` appear
as raw string literals across many files. A typo causes a silent runtime null. A central
constants class catches these at compile time.

**Step 1** — Read `Resources/DarkTheme.xaml` and collect every `x:Key` value used
in code-behind (not XAML — XAML references are fine as strings).

**Step 2** — Create `UI/ResourceKeys.cs`:
```csharp
namespace SnipShottyBoard.UI
{
    /// <summary>
    /// Compile-time constants for DarkTheme.xaml resource keys.
    /// Use instead of raw string literals to catch typos at compile time.
    /// </summary>
    public static class ResourceKeys
    {
        public const string AccentBrush = "AccentBrush";
        public const string AppForegroundBrush = "AppForegroundBrush";
        public const string AppBackgroundBrush = "AppBackgroundBrush";
        public const string TabBackgroundBrush = "TabBackgroundBrush";
        public const string ContentCardBrush = "ContentCardBrush";
        public const string TabButtonStyle = "TabButtonStyle";
        public const string NativeContextMenuStyle = "NativeContextMenuStyle";
        public const string AccentGlowEffect = "AccentGlowEffect";
        public const string EditorFocusGlow = "EditorFocusGlow";
        // Add every key found in code-behind
    }
}
```

**Step 3** — In `TabManager.cs`, replace the most frequent string literal usages:
```csharp
// Old:
Application.Current.FindResource("AccentBrush")
Application.Current.FindResource("TabBackgroundBrush")

// New:
Application.Current.FindResource(ResourceKeys.AccentBrush)
Application.Current.FindResource(ResourceKeys.TabBackgroundBrush)
```

Replace in `TabManager.cs` only for this sprint. Other files can be updated incrementally.
Do not do a global find-replace across all files in one pass — too risky.

---

## AFTER ALL 6 FIXES PASS BUILD

Write devnote to:
`docs/devnotes/2026-05-19-code-quality-sprint-D.md`

```
# Code Quality Sprint D — Dead Code, Hygiene, Dependencies, Style
Date: 2026-05-19

## Fixes

FIX-1: Examples/ folder excluded from compiled assembly via csproj (if applicable)
FIX-2: Duplicate <summary> XML doc blocks removed from DataManager.cs
FIX-3: AssemblyVersion and FileVersion synced to 1.7.0.0 in csproj
FIX-4: CommunityToolkit.Mvvm removed (or retained — see note)
       ObservableCollection<T> is BCL; no package needed if only usage
FIX-5: Private field naming standardized to _camelCase in TabManager.cs + MainWindow.xaml.cs
FIX-6: ResourceKeys.cs created with compile-time constants for DarkTheme resource keys
        TabManager.cs updated to use ResourceKeys.X instead of raw string literals

## Build status
0 errors, 0 warnings
```
