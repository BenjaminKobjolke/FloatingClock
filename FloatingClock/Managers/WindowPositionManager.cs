using System;
using System.Windows;

namespace FloatingClock.Managers
{
    /// <summary>
    /// Screen corner positions for docking
    /// </summary>
    public enum Corner
    {
        TopLeft = 1,
        TopRight = 2,
        BottomLeft = 3,
        BottomRight = 4
    }

    /// <summary>
    /// Represents a calculated window position
    /// </summary>
    public struct WindowPosition
    {
        public double Left { get; set; }
        public double Top { get; set; }
    }

    /// <summary>
    /// Manages window positioning and corner docking logic
    /// </summary>
    public class WindowPositionManager
    {
        private readonly MonitorManager monitorManager;

        public WindowPositionManager(MonitorManager monitorManager)
        {
            this.monitorManager = monitorManager ?? throw new ArgumentNullException(nameof(monitorManager));
        }

        /// <summary>
        /// Calculates the position for docking a window to a specific corner
        /// </summary>
        /// <param name="corner">The corner to dock to</param>
        /// <param name="monitor">The monitor to dock on</param>
        /// <param name="window">The window to position</param>
        /// <param name="windowWidth">The width of the window</param>
        /// <param name="windowHeight">The height of the window</param>
        /// <returns>The calculated position</returns>
        public WindowPosition CalculateCornerPosition(
            Corner corner,
            System.Windows.Forms.Screen monitor,
            Window window,
            double windowWidth,
            double windowHeight)
        {
            if (monitor == null)
                throw new ArgumentNullException(nameof(monitor));

            // Get DPI scaling to convert from physical pixels to DIPs
            double dpiScaleX, dpiScaleY;
            monitorManager.GetDpiScale(window, out dpiScaleX, out dpiScaleY);

            // Convert from physical pixels (Screen API) to device-independent pixels (WPF)
            double workAreaLeft = monitor.WorkingArea.X / dpiScaleX;
            double workAreaTop = monitor.WorkingArea.Y / dpiScaleY;
            double workAreaWidth = monitor.WorkingArea.Width / dpiScaleX;
            double workAreaHeight = monitor.WorkingArea.Height / dpiScaleY;

            WindowPosition position = new WindowPosition();

            switch (corner)
            {
                case Corner.TopLeft:
                    position.Left = workAreaLeft + 10;
                    position.Top = workAreaTop + 10;
                    break;

                case Corner.TopRight:
                    position.Left = workAreaLeft + workAreaWidth - windowWidth - 10;
                    position.Top = workAreaTop + 10;
                    break;

                case Corner.BottomLeft:
                    position.Left = workAreaLeft + 10;
                    position.Top = workAreaTop + workAreaHeight - windowHeight - 20;
                    break;

                case Corner.BottomRight:
                    position.Left = workAreaLeft + workAreaWidth - windowWidth - 10;
                    position.Top = workAreaTop + workAreaHeight - windowHeight - 20;
                    break;
            }

            return position;
        }

        /// <summary>
        /// Validates and adjusts a window position to ensure it stays within the monitor's work area
        /// </summary>
        /// <param name="left">The left position (will be adjusted if needed)</param>
        /// <param name="top">The top position (will be adjusted if needed)</param>
        /// <param name="windowWidth">The window width</param>
        /// <param name="windowHeight">The window height</param>
        /// <param name="monitor">The monitor to validate against</param>
        /// <param name="window">The window (for DPI calculation)</param>
        public void ValidateAndAdjustPosition(
            ref double left,
            ref double top,
            double windowWidth,
            double windowHeight,
            System.Windows.Forms.Screen monitor,
            Window window)
        {
            if (monitor == null)
                throw new ArgumentNullException(nameof(monitor));

            // Get DPI scaling to convert from physical pixels to DIPs
            double dpiScaleX, dpiScaleY;
            monitorManager.GetDpiScale(window, out dpiScaleX, out dpiScaleY);

            // Get current monitor's work area (respects taskbar)
            // Convert from physical pixels (Screen API) to device-independent pixels (WPF)
            double workAreaLeft = monitor.WorkingArea.X / dpiScaleX;
            double workAreaTop = monitor.WorkingArea.Y / dpiScaleY;
            double workAreaWidth = monitor.WorkingArea.Width / dpiScaleX;
            double workAreaHeight = monitor.WorkingArea.Height / dpiScaleY;

            // Ensure window stays within work area
            // Left edge: clamp to work area left
            if (left < workAreaLeft)
                left = workAreaLeft;

            // Right edge: ensure window doesn't go off right side
            if (left + windowWidth > workAreaLeft + workAreaWidth)
                left = workAreaLeft + workAreaWidth - windowWidth;

            // Top edge: clamp to work area top
            if (top < workAreaTop)
                top = workAreaTop;

            // Bottom edge: ensure window doesn't go off bottom
            if (top + windowHeight > workAreaTop + workAreaHeight)
                top = workAreaTop + workAreaHeight - windowHeight;
        }

        /// <summary>
        /// Gets the next corner in the cycle (1->2->3->4->1)
        /// </summary>
        /// <param name="current">The current corner</param>
        /// <returns>The next corner</returns>
        public Corner GetNextCorner(Corner current)
        {
            return (Corner)(((int)current % 4) + 1);
        }
    }
}
