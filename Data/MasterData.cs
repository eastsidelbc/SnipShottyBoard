using System.Collections.Generic;
using SnipShottyBoard.Core.Managers;
using SnipShottyBoard.Core.Models;

namespace SnipShottyBoard.Data
{
    /// <summary>
    /// Single source of truth for all application data.
    /// Replaces notes.json + notewindows.json + settings.json consolidation.
    /// </summary>
    public class MasterData
    {
        /// <summary>
        /// Schema version of the master file format.
        /// Version 1: initial format with Windows + Settings.
        /// </summary>
        public int Version { get; set; } = 1;

        /// <summary>
        /// All note windows (each window contains its own notes).
        /// </summary>
        public List<NoteWindowData> Windows { get; set; } = new List<NoteWindowData>();

        /// <summary>
        /// Global application settings.
        /// </summary>
        public AppSettings Settings { get; set; } = new AppSettings();
    }
}
