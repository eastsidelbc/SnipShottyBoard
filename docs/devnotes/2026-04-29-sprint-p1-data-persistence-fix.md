---
Title: Sprint P Phase 1 — Data Persistence Fix (AtomicFileManager camelCase bug)
Date: 2026-04-29
Owner: Jeremy
Versions Affected: 1.7.0
Links:
 - Planning: docs/PLANNING.md §SPRINT P
---

## Context & Goal

Fix a critical bug in `AtomicFileManager.cs` that causes ALL saved data (notes, tabs, images, window position, settings) to be lost on every app restart. This was the most critical bug in the codebase — users would type notes, close the app, reopen, and see a blank app every single time.

## Root Cause

`AtomicSave()` serializes with `PropertyNamingPolicy = JsonNamingPolicy.CamelCase`, writing JSON keys like `"windowLeft"`, `"isActive"`, `"notes"`, `"windows"`.

`LoadWithRecovery()` deserializes with NO options (default = PascalCase matching), looking for `"WindowLeft"`, `"IsActive"`, `"Notes"`, `"Windows"`.

Nothing matches. All properties return defaults (null, 0, false, empty). `MasterData.Windows = null` → no windows loaded → fresh empty window every startup.

## Implementation Notes

### Fix Applied — 4 Deserialize calls updated across 3 methods

All 4 calls now use:
```csharp
var jsonOptions = new JsonSerializerOptions
{
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    PropertyNameCaseInsensitive = true
};
```

**Method 1: LoadWithRecovery** — 2 calls fixed
- Primary file load (line 100): `JsonSerializer.Deserialize<T>(json, jsonOptions)`
- Backup file load (line 115): `JsonSerializer.Deserialize<T>(json, jsonOptions)`
- jsonOptions object created once at top of method, reused for both calls

**Method 2: VerifyJsonFile** — 1 call fixed
- Verification check: `JsonSerializer.Deserialize<T>(json, jsonOptions)`
- Without this fix, VerifyJsonFile would succeed on camelCase JSON but LoadWithRecovery would still fail

**Method 3: TryRollingBackupRecovery** — 1 call fixed
- Rolling backup load: `JsonSerializer.Deserialize<T>(json, jsonOptions)`

### Why PropertyNameCaseInsensitive = true

Defense in depth. If someone saves a file with PascalCase JSON (e.g., from an older version or manual edit), the deserializer will still match correctly. This makes the load path tolerant of both naming conventions.

### What Was NOT Changed

- `AtomicSave()` — correctly uses camelCase, left untouched
- `WriteVerificationInfo()` — writes metadata, not data, left untouched
- `GetBackupInfo()` — reads file system info only, no deserialization
- No other files touched

## Testing & Acceptance

- [x] `dotnet build` passes with 0 errors, 0 new warnings
- [ ] Run app → type text in a tab → close app → reopen → text is still there
- [ ] Run app → add an image → close → reopen → image is still there
- [ ] Run app → resize window → close → reopen → window position restored
- [ ] Run app → create a new tab and rename it → close → reopen → tab persists

**NOTE:** First run after the fix will start fresh (previous data was never loaded correctly). From that first run forward, everything persists normally.

## Performance & Limits

- Negligible — JsonSerializerOptions allocation is tiny and happens once per load operation
- No change to save path performance

## Follow-ups

- Test data persistence manually (see checklist above)
- Continue to Sprint D (GIF Viewer)

Architecture rules live in docs/PROJECT_MEMORY.md.
This note records implementation details and rationale.
