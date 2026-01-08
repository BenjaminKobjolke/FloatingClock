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

            // Get work area in DIPs (already converted from physical pixels)
            var workArea = monitorManager.GetWorkAreaInDips(monitor, window);

            WindowPosition position = new WindowPosition();

            switch (corner)
            {
                case Corner.TopLeft:
                    position.Left = workArea.Left + 10;
                    position.Top = workArea.Top + 10;
                    break;

                case Corner.TopRight:
                    position.Left = workArea.Right - windowWidth - 10;
                    position.Top = workArea.Top + 10;
                    break;

                case Corner.BottomLeft:
                    position.Left = workArea.Left + 10;
                    position.Top = workArea.Bottom - windowHeight - 20;
                    break;

                case Corner.BottomRight:
                    position.Left = workArea.Right - windowWidth - 10;
                    position.Top = workArea.Bottom - windowHeight - 20;
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

            // Get work area in DIPs (already converted from physical pixels)
            var workArea = monitorManager.GetWorkAreaInDips(monitor, window);

            // Ensure window stays within work area
            if (left < workArea.Left)
                left = workArea.Left;

            if (left + windowWidth > workArea.Right)
                left = workArea.Right - windowWidth;

            if (top < workArea.Top)
                top = workArea.Top;

            if (top + windowHeight > workArea.Bottom)
                top = workArea.Bottom - windowHeight;
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
