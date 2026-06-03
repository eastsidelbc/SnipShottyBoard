using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using SnipShottyBoard.Data;

namespace SnipShottyBoard.Core.Utils
{
    /// <summary>
    /// Fixes white flash on resize, ghost caption buttons, and narrow resize hit areas.
    ///
    /// Problem: WPF FluentWindow with WindowBackdropType="None" exposes three issues.
    /// 1. DWM composition gap — during resize, the Win32 HWND erase brush paints white
    ///    before WPF renders. CompositionTarget.BackgroundColor sets the DWM gap color.
    /// 2. WM_ERASEBKGND — the default Win32 background brush paints during rapid resize.
    ///    Swallowing this message prevents the white flash.
    /// 3. Resize hit area — WindowChrome default is 1px. ui:TitleBar content control
    ///    intercepts top-edge mouse events, so WindowChrome.ResizeBorderThickness fails
    ///    on the top. WM_NCHITTEST at the WndProc level forces all 4 edges to work.
    /// </summary>
    public static class WindowChromeFix
    {
        #region Win32 Interop

        private const int WM_NCHITTEST = 0x0084;
        private const int WM_ERASEBKGND = 0x0014;

        private const int HTCLIENT = 1;
        private const int HTLEFT = 10;
        private const int HTRIGHT = 11;
        private const int HTTOP = 12;
        private const int HTTOPLEFT = 13;
        private const int HTTOPRIGHT = 14;
        private const int HTBOTTOM = 15;
        private const int HTBOTTOMLEFT = 16;
        private const int HTBOTTOMRIGHT = 17;

        [DllImport("user32.dll")]
        private static extern bool GetClientRect(IntPtr hwnd, out RECT lpRect);

        [DllImport("user32.dll")]
        private static extern int MapWindowPoints(IntPtr hwndFrom, IntPtr hwndTo, ref POINT lpPoint, int cPoints);

        [DllImport("user32.dll")]
        private static extern int MapWindowPoints(IntPtr hwndFrom, IntPtr hwndTo, ref RECT lpRect, int cPoints);

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int Left, Top, Right, Bottom;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT
        {
            public int X, Y;
        }

        #endregion

        /// <summary>
        /// Apply the chrome fix to a FluentWindow.
        /// Call once in the window constructor after InitializeComponent().
        /// </summary>
        public static void Apply(Window window, string brushResourceName = "AppBackgroundBrush")
        {
            window.SourceInitialized += (s, e) =>
            {
                var source = HwndSource.FromHwnd(new WindowInteropHelper(window).Handle);
                if (source?.CompositionTarget == null)
                    return;

                // Resolve theme brush at runtime — works for both dark and light themes.
                Color bgColor = Colors.Black;
                var brush = window.TryFindResource(brushResourceName) as SolidColorBrush;
                if (brush != null)
                    bgColor = brush.Color;

                source.CompositionTarget.BackgroundColor = bgColor;
                source.AddHook(WndProc);

                // Also set WindowChrome resize border (sides/bottom fallback; top is handled by NCHITTEST hook).
                SetResizeBorderThickness(window);
            };
        }

        /// <summary>
        /// Sets WindowChrome.ResizeBorderThickness to AppConstants.WindowResizeBorderThickness (8px).
        /// Must be called after SourceInitialized so FluentWindow chrome is already in place.
        /// </summary>
        public static void SetResizeBorderThickness(Window window)
        {
            var existing = System.Windows.Shell.WindowChrome.GetWindowChrome(window);
            if (existing == null) return;

            existing.ResizeBorderThickness = new Thickness(AppConstants.WindowResizeBorderThickness);
            System.Windows.Shell.WindowChrome.SetWindowChrome(window, existing);
        }

        private static IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WM_ERASEBKGND)
            {
                handled = true;
                return new IntPtr(1);
            }

            if (msg == WM_NCHITTEST)
            {
                IntPtr result = HandleNCHitTest(hwnd, lParam);
                if (result != IntPtr.Zero)
                    handled = true; // Block ui:TitleBar from overriding
                return result;
            }

            return IntPtr.Zero;
        }

        /// <summary>
        /// Forces 8px resize hit area on all 4 edges at the Win32 level.
        /// Runs before WPF content controls (like ui:TitleBar) can intercept.
        /// </summary>
        private static IntPtr HandleNCHitTest(IntPtr hwnd, IntPtr lParam)
        {
            int x = (short)(lParam.ToInt32() & 0xFFFF);
            int y = (short)(lParam.ToInt32() >> 16);

            // Convert screen coords to client coords.
            POINT pt = new POINT { X = x, Y = y };
            MapWindowPoints(IntPtr.Zero, hwnd, ref pt, 1);

            if (!GetClientRect(hwnd, out RECT client))
                return IntPtr.Zero;

            int thickness = AppConstants.WindowResizeBorderThickness;
            int clientWidth = client.Right - client.Left;
            int clientHeight = client.Bottom - client.Top;

            bool topEdge = pt.Y < thickness;
            bool bottomEdge = pt.Y >= clientHeight - thickness;
            bool leftEdge = pt.X < thickness;
            bool rightEdge = pt.X >= clientWidth - thickness;

            // Corners first (more specific), then edges.
            if (topEdge && rightEdge) return new IntPtr(HTTOPRIGHT);
            if (topEdge && leftEdge) return new IntPtr(HTTOPLEFT);
            if (bottomEdge && rightEdge) return new IntPtr(HTBOTTOMRIGHT);
            if (bottomEdge && leftEdge) return new IntPtr(HTBOTTOMLEFT);

            if (topEdge) return new IntPtr(HTTOP);
            if (bottomEdge) return new IntPtr(HTBOTTOM);
            if (leftEdge) return new IntPtr(HTLEFT);
            if (rightEdge) return new IntPtr(HTRIGHT);

            return IntPtr.Zero; // Let default handler return HTCLIENT.
        }
    }
}
