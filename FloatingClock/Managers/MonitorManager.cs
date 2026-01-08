using System;
using System.Linq;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;

namespace FloatingClock.Managers
{
    /// <summary>
    /// Represents screen work area coordinates converted to WPF device-independent pixels
    /// </summary>
    public struct WorkAreaDips
    {
        public double Left { get; set; }
        public double Top { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }

        public double Right => Left + Width;
        public double Bottom => Top + Height;
    }

    /// <summary>
    /// Manages monitor detection, cycling, and DPI scaling
    /// </summary>
    public class MonitorManager
    {
        /// <summary>
        /// Gets the DPI scaling factors from a visual element (static utility method)
        /// </summary>
        /// <param name="visual">The visual element to get DPI scaling for</param>
        /// <param name="dpiScaleX">Horizontal DPI scale where 1.0 = 100%, 1.25 = 125%, etc.</param>
        /// <param name="dpiScaleY">Vertical DPI scale where 1.0 = 100%, 1.25 = 125%, etc.</param>
        public static void GetDpiScaleFromVisual(Visual visual, out double dpiScaleX, out double dpiScaleY)
        {
            PresentationSource source = PresentationSource.FromVisual(visual);
            if (source?.CompositionTarget != null)
            {
                dpiScaleX = source.CompositionTarget.TransformToDevice.M11;
                dpiScaleY = source.CompositionTarget.TransformToDevice.M22;
            }
            else
            {
                dpiScaleX = 1.0;
                dpiScaleY = 1.0;
            }
        }

        /// <summary>
        /// Gets the monitor that currently contains the specified window
        /// </summary>
        /// <param name="window">The window to locate</param>
        /// <returns>The Screen object containing the window</returns>
        public System.Windows.Forms.Screen GetCurrentMonitor(Window window)
        {
            WindowInteropHelper windowInteropHelper = new WindowInteropHelper(window);
            System.Windows.Forms.Screen screen = System.Windows.Forms.Screen.FromHandle(windowInteropHelper.Handle);
            return screen;
        }

        /// <summary>
        /// Gets the DPI scaling factors for the specified window
        /// </summary>
        /// <param name="window">The window to get DPI scaling for</param>
        /// <param name="dpiScaleX">Horizontal DPI scale where 1.0 = 100%, 1.25 = 125%, etc.</param>
        /// <param name="dpiScaleY">Vertical DPI scale where 1.0 = 100%, 1.25 = 125%, etc.</param>
        public void GetDpiScale(Window window, out double dpiScaleX, out double dpiScaleY)
        {
            GetDpiScaleFromVisual(window, out dpiScaleX, out dpiScaleY);
        }

        /// <summary>
        /// Gets the monitor's working area converted to WPF device-independent pixels
        /// </summary>
        /// <param name="monitor">The monitor to get work area from</param>
        /// <param name="window">The window used to determine DPI scaling</param>
        /// <returns>Work area coordinates in DIPs</returns>
        public WorkAreaDips GetWorkAreaInDips(System.Windows.Forms.Screen monitor, Window window)
        {
            GetDpiScale(window, out double dpiScaleX, out double dpiScaleY);

            return new WorkAreaDips
            {
                Left = monitor.WorkingArea.X / dpiScaleX,
                Top = monitor.WorkingArea.Y / dpiScaleY,
                Width = monitor.WorkingArea.Width / dpiScaleX,
                Height = monitor.WorkingArea.Height / dpiScaleY
            };
        }

        /// <summary>
        /// Cycles to the next available monitor
        /// </summary>
        /// <param name="currentMonitor">The current monitor</param>
        /// <param name="currentCorner">The corner position to preserve on the new monitor</param>
        /// <returns>The next monitor in the cycle, or the current monitor if only one exists</returns>
        public System.Windows.Forms.Screen CycleToNextMonitor(System.Windows.Forms.Screen currentMonitor)
        {
            System.Windows.Forms.Screen[] allScreens = System.Windows.Forms.Screen.AllScreens;

            // If only one monitor, return current
            if (allScreens.Length <= 1)
                return currentMonitor;

            // Find current monitor index
            int currentIndex = -1;
            for (int i = 0; i < allScreens.Length; i++)
            {
                if (allScreens[i].DeviceName == currentMonitor.DeviceName)
                {
                    currentIndex = i;
                    break;
                }
            }

            // Move to next monitor (wrap around to first if at end)
            int nextIndex = (currentIndex + 1) % allScreens.Length;
            return allScreens[nextIndex];
        }

        /// <summary>
        /// Gets a monitor by its device name
        /// </summary>
        /// <param name="deviceName">The device name to search for</param>
        /// <returns>The monitor with the specified device name, or null if not found</returns>
        public System.Windows.Forms.Screen GetMonitorByDeviceName(string deviceName)
        {
            if (string.IsNullOrEmpty(deviceName))
                return null;

            return System.Windows.Forms.Screen.AllScreens
                .FirstOrDefault(s => s.DeviceName == deviceName);
        }

        /// <summary>
        /// Gets all available monitors
        /// </summary>
        /// <returns>Array of all screens</returns>
        public System.Windows.Forms.Screen[] GetAllMonitors()
        {
            return System.Windows.Forms.Screen.AllScreens;
        }

        /// <summary>
        /// Gets the primary monitor
        /// </summary>
        /// <returns>The primary screen</returns>
        public System.Windows.Forms.Screen GetPrimaryMonitor()
        {
            return System.Windows.Forms.Screen.PrimaryScreen;
        }

        /// <summary>
        /// Checks if a monitor is still valid (connected)
        /// </summary>
        /// <param name="monitor">The monitor to validate</param>
        /// <returns>True if the monitor is still connected, false otherwise</returns>
        public bool IsMonitorValid(System.Windows.Forms.Screen monitor)
        {
            if (monitor == null)
                return false;

            return System.Windows.Forms.Screen.AllScreens
                .Any(s => s.DeviceName == monitor.DeviceName);
        }
    }
}
