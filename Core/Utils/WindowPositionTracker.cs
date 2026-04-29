using System;
using System.Windows;
using System.Windows.Threading;

namespace SnipShottyBoard.Core.Utils
{
    /// <summary>
    /// Debounces window position and size saves.
    /// 
    /// Problem it solves:
    /// When a user drags a window, Windows fires LocationChanged
    /// hundreds of times per second. Without debouncing, each event
    /// would trigger a full JSON save to disk — causing lag and
    /// excessive disk writes during normal window dragging.
    /// 
    /// Solution:
    /// This tracker waits until the window has been still for
    /// DebounceMilliseconds before triggering the save callback.
    /// The timer resets on every position change — so the save
    /// only happens once the user stops moving the window.
    /// </summary>
    public class WindowPositionTracker : IDisposable
    {
        // How long to wait after the last position change before saving
        private const int DebounceMilliseconds = 500;

        private readonly DispatcherTimer _debounceTimer;
        private readonly Action _onPositionSettled;
        private readonly Window _window;
        private bool _disposed;

        /// <summary>
        /// Creates a WindowPositionTracker attached to the given window.
        /// The onPositionSettled callback fires after the window stops moving.
        /// </summary>
        /// <param name="window">The WPF window to track</param>
        /// <param name="onPositionSettled">
        /// Callback that fires after debounce period — do your save here
        /// </param>
        public WindowPositionTracker(Window window, Action onPositionSettled)
        {
            _window = window ?? throw new ArgumentNullException(nameof(window));
            _onPositionSettled = onPositionSettled
                ?? throw new ArgumentNullException(nameof(onPositionSettled));

            _debounceTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(DebounceMilliseconds)
            };
            _debounceTimer.Tick += OnDebounceTimerTick;

            // Attach to window events
            _window.LocationChanged += OnWindowPositionChanged;
            _window.SizeChanged += OnWindowSizeChanged;
        }

        /// <summary>
        /// Fires every time the window moves — resets the debounce timer.
        /// </summary>
        private void OnWindowPositionChanged(object? sender, EventArgs e)
        {
            ResetTimer();
        }

        /// <summary>
        /// Fires every time the window is resized — resets the debounce timer.
        /// </summary>
        private void OnWindowSizeChanged(object? sender, SizeChangedEventArgs e)
        {
            ResetTimer();
        }

        /// <summary>
        /// Resets the debounce timer.
        /// Called on every position or size change.
        /// </summary>
        private void ResetTimer()
        {
            _debounceTimer.Stop();
            _debounceTimer.Start();
        }

        /// <summary>
        /// Fires after the window has been still for DebounceMilliseconds.
        /// Triggers the save callback.
        /// </summary>
        private void OnDebounceTimerTick(object? sender, EventArgs e)
        {
            _debounceTimer.Stop();
            _onPositionSettled?.Invoke();
        }

        /// <summary>
        /// Force an immediate save without waiting for debounce.
        /// Call this on window close to ensure position is saved.
        /// </summary>
        public void SaveNow()
        {
            _debounceTimer.Stop();
            _onPositionSettled?.Invoke();
        }

        /// <summary>
        /// Detaches all event handlers and stops the timer.
        /// Call this when the window is closing.
        /// </summary>
        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            _debounceTimer.Stop();
            _debounceTimer.Tick -= OnDebounceTimerTick;

            _window.LocationChanged -= OnWindowPositionChanged;
            _window.SizeChanged -= OnWindowSizeChanged;
        }
    }
}
