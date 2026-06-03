---
Title: Sprint HYGIENE-2 — Gate Serilog MinimumLevel behind DEBUG
Date: 2026-05-19
Owner: Jeremy
Versions Affected: 1.7.0
Links:
 - Planning: docs/PLANNING.md §SPRINT HYGIENE-2
---

## Context & Goal

Serilog was hardcoded to `MinimumLevel.Debug()` in all builds. Production logs
record every Debug trace (file loads, migration checks, infrastructure traces)
filling log files on user machines. Production should only log Info/Warning/Error.

## Decision

Gate the minimum log level behind `#if DEBUG` preprocessor. Debug builds keep
full visibility; Release builds get clean logs.

## Implementation

File: `Infrastructure/Logging/LoggingService.cs`

- Added `#if DEBUG` / `#else` block before `try` in `CreateLogger()`
- Variable `minLevel` set to `LogEventLevel.Debug` in DEBUG, `LogEventLevel.Information` in Release
- Both main and fallback logger configs now use `.MinimumLevel.Is(minLevel)`

## Testing & Acceptance

- `dotnet build` — 0 errors
- Debug builds: logs Debug+ (unchanged behavior)
- Release builds: logs Info+ only (Debug traces suppressed)

## Follow-ups

None — HYGIENE-2 complete.

Architecture rules live in docs/PROJECT_MEMORY.md.
This note records implementation details and rationale.
