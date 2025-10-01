#if DEBUG
using System;
using System.Diagnostics;
using Serilog;

namespace SnipShottyBoard.Infrastructure.Diagnostics
{
    // ✅ DEBUG-only: isolates verbose logging out of Release builds
    public static class GifDiagnostics
    {
        private static readonly ILogger LogGif = Log.ForContext("Area", "GIF");

        public static void LogOpen(string path, string ext, (int w, int h) pixels, bool isGif,
                                   string strategy, int screenW, int screenH,
                                   int? decodeW, int? decodeH, double dpix, double dpiy,
                                   string cacheOption, string createOptions)
        {
            LogGif.Information("Open {Path} ext={Ext} isGif={IsGif} strat={Strategy} px={W}x{H} screen={SW}x{SH} decode={DW}x{DH} dpi={DPIX}x{DPIY} cache={Cache} create={Create}",
                path, ext, isGif, strategy, pixels.w, pixels.h, screenW, screenH, decodeW, decodeH, dpix, dpiy, cacheOption, createOptions);
        }

        public static void LogGifMeta(string path, int frameCount, int totalDurationMs, string delaysCsv, int? loopCount)
        {
            LogGif.Information("Meta {Path} frames={Frames} total={Total}ms delays=[{Delays}] loop={Loop}",
                path, frameCount, totalDurationMs, delaysCsv, loopCount);
        }

        public static void LogFrameTick(int index)
        {
            LogGif.Debug("Tick frame={Index}", index);
        }

        public static void LogClose(long wsBeforeMB, long wsAfterMB, long gcBeforeMB, long gcAfterMB)
        {
            LogGif.Information("Close WS={WSb}->{WSa}MB GC={GCb}->{GCa}MB",
                wsBeforeMB, wsAfterMB, gcBeforeMB, gcAfterMB);
        }

        public static void LogError(string path, string step, Exception ex)
        {
            LogGif.Error(ex, "GIF error at {Step} for {Path}", step, path);
        }
    }
}
#endif
