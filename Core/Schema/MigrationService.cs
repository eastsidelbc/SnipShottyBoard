using System;
using System.Collections.Generic;
using SnipShottyBoard.Core.Managers;
using SnipShottyBoard.Core.Models;
using SnipShottyBoard.Data;

namespace SnipShottyBoard.Core.Schema
{
    /// <summary>
    /// Handles migration of saved data between schema versions.
    /// Called by DataManager on load when it detects an older data format.
    /// 
    /// Migration rules:
    /// - Always migrate forward — never backward
    /// - Migrations are additive — never delete data
    /// - If migration fails — log the error and use the original data
    /// - Each migration method handles one specific version transition
    /// </summary>
    public static class MigrationService
    {
        // Current schema versions — increment when model structures change
        public const int CurrentMasterVersion = 1;
        public const int CurrentNoteSchemaVersion = 2; // 2: ImageFiles → Media (filename-only refs)
        public const int CurrentWindowSchemaVersion = 1;
        public const int CurrentSettingsVersion = 1;

        /// <summary>
        /// Migrates a list of SavedNote objects to the current schema version.
        /// Called when loading notes.json (legacy single-window format).
        /// Returns the migrated list — original list is not modified.
        /// </summary>
        public static List<SavedNote> MigrateNotes(List<SavedNote>? notes)
        {
            if (notes == null)
                return new List<SavedNote>();

            var migrated = new List<SavedNote>(notes);

            // Version 0 → 1: No structural changes needed for initial migration
            // Add future version migrations here as else-if blocks:
            // if (note.DataVersion < 2) { /* migrate 1 → 2 */ }

            return migrated;
        }

        /// <summary>
        /// Migrates NoteWindowData objects to the current schema version.
        /// Called when loading notewindows.json (multi-window format).
        /// Returns the (potentially modified) windows list.
        /// </summary>
        public static List<NoteWindowData> MigrateNoteWindows(List<NoteWindowData>? windows)
        {
            if (windows == null || windows.Count == 0)
                return new List<NoteWindowData>();

            foreach (var window in windows)
            {
                // Version 0 → 1: Ensure all notes have valid defaults
                foreach (var note in window.Notes)
                {
                    // Ensure SplitterTextMediaRatio is within valid bounds
                    if (note.SplitterTextMediaRatio <= 0 || note.SplitterTextMediaRatio >= 1)
                        note.SplitterTextMediaRatio = 0.5;

                    // Ensure Title is never null or empty
                    if (string.IsNullOrWhiteSpace(note.Title))
                        note.Title = "Note";
                }

                // Version 1 → 2: Migrate full-path ImageFiles → filename-only Media refs
                foreach (var note in window.Notes)
                {
                    MigrateNoteToMediaRefs(note);
                }
            }

            // Add future version migrations here
            // if (window.SchemaVersion < 2) { /* migrate 1 → 2 */ }

            return windows;
        }

        /// <summary>
        /// Migrates AppSettings to the current schema version.
        /// Called when loading settings.json.
        /// Returns the migrated settings object.
        /// </summary>
        public static AppSettings MigrateAppSettings(AppSettings? settings)
        {
            if (settings == null)
                return new AppSettings(); // Return fresh defaults

            // Version 0 → 1: Clamp any out-of-range values to valid bounds
            settings.AutoSaveIntervalSeconds = Math.Clamp(
                settings.AutoSaveIntervalSeconds, 1, 300);

            settings.FontSize = Math.Clamp(
                settings.FontSize, 8, 72);

            // Add future version migrations here
            // if (settings.SettingsVersion < 2) { /* migrate 1 → 2 */ }

            return settings;
        }

        /// <summary>
        /// Migrates MasterData to the current schema version.
        /// Called when loading master.json.
        /// Returns the migrated master data object.
        /// </summary>
        public static MasterData MigrateMasterData(MasterData? masterData)
        {
            if (masterData == null)
                return new MasterData();

            // Migrate nested windows and their notes
            masterData.Windows = MigrateNoteWindows(masterData.Windows);

            // Migrate nested settings
            masterData.Settings = MigrateAppSettings(masterData.Settings);

            // Version 0 → 1: Ensure non-null defaults
            masterData.Windows ??= new List<NoteWindowData>();
            masterData.Settings ??= new AppSettings();

            return masterData;
        }

        /// <summary>
        /// Migrates a single note from full-path ImageFiles to filename-only Media references.
        /// 
        /// Note: The actual data conversion happens automatically via the backward-compatible
        /// ImageFiles/ImageTimestamps property setters during JSON deserialization. This method
        /// ensures the version stamp is bumped and Media is non-null.
        /// </summary>
        private static void MigrateNoteToMediaRefs(SavedNote note)
        {
            if (note.DataVersion < CurrentNoteSchemaVersion)
            {
                // Ensure Media list is initialized
                note.Media ??= new List<MediaReference>();

                note.DataVersion = CurrentNoteSchemaVersion;
            }
        }
    }
}
