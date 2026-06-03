---
Title: Sprint HYGIENE-1 — Issue 1: Gate debugImageLogging behind DEBUG
Date: 2026-05-19
Owner: Jeremy
Versions Affected: 1.7.0
Links:
 - Planning: docs/PLANNING.md §SPRINT HYGIENE-1
---

## Context & Goal

`debugImageLogging` was hardcoded to `true` in production builds, causing verbose image load logs in Release builds.

## Decision

Gate the field behind `#if DEBUG` so Release builds get `false`.

## Implementation

File: `UI/ImageViewerWindow.xaml.cs` (line 46)

Wrapped the static field with preprocessor directives:
- `#if DEBUG` → `debugImageLogging = true`
- `#else` → `debugImageLogging = false`

## Testing & Acceptance

- `dotnet build` — 0 errors
- Release builds no longer produce image load debug logs

## Follow-ups

None — Issue 1 of 4 in HYGIENE-1 complete.

Architecture rules live in docs/PROJECT_MEMORY.md.
This note records implementation details and rationale.
