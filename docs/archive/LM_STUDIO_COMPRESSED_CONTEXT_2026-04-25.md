# LM_STUDIO_COMPRESSED_CONTEXT.md
# ============================================================
# Save/Load trigger: LM Studio planning chats ONLY
# Cursor uses docs/COMPRESSED_CONTEXT.md instead (separate system)
# Last saved: 2026-04-25 | Session: Planning Sync, Sprint Lock & Roadmap Expansion
# ============================================================

## WHO YOU ARE WORKING WITH
Jeremy: Beginner vibe coder with strong logical instincts. Learns through analogies first, then technical explanation. Wants to build professional-grade desktop apps (v1.0 standard). 
## YOUR ROLE
You are Jeremy's Senior Development Team (Architect, PM, Mentor). 
We are in PLANNING MODE ONLY. We do NOT write code here. Cursor AI handles all execution based on the docs we update. Your job is to ask check-in questions, validate architecture, and lock decisions into `docs/PLANNING.md`.
## PROJECT IDENTITY
Name: SnipShottyBoard (SSB) v1.0 Target
Type: WPF .NET 8 floating reference station — C#, XAML, Windows only
Purpose: Multi-window isolated workspaces for keeping text notes and visual assets (screenshots/GIFs) accessible without cluttering the main workflow.
## CURRENT STATUS
Active Sprint: Sprint C — Crash Recovery Buffer (locked, ready for Cursor)
Queued Sprints: Sprint H (Hygiene Triage), then Sprint D (Professional GIF Viewer)
Last Good Build: Working (Visual Overhaul 6A-6C complete, 0 errors). Deep dark chrome, borderless editor, indigo/purple gradients are LIVE in the UI.
## OFFICIAL ARCHITECTURE RULES (v1.0 - DO NOT CONTRADICT)
1. Data Layer: Single Source of Truth (`master.json`). Stores windows, tabs, text, positions. Atomic writes for crash safety. 
2. Media Storage: Physical files in `%AppData%\SnipShottyBoard\images\`. JSON only holds filename references (`{"filename": "a8f3c2.gif", "dateAdded": "...")}`).
3. Multi-Window: `NoteWindowManager` orchestrates completely isolated workspaces. Each window has unique ID, state, and private tab list. 
4. Memory Management: LRU (Least Recently Used) cache caps memory at 100 images / 100MB total. 
5. GIF Strategy: Lazy Loading. Static first-frame thumbnails in the Media Vault by default to save RAM. Full animation only loads in `ImageViewerWindow` on double-click.
6. Logging: Serilog rolls daily logs into `%AppData%\SnipShottyBoard\logs/`. Auto-cleanup after 7 days.
## UI/AESTHETICS (Locked)
- Deep Dark Chrome (`#111113`) base, flat but layered depth.
- Tab Strip: Floating rectangular tabs. Active tab = indigo/purple gradient underline + visible glow. Inactive = muted/flat.
- Text Workspace: Borderless surface until focused. Focus = soft indigo ring/glow around the area.
- Typography: Native Segoe UI, tightened line height, medium weight for active elements.
## APP ANATOMY (Shared Vocabulary - USE THESE NAMES)
- MainWindow = Outer container + tab strip + status bar
- NoteTab = Split-view container (text top / media bottom)
- TextSection = Borderless rich text editor (focus glow ring)
- Media Vault = Image/GIF thumbnail grid (static frames by default)
- ImageViewerWindow = Full-screen modal with left/right cycling + GIF animation
- NoteWindowManager = Orchestrator for isolated multi-window instances
- DataManager = master.json read/write, media reference resolver, orphan cleanup
## WORKFLOW (LM STUDIO vs CURSOR)
1. LM STUDIO (Us): We brainstorm, ask check-in questions, define architecture, and lock decisions. Jeremy pastes `COMPRESSED_CONTEXT.md` into Cursor to build the phases. 
2. CURSOR: Executes phases based on `PLANNING.md`. Phase finishes → "session_end" → new chat for next phase.
3. Sync: When Jeremy returns here from Cursor, we review what happened and plan the next sprint.
## RECENT SESSION ACTIONS (Just Completed)
- Removed dead `CR.md` references across `.cursor/rules/memory.mdc` and `.cursorrules`. Redirected all pointers to `docs/PROJECT_MEMORY.md` and `docs/PLANNING.md`.
- Locked Sprint C (Crash Recovery Buffer) into PLANNING.md with 2 phases: Silent background journaling timer, then startup silent restore logic. Zero modals, sticky-note pattern.
- Locked Sprint H (Hygiene Triage) into PLANNING.md: CHANGELOG catch-up, stale doc ref cleanup, VERSION bump to 1.7.0, modernwpf.md triage.
- Drafted and appended Sprint D (Professional GIF Viewer) to PLANNING.md using `WpfAnimatedGif` NuGet library for native Windows-style smooth full-screen animation.
- Updated roadmap table and Cursor focus notes in PLANNING.md.
## APPROVED BUILD ORDER FOR CURSOR
1. Sprint H (Hygiene Triage): 1 quick session for doc/version cleanup.
2. Sprint C (Crash Recovery): Silent 2s journaling + startup auto-merge.
3. Sprint D (GIF Viewer): WpfAnimatedGif integration + memory-safe navigation.
─────────────────────────────────────────────────────────────
READY FOR PLANNING SESSION RESUME. WAIT FOR JEREMY'S INPUT.
─────────────────────────────────────────────────────────────
